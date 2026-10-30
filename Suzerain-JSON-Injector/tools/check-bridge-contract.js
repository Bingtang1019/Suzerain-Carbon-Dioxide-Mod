'use strict';

















const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');          
const repo = path.resolve(root, '..');               
const json = process.argv.includes('--json');

const problems = [];
const warns = [];
const lines = [];
const log = (s) => { lines.push(s); if (!json) console.log(s); };
const note = (s) => log('  ' + s);


function find(rel) {
  const p = path.isAbsolute(rel) ? rel : path.join(repo, rel);
  return fs.existsSync(p) ? p : null;
}

const MODS = [
  {
    id: 'Oikonomia',
    name: '安济艺术',
    bridgeClass: 'OiBridge',
    mod: find('Oikonomia/src/Oikonomia/Bridge.cs'),
    gui: find('Suzerain-JSON Injector/src/SuzerainGUI.Bridge.Oi.cs'),
    modPrefix: '模组 Oikonomia',
  },
  {
    id: 'HB',
    name: 'Horrifying Bombardment',
    bridgeClass: 'HbBridge',
    mod: find('Horrifying Bombardment/src/HorrifyingBombardment/Bridge.cs'),
    gui: find('Suzerain-JSON Injector/src/SuzerainGUI.Bridge.Hb.cs'),
    
    modAlt: find('Horrifying Bombardment/src/HorrifyingBombardment'),
    modPrefix: '模组 HB',
  },
];


function guiFiles(primary) {
  const dir = path.join(root, 'src');
  const list = [];
  if (primary) list.push(primary);
  if (fs.existsSync(dir)) {
    for (const f of fs.readdirSync(dir)) {
      if (/^SuzerainGUI.*\.cs$/.test(f)) {
        const p = path.join(dir, f);
        if (!list.includes(p)) list.push(p);
      }
    }
  }
  return list;
}




const META_STATE_KEYS = new Set(['apiVersion', 'name', 'pid', 'updatedAt', 'source', 'reason']);

const META_OVERRIDE_KEYS = new Set(['apiVersion', 'updatedAt', 'source']);


function readAllCs(dirOrFile) {
  if (!dirOrFile) return '';
  const st = fs.statSync(dirOrFile);
  if (st.isFile()) return fs.readFileSync(dirOrFile, 'utf8');
  const out = [];
  (function walk(d) {
    for (const e of fs.readdirSync(d, { withFileTypes: true })) {
      const p = path.join(d, e.name);
      if (e.isDirectory()) { if (e.name !== 'obj' && e.name !== 'bin') walk(p); }
      else if (e.name.endsWith('.cs')) out.push(fs.readFileSync(p, 'utf8'));
    }
  })(dirOrFile);
  return out.join('\n');
}


const uniqSorted = (a) => [...new Set(a)].sort();



function modStateKeys(src) {
  const keys = [];
  const re = /Append(?:Line)?\(\s*"\s*\\?"([A-Za-z_]\w*)\\?"\s*:/g;
  let m;
  while ((m = re.exec(src))) keys.push(m[1]);
  return uniqSorted(keys);
}

function modOverrideKeys(src) {
  const keys = [];
  const re = /(?:Read|Get)(?:NullableInt|Int|Bool|String|Double|Float)\(\s*\w+\s*,\s*"([A-Za-z_]\w*)"/g;
  let m;
  while ((m = re.exec(src))) keys.push(m[1]);
  return uniqSorted(keys);
}

function guiStateKeys(src, bridgeClass) {
  const keys = [];
  const re = new RegExp(bridgeClass + '\\.State\\(\\s*"([A-Za-z_]\\w*)"', 'g');
  let m;
  while ((m = re.exec(src))) keys.push(m[1]);
  return uniqSorted(keys);
}

function guiOverrideReadKeys(src, bridgeClass) {
  const keys = [];
  const re = new RegExp(bridgeClass + '\\.Override(?:Int|Bool)\\(\\s*"([A-Za-z_]\\w*)"', 'g');
  let m;
  while ((m = re.exec(src))) keys.push(m[1]);
  return uniqSorted(keys);
}

function guiOverrideWriteKeys(src, bridgeClass) {
  const keys = [];
  const m = src.match(new RegExp('class\\s+' + bridgeClass + '\\b[\\s\\S]*?(?=\\n\\s*(?:internal|public|private|protected|static)\\s+(?:static\\s+)?class\\s|$)'));
  const body = m ? m[0] : src;
  const re = /Append(?:Line)?\(\s*"\s*\\?"([A-Za-z_]\w*)\\?"\s*:/g;
  let x;
  while ((x = re.exec(body))) keys.push(x[1]);
  return uniqSorted(keys);
}

log('桥协议契约核对（模组只读，不修改任何文件）');
log('仓库根：' + repo);
log('');

let totalGaps = 0;

for (const M of MODS) {
  log('── ' + M.name + ' (' + M.id + ') ──');
  if (!M.gui) { warns.push(M.id + ' 缺少修改器桥文件，跳过'); log('  ⚠ 缺少修改器侧桥文件，跳过'); log(''); continue; }
  const modSrc = M.mod ? fs.readFileSync(M.mod, 'utf8') : (M.modAlt ? readAllCs(M.modAlt) : '');
  if (!modSrc) { warns.push(M.id + ' 找不到模组侧 Bridge.cs，跳过'); log('  ⚠ 找不到模组侧 Bridge.cs，跳过（只读侧无法核对）'); log(''); continue; }
  const guiSrc = guiFiles(M.gui).map(f => fs.readFileSync(f, 'utf8')).join('\n');

  const mState = modStateKeys(modSrc);
  const mOvRead = modOverrideKeys(modSrc);
  const gState = guiStateKeys(guiSrc, M.bridgeClass);
  const gOvRead = guiOverrideReadKeys(guiSrc, M.bridgeClass);
  const gOvWrite = guiOverrideWriteKeys(guiSrc, M.bridgeClass);

  log('  模组写 state 键 ' + mState.length + ' 个；修改器读 state 键 ' + gState.length + ' 个');
  log('  模组读 override 键 ' + mOvRead.length + ' 个；修改器写 override 键 ' + gOvWrite.length + ' 个；修改器自读 override 键 ' + gOvRead.length + ' 个');
  log('');

  const stateMissing = mState.filter(k => !META_STATE_KEYS.has(k) && !gState.includes(k));
  const stateExtra = gState.filter(k => !mState.includes(k));
  const ovNotRead = gOvWrite.filter(k => !META_OVERRIDE_KEYS.has(k) && !mOvRead.includes(k));
  const ovReadNotWritten = gOvRead.filter(k => !gOvWrite.includes(k));
  const ovReadNotModRead = gOvRead.filter(k => !mOvRead.includes(k));

  const report = (title, list, hint) => {
    if (list.length === 0) { note('✓ ' + title + '：无缺口'); return 0; }
    for (const k of list) { problems.push(M.id + ' ' + title + ' 缺 ' + k); note('✗ ' + title + '：' + k + (hint ? '  （' + hint + '）' : '')); }
    return list.length;
  };

  let gaps = 0;
  gaps += report('模组 state → 修改器', stateMissing, '模组写了但修改器没有读取代码');
  gaps += report('修改器 → 模组 state（反向）', stateExtra, '修改器读了但模组不写该键');
  gaps += report('修改器 override → 模组', ovNotRead, '修改器写了但模组不读该键');
  gaps += report('修改器自读 override → 写入', ovReadNotWritten, '修改器读了自己不写的键（会一直取 fallback）');
  gaps += report('修改器自读 override → 模组读取', ovReadNotModRead, '修改器读的键模组不读，写了也不生效');

  log('');
  log('  清单（模组 state 键，非元数据）：' + ((mState.filter(k => !META_STATE_KEYS.has(k)).join(', ')) || '（无）'));
  log('  清单（修改器正确读取的 state 键）：' + (gState.filter(k => mState.includes(k)).join(', ') || '（无）'));
  log('  清单（修改器写 override 键）：' + (gOvWrite.join(', ') || '（无）'));
  log('  清单（模组读 override 键）：' + (mOvRead.join(', ') || '（无）'));
  log('');
  totalGaps += gaps;
}

log('────────────────────────────────────────────');
if (totalGaps === 0 && problems.length === 0) {
  log('结果：桥协议契约一致（0 缺口' + (warns.length ? '，' + warns.length + ' 项跳过' : '') + '）');
} else {
  log('结果：发现 ' + problems.length + ' 项契约缺口');
  for (const p of problems) log('  ✗ ' + p);
}
if (warns.length) for (const w of warns) log('  ⚠ ' + w);

if (json) {
  process.stdout.write(JSON.stringify({ problems, warnings: warns, exitCode: problems.length ? 1 : 0 }, null, 2) + '\n');
}
process.exit(problems.length ? 1 : 0);
