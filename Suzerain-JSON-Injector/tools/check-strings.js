'use strict';






















































const fs = require('fs');
const path = require('path');
const os = require('os');


function norm(s) {
  return String(s)
    .replace(/[`*_\s]/g, '')
    .replace(/[“”"'']/g, '')
    .replace(/[，。、；：！？（）【】《》〈〉·—…～]/g, '')
    .toLowerCase();
}

function probe(text, ntext, raw) {
  const n = norm(raw);
  if (!n) return { state: 'empty' };
  if (ntext.indexOf(n) >= 0) return { state: 'hit' };
  const clue = n.slice(0, 6);
  const i = ntext.indexOf(clue);
  const around = i >= 0
    ? text.slice(Math.max(0, i - 20), i + 60).replace(/\s+/g, ' ')
    : '(连前 6 字的线索都找不到)';
  return { state: 'miss', around };
}

function parseArgs(argv) {
  const r = { file: null, musts: [], absents: [], tsv: false, err: null };
  let dir = 'must', endOfOpts = false;
  let mustGiven = false, absentGiven = false;
  let sawAfterMust = false, sawAfterAbsent = false;

  for (const a of argv) {
    if (!endOfOpts) {
      if (a === '--') { endOfOpts = true; continue; }
      if (a === '--tsv') { r.tsv = true; continue; }
      if (a.startsWith('--must=')) {
        dir = 'must'; mustGiven = true;
        const v = a.slice('--must='.length);
        if (!v) return { ...r, err: '--must= 后面是空的：要么给串，要么改用 --must <串>' };
        r.musts.push(v); sawAfterMust = true; continue;
      }
      if (a.startsWith('--absent=')) {
        dir = 'absent'; absentGiven = true;
        const v = a.slice('--absent='.length);
        if (!v) return { ...r, err: '--absent= 后面是空的：要么给串，要么改用 --absent <串>' };
        r.absents.push(v); sawAfterAbsent = true; continue;
      }
      if (a === '--must') { dir = 'must'; mustGiven = true; continue; }
      if (a === '--absent') { dir = 'absent'; absentGiven = true; continue; }
      if (a.startsWith('--')) {
        return { ...r, err: '未知参数: ' + a + '（若它本来就是字面串，用 --must=' + a + ' / --absent=' + a + '，或先写 -- 终止符）' };
      }
    }
    if (r.file === null) { r.file = a; continue; }
    if (dir === 'absent') { r.absents.push(a); sawAfterAbsent = true; }
    else { r.musts.push(a); sawAfterMust = true; }
  }

  if (mustGiven && !sawAfterMust) {
    return { ...r, err: '--must 后面没有跟任何特征串（想核验以 -- 开头的字面串？用 --must=串 或 --must -- 串）' };
  }
  if (absentGiven && !sawAfterAbsent) {
    return { ...r, err: '--absent 后面没有跟任何特征串（想核验以 -- 开头的字面串？用 --absent=串 或 --absent -- 串）' };
  }
  if (r.file === null || (r.musts.length === 0 && r.absents.length === 0)) {
    return { ...r, err: null, needUsage: true };
  }
  return r;
}

function check(file, musts, absents, tsv) {
  const text = fs.readFileSync(file, 'utf8');
  const ntext = norm(text);
  const rows = [];
  let unexpected = 0;

  for (const raw of musts) {
    const r = probe(text, ntext, raw);
    if (r.state === 'empty') { rows.push({ dir: 'must', raw, state: 'empty' }); continue; }
    if (r.state === 'hit') rows.push({ dir: 'must', raw, state: 'ok' });
    else { unexpected++; rows.push({ dir: 'must', raw, state: 'unexpected', around: r.around }); }
  }
  for (const raw of absents) {
    const r = probe(text, ntext, raw);
    if (r.state === 'empty') { rows.push({ dir: 'absent', raw, state: 'empty' }); continue; }
    if (r.state === 'miss') rows.push({ dir: 'absent', raw, state: 'ok' });   // 期望未命中 ∩ 实际未命中 = 已归零
    else { unexpected++; rows.push({ dir: 'absent', raw, state: 'unexpected' }); }
  }

  const okMust = rows.filter(r => r.dir === 'must' && r.state === 'ok').length;
  const okAbs = rows.filter(r => r.dir === 'absent' && r.state === 'ok').length;

  if (tsv) {
    console.log('dir\tstate\traw');
    for (const r of rows) {
      const st = r.state === 'ok' ? 'ok' : (r.state === 'empty' ? 'empty' : 'UNEXPECTED');
      console.log([r.dir, st, r.raw].join('\t'));
    }
    console.log(['summary', unexpected === 0 ? 'PASS' : 'FAIL',
      'must_ok=' + okMust + '/' + musts.length,
      'absent_ok=' + okAbs + '/' + absents.length].join('\t'));
  } else {
    for (const r of rows) {
      if (r.state === 'empty') { console.log('  [跳过 · 空串]       ' + r.raw); continue; }
      if (r.state === 'ok') {
        console.log(r.dir === 'must'
          ? '  [must · 命中]       ' + r.raw
          : '  [absent · 已归零]   ' + r.raw);
      } else {
        console.log(r.dir === 'must'
          ? '  [must · 未命中] ✗   ' + r.raw
          : '  [absent · 仍存在] ✗ ' + r.raw);
        if (r.around) console.log('            可疑 ≠ 失败；相邻线索 → …' + r.around + '…');
      }
    }
    console.log('');
    console.log('  ── 方向与计数（务必看这两行，别只看退出码）──');
    console.log('  [must]   期望命中   ' + okMust + '/' + musts.length + ' 命中');
    if (absents.length) console.log('  [absent] 期望未命中 ' + okAbs + '/' + absents.length + ' 已归零');
    else console.log('  （本次未用 --absent ⇒ 只判了「新串出现」这半，**未判「旧串归零」**）');
    console.log('  意外 ' + unexpected + ' 项 ⇒ 退出码 ' + (unexpected === 0 ? '0' : '1'));
  }
  return unexpected === 0 ? 0 : 1;
}

function selfTest() {
  const tmp = path.join(os.tmpdir(), 'check-strings-selftest.txt');
  fs.writeFileSync(tmp,
    '这是 `ForEach-Object` 的脚本块有自己的作用域，共同的教训是：我改对了不是证据。'
    + '旧串OLDTOKEN 文中确实写着 --absent 这个开关名\n', 'utf8');
  const R = [];
  const say = (n, s) => { console.log('自测 ' + n + '  ' + s); };

  say('1/6', 'must 正向：文中含反引号与标点，宽松匹配应命中');
  R.push(['must 正', check(tmp, ['ForEach-Object 的脚本块有自己的作用域', '共同的教训'], [], true), 0]);
  say('2/6', 'must 负向：不存在的串必须判「意外」（证明它不是"永远 OK"）');
  R.push(['must 负', check(tmp, ['这句话根本不在文件里xyz'], [], true), 1]);
  say('3/6', 'absent 正向：确实不存在的串作 --absent ⇒ 应「已归零」');
  R.push(['absent 正', check(tmp, [], ['这个旧串早就删掉了XYZ'], true), 0]);
  say('4/6', 'absent 负向：把"确实存在"的串当 --absent ⇒ 必须判「意外」');
  R.push(['absent 负', check(tmp, [], ['旧串OLDTOKEN'], true), 1]);

  say('5/6', '悬空开关：--must x --absent（后者后面空着）⇒ 必须返回用法错误，不许静默');
  const p5 = parseArgs(['x.txt', '--must', '一个串', '--absent']);
  console.log('      → parseArgs.err = ' + JSON.stringify(p5.err));
  R.push(['悬空开关报错', p5.err ? 2 : 0, 2]);

  say('6/6', '-- 终止符：--must -- --absent ⇒ 一个字面量、计数=1，且文中确有该串 ⇒ 命中');
  const p6 = parseArgs([tmp, '--must', '--', '--absent']);
  console.log('      → musts = ' + JSON.stringify(p6.musts) + '（长度应为 1）');
  const r6 = (!p6.err && p6.musts.length === 1) ? check(tmp, p6.musts, p6.absents, true) : 9;
  R.push(['终止符字面量', r6, 0]);

  fs.unlinkSync(tmp);
  const bad = R.filter(([, got, want]) => got !== want);
  if (bad.length === 0) {
    console.log('\nSELFTEST_OK（6 例全过：must 正/负 · absent 正/负 · 悬空开关被拒 · 终止符能验开关名）');
    return 0;
  }
  console.log('\nSELFTEST_FAIL：' + bad.map(([n, got, want]) => n + ' got=' + got + ' want=' + want).join(' · '));
  return 1;
}

const argv = process.argv.slice(2);
if (argv.includes('--self-test')) process.exit(selfTest());

function usage() {
  console.error('用法:');
  console.error('  node tools/check-strings.js <文件> <特征串...>                        # 全部按「期望命中」');
  console.error('  node tools/check-strings.js <文件> --must 新串... --absent 旧串...    # 两半一次判');
  console.error('  node tools/check-strings.js <文件> --must=串 --absent=串              # 内联：值一律当字面量');
  console.error('  node tools/check-strings.js <文件> --must -- --absent                # -- 终止符：其后当字面量');
  console.error('  node tools/check-strings.js <文件> --tsv [--must ...] [--absent ...] # 机器可读');
  console.error('  node tools/check-strings.js --self-test');
  console.error('退出码: 0 = 全部符合预期; 1 = 有"意外"; 2 = 用法错误');
}

const P = parseArgs(argv);
if (P.err) { console.error('用法错误: ' + P.err); usage(); process.exit(2); }
if (P.needUsage) { usage(); process.exit(2); }
process.exit(check(P.file, P.musts, P.absents, P.tsv));
