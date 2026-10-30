#!/usr/bin/env node

















'use strict';
const fs = require('fs');
const path = require('path');
const cp = require('child_process');

const REPO = path.resolve(__dirname, '..');
const GATE = path.join(REPO, 'tools', 'check-refs.js');
const FX = path.join(REPO, 'tools', 'testdata', 'check-refs');
let fail = 0;

console.log('=== C5′ check-refs 自检（只读；★ 每次运行都跑）===');
console.log('  闸   : ' + path.relative(REPO, GATE));
console.log('  夹具 : ' + path.relative(REPO, FX));


const reg = path.join(FX, 'FIXTURES.tsv');
if (!fs.existsSync(reg)) { console.log('[FAIL] 缺 FIXTURES.tsv（登记表）—— 缺失即 FAIL，不跳过'); process.exit(1); }
const rows = [];
for (const line of fs.readFileSync(reg, 'utf8').split(/\r?\n/)) {
  const t = line.trim();
  if (!t || t.startsWith('#')) continue;
  const c = t.split('\t').map(x => x.trim());
  if (c.length < 4) { console.log('[FAIL] 登记行格式错：' + t); fail++; continue; }
  rows.push({ file: c[0], bytes: parseInt(c[1], 10), sha: c[2], token: c[3] });
}
if (rows.length === 0) { console.log('[FAIL] 登记表为空 ⇒ 空集假绿 ⇒ FAIL'); process.exit(1); }
for (const r of rows) {
  const p = path.join(FX, r.file);
  if (!fs.existsSync(p)) { console.log('[FAIL] 夹具缺失：' + r.file); fail++; continue; }
  const buf = fs.readFileSync(p);
  const sha = require('crypto').createHash('sha256').update(buf).digest('hex').toUpperCase();
  if (buf.length !== r.bytes || sha !== r.sha) {
    console.log('[FAIL] 夹具与登记不符：' + r.file + '  实测 ' + buf.length + 'B/' + sha.slice(0, 16) + '  登记 ' + r.bytes + 'B/' + r.sha.slice(0, 16));
    fail++;
  } else console.log('  [OK] 夹具登记一致：' + r.file);
}


console.log('=== 两方向对照 ＋ 空来源（★ root 必须是【文件】：目录 root 走 git ls-files）===');
for (const r of rows) {
  const p = path.join(FX, r.file);
  const out = cp.spawnSync('node', [GATE, '--root', p], { encoding: 'utf8' });
  const m = (out.stdout || '').match(/^RESULT=(.*)$/m);
  const got = m ? m[1].trim() : '(无 RESULT 行)';
  const code = out.status;
  const ok = got === r.token && code !== 0 === (r.token !== 'PASS');
  if (!ok) fail++;
  console.log('  [' + (ok ? 'OK' : 'FAIL') + '] ' + r.file.padEnd(14) + ' 期望 ' + r.token.padEnd(16) + ' 实测 ' + got.padEnd(16) + ' exit ' + code);
}

console.log('');
if (fail) { console.log('C5_SELFTEST=FAILED（' + fail + ' 例不符）'); process.exit(1); }
console.log('C5_SELFTEST=OK（' + rows.length + '/' + rows.length + ' 例均按预期 token 与 exit code 复现）');
