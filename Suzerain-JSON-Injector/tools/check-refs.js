#!/usr/bin/env node
















'use strict';
const fs = require('fs');
const os = require('os');
const path = require('path');
const cp = require('child_process');

const REPO = path.resolve(__dirname, '..');
const DEFAULT_LIST = path.join(__dirname, 'refs-exempt.tsv');












const SCOPE = {
  absOnly: true,    
  unc: false,       
  envVars: false,   
  
  
  scriptExt: ['.js', '.mjs', '.cjs', '.ps1', '.psm1', '.cs'],
  docExt: ['.md', '.json', '.tsv', '.txt'],
};


const SCRIPT_EXT = SCOPE.scriptExt;
const DOC_EXT = SCOPE.docExt;

function scopeHint() {
  const gaps = [];
  if (SCOPE.absOnly) gaps.push('相对路径不在射程');
  if (!SCOPE.unc) gaps.push('UNC 不在射程');
  if (!SCOPE.envVars) gaps.push('环境变量路径不在射程');
  return gaps.length ? gaps.join('？／') + '？' : '（当前射程未声明缺口）';
}




function scopeShape() {
  const shape = SCOPE.absOnly ? '【盘符开头的绝对路径字面量】' : '【绝对路径 ＋ 相对路径（以仓库根解析）】';
  const gaps = [];
  if (SCOPE.absOnly) gaps.push('相对路径');
  if (!SCOPE.unc) gaps.push('UNC');
  if (!SCOPE.envVars) gaps.push('环境变量路径');
  return shape + (gaps.length ? '；不含 ' + gaps.join(' 与 ') : '；无已知缺口');
}






function scopeSelfCheck() {
  const absSample = 'C:\\Users\\dell\\x.txt';
  const relSample = 'src\\_split-source\\a.cs';
  const absHit = (absSample.match(RE_WIN_ABS) || []).length > 0;
  const relHit = (relSample.match(RE_WIN_ABS) || []).length > 0;
  const wantRel = !SCOPE.absOnly;   
  
  
  const extSame = (SCRIPT_EXT === SCOPE.scriptExt) && (DOC_EXT === SCOPE.docExt);
  const extShape = (arr) => Array.isArray(arr) && arr.length > 0 &&
    arr.every(e => typeof e === 'string' && e.startsWith('.')) &&
    new Set(arr.map(e => e.toLowerCase())).size === arr.length;
  const extOk = extSame && extShape(SCRIPT_EXT) && extShape(DOC_EXT);
  return {
    ran: true,
    input: 'SCOPE 声明 vs 实际行为（absOnly=' + SCOPE.absOnly + ' ／ 含扩展名维度）',
    expected: '相对路径命中=' + wantRel + ' ／ 绝对路径命中=true ／ 白名单由 SCOPE 派生且形态合法=true',
    got: '相对路径命中=' + relHit + ' ／ 绝对路径命中=' + absHit + ' ／ 白名单由 SCOPE 派生且形态合法=' + extOk,
    ok: (relHit === wantRel) && absHit && extOk
  };
}











const RE_WIN_ABS = /[A-Za-z]:(?:\\{1,2}[^\\/:*?"<>|\r\n\t'"`,;]+){2,}/g;

function normAbs(s) {
  let t = s.replace(/\\\\/g, '\\');
  t = t.replace(/[。，、；：）】》」』”’"',;:)\]}>.\s]+$/, '');
  return t;
}

function classify(p, exempts) {
  if (/[*?]/.test(p)) return { cls: 'wildcard', why: '含通配符' };
  for (const e of exempts) {
    if (e.kind === 'prefix' ? p.toLowerCase().startsWith(e.pat.toLowerCase())
      : e.kind === 'contains' ? p.toLowerCase().includes(e.pat.toLowerCase())
        : p.toLowerCase() === e.pat.toLowerCase()) {
      return { cls: 'may-be-absent', why: e.reason, who: e.who, at: e.at };
    }
  }
  return { cls: 'must-exist', why: '' };
}

function loadExempts(p) {
  if (!fs.existsSync(p)) {
    console.log('[FAIL] 豁免清单不存在：' + p);
    console.log('       ★ 缺失即 FAIL（不跳过、不视为通过）—— 与 check-shared.js 约束②同源');
    return null;
  }
  const rows = [];
  for (const line of fs.readFileSync(p, 'utf8').split(/\r?\n/)) {
    const s = line.trim();
    if (!s || s.startsWith('#')) continue;
    const c = s.split('\t').map(x => x.trim());
    if (c.length < 5) { console.log('[FAIL] 豁免行格式错（需 匹配方式\\t模式\\t理由\\t登记人\\t时点）：' + s); return null; }
    if (!c[2]) { console.log('[FAIL] 豁免行【缺理由】⇒ 拒绝（理由必填，否则无法区分"故意"与"忘记"）：' + s); return null; }
    rows.push({ kind: c[0], pat: c[1], reason: c[2], who: c[3], at: c[4] });
  }
  return rows;
}

function enumTracked(repo) {
  try {
    const out = cp.execSync('git -C "' + repo + '" -c core.quotepath=false ls-files',
      { stdio: ['ignore', 'pipe', 'ignore'] }).toString();
    return out.split(/\r?\n/).filter(Boolean).map(f => path.join(repo, f));
  } catch (e) { return null; }   // ★ git 不可用 ⇒ 返回 null，由调用方 FAIL（禁静默降级）
}

function positiveControl() {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'check-refs-control-'));
  const real = path.join(dir, 'real-target.txt');
  fs.writeFileSync(real, 'x', 'utf8');
  const ghost = path.join(dir, 'ghost-does-not-exist', 'nope.bundle');
  const fOk = path.join(dir, 'sample-ok.txt');
  const fBad = path.join(dir, 'sample-bad.txt');
  fs.writeFileSync(fOk, "const A = '" + real + "';", 'utf8');
  fs.writeFileSync(fBad, "const B = '" + ghost + "';", 'utf8');
  const litOk = (fs.readFileSync(fOk, 'utf8').match(RE_WIN_ABS) || []).map(normAbs);
  const litBad = (fs.readFileSync(fBad, 'utf8').match(RE_WIN_ABS) || []).map(normAbs);
  const okExists = litOk.length === 1 && fs.existsSync(litOk[0]);
  const badFlagged = litBad.length === 1 && !fs.existsSync(litBad[0]);
  try { fs.rmSync(dir, { recursive: true, force: true }); } catch (e) { }
  return {
    ran: true,
    input: '合成夹具（1 条真路径 + 1 条假路径）',
    expected: '真路径判存在 / 假路径判缺失',
    got: '真路径判' + (okExists ? '存在' : '★缺失') + ' / 假路径判' + (badFlagged ? '缺失' : '★存在'),
    ok: okExists && badFlagged
  };
}

function scan(roots, exts, exempts) {
  const files = [];
  let skippedSources = [];
  for (const r of roots) {
    if (!fs.existsSync(r)) { skippedSources.push({ src: r, why: '不存在' }); continue; }
    const list = fs.statSync(r).isDirectory() ? enumTracked(r) : [r];
    if (list === null) { skippedSources.push({ src: r, why: 'git 不可用（非仓或 git 缺失）' }); continue; }
    const keep = list.filter(f => exts.includes(path.extname(f).toLowerCase()));
    if (keep.length === 0) { skippedSources.push({ src: r, why: '枚举到 0 个匹配文件' }); continue; }
    files.push(...keep);
  }
  const rows = [];
  const unreadable = [];
  for (const f of files) {
    let txt;
    try { txt = fs.readFileSync(f, 'utf8'); } catch (e) { unreadable.push({ file: f, why: e.code || String(e) }); continue; }
    const lines = txt.split(/\r?\n/);
    lines.forEach((ln, i) => {
      const hits = ln.match(RE_WIN_ABS);
      if (!hits) return;
      for (const h of hits) {
        const p = normAbs(h);
        const c = classify(p, exempts);
        const exists = fs.existsSync(p);
        let ok = exists;
        if (c.cls === 'wildcard') {
          const pre = p.slice(0, p.search(/[*?]/));
          const dir = pre.slice(0, pre.lastIndexOf('\\'));
          ok = dir ? fs.existsSync(dir) : false;
        }
        rows.push({ file: f, line: i + 1, path: p, cls: c.cls, why: c.why, ok, exists });
      }
    });
  }
  return { files, rows, skippedSources, unreadable };
}

function main(argv) {
  const includeDocs = argv.includes('--include-docs');
  const li = argv.indexOf('--list');
  const listPath = li >= 0 && argv[li + 1] ? argv[li + 1] : DEFAULT_LIST;
  const rootArgs = [];
  for (let i = 0; i < argv.length; i++) if (argv[i] === '--root' && argv[i + 1]) rootArgs.push(argv[++i]);
  const roots = rootArgs.length ? rootArgs : [REPO];
  const exts = includeDocs ? SCRIPT_EXT.concat(DOC_EXT) : SCRIPT_EXT;

  console.log('=== check-refs 引用闸（只读）· 空间声明 ===');
  console.log('  空间     : ' + roots.join(' ｜ '));
  console.log('  口径     : 扩展名 ' + exts.join(' ') + (includeDocs ? '（含文档）' : '（仅脚本）'));
  console.log('  数的串   : ' + scopeShape());   // ★ 由 SCOPE 派生（不再静态复述射程）
  console.log('  时点     : ' + new Date().toISOString());

  const sc = scopeSelfCheck();
  console.log('=== SCOPE 自检（每次运行都跑 · 拦"配置与实现不一致"）===');
  console.log('  ' + JSON.stringify(sc));
  if (!sc.ok) {
    console.log('  ⇒ ★★ 判【仪器坏】：SCOPE 声明与正则实际行为【不一致】✗');
    console.log('     ★ 处置：★ 改 SCOPE 或改正则，★ 使二者一致 —— ★ 而不是去修被扫的引用');
    console.log('RESULT=INSTRUMENT_BROKEN'); process.exitCode = 1; return;
  }
  console.log('  ⇒ SCOPE 声明与正则行为一致 ✓');

  const ctl = positiveControl();
  console.log('=== 阳性对照（每次运行都跑）===');
  console.log('  ' + JSON.stringify(ctl));
  if (!ctl.ok) {
    console.log('  ⇒ ★★ 判【仪器坏】—— 不判"被测对象干净"（§7.6 第 15 例 / 规则 G）');
    console.log('RESULT=INSTRUMENT_BROKEN');
    process.exitCode = 1; return;
  }
  console.log('  ⇒ 对照两侧均符合预期 ⇒ 仪器可用 ✓');

  const exempts = loadExempts(listPath);
  if (!exempts) { console.log('RESULT=FAIL'); process.exitCode = 1; return; }

  const { files, rows, skippedSources, unreadable } = scan(roots, exts, exempts);

  if (files.length === 0) {
    console.log('=== 来源侧 ===');
    skippedSources.forEach(s => console.log('  [SKIPPED] ' + s.src + ' —— ' + s.why));
    console.log('  ⇒ ★★ 枚举到 0 个文件 ⇒ FAIL（禁静默）');
    console.log('RESULT=FAIL(EMPTY_SPACE)'); process.exitCode = 1; return;
  }
  skippedSources.forEach(s => console.log('  [SKIPPED] ' + s.src + ' —— ' + s.why));

  if (unreadable.length) {
    console.log('=== 不可读文件（★ 显式登记，禁静默）===');
    unreadable.forEach(u => console.log('  [UNREADABLE] ' + path.relative(REPO, u.file) + ' —— ' + u.why));
    console.log('  ⇒ ★★ 扫描空间不完整 ⇒ FAIL（fail-closed）');
    console.log('RESULT=FAIL(UNREADABLE_' + unreadable.length + ')'); process.exitCode = 1; return;
  }

  const must = rows.filter(r => r.cls === 'must-exist');
  const wild = rows.filter(r => r.cls === 'wildcard');
  const exempt = rows.filter(r => r.cls === 'may-be-absent');
  const bad = rows.filter(r => !r.ok && r.cls !== 'may-be-absent');

  console.log('=== 扫描结果 ===');
  console.log('  扫了 ' + files.length + ' 个文件 ｜ 提取绝对路径字面量 ' + rows.length + ' 条');
  console.log('  其中 must-exist ' + must.length + ' ｜ wildcard ' + wild.length + ' ｜ may-be-absent ' + exempt.length);
  for (const r of bad) console.log('  [FAIL] ' + path.relative(REPO, r.file) + ' : 指向不存在之物 ⇒ ' + r.path);
  for (const r of exempt) console.log('  [EXEMPT] ' + path.relative(REPO, r.file) + ' ⇒ ' + r.path + '（理由：' + r.why + '）');
  if (wild.length) for (const r of wild) console.log('  [' + (r.ok ? 'OK' : 'FAIL') + '] wildcard ' + r.path);

  console.log('=== 防空来源判据 ===');
  console.log('  ① must-exist 计数 K = ' + must.length + (must.length === 0 ? ' ⇒ ★★ K=0 ⇒ FAIL（不是 PASS）' : ' ⇒ ≥1 ✓'));
  console.log('  ② 阳性对照 ran=' + ctl.ran + ' ok=' + ctl.ok + ' ✓');
  console.log('  ③ 来源侧 skipped = ' + skippedSources.length + '（已显式列出，无静默）');

  if (must.length === 0) {
    console.log('  ⇒ ★★ 本条的含义 = 【空间为空：没有可判的 must-exist 引用】');
    console.log('     ★ 它【不是】"发现了坏引用" —— 本结果里【没有任何一条指向不存在之物】✗');
    console.log('     ★ 处置方向：★ 先查【口径/空间】是否选错（扩展名？root？' + scopeHint() + '）');
    console.log('                  —— 而不是去修引用 ✓');
    console.log('RESULT=FAIL(K_ZERO)'); process.exitCode = 1; return;
  }
  if (bad.length) { console.log('RESULT=FAIL(' + bad.length + '_MISSING)'); process.exitCode = 1; return; }
  console.log('RESULT=PASS');
  process.exitCode = 0;
}

const argv = process.argv.slice(2);
main(argv);
