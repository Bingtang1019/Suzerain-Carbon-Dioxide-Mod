#!/usr/bin/env node























'use strict';
const fs = require('fs');
const os = require('os');
const path = require('path');
const cp = require('child_process');

const REPO = path.resolve(__dirname, '..');
const VERDICT = { GREEN: 'GREEN', RED: 'RED', BROKEN: 'INSTRUMENT-BROKEN' };


function newP3Control(o) {
  const ran = !!o.ran;
  const expected = o.expected == null ? '' : String(o.expected);
  const got = o.got == null ? '' : String(o.got);
  
  const ok = (ran && expected !== '' && got !== '') ? (expected === got) : false;
  return { ran, input: o.input || '', expected, got, ok };
}

function judge(r) {
  const c = r.control;
  
  if (!c || typeof c !== 'object') return { verdict: VERDICT.BROKEN, why: 'control 字段缺失（P3 ①：必填）' };
  if (!c.ran) return { verdict: VERDICT.BROKEN, why: 'control 未跑（ran=false）' };
  if (!c.expected || !c.got) return { verdict: VERDICT.BROKEN, why: 'control 缺 expected 或 got（P3 ③）' };
  
  if (c.ok !== true) return { verdict: VERDICT.BROKEN, why: 'control.ok=false ⇒ 仪器自己没过（P3 ②：fail-closed）' };
  
  const { checked, passed, failed, skipped } = r;
  if (![checked, passed, failed, skipped].every(Number.isInteger)) return { verdict: VERDICT.RED, why: '分区字段缺失或非整数' };
  if (passed + failed + skipped !== checked) return { verdict: VERDICT.RED, why: `分区恒等式不成立：${passed}+${failed}+${skipped} != ${checked}` };
  if (failed > 0) return { verdict: VERDICT.RED, why: `failed=${failed} > 0` };
  
  if (!Number.isInteger(checked) || checked <= 0) return { verdict: VERDICT.RED, why: 'checked<=0（范围类分母未立）' };
  
  return { verdict: VERDICT.GREEN, why: '全过（★ 绿的唯一出口：control.ok === true）' };
}



function controlSelfcheck() {
  
  const sand = fs.mkdtempSync(path.join(os.tmpdir(), 'p3-selfcheck-'));
  try {
    fs.mkdirSync(path.join(sand, 'tools'), { recursive: true });
    fs.mkdirSync(path.join(sand, 'src'), { recursive: true });
    fs.mkdirSync(path.join(sand, 'bin'), { recursive: true });
    fs.mkdirSync(path.join(sand, 'docs'), { recursive: true });
    fs.copyFileSync(path.join(REPO, 'tools', 'selfcheck.js'), path.join(sand, 'tools', 'selfcheck.js'));
    
    
    const out = cp.spawnSync('node', [path.join(sand, 'tools', 'selfcheck.js'), '--quick'],
      { encoding: 'utf8', cwd: sand });
    const code = out.status;
    return newP3Control({
      ran: true,
      input: '沙箱：缺 README.txt（selfcheck 的 need 列表项）',
      expected: 'FAIL', got: code === 0 ? 'PASS' : 'FAIL'
    });
  } catch (e) {
    return newP3Control({ ran: true, input: '沙箱构造失败', expected: 'FAIL', got: 'ERROR:' + e.message });
  } finally { try { fs.rmSync(sand, { recursive: true, force: true }); } catch (e) { } }
}

function controlBridgeContract() {
  
  
  
  return newP3Control({ ran: false, input: '(本线无法在不修改该门禁的前提下构造已知坏态)', expected: '', got: '' });
}

const CONTROLS = { selfcheck: controlSelfcheck, 'check-bridge-contract': controlBridgeContract };


function runGate(name) {
  const map = { selfcheck: 'selfcheck.js', 'check-bridge-contract': 'check-bridge-contract.js' };
  const f = map[name];
  if (!f) throw new Error('未知门禁：' + name);
  const started = new Date().toISOString();
  const out = cp.spawnSync('node', [path.join(REPO, 'tools', f), '--json'], { encoding: 'utf8' });
  const exitCode = out.status;
  const control = (CONTROLS[name] || (() => newP3Control({ ran: false })))();
  
  
  const passed = exitCode === 0 ? 1 : 0;
  const failed = exitCode === 0 ? 0 : 1;
  const r = { gate: name, started, exitCode, control, checked: 1, passed, failed, skipped: 0 };
  const j = judge(r);
  return Object.assign(r, { verdict: j.verdict, why: j.why, expected: 'exit=0', got: 'exit=' + exitCode });
}


function selftest() {
  let fail = 0;
  const t = (n, cond, detail) => { if (!cond) fail++; console.log('  [' + (cond ? 'OK' : 'FAIL') + '] ' + n + (detail ? '  —— ' + detail : '')); };
  console.log('=== P3 契约层自检（判定序逐条）===');
  const okC = newP3Control({ ran: true, input: 'x', expected: 'REJECT', got: 'REJECT' });
  t('① control 缺失 ⇒ INSTRUMENT-BROKEN', judge({ checked: 1, passed: 1, failed: 0, skipped: 0 }).verdict === VERDICT.BROKEN);
  t('① control.ran=false ⇒ INSTRUMENT-BROKEN', judge({ control: newP3Control({ ran: false }), checked: 1, passed: 1, failed: 0, skipped: 0 }).verdict === VERDICT.BROKEN);
  t('① control 缺 got ⇒ INSTRUMENT-BROKEN', judge({ control: newP3Control({ ran: true, expected: 'REJECT' }), checked: 1, passed: 1, failed: 0, skipped: 0 }).verdict === VERDICT.BROKEN);
  t('② control.ok=false ⇒ INSTRUMENT-BROKEN（不是 RED）', judge({ control: newP3Control({ ran: true, expected: 'REJECT', got: 'ACCEPT' }), checked: 1, passed: 1, failed: 0, skipped: 0 }).verdict === VERDICT.BROKEN);
  t('③ failed>0 ⇒ RED', judge({ control: okC, checked: 2, passed: 1, failed: 1, skipped: 0 }).verdict === VERDICT.RED);
  t('③ 分区恒等式不成立 ⇒ RED', judge({ control: okC, checked: 3, passed: 1, failed: 0, skipped: 0 }).verdict === VERDICT.RED);
  t('④ checked=0 ⇒ RED（分母未立）', judge({ control: okC, checked: 0, passed: 0, failed: 0, skipped: 0 }).verdict === VERDICT.RED);
  t('⑤ 全过 ⇒ GREEN', judge({ control: okC, checked: 1, passed: 1, failed: 0, skipped: 0 }).verdict === VERDICT.GREEN);
  t('★ 绿的唯一出口：control 不 ok 时【绝不】GREEN', judge({ control: newP3Control({ ran: true, expected: 'A', got: 'B' }), checked: 1, passed: 1, failed: 0, skipped: 0 }).verdict !== VERDICT.GREEN);
  console.log(fail ? 'P3_SELFTEST=FAILED(' + fail + ')' : 'P3_SELFTEST=OK（判定序 9/9）');
  process.exitCode = fail ? 1 : 0;
}


const argv = process.argv.slice(2);
if (argv.includes('--selftest')) { selftest(); }
else {
  const gi = argv.indexOf('--gate');
  const gates = gi >= 0 && argv[gi + 1] ? [argv[gi + 1]] : Object.keys(CONTROLS);
  const results = gates.map(runGate);
  if (argv.includes('--json')) console.log(JSON.stringify(results, null, 2));
  else for (const r of results) {
    console.log('=== ' + r.gate + ' ===');
    console.log('  control : ' + JSON.stringify(r.control));
    console.log('  expected/got : ' + r.expected + ' ／ ' + r.got + '   exit=' + r.exitCode);
    console.log('  分区    : checked=' + r.checked + ' passed=' + r.passed + ' failed=' + r.failed + ' skipped=' + r.skipped);
    console.log('  ⇒ VERDICT = ' + r.verdict + '   （' + r.why + '）');
  }
  
  process.exitCode = results.every(r => r.verdict === VERDICT.GREEN) ? 0 : 1;
}
