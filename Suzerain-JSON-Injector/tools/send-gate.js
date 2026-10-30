#!/usr/bin/env node


























'use strict';
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const REPO = path.resolve(__dirname, '..');
const TSV = path.join(REPO, 'tools', 'deliverables.tsv');
const SHAFILE = TSV + '.sha256';   
const update = process.argv.includes('--update');
const cp = require('child_process');

function sha256(p) { return crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex').toUpperCase(); }



function isGitNewOrModified(rel) {
  try {
    const out = cp.execSync('git -C "' + REPO + '" -c core.quotepath=false status --porcelain -- "' + rel + '"',
      { stdio: ['ignore', 'pipe', 'ignore'] }).toString().trim();
    return out.length > 0;   
  } catch (e) { return false; }
}

if (!fs.existsSync(TSV)) { console.log('[FAIL] 缺登记表：' + TSV); process.exit(1); }
const raw = fs.readFileSync(TSV, 'utf8').split(/\r?\n/);
const header = [], rows = [], scopes = [], scopeLines = [];
for (const line of raw) {
  if (!line.trim()) continue;
  if (line.startsWith('#@scope')) {
    const c = line.split('\t');
    const g = (c[1] || '').trim(), s = (c[2] || '').trim();
    if (g) scopes.push({ glob: g, since: s.startsWith('since:') ? s.slice(6).trim() : '' });
    scopeLines.push(line);   
    continue;                
  }
  if (line.startsWith('#')) { header.push(line); continue; }
  const c = line.split('\t');
  rows.push({ path: c[0], bytes: c[1], sha: c[2], note: c[3] || '' });
}
if (rows.length === 0) { console.log('[FAIL] 登记表里 0 条数据 ⇒ 空集假绿 ⇒ FAIL'); process.exit(1); }

console.log('=== 送审时闸（★ 送审前必跑 · fail-closed）===');
console.log('  登记表 : tools\\deliverables.tsv');
console.log('  时点   : ' + new Date().toISOString());
console.log('  条目   : ' + rows.length);

let bad = 0, updated = 0;
for (const r of rows) {
  const p = path.join(REPO, r.path);
  if (!fs.existsSync(p)) { console.log('  [FAIL] 缺失：' + r.path); bad++; continue; }
  const b = fs.statSync(p).size, s = sha256(p);
  if (update) {
    const ch = (String(b) !== r.bytes) || (s !== r.sha);
    if (ch) updated++;
    r.bytes = String(b); r.sha = s;
    console.log('  [' + (ch ? 'UPDATED' : 'OK') + '] ' + r.path + '  ' + b + ' B / ' + s.slice(0, 16) + '…');
  } else {
    const ok = (String(b) === r.bytes) && (s === r.sha);
    if (!ok) bad++;
    console.log('  [' + (ok ? 'OK' : 'FAIL') + '] ' + r.path.padEnd(50) +
      ' 实测 ' + b + ' B/' + s.slice(0, 16) + '… ｜ 登记 ' + r.bytes + ' B/' + String(r.sha).slice(0, 16) + '…');
  }
}


const registered = new Set(rows.map(r => r.path.replace(/\\/g, '/')));
const missing = [], preexisting = [];



let coverageState = '已断言';        
let nAsserted = 0, nNotApplicable = 0;
if (scopes.length === 0) {
  console.log('  ⚠ 未声明 `#@scope` ⇒ ★ 覆盖性未声明（★ 本闸不知道表外该有什么）⚪');
  coverageState = '未声明';
} else {
  console.log('  #@scope 声明 ' + scopes.length + ' 条（★ 按批次）：');
  for (const sc of scopes) {
    const dir = path.dirname(sc.glob), base = path.basename(sc.glob);
    const re = new RegExp('^' + base.replace(/[.+^${}()|[\]\\]/g, '\\$&').replace(/\*/g, '.*') + '$');
    const abs = path.join(REPO, dir);
    if (!fs.existsSync(abs)) continue;
    const sinceMs = sc.since ? Date.parse(sc.since) : NaN;
    let nHit = 0, nNew = 0, nPre = 0;
    for (const name of fs.readdirSync(abs)) {
      if (!re.test(name)) continue;
      nHit++;
      const rel = (dir + '/' + name).replace(/\\/g, '/');
      if (registered.has(rel)) continue;
      const mt = fs.statSync(path.join(abs, name)).mtimeMs;
      
      
      if (isGitNewOrModified(rel)) { missing.push(rel); nNew++; }
      else if (!isNaN(sinceMs) && mt < sinceMs) { preexisting.push(rel); nPre++; }
      else { missing.push(rel); nNew++; }
    }
    console.log('    ' + sc.glob + (sc.since ? '   since:' + sc.since : '   (无 since ⇒ 严格)') +
      '  ⇒ 命中 ' + nHit + ' ／ 本批新增未登记 ' + nNew + ' ／ 既有 ' + nPre);
    
    
    
    if (!isNaN(sinceMs)) {
      let earliest = Infinity, nBatch = 0;
      for (const nm of fs.readdirSync(abs)) {
        if (!re.test(nm)) continue;
        const rl = (dir + '/' + nm).replace(/\\/g, '/');
        if (!registered.has(rl)) continue;
        
        
        
        
        
        if (!isGitNewOrModified(rl)) continue;
        const t = fs.statSync(path.join(abs, nm)).mtimeMs;
        nBatch++;
        if (t < earliest) earliest = t;
      }
      if (nBatch === 0) {
        console.log('      ⚠ `since` 自检【不适用】：本 scope 无"git 未跟踪/已改动"的已登记件 ⚪');
        nNotApplicable++;
      } else if (sinceMs > earliest) {
        console.log('      ★★ [FAIL] `since` 晚于【本批】最早件 ⇒ 会把本批件误判为"既有" ⇒ 漏报 ✗');
        console.log('         since  = ' + new Date(sinceMs).toISOString() +
          ' ／ 本批最早件 = ' + new Date(earliest).toISOString() + '（本批成员 ' + nBatch + " 件）");
        if (!update) bad++;
      } else {
        console.log('      ✓ `since` 自检通过：since ≤ 本批最早件（' + new Date(earliest).toISOString() +
          '，本批成员 ' + nBatch + ' 件）');
        nAsserted++;
      }
    }
  }
  for (const u of missing) console.log('    [未登记·本批] ' + u + '  ⇐ ★ 必须登记 ✗');
  if (preexisting.length) console.log('    （既有 ' + preexisting.length + ' 件 ⇒ 不报错；`--update` 会纳入为"既有"行）');
}
if (missing.length && !update) bad += missing.length;



if (update) {
  const have = new Set(rows.map(r => r.path.replace(/\\/g, '/')));
  const add = (rel, note) => {
    if (have.has(rel)) return;
    const p = path.join(REPO, rel);
    if (!fs.existsSync(p)) return;
    rows.push({ path: rel, bytes: String(fs.statSync(p).size), sha: sha256(p), note });
    have.add(rel); updated++;
  };
  for (const u of missing) add(u, '（--update 纳入·本批）');
  for (const u of preexisting) add(u, '（--update 纳入·既有）');
  const body = header.concat(scopeLines, rows.map(r => [r.path, r.bytes, r.sha, r.note].join('\t'))).join('\n') + '\n';
  fs.writeFileSync(TSV, body, 'utf8');
  
  
  const th = crypto.createHash('sha256').update(fs.readFileSync(TSV)).digest('hex');
  fs.writeFileSync(SHAFILE, th + '\n', 'utf8');
  console.log('  ⇒ 表外哈希已刷新：tools\\deliverables.tsv.sha256 = ' + th);
  console.log('     ★ 即：★ 表内不含自身哈希，★ 但由该 .sha256 文件覆盖 ⇒ ★ **自指链条闭合** ✓');
  console.log('  ⇒ 登记表已写回：★ 更新/新增 ' + updated + ' 处 ⇒ ★ 【报警已有出路】✓');
  console.log('SEND_GATE=UPDATED');
  process.exit(0);
}

console.log('');
console.log('  覆盖性小结：★ 已断言 ' + nAsserted + ' 条 ／ 不适用 ' + nNotApplicable + ' 条 ／ 声明 ' + scopes.length + ' 条');
if (scopes.length === 0) coverageState = '未声明';
else if (nNotApplicable > 0 && nAsserted === 0) coverageState = '不适用';
else if (nNotApplicable > 0) coverageState = '部分不适用';
else coverageState = '已断言且通过';
if (bad) {
  console.log('  ⇒ ★★ ' + bad + ' 件与登记【不一致或本批未登记】⇒ ★ 【拒绝发送】（fail-closed）✗');
  console.log('     ★★ 出路：★ 跑 `--update` ⇒ 重算并补齐（★ 本版**可新增行** ⇒ 报警不是死锁）✓');
  console.log('SEND_GATE=REFUSE');
  process.exit(1);
}
console.log('  ⇒ ★★ 全部 ' + rows.length + ' 件【逐位一致】＋ 覆盖性【' + coverageState + '】⇒ 允许发送 ✓');
console.log('     ★ 含义：★ "我送审的那一份" = "我核过的那一份"（★ 这正是审查的前提）');

if (coverageState === '已断言且通过') console.log('SEND_GATE=ALLOW');
else console.log('SEND_GATE=ALLOW(覆盖性' + coverageState + ')');
