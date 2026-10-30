'use strict';




























const fs = require('fs');
const path = require('path');

const root = path.resolve(__dirname, '..');
const SRC = path.join(root, 'src');
const ARCHIVE = path.join(SRC, '_split-source', 'SuzerainGUI.mono.cs');






const CHUNKS = [
  
  
  [1, 183, 'SuzerainGUI.cs', /^using System;$/],
  
  [184, 413, 'SuzerainGUI.Core.cs', /^\s*\/\/ =+ 核心逻辑/],
  
  [414, 575, 'SuzerainGUI.Bridge.Hb.cs', /^\s*$/],
  
  [576, 729, 'SuzerainGUI.Bridge.Oi.cs', /^\s*$/],
  
  [730, 978, 'SuzerainGUI.MainForm.cs', /^\s*class MainForm\s*:\s*Form\b/],
  
  [979, 1109, 'SuzerainGUI.MainForm.Pages.Feature.cs', /^\s*void PopulateNav\(\)/],
  
  [1110, 1301, 'SuzerainGUI.MainForm.Commands.cs', /^\s*\/\/ =+ 命令面板/],
  
  [1302, 2017, 'SuzerainGUI.MainForm.Pages.Data.cs', /^\s*Control BuildQuickPanel\(\)/],
  
  [2018, 2060, 'SuzerainGUI.MainForm.Common.cs', /^\s*bool SyncChangesToFile\(string path\)/],
  [2061, 2095, 'SuzerainGUI.MainForm.Common.cs', /^\s*\/\/ -+ 变量名翻译/],
  
  [2061, 2060, 'SuzerainGUI.MainForm.ModPages.Contract.cs', null],
  
  [2096, 3154, 'SuzerainGUI.MainForm.ModPages.cs', /^\s*Control BuildBasePanel\(\)/],
  
  [3155, 3239, 'SuzerainGUI.MainForm.Pages.Other.cs', /^\s*Control BuildOtherPanel\(TabControl tabs\)/],
  
  [3240, 3538, 'SuzerainGUI.MainForm.Common.cs', /^\s*\/\/ -+ Win11 属性表风格顶部标签绘制/],
];




const ENTRY_EPILOGUE = '}';


const SPLIT_USINGS = [
  'using System;',
  'using System.Collections.Generic;',
  'using System.Drawing;',
  'using System.Globalization;',
  'using System.IO;',
  'using System.Text;',
  'using System.Text.RegularExpressions;',
  'using System.Windows.Forms;',
];


const NAMESPACE_HEADER = ['', 'namespace SuzerainTrainer', '{'];


const EXTRA_USING = {
  'SuzerainGUI.MainForm.ModPages.cs': ['using System.Threading;'],
};





const FILE_KIND = {
  'SuzerainGUI.cs': 'asis',
  'SuzerainGUI.Core.cs': 'plain',
  'SuzerainGUI.Bridge.Hb.cs': 'plain',
  'SuzerainGUI.Bridge.Oi.cs': 'plain',
  'SuzerainGUI.MainForm.cs': 'asis',
  'SuzerainGUI.MainForm.Pages.Feature.cs': 'partial',
  'SuzerainGUI.MainForm.Commands.cs': 'partial',
  'SuzerainGUI.MainForm.Pages.Data.cs': 'partial',
  'SuzerainGUI.MainForm.Common.cs': 'partial',
  'SuzerainGUI.MainForm.ModPages.Contract.cs': 'partial',
  'SuzerainGUI.MainForm.ModPages.cs': 'partial',
  'SuzerainGUI.MainForm.Pages.Other.cs': 'partial',
};

function readMono(p) {
  return fs.readFileSync(p, 'utf8').replace(/^\uFEFF/, '').replace(/\r\n/g, '\n');
}
function escapeRe(s) { return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); }
function realChunks() { return CHUNKS.filter(([a, b]) => b >= a); }



const CONTRACT_SEED = [
  '    /// <summary>模组联动页的注册契约：新增一个模组页 = 实现本接口 + 在导航树注册表里登记。</summary>',
  '    internal interface IModPage',
  '    {',
  '        /// <summary>导航树分组标题。</summary>',
  '        string GroupTitle { get; }',
  '        /// <summary>分组内该页的标题。</summary>',
  '        string PageTitle { get; }',
  '        /// <summary>构建页面控件（只调用一次）。</summary>',
  '        Control Build();',
  '        /// <summary>检测模组是否已安装；false 时该分组不出现在导航树里。</summary>',
  '        bool Installed();',
  '    }',
];


function separatorsFor(file) {
  const real = realChunks();
  let n = 0;
  for (let i = 1; i < real.length; i++) if (real[i][2] === file && real[i - 1][2] === file) n++;
  return n;
}


function generate() {
  if (!fs.existsSync(ARCHIVE)) { console.error('缺少单文件源：' + ARCHIVE); process.exit(2); }
  const skipped = [];          
  const lines = readMono(ARCHIVE).split('\n');
  const real = realChunks();
  let problems = 0;
  let covered = 0;
  const out = new Map();          
  for (const [, , file] of CHUNKS) if (!out.has(file)) out.set(file, []);   
  let prev = null;
  for (const [a, b, file, anchor] of real) {
    const seg = lines.slice(a - 1, b);
    covered += seg.length;
    if (anchor && !anchor.test(seg[0])) {
      console.error('✗ 锚点不符 [' + a + '-' + b + '] → ' + file + '\n   期望: ' + anchor + '\n   实际: ' + JSON.stringify(seg[0]));
      problems++;
    }
    if (!out.has(file)) out.set(file, []);
    else if (prev && prev[2] === file) out.get(file).push('');   
    out.get(file).push(...seg);
    prev = [a, b, file];
  }
  
  const expected = lines.length - 3;
  if (covered !== expected) { console.error('✗ 未覆盖全文：' + covered + '/' + expected); problems++; }
  if (problems) process.exit(1);

  for (const [file, seg] of out) {
    let body = seg;
    if (file === 'SuzerainGUI.cs') {
      body = seg.concat([ENTRY_EPILOGUE]);          
    } else {
      const extra = (EXTRA_USING[file] || []);
      const kind = FILE_KIND[file] || 'partial';
      
      const content = (file === 'SuzerainGUI.MainForm.ModPages.Contract.cs' && seg.length === 0) ? CONTRACT_SEED : seg;
      let head;
      if (kind === 'partial') {
        head = ['    /// <summary>MainForm 分部实现（拆分自 SuzerainGUI.cs，见 tools/dev-split-gui.js 拆分表）。</summary>',
                '    partial class MainForm', '    {'].concat(
          content.map(l => (l.length ? '    ' + l : l)),
          ['    }']);
      } else {
        
        
        head = content.map((l, i) => (i === 0 ? l.replace(/^(\s*)class\s+MainForm/, '$1partial class MainForm') : l));
        if (file === 'SuzerainGUI.MainForm.cs') head = head.concat(['    }']);
      }
      
      body = SPLIT_USINGS.concat(extra).concat(NAMESPACE_HEADER).concat(head).concat([ENTRY_EPILOGUE]);
    }
    
    
    const text = '\uFEFF' + body.join('\r\n') + '\r\n';
    const target = path.join(SRC, file);
    if (fs.existsSync(target)) {
      if (fs.readFileSync(target, 'utf8') === text) {
        
        
        console.log('  未变 ' + file + '（内容已一致，跳过写入以保持时间戳）');
        continue;
      }
      if (!FORCE) {
        console.log('  跳过 ' + file + '（内容已与生成结果分叉，未覆盖；确要覆盖加 --force）');
        skipped.push(file);
        continue;
      }
      console.log('  覆盖 ' + file + '（--force：本次会丢掉该文件上的人工改动）');
    }
    fs.writeFileSync(target, text, 'utf8');
    console.log('  写入 ' + file + '（' + body.length + ' 行）');
  }

  if (skipped.length) {
    console.log('');
    console.log('⚠ 有 ' + skipped.length + ' 个文件因为「已分叉」被跳过，未覆盖：');
    for (const f of skipped) console.log('    ' + f);
    console.log('  这些文件承载着拆分之后的开发成果，重跑生成器不会自动带上它们。');
    console.log('  确需覆盖，请先把改动回写单文件源，或显式用 --force 承担丢失风险。');
  }
}


function verify() {
  const monoPath = process.argv[3] || ARCHIVE;
  if (!fs.existsSync(monoPath)) { console.error('缺少单文件源：' + monoPath); process.exit(2); }
  const origLines = readMono(monoPath).split('\n');         
  const real = realChunks();
  const prefixStr = SPLIT_USINGS.concat(NAMESPACE_HEADER).join('\n');

  
  const contentByFile = new Map();
  for (const file of new Set(CHUNKS.map(c => c[2]))) {
    const ranges = real.filter(c => c[2] === file);
    let content = readMono(path.join(SRC, file));
    if (content.endsWith('\n')) content = content.slice(0, -1);
    if (file === 'SuzerainGUI.cs') {
      if (!content.endsWith('\n' + ENTRY_EPILOGUE)) {
        console.error('✗ ' + file + ' 末尾缺少生成器补的 namespace 收尾 ' + ENTRY_EPILOGUE);
        process.exit(1);
      }
      content = content.slice(0, content.length - (ENTRY_EPILOGUE.length + 1));
    } else {
      
      const fullPrefix = SPLIT_USINGS.concat(EXTRA_USING[file] || []).concat(NAMESPACE_HEADER).join('\n');
      if (content.indexOf(fullPrefix) !== 0) { console.error('✗ ' + file + ' 头部与拆分器记录不一致'); process.exit(1); }
      content = content.slice(fullPrefix.length).replace(/^\n/, '');
      
      if (!content.endsWith('\n' + ENTRY_EPILOGUE)) {
        console.error('✗ ' + file + ' 末尾缺少生成器补的 namespace 收尾 ' + ENTRY_EPILOGUE);
        process.exit(1);
      }
      content = content.slice(0, content.length - (ENTRY_EPILOGUE.length + 1));
      if ((FILE_KIND[file] || 'partial') === 'partial') {
        
        const l = content.split('\n');
        if (!/partial class MainForm/.test(l[1]) || l[2].trim() !== '{' || l[l.length - 1] !== '    }') {
          console.error('✗ ' + file + ' 未找到拆分器补的 partial class 包装'); process.exit(1);
        }
        content = l.slice(3, l.length - 1).map(x => (x.startsWith('    ') ? x.slice(4) : x)).join('\n');
      } else {
        content = content.replace(/^([ \t]*)partial class MainForm/, '$1class MainForm');
        
        if (file === 'SuzerainGUI.MainForm.cs' && content.endsWith('\n    }')) content = content.slice(0, -6);
      }
    }
    const lines = content.split('\n');
    
    const segLines = (file === 'SuzerainGUI.MainForm.ModPages.Contract.cs' && ranges.length === 0)
      ? CONTRACT_SEED.length
      : ranges.reduce((s, [a, b]) => s + (b - a + 1), 0);
    const want = segLines + separatorsFor(file);
    if (lines.length !== want) { console.error('✗ ' + file + ' 行数不符：' + lines.length + ' ≠ ' + want); process.exit(1); }
    contentByFile.set(file, lines);
  }

  
  const cursor = new Map();
  const out = [];
  for (let i = 0; i < real.length; i++) {
    const [a, b, file] = real[i];
    const prev = i > 0 ? real[i - 1] : null;
    if (prev && a !== prev[1] + 1) { console.error('✗ 区间不连续：' + prev[1] + ' → ' + a); process.exit(1); }
    const lines = contentByFile.get(file);
    let off = cursor.get(file) || 0;
    if (prev && prev[2] === file && prev[1] + 1 === a) off += 1;   
    const n = b - a + 1;
    if (off + n > lines.length) { console.error('✗ ' + file + ' 内容不足（区间 ' + a + '-' + b + '，off=' + off + '）'); process.exit(1); }
    for (let j = 0; j < n; j++) out.push(lines[off + j]);
    cursor.set(file, off + n);
  }
  
  const rebuilt = out.concat(origLines.slice(origLines.length - 3));

  
  if (rebuilt.length === origLines.length && rebuilt.every((v, i) => v === origLines[i])) {
    console.log('✓ 反向校验通过：' + real.length + ' 段拼接结果与原单文件逐行一致（' + rebuilt.length + ' 行）');
    process.exit(0);
  }
  let shown = 0;
  for (let i = 0; i < Math.max(rebuilt.length, origLines.length) && shown < 8; i++) {
    if (rebuilt[i] !== origLines[i]) {
      console.error('✗ 第 ' + (i + 1) + ' 行不同\n   期望: ' + JSON.stringify(origLines[i]) + '\n   实际: ' + JSON.stringify(rebuilt[i]));
      shown++;
    }
  }
  console.error('✗ 反向校验失败：行数 ' + rebuilt.length + ' vs ' + origLines.length);
  process.exit(1);
}


const ARGV = process.argv.slice(2);
const FORCE = ARGV.includes('--force');

function refuseGenerate() {
  console.error('');
  console.error('✗ 拒绝运行：generate 模式会覆盖 src/SuzerainGUI*.cs，而本工具已与现行 src 脱节。');
  console.error('');
  console.error('  2026-10-02 实测（副本重放后与真实 src 逐字节比对）：');
  console.error('    · 5 个产物与生成结果逐字节相同 → 拆分逻辑本身仍自洽');
  console.error('    · 7 个产物已被人工编辑过，重跑会丢掉这些改动：');
  console.error('        SuzerainGUI.cs / SuzerainGUI.Bridge.Hb.cs / SuzerainGUI.Bridge.Oi.cs');
  console.error('        SuzerainGUI.MainForm.cs / SuzerainGUI.MainForm.Pages.Feature.cs');
  console.error('        SuzerainGUI.MainForm.ModPages.cs / SuzerainGUI.MainForm.ModPages.Contract.cs');
  console.error('');
  console.error('  确实要重跑：');
  console.error('      node tools/dev-split-gui.js --yes            # 只写「未分叉」的文件，分叉的自动跳过');
  console.error('      node tools/dev-split-gui.js --yes --force     # 连分叉的一起覆盖（会丢改动！）');
  console.error('');
  console.error('  只做校验、不写任何文件：');
  console.error('      node tools/dev-split-gui.js --verify');
  console.error('');
  process.exit(3);
}

if (ARGV.includes('--verify')) verify();
else if (ARGV.includes('--yes')) generate();
else refuseGenerate();
