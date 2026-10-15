#!/usr/bin/env node
'use strict';
// 交叉验证：用同一批用例（cases.json）分别跑
//   A) player-template.html 里的 JS 结局匹配（flagsHas/topAffection/whenSatisfied/
//      ruleTitleFor/resolveEnding，逐字提取后在 node vm 中 eval）
//   B) C# EndingResolver（先跑 dotnet run --project tools/ending-resolver-tests，
//      产出 csharp-results.json）
// 逐用例比对 {scene, title, index}，不一致即报告并以非零码退出。
// 用法：node tools/ending-resolver-tests/cross-check.js
const fs = require('fs');
const path = require('path');
const vm = require('vm');

const projectDir = __dirname;
const repoRoot = path.resolve(projectDir, '..', '..');

// ---------- 1. 提取 JS 实现 ----------
const html = fs.readFileSync(
  path.join(repoRoot, 'story', 'tools', 'player-template.html'), 'utf8');
const start = html.indexOf('function flagsHas');
const end = html.indexOf('/* ---------- 主流程', start);
if (start < 0 || end < 0) {
  console.error('无法在 player-template.html 中定位结局匹配函数区段。');
  process.exit(2);
}
const fnSource = html.slice(start, end);

// ---------- 2. 载入共享数据 ----------
const realEndings = JSON.parse(fs.readFileSync(
  path.join(repoRoot, 'Assets', 'Resources', 'StoryConfig', 'endings.json'), 'utf8'));
const casesDoc = JSON.parse(fs.readFileSync(path.join(projectDir, 'cases.json'), 'utf8'));
const csPath = path.join(projectDir, 'csharp-results.json');
if (!fs.existsSync(csPath)) {
  console.error('缺少 csharp-results.json，请先运行 dotnet run --project tools/ending-resolver-tests');
  process.exit(2);
}
const csResults = JSON.parse(fs.readFileSync(csPath, 'utf8'));

// ---------- 3. 在 vm 沙箱里装好 JS 侧函数 ----------
const sandbox = { state: null, CAST_ORDER: [], ENDINGS: null };
const context = vm.createContext(sandbox);
vm.runInContext(fnSource, context, { filename: 'player-template.html#endings' });

// ---------- 4. 逐用例比对 ----------
let compared = 0;
let matched = 0;
const mismatches = [];
const skipped = [];

for (const c of casesDoc.cases) {
  if (c.csharpOnly || c.expectError) {
    skipped.push(c.name); // JS 侧宽松跳过 / 无加载期校验，不参与比对
    continue;
  }
  sandbox.ENDINGS = c.tableText ? JSON.parse(c.tableText) : realEndings;
  sandbox.CAST_ORDER = c.castOrder || casesDoc.castOrder;
  const s = c.state || {};
  sandbox.state = {
    flags: s.flags || [],
    insomnia: typeof s.insomnia === 'number' ? s.insomnia : 0,
    memoryShards: new Array(s.shards || 0).fill('x'),
    affection: s.affection || {},
  };
  const pick = vm.runInContext('resolveEnding()', context);
  const js = (pick === null || pick === undefined)
    ? null
    : { scene: pick.sceneId, title: String(pick.title === undefined ? '' : pick.title), index: pick.index };
  const cs = Object.prototype.hasOwnProperty.call(csResults, c.name) ? csResults[c.name] : undefined;
  if (cs === undefined) {
    mismatches.push(`${c.name}: C# 未记录该用例结果`);
    continue;
  }
  compared++;
  if (JSON.stringify(js) === JSON.stringify(cs)) {
    matched++;
  } else {
    mismatches.push(`${c.name}: JS=${JSON.stringify(js)} C#=${JSON.stringify(cs)}`);
  }
}

console.log(`交叉验证：比对 ${compared} 个用例，一致 ${matched}，不一致 ${mismatches.length}；` +
  `跳过（仅 C# 侧）${skipped.length} 个。`);
for (const m of mismatches) console.log('MISMATCH ' + m);
process.exit(mismatches.length ? 1 : 0);
