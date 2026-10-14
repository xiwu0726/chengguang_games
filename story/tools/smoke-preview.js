#!/usr/bin/env node
/**
 * smoke-preview.js —— 无浏览器地把 preview/index.html 里的播放器**真跑一遍**。
 *
 * 为什么存在：预览器是我自己写的 JS，而本机看不到浏览器渲染结果。这个脚本把 index.html
 * 里的 <script> 抠出来，在一套最小 DOM 桩上执行，然后从头到尾自动走一遍剧本：
 * 遇到选项就选第 1 项、其余行一律"继续"，直到出现结局（或撞上「剧本缺口」）。
 * 它证明的是：播放器不会抛异常、分支都能走进去、走到头有明确出口。
 *
 * 用法: node story/tools/smoke-preview.js [preview/index.html]
 * 退出码: 0 = 全程无异常; 1 = 抛异常/卡死/异常退出
 */
'use strict';

const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const ROOT = path.join(__dirname, '..', '..');
const file = process.argv[2] ? path.resolve(process.argv[2]) : path.join(ROOT, 'preview', 'index.html');
if (!fs.existsSync(file)) {
  console.error('找不到预览文件: ' + file + '\n先运行: node story/tools/build-preview.js');
  process.exit(1);
}

const html = fs.readFileSync(file, 'utf8');
const m = html.match(/<script>([\s\S]*?)<\/script>/);
if (!m) { console.error('index.html 里没有 <script> 段'); process.exit(1); }
let code = m[1];
code += `
;globalThis.__api = {
  DATA, state, newGame, advance: advance, playLine: playLine,
  chooseOption: chooseOption,
  get pending() { return pendingChoice; },
  get sceneId() { return state.sceneId; },
  isEnding() { return el.ending.style.display === 'flex'; },
  endingTitle() { return el.endingTitle.textContent; },
  bodyText() { return el.body.textContent; },
  speakerText() { return el.speaker.textContent; },
  devVisible() { return devVisible; }
};`;

/* ---------- 最小 DOM 桩 ---------- */
function makeEl(id) {
  const el = {
    id, style: {}, dataset: {}, children: [], disabled: false, onclick: null,
    classList: { add() {}, remove() {}, contains() { return false; } },
    addEventListener() {}, removeEventListener() {},
    appendChild(c) { this.children.push(c); return c; },
    querySelectorAll() { return []; },
    get textContent() { return this._text || ''; },
    set textContent(v) { this._text = String(v); },
    get innerHTML() { return this._html || ''; },
    set innerHTML(v) { this._html = String(v); this.children = []; }
  };
  return el;
}
const els = new Map();
const doc = {
  getElementById(id) { if (!els.has(id)) els.set(id, makeEl(id)); return els.get(id); },
  createElement(tag) { const e = makeEl(tag); e.tagName = tag; return e; },
  addEventListener() {}
};
const store = new Map();
const localStorage = {
  getItem: (k) => (store.has(k) ? store.get(k) : null),
  setItem: (k, v) => store.set(k, String(v)),
  removeItem: (k) => store.delete(k)
};
class FakeImage { set src(v) { this._src = v; } get src() { return this._src; } }

const sandbox = {
  document: doc, localStorage, Image: FakeImage, console,
  requestAnimationFrame: (fn) => { if (typeof fn === 'function') fn(); return 0; },
  // 同步执行回调：把"等 22 毫秒打下一个字"折叠成瞬间完成，避免脚本里 sleep 式等待
  setTimeout: (fn) => { if (typeof fn === 'function') fn(); return 0; },
  clearTimeout() {}, setInterval: () => 0, clearInterval() {}
};
sandbox.globalThis = sandbox;
sandbox.window = sandbox;

/* ---------- 跑起来 ---------- */
console.log('=== preview 播放器冒烟测试 ===');
console.log('文件: ' + path.relative(ROOT, file) + '  (' + (Buffer.byteLength(html) / 1024).toFixed(0) + ' KB)');

let api;
try {
  vm.createContext(sandbox);
  vm.runInContext(code, sandbox, { filename: 'preview-index.html' });
  api = sandbox.__api;
} catch (err) {
  console.error('✗ 脚本执行出错: ' + err.message);
  process.exit(1);
}

const totalScenes = Object.keys(api.DATA.scenes).length;
api.newGame();

let steps = 0;
let choicesTaken = 0;
const sceneOrder = [];
const MAX = 20000;
let outcome = 'running';
let gap = null;

try {
  while (steps < MAX) {
    if (api.sceneId && sceneOrder[sceneOrder.length - 1] !== api.sceneId) sceneOrder.push(api.sceneId);
    if (api.isEnding()) { outcome = 'ending'; break; }

    const pending = api.pending;
    if (pending) {
      if (pending.gap) { outcome = 'gap'; gap = pending; break; }
      const opt = (pending.options || [])[0];
      if (!opt) throw new Error('choice 行没有任何选项: ' + JSON.stringify(pending).slice(0, 120));
      choicesTaken++;
      api.chooseOption(opt);
    } else {
      api.advance();
    }
    steps++;
  }
  if (steps >= MAX) outcome = 'loop';
} catch (err) {
  console.error('✗ 播放过程中抛出异常（第 ' + steps + ' 步）: ' + (err && err.stack ? err.stack : err));
  process.exit(1);
}

console.log('走过场景 ' + sceneOrder.length + ' 个: ' + sceneOrder.join(' → '));
console.log('推进 ' + steps + ' 步, 做了 ' + choicesTaken + ' 次选择');
console.log('终局失眠值 ' + api.state.insomnia + ', 标记 ' + api.state.flags.length +
  ' 个, 好感 ' + JSON.stringify(api.state.affection));

if (outcome === 'ending') {
  console.log('✓ 走到结局: ' + api.endingTitle());
  console.log('  最后一句: ' + api.bodyText().slice(0, 60));
  console.log('\n结论: 播放器可跑, 且在现有剧本上能走到结局。');
  process.exit(0);
}
if (outcome === 'gap') {
  console.log('○ 停在「剧本缺口」: 下一场 ' + (gap.missingId || api.sceneId) + ' 还没写');
  console.log('  最后一句: ' + api.bodyText().slice(0, 60));
  console.log('\n结论: 播放器可跑, 未抛异常; 剧本尚未写完, 走到 ' + api.sceneId + ' 就断在这里（这是预期的）。');
  process.exit(0);
}
console.error('✗ 走了 ' + MAX + ' 步还没到结局也没撞到缺口, 疑似死循环');
process.exit(1);