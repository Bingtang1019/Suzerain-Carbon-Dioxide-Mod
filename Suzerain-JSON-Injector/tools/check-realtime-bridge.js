'use strict';


















const fs = require('fs');
const os = require('os');
const path = require('path');
const { spawn, spawnSync } = require('child_process');

const root = path.resolve(__dirname, '..');
const exe = path.join(root, 'SuzerainGUI.exe');
const mockExe = path.join(root, 'tools', 'mockmod', 'MockModBridge.exe');
const mockSrc = path.join(root, 'tools', 'mockmod', 'MockModBridge.cs');
const mockLog = path.join(root, 'tools', 'mockmod', 'mock.log');
const PIPE = 'OikonomiaBridge';
const OI_DIR = path.join(process.env.LOCALAPPDATA || path.join(os.homedir(), 'AppData', 'Local'), 'Oikonomia');
const OVERRIDE = path.join(OI_DIR, 'Oikonomia.override.json');
const CSC = 'C:\\Windows\\Microsoft.NET\\Framework64\\v4.0.30319\\csc.exe';

const problems = [];
const warns = [];
const log = (...a) => console.log(...a);
const ok = (m) => log('  ✓ ' + m);
const bad = (m) => { problems.push(m); log('  ✗ ' + m); };
const warn = (m) => { warns.push(m); log('  ⚠ ' + m); };
const sleep = (ms) => new Promise(r => setTimeout(r, ms));

if (!fs.existsSync(exe)) { console.error('缺少 ' + exe); process.exit(2); }


function ensureMock() {
  if (fs.existsSync(mockExe) && fs.statSync(mockExe).mtimeMs >= fs.statSync(mockSrc).mtimeMs) return true;
  if (!fs.existsSync(CSC)) { warn('未找到 csc.exe，无法编译假模组'); return false; }
  
  
  const r = spawnSync(CSC, ['-nologo', '-noconfig', '-target:exe', '-codepage:65001', '-out:' + mockExe,
    '-r:' + path.join(path.dirname(CSC), 'System.dll'),
    '-r:' + path.join(path.dirname(CSC), 'System.Core.dll'), mockSrc], { encoding: 'utf8' });
  if (r.status !== 0) { warn('假模组编译失败：' + String(r.stdout || r.stderr || '').split('\n')[0]); return false; }
  return true;
}


let backup = null, backupExisted = false;
function backupOverride() {
  try {
    backupExisted = fs.existsSync(OVERRIDE);
    backup = backupExisted ? fs.readFileSync(OVERRIDE) : null;
  } catch (e) { warn('无法备份 override：' + e.message); }
}
function restoreOverride() {
  try {
    if (backupExisted && backup) fs.writeFileSync(OVERRIDE, backup);
    else if (!backupExisted && fs.existsSync(OVERRIDE)) fs.unlinkSync(OVERRIDE);
  } catch (e) { warn('无法恢复 override：' + e.message); }
}

function runExe(args, timeout) {
  const r = spawnSync(exe, args, { encoding: 'utf8', timeout: timeout || 120000 });
  return { code: r.status, out: String(r.stdout || '') + String(r.stderr || ''), err: r.error };
}
const val = (out, key) => {
  const m = out.match(new RegExp('^' + key + '=(.*)$', 'm'));
  return m ? m[1].trim() : null;
};

let mockProc = null;

async function main() {
  
  log('[1/3] 无管道场景（回退文件桥）');
  backupOverride();
  {
    const r = runExe(['--oitest']);
    if (r.err) bad('exe 无法运行：' + r.err.message);
    else if (/OITEST_OK/.test(r.out)) ok('无管道时 --oitest 输出 OITEST_OK（文件桥正常）');
    else if (/OITEST_WARN/.test(r.out)) warn('无管道时 --oitest 输出 OITEST_WARN');
    else bad('无管道时 --oitest 未输出预期结果：' + r.out.split('\n').filter(Boolean).slice(-3).join(' | '));
  }
  {
    const r = runExe(['--pipetest']);
    if (r.err) bad('--pipetest 无法运行：' + r.err.message);
    else {
      const avail = val(r.out, 'PIPE_AVAILABLE');
      const sent = val(r.out, 'PIPE_SEND');
      const pull = val(r.out, 'PIPE_PULL');
      if (avail === 'False' && sent === 'False' && pull === 'null') ok('无管道时探测/推送/拉取都干净失败（' + pull + '），调用方回退文件桥');
      else bad('无管道时管道接口未按预期失败：AVAILABLE=' + avail + ' SEND=' + sent + ' PULL=' + pull);
      if (/PIPETEST_OK/.test(r.out)) ok('--pipetest 判定 PIPETEST_OK');
    }
  }

  
  log('[2/3] 有管道场景（.NET 假模组服务端）');
  if (!ensureMock()) { warn('假模组不可用，跳过"有管道"场景'); return finish(problems.length ? 1 : 0); }
  try { fs.unlinkSync(mockLog); } catch { }

  
  
  mockProc = spawn(mockExe, [PIPE, '24', mockLog], { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
  let mockOut = '';
  mockProc.stdout.on('data', d => { mockOut += d.toString(); });
  mockProc.stderr.on('data', d => { mockOut += d.toString(); });
  await sleep(900);

  const r2 = runExe(['--pipetest'], 60000);
  await sleep(400);
  const logText = fs.existsSync(mockLog) ? fs.readFileSync(mockLog, 'utf8') : mockOut;

  if (r2.err) {
    
    
    
    
    
    
    bad('有管道时 --pipetest 未返回（' + r2.err.code + '）—— 管道没通；' +
        '多半是服务端缓冲区 / 部分读问题（见 docs/实时桥-模组侧改造方案.md §6 第 8、9 条）');
  } else {
    const avail = val(r2.out, 'PIPE_AVAILABLE');
    const sent = val(r2.out, 'PIPE_SEND');
    const pull = val(r2.out, 'PIPE_PULL');
    if (avail === 'True') ok('探测到模组管道（PIPE_AVAILABLE=True）');
    else bad('未探测到管道（PIPE_AVAILABLE=' + avail + '），错误：' + (val(r2.out, 'PIPE_AVAIL_ERR') || ''));

    if (sent === 'True') ok('override 推送成功并收到模组 ACK');
    else warn('override 推送未收到 ACK（' + (val(r2.out, 'PIPE_SEND_ERR') || '') + '）—— 回退文件桥，不影响功能');

    if (pull && /bytes/.test(pull)) ok('state 拉取成功（' + pull + '）');
    else warn('state 拉取未成功（PIPE_PULL=' + pull + '）—— 模组侧未实现该方向时属预期');

    if (/PIPETEST_OK/.test(r2.out)) ok('--pipetest 判定 PIPETEST_OK');
  }

  
  if (/STATE replied/.test(logText)) ok('假模组成功回复过 state（协议 S 请求/长度头/JSON 正文打通）');
  else if (/connected/.test(logText)) warn('假模组收到连接但未走到 state 回复');
  else warn('假模组未收到任何连接');
  if (/OVERRIDE \d+ chars/.test(logText)) ok('假模组成功收到 override 正文（' + (logText.match(/OVERRIDE \d+ chars/) || [''])[0] + '）');
  if (/ACK sent/.test(logText)) ok('假模组已回 ACK');

  
  log('[3/3] 有管道时文件桥语义不变');
  {
    const r = runExe(['--oitest'], 60000);
    if (r.err) bad('有管道时 --oitest 未返回（' + r.err.code + '）—— 同上传导：管道没通会连带把文件桥自检挂住，属真问题');
    else if (/OITEST_OK/.test(r.out)) ok('有管道时 --oitest 仍输出 OITEST_OK');
    else if (/OITEST_WARN/.test(r.out)) warn('有管道时 --oitest 输出 OITEST_WARN');
    else bad('有管道时 --oitest 未输出预期结果：' + r.out.split('\n').filter(Boolean).slice(-3).join(' | '));
  }
  finish(problems.length ? 1 : 0);
}

function finish(code) {
  if (mockProc) { try { mockProc.kill(); } catch { } }
  restoreOverride();
  log('');
  log('────────────────────────────────────────────');
  if (problems.length === 0) log('结果：通过' + (warns.length ? '（' + warns.length + ' 项警告）' : ''));
  else { log('结果：' + problems.length + ' 项问题'); problems.forEach(p => log('  ✗ ' + p)); }
  warns.forEach(w => log('  ⚠ ' + w));
  process.exit(code);
}

main().catch(e => { bad('测试异常：' + e.message); finish(1); });
