'use strict';












const { execFileSync, spawnSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const os = require('os');




const CLI_SOURCES = ['src/SuzerainTrainer.cs', 'src/SuzerainGUI.Version.cs'];

const root = path.resolve(__dirname, '..');
const args = process.argv.slice(2);
const VERSION = 'v1.9.1';




function guiSources() {
  const dir = path.join(root, 'src');
  if (!fs.existsSync(dir)) return [];
  return fs.readdirSync(dir)
    .filter(f => /^SuzerainGUI.*\.cs$/.test(f))
    .sort()
    .map(f => 'src/' + f);
}
const GUI_SOURCES = guiSources();


const OPT = {
  quick: args.includes('--quick'),
  json: args.includes('--json'),
  strictWarnings: args.includes('--strict-warnings'),
  skip: new Set(),
};
for (const a of args) {
  if (a.startsWith('--skip=')) a.slice(7).split(',').forEach(x => OPT.skip.add(x.trim()));
}


const log = (...m) => (OPT.json ? console.error(...m) : console.log(...m));
const report = { tool: 'suzerain-selfcheck', version: '1.0.0', targetVersion: VERSION, checks: [], exitCode: 0 };
const stats = { problems: 0, warnings: 0, skipped: 0 };
const t0 = Date.now();


function runCmd(cmd, cwd) {
  try {
    const out = execFileSync(cmd[0], cmd.slice(1), { cwd: cwd || root, stdio: 'pipe', timeout: 120000, encoding: 'utf8' });
    return { ok: true, out };
  } catch (e) {
    const raw = String(e.stdout || '') + String(e.stderr || '');
    return { ok: false, out: raw };
  }
}
function exists(p) { return fs.existsSync(path.join(root, p)); }
function mtime(p) { return exists(p) ? fs.statSync(path.join(root, p)).mtimeMs : 0; }


const CHECKS = [];
const check = (id, title, quick, fn) => CHECKS.push({ id, title, quick, fn });
const ok = (m) => log('  \u2713 ' + m);
const bad = (m) => { stats.problems++; log('  \u2717 ' + m); };
const warn = (m) => { stats.warnings++; log('  \u26A0 ' + m); };




const skip = (m) => { stats.skipped++; log('  \u2298 跳过 ' + m); };


function findCsc() {
  const cands = [
    'C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe',
    'C:/Windows/Microsoft.NET/Framework/v4.0.30319/csc.exe',
  ];
  for (const c of cands) if (fs.existsSync(c)) return c;
  return null;
}
function compileTarget(csc, out, refs, sources) {
  const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'szcheck_'));
  const exe = path.join(tmp, out);
  const cmd = [csc, '-nologo', '-noconfig', '-target:winexe', '-out:' + exe, '-codepage:65001'];
  for (const r of refs) cmd.push('-r:' + r);
  for (const s of sources) cmd.push(path.join(root, s));
  const r = runCmd(cmd);
  try { fs.rmSync(tmp, { recursive: true, force: true }); } catch (e) { }
  return r;
}
check('cs-syntax', 'C# 编译检查（GUI + CLI 两个目标）', true, () => {
  const csc = findCsc();
  if (!csc) { bad('未找到 csc.exe（.NET Framework 编译器）'); return; }
  const fw = path.dirname(csc);
  const refs = [fw + '/System.Windows.Forms.dll', fw + '/System.Drawing.dll', fw + '/System.dll', fw + '/System.Core.dll'];
  let fail = 0;
  if (GUI_SOURCES.length === 0) { bad('缺少 src/SuzerainGUI*.cs'); fail++; }
  else {
    const sources = GUI_SOURCES.concat(exists('src/TranslateDict.cs') ? ['src/TranslateDict.cs'] : []);
    const r = compileTarget(csc, 'gui_tmp.exe', refs, sources);
    const errs = r.out.split('\n').filter(l => /error CS/.test(l));
    if (r.ok && errs.length === 0) ok('GUI 编译通过（' + sources.length + ' 个源文件：' + GUI_SOURCES.length + ' 个 GUI 分页 + 词典）');
    else { fail++; bad('GUI 编译失败：' + (errs[0] || '未知错误').trim()); }
  }
  if (!exists('src/SuzerainTrainer.cs')) { bad('缺少 src/SuzerainTrainer.cs'); fail++; }
  else {
    const r = compileTarget(csc, 'cli_tmp.exe', [fw + '/System.dll', fw + '/System.Core.dll'], CLI_SOURCES);
    const errs = r.out.split('\n').filter(l => /error CS/.test(l));
    if (r.ok && errs.length === 0) ok('CLI 编译通过（' + CLI_SOURCES.join(' + ') + '）');
    else { fail++; bad('CLI 编译失败：' + (errs[0] || '未知错误').trim()); }
  }
  if (fail === 0 && !exists('src/TranslateDict.cs')) { warn('src/TranslateDict.cs 缺失（词典功能不可用）'); }
});


check('build-fresh', '编译产物新鲜度（exe 时间戳 >= 源码）', true, () => {
  let stale = 0;
  const guiSrcs = GUI_SOURCES.concat(['src/TranslateDict.cs']);
  for (const [exe, srcs] of [['SuzerainGUI.exe', guiSrcs], ['bin/SuzerainTrainer.exe', CLI_SOURCES]]) {
    if (!exists(exe)) { bad('缺少编译产物 ' + exe); stale++; continue; }
    const exeT = mtime(exe);
    const srcT = Math.max(...srcs.map(mtime));
    if (srcT > exeT + 2000) { warn(exe + ' 早于源码 ' + Math.round((srcT - exeT) / 1000) + ' 秒，可能是旧产物'); stale++; }
    else ok(exe + ' 是最新产物');
  }
  if (stale > 0) bad('存在过期/缺失产物，请重新编译后打包');
});


check('dict-sync', '翻译词典完整性（Words/Exact/Prefixes + 重复键）', true, () => {
  if (!exists('src/TranslateDict.cs')) { bad('src/TranslateDict.cs 不存在'); return; }
  const s = fs.readFileSync(path.join(root, 'src/TranslateDict.cs'), 'utf8');
  let n = 0;
  for (const k of ['Words =', 'Exact =', 'Prefixes =']) if (s.indexOf(k) < 0) n++;
  if (n > 0) { bad('词典缺少 ' + n + ' 个字典定义（Words/Exact/Prefixes）'); return; }
  const words = (s.match(/\{ "[A-Za-z0-9_]+", "[^"]+" \}/g) || []).length;
  if (words < 500) { bad('词根数量异常：' + words + '（期望 >= 500，对照表生成后应同步更新）'); return; }
  const seen = new Set(); const dups = [];
  const wre = /\{ "([A-Za-z0-9_]+)", "[^"]*" \}/g;
  let dm;
  while ((dm = wre.exec(s))) { if (seen.has(dm[1])) dups.push(dm[1]); else seen.add(dm[1]); }
  if (dups.length > 0) { bad('词典存在重复键（会导致 GUI 列表页空白）：' + dups.join(', ')); return; }
  ok('词典完整：词根 ' + words + ' 条，无重复键');
});


check('docs-sync', '文档一致性（版本串/对照表/说明）', false, () => {
  const need = [
    ['docs/Suzerain_变量术语对照表.md', '术语对照表'],
    ['docs/Suzerain_本体与DLC变量区分.txt', '本体与DLC区分'],
    ['README.txt', '使用说明'],
  ];
  let miss = 0;
  for (const [f, name] of need) {
    if (!exists(f)) { bad('缺少文档：' + f + '（' + name + '）'); miss++; continue; }
    const s = fs.readFileSync(path.join(root, f), 'utf8');
    if (f === 'README.txt' && s.indexOf(VERSION) < 0) { warn('README.txt 未标注当前版本 ' + VERSION); }
    ok(f + ' 存在');
  }
  if (miss === 0) ok('文档齐全');
});


check('version-sync', '版本一致性（唯一来源 BuildInfo / GUI+CLI 引用 / 使用说明）', true, () => {
  let n = 0;

  
  const VF = 'src/SuzerainGUI.Version.cs';
  if (!exists(VF)) { bad('缺少版本号唯一来源 ' + VF); n++; }
  else {
    const vs = fs.readFileSync(path.join(root, VF), 'utf8');
    const m = vs.match(/Version\s*=\s*"v?(\d+\.\d+\.\d+)"/);
    if (!m) { bad(VF + ' 中未找到 BuildInfo.Version = "vX.Y.Z"'); n++; }
    else if (m[1] !== VERSION.replace('v', '')) { bad('唯一来源版本 v' + m[1] + ' ≠ 本脚本 ' + VERSION + '，请同步修改'); n++; }
    else ok('版本号唯一来源 = ' + VERSION + '（' + VF + '）');
  }

  
  
  const users = [
    ['src/SuzerainGUI.MainForm.cs', 'GUI 窗口标题'],
    ['src/SuzerainTrainer.cs', 'CLI 横幅与 --version'],
  ];
  for (const [f, who] of users) {
    if (!exists(f)) { bad('缺少 ' + f); n++; continue; }
    const s = fs.readFileSync(path.join(root, f), 'utf8');
    if (s.indexOf('BuildInfo.Version') < 0) { bad(f + ' 未使用 BuildInfo.Version（' + who + '必须共用唯一来源）'); n++; continue; }
    const hard = s.match(/v\d+\.\d+\.\d+/g);
    if (hard) { bad(f + ' 里仍有写死的版本串：' + hard.join(', ') + '（应改用 BuildInfo.Version）'); n++; }
    else ok(who + ' 引用唯一来源，无写死版本串');
  }

  
  if (exists('README.txt')) {
    const doc = fs.readFileSync(path.join(root, 'README.txt'), 'utf8');
    if (doc.indexOf(VERSION) < 0) { warn('README.txt 未标注版本 ' + VERSION); n++; }
  }
  if (n > 0) bad('版本不一致，请统一修改后再打包');
  else ok('唯一来源 = GUI/CLI 引用 = 使用说明，' + VERSION + ' 全部对齐');
});


check('leftover', '遗留标记（TODO/FIXME/HACK）', true, () => {
  let leftover = 0, scanned = 0;
  const missing = [];
  for (const f of GUI_SOURCES.concat(['src/SuzerainTrainer.cs', 'src/TranslateDict.cs'])) {
    if (!exists(f)) { missing.push(f); continue; }   
    scanned++;
    const s = fs.readFileSync(path.join(root, f), 'utf8');
    for (const m of s.matchAll(/(TODO|FIXME|HACK)[：:]?\s*(.*?)(?:\n|\r)/g)) { leftover++; warn(f + ': ' + m[1] + ' ' + m[2].trim().slice(0, 60)); }
  }
  
  if (scanned === 0) { skip('遗留标记扫描：源文件一个都没找到（' + missing.join(', ') + '）—— 本项未运行'); return; }
  if (missing.length > 0) warn('遗留标记扫描：' + missing.length + ' 个源文件缺失、未纳入扫描（' + missing.join(', ') + '）');
  if (leftover === 0) ok('未发现遗留标记（已扫描 ' + scanned + ' 个源文件）');
});


check('core-check', '核心逻辑自检（读档/写值/回滚/StoryMode/扫描计数）', true, () => {
  const testSave = 'tools/testdata/t1.json';
  if (!exists(testSave)) { bad('缺少测试存档副本 ' + testSave + '（tools/testdata/t1.json）'); return; }
  if (!exists('SuzerainGUI.exe')) { bad('缺少 SuzerainGUI.exe'); return; }
  const r = runCmd([path.join(root, 'SuzerainGUI.exe'), '--check', path.join(root, testSave)]);
  if (!r.ok) { bad('自检进程运行失败：' + r.out.split('\n').slice(-3).join(' | ')); return; }
  const lines = r.out.split('\n');
  const must = ['LOAD_OK', 'BudgetSet=7', 'BudgetRestore=-1', 'VarCount=56', 'BoolCount=190', 'EntityCount=927', 'StoryMode='];
  let miss = [];
  for (const m of must) {
    const hit = lines.some(l => l.indexOf(m) >= 0);
    if (!hit) miss.push(m);
  }
  if (miss.length === 0) ok('核心逻辑自检全绿（' + lines.filter(l => /^(LOAD_OK|StoryMode|VarCount|BoolCount|EntityCount)/.test(l)).join(' / ') + '）');
  else bad('自检输出缺少关键项：' + miss.join(', '));
});


check('save-sync', '测试存档 JSON 可解析', false, () => {
  const f = 'tools/testdata/t1.json';
  if (!exists(f)) { bad('缺少 ' + f); return; }
  try {
    const d = JSON.parse(fs.readFileSync(path.join(root, f), 'utf8'));
    if (typeof d.variables !== 'string' || d.variables.length < 100000) bad('variables 字段异常（长度 ' + (d.variables || '').length + '）');
    else ok('JSON 可解析，variables ' + (d.variables.length / 1024).toFixed(0) + ' KB');
  } catch (e) { bad('JSON 解析失败：' + e.message); }
});


check('bridge-contract', '桥协议契约（模组 state/override 键 ↔ 修改器读取键双向核对）', true, () => {
  const script = 'tools/check-bridge-contract.js';
  if (!exists(script)) { skip('桥契约核对：缺少 ' + script + ' —— 本项未运行'); return; }
  const r = runCmd([process.execPath, path.join(root, script)]);
  if (r.ok) ok('桥契约核对通过（' + r.out.split('\n').filter(l => l.trim()).slice(-1)[0].trim().slice(0, 90) + '）');
  else {
    const lines = r.out.split('\n').filter(l => /✗|缺失|MISSING/.test(l));
    bad('桥契约不一致：' + (lines.slice(0, 4).join(' | ') || r.out.split('\n').slice(-3).join(' | ')));
  }
});






function runLayoutScript(rel, exe) {
  const p = path.join(root, rel);
  if (!fs.existsSync(p)) return { status: 'missing' };
  const r = spawnSync('powershell', ['-STA', '-ExecutionPolicy', 'Bypass', '-File', p, '-Exe', exe],
    { cwd: root, encoding: 'utf8', timeout: 180000 });
  if (r.error) {
    
    if (String(r.error.code) === 'ETIMEDOUT' || /timed?out/i.test(r.error.message || '')) return { status: 'timeout', detail: r.error.message };
    return { status: 'error', detail: r.error.message };
  }
  const out = String(r.stdout || '') + String(r.stderr || '');
  if (r.status === 0) return { status: 'ok', out };
  return { status: 'fail', out };
}
check('panel-layout', '面板布局实测（check-panel-layout.ps1：截断/越界/重叠）', false, () => {
  const exe = path.join(root, 'SuzerainGUI.exe');
  if (!exists('SuzerainGUI.exe')) { skip('面板布局实测：缺少 SuzerainGUI.exe —— 本项未运行'); return; }
  const R = [['tools/check-panel-layout.ps1', 'check-panel-layout'], ['tools/verify-anchor.ps1', 'verify-anchor（安济艺术页视口）'], ['tools/verify-anchor-hb.ps1', 'verify-anchor-hb（HB 页视口）']];
  let pass = 0, fail = 0, skipped = 0;
  for (const [rel, name] of R) {
    const r = runLayoutScript(rel, exe);
    if (r.status === 'ok') { ok(name + ' 通过'); pass++; }
    else if (r.status === 'missing') { skip(name + ' 脚本缺失 —— 本项未运行'); skipped++; }
    else if (r.status === 'timeout') { bad(name + ' 运行超时（180 秒）—— 可能是脚本卡住，不是环境问题'); fail++; }
    else {
      const tail = (r.out || r.detail || '').split('\n').filter(l => /✗|FAIL|结论|Exception/.test(l)).slice(-2).join(' | ');
      
      
      if (/LoadFrom|Activator|CreateInstance|无法加载|FileNotFound|UnauthorizedAccess/i.test(r.out || r.detail || '')) { skip(name + ' 无法在当前环境运行 —— 本项未运行：' + (r.detail || '').slice(0, 80)); skipped++; }
      else { bad(name + ' 失败：' + (tail || '退出码非 0')); fail++; }
    }
  }
  
  if (fail === 0 && skipped === 0) ok('三项布局/视口实测全部通过');
  else if (skipped > 0) skip('三项布局/视口实测：通过 ' + pass + ' · 跳过 ' + skipped + ' · 失败 ' + fail + '（跳过 = 没验）');
});


check('package', '交付目录完整性（桌面文件夹直接交付）', false, () => {
  const files = fs.readdirSync(root);
  let miss = 0;
  const need = ['SuzerainGUI.exe', 'README.txt', 'src', 'bin', 'docs', 'tools'];
  for (const f of need) {
    if (!files.includes(f)) { bad('交付目录缺少 ' + f); miss++; }
  }
  if (miss === 0) ok('交付目录清单完整（' + files.length + ' 个条目）');
});


let ran = 0;
const skippedByUser = [];
for (const c of CHECKS) {
  
  if (OPT.skip.has(c.id)) { skippedByUser.push(c.id + '（' + c.title + '）'); continue; }
  if (OPT.quick && !c.quick) { skippedByUser.push(c.id + '（--quick 未包含）'); continue; }
  log('[' + (++ran) + '/' + CHECKS.length + '] ' + c.title);
  try { c.fn(); } catch (e) { bad('检查器异常：' + e.message); }
}
if (skippedByUser.length) {
  log('\u2298 按参数跳过 ' + skippedByUser.length + ' 项（用户有意为之，不计入失败）：');
  for (const s of skippedByUser) log('    ' + s);
}

report.checks = CHECKS.map(c => ({
  id: c.id, title: c.title,
  status: OPT.skip.has(c.id) ? 'skipped' : 'run',
}));
report.summary = { problems: stats.problems, warnings: stats.warnings, skipped: stats.skipped, elapsedMs: Date.now() - t0 };


report.exitCode = stats.problems > 0 || stats.skipped > 0 || (OPT.strictWarnings && stats.warnings > 0) ? 1 : 0;

log('----------------------------------------');
log('问题 ' + stats.problems + ' | 警告 ' + stats.warnings + ' | 跳过 ' + stats.skipped + ' | 耗时 ' + (report.summary.elapsedMs / 1000).toFixed(1) + 's');
if (stats.skipped > 0) log('\u26A0 有 ' + stats.skipped + ' 项跳过（= 没验），因此退出码非零 —— 跳过不等于通过');
if (OPT.json) process.stdout.write(JSON.stringify(report, null, 2) + '\n');
process.exit(report.exitCode);
