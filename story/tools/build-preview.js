#!/usr/bin/env node
/**
 * build-preview.js —— 把剧本 JSON 打成一个**双击就能玩**的单文件网页。
 *
 * 为什么存在：Unity 编辑器必须先由人工登录账号激活许可证才能跑，而那一步我代替不了。
 * 这个预览器读的是**和 Unity 运行时完全相同的** Assets/Resources/Story/**.json 与
 * Assets/Resources/Art/**  占位图，因此：剧本一写出来就能立刻试玩、走分支、看结局，
 * 不必等编辑器、也不必等美术。它不是游戏本体，是**剧本的可玩验收面**。
 *
 * 用法:
 *   node story/tools/build-preview.js            # 输出 preview/index.html
 *   node story/tools/build-preview.js --out x.html
 */
'use strict';

const fs = require('node:fs');
const path = require('node:path');

const ROOT = path.join(__dirname, '..', '..');
const STORY_DIR = path.join(ROOT, 'Assets', 'Resources', 'Story');
const ART_DIR = path.join(ROOT, 'Assets', 'Resources', 'Art');
const OUT_DEFAULT = path.join(ROOT, 'preview', 'index.html');
const ENDINGS_DEFAULT = path.join(ROOT, 'Assets', 'Resources', 'StoryConfig', 'endings.json');
const CAST_FILE = path.join(ROOT, 'story', 'cast.json');
const ACTS = ['act1', 'act2', 'act3'];
const LABEL_RE = /^l[0-9]{2,}$/;
const DEFAULT_CAST_ORDER = ['林晓', '艾莉丝', '雷恩', '小雅', '时间领主', '老周', '维拉'];

/**
 * §8.1 option.next / 顶层 next 的四种写法：
 *   ""              → none        继续本行下一行（顶层 next 为空则收尾）
 *   "1-2"           → scene       跳到该场景第 0 行
 *   "#l07"          → label       跳到本场景 label l07 所在行
 *   "1-2#l07"       → sceneLabel  跳到场景 1-2 的 label l07 所在行
 *   "@ending"       → ending      按结局规则表解析（只允许写在场景顶层 next）
 * 其余（如 "@@ending"）→ illegal
 */
function parseJump(next) {
  const raw = String(next == null ? '' : next).trim();
  if (raw === '') return { kind: 'none' };
  if (raw === '@ending') return { kind: 'ending' };
  if (raw.charAt(0) === '@') return { kind: 'illegal', raw };
  const hash = raw.indexOf('#');
  if (hash < 0) return { kind: 'scene', sceneId: raw };
  const sceneId = raw.slice(0, hash);
  const label = raw.slice(hash + 1);
  if (!LABEL_RE.test(label)) return { kind: 'illegal', raw };
  if (sceneId === '') return { kind: 'label', label };
  return { kind: 'sceneLabel', sceneId, label };
}

function parseArgs(argv) {
  const out = { out: null, standalone: false };
  for (let i = 0; i < argv.length; i++) {
    if (argv[i] === '--out') out.out = path.resolve(argv[++i]);
    else if (argv[i] === '--standalone') out.standalone = true;
  }
  if (!out.out) {
    out.out = out.standalone
      ? path.join(ROOT, 'preview', '时光之痕-试玩.html')
      : OUT_DEFAULT;
  }
  return out;
}

/** 收集剧本里用到的全部美术 id，按 Art 下的子目录归类。 */
function collectArtIds(scenes) {
  const wanted = { Bg: new Set(), Portrait: new Set(), Cg: new Set() };
  const add = (kind, id) => { if (id && typeof id === 'string' && /^[a-z0-9_]+$/.test(id)) wanted[kind].add(id); };
  for (const scene of Object.values(scenes)) {
    add('Bg', scene.background);
    for (const line of scene.lines || []) {
      add('Bg', line.background);
      add('Cg', line.background && String(line.background).startsWith('cg_') ? line.background : null);
      add('Portrait', line.sprite);
    }
  }
  return wanted;
}

/** 把 PNG 读成 data URI，做成一页真正自包含的试玩版。 */
function inlineArt(scenes) {
  const wanted = collectArtIds(scenes);
  const map = {};
  let bytes = 0;
  let missing = 0;
  for (const kind of Object.keys(wanted)) {
    for (const id of wanted[kind]) {
      const file = path.join(ART_DIR, kind, id + '.png');
      if (!fs.existsSync(file)) { missing++; continue; }
      const buf = fs.readFileSync(file);
      bytes += buf.length;
      map[kind + '/' + id] = 'data:image/png;base64,' + buf.toString('base64');
    }
  }
  return { map, bytes, missing };
}

function readScenes() {
  const scenes = {};
  const files = [];
  for (const act of ACTS) {
    const dir = path.join(STORY_DIR, act);
    if (!fs.existsSync(dir)) continue;
    for (const name of fs.readdirSync(dir).sort()) {
      if (!name.endsWith('.json')) continue;
      const full = path.join(dir, name);
      files.push(full);
      const base = name.replace(/\.json$/, '');
      try {
        scenes[base] = JSON.parse(fs.readFileSync(full, 'utf8'));
      } catch (err) {
        console.error('[坏 JSON] ' + path.relative(ROOT, full) + ' -> ' + err.message);
        process.exitCode = 1;
      }
    }
  }
  return { scenes, files };
}

/** 统计 CJK 汉字，口径与 count-story.js 一致。 */
function cjk(s) {
  const m = String(s == null ? '' : s).match(/[\u4e00-\u9fff]/g);
  return m ? m.length : 0;
}

function sceneChars(scene) {
  let n = cjk(scene.title) + cjk(scene.chapter);
  for (const line of scene.lines || []) {
    n += cjk(line.text);
    n += cjk(line.prompt);
    n += cjk(line.speaker);
    for (const opt of line.options || []) n += cjk(opt.text);
  }
  return n;
}

/** 场景内 label → 行索引（0 基）。§8.1：label 只写在行上，同场景唯一由 count-story.js 把关。 */
function sceneLabels(scene) {
  const map = new Map();
  if (!scene) return map;
  (scene.lines || []).forEach((line, i) => {
    if (line && typeof line.label === 'string' && line.label) map.set(line.label, i);
  });
  return map;
}

/** 读结局规则表 + cast 顺序（缺失时返回空表，播放器回落到老的硬编码结局画面）。 */
function readEndings() {
  let table = null;
  try {
    if (fs.existsSync(ENDINGS_DEFAULT)) {
      table = JSON.parse(fs.readFileSync(ENDINGS_DEFAULT, 'utf8'));
    }
  } catch (err) {
    console.log('[坏 JSON] ' + path.relative(ROOT, ENDINGS_DEFAULT) + ' -> ' + err.message);
    process.exitCode = 1;
    table = null;
  }
  let castOrder = DEFAULT_CAST_ORDER.slice();
  try {
    if (fs.existsSync(CAST_FILE)) {
      const cast = JSON.parse(fs.readFileSync(CAST_FILE, 'utf8'));
      const names = (cast.characters || []).map((c) => c && c.name).filter((n) => typeof n === 'string' && n);
      if (names.length) castOrder = names;
    }
  } catch (err) { /* cast.json 有问题时用内置顺序，不影响预览 */ }
  return { table, castOrder };
}

function audit(scenes, endings) {
  const problems = [];
  const check = (prefix, next, choiceIndex, sceneId) => {
    const jump = parseJump(next);
    if (jump.kind === 'none' || jump.kind === 'ending') return; // @ending 交给结局规则表，不算悬空引用
    if (jump.kind === 'illegal') {
      problems.push(prefix + 'next(' + next + ') 写法非法（可用 sceneId / "#lNN" / "sceneId#lNN"）');
      return;
    }
    if (jump.kind === 'label' || jump.kind === 'sceneLabel') {
      const target = jump.kind === 'label' ? sceneId : jump.sceneId;
      if (!scenes[target]) { problems.push(prefix + 'next(' + next + ') 指向不存在的场景 ' + target); return; }
      const targetLabels = sceneLabels(scenes[target]);
      if (!targetLabels.has(jump.label)) {
        problems.push(prefix + 'next(' + next + ') 在场景 ' + target + ' 找不到 label ' + jump.label);
        return;
      }
      if (choiceIndex !== undefined && target === sceneId && targetLabels.get(jump.label) <= choiceIndex) {
        problems.push(prefix + 'next(' + next + ') 回跳到第 ' + (targetLabels.get(jump.label) + 1) +
          ' 行（本行是第 ' + (choiceIndex + 1) + ' 行），只允许前向跳转');
      }
      return;
    }
    if (!scenes[jump.sceneId]) problems.push(prefix + 'next(' + next + ') 不存在');
  };
  for (const [id, scene] of Object.entries(scenes)) {
    const lines = scene.lines || [];
    lines.forEach((line, i) => {
      for (const opt of line.options || []) {
        if (opt.next) check(id + ' L' + (i + 1) + ' 选项 ', opt.next, i, id);
      }
    });
    if (scene.next) check(id + ' ', scene.next, undefined, id);
  }
  return problems;
}

/** 规则表引用的结局场景在不在（不属于「剧本缺口」，单独报）。 */
function missingEndingScenes(scenes, endings) {
  if (!endings || !Array.isArray(endings.rules)) return [];
  const missing = [];
  const want = new Set();
  for (const r of endings.rules) if (r && typeof r.scene === 'string' && r.scene) want.add(r.scene);
  if (typeof endings.fallback === 'string' && endings.fallback) want.add(endings.fallback);
  for (const id of [...want].sort()) if (!scenes[id]) missing.push(id);
  return missing;
}

function buildHtml(scenes, artData, endings) {
  const ids = Object.keys(scenes).sort((a, b) => {
    const pa = a.split('-');
    const pb = b.split('-');
    if (pa[0] !== pb[0]) return Number(pa[0]) - Number(pb[0]);
    const na = parseInt(pa[1], 10);
    const nb = parseInt(pb[1], 10);
    if (na !== nb) return na - nb;
    return a.localeCompare(b);
  });
  const totalChars = ids.reduce((sum, id) => sum + sceneChars(scenes[id]), 0);
  const startId = scenes['1-1'] ? '1-1' : ids[0];
  const data = {
    scenes,
    order: ids,
    start: startId,
    totalChars,
    builtAt: new Date().toISOString().replace('T', ' ').slice(0, 19),
  };
  const json = JSON.stringify(data).replace(/</g, '\\u003c');
  const artJson = JSON.stringify(artData || {}).replace(/</g, '\\u003c');
  const endingsJson = JSON.stringify(endings && endings.table ? endings.table : null).replace(/</g, '\\u003c');
  const castJson = JSON.stringify((endings && endings.castOrder) || DEFAULT_CAST_ORDER).replace(/</g, '\\u003c');

  return `<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>时光之痕 · 剧本试玩 (preview)</title>
<style>
  :root{
    --ink:#f2f3f5; --dim:#a9adb6; --accent:#7fb3ff; --panel:rgba(12,14,20,.82);
    --line:rgba(255,255,255,.14);
  }
  *{box-sizing:border-box}
  html,body{margin:0;height:100%;background:#07080b;color:var(--ink);
    font-family:"Microsoft YaHei","PingFang SC","Noto Sans CJK SC",system-ui,sans-serif;}
  #app{position:relative;width:100vw;height:100vh;overflow:hidden}
  /* 16:9 舞台: 居中留黑边 */
  #stage-wrap{position:absolute;inset:0;display:flex;align-items:center;justify-content:center}
  #stage{position:relative;width:100%;height:100%;max-width:calc(100vh * 16 / 9);max-height:calc(100vh * 9 / 16);
    aspect-ratio:16/9;background:#0b0d12;overflow:hidden;box-shadow:0 0 60px rgba(0,0,0,.8)}
  .layer{position:absolute;inset:0}
  #bg img,#cglayer img{width:100%;height:100%;object-fit:cover;display:block}
  #cglayer{display:none}
  #bg-fallback{position:absolute;inset:0;display:flex;align-items:center;justify-content:center;
    font-size:26px;color:rgba(255,255,255,.35)}
  #portrait{position:absolute;bottom:22%;left:50%;transform:translateX(-50%);height:74%;
    filter:drop-shadow(0 12px 30px rgba(0,0,0,.6));transition:opacity .18s}
  #portrait img{height:100%;display:block}
  #tint{background:linear-gradient(180deg,rgba(40,90,200,.06),rgba(20,50,140,.30));mix-blend-mode:screen;
    opacity:0;transition:opacity .6s}
  #grain{opacity:0;background-image:radial-gradient(rgba(255,255,255,.10) 1px,transparent 1px);
    background-size:3px 3px;transition:opacity .6s}
  #vignette{pointer-events:none;box-shadow:inset 0 0 200px rgba(0,0,0,.75)}
  #flash{background:#fff;opacity:0;pointer-events:none}
  #hud{top:0;left:0;right:0;height:56px;padding:12px 22px;display:flex;justify-content:space-between;
    align-items:center;font-size:15px;color:var(--dim);background:linear-gradient(180deg,rgba(0,0,0,.55),transparent)}
  #hud b{color:var(--ink);font-weight:600}
  #dialog{position:absolute;left:6%;right:6%;bottom:5%;min-height:26%;padding:22px 26px 26px;
    background:var(--panel);border:1px solid var(--line);border-radius:14px;backdrop-filter:blur(6px);display:none}
  #speaker{font-size:20px;color:var(--accent);margin-bottom:10px;min-height:26px;font-weight:600}
  #body{font-size:22px;line-height:1.85;white-space:pre-wrap;letter-spacing:.2px}
  #cursor{display:inline-block;color:var(--accent);animation:blink 1s steps(1) infinite}
  @keyframes blink{50%{opacity:0}}
  #next-hint{position:absolute;right:20px;bottom:12px;font-size:14px;color:var(--dim);opacity:0;transition:opacity .3s}
  #choices{position:absolute;inset:0;display:none;flex-direction:column;align-items:center;
    justify-content:center;gap:16px;background:rgba(4,5,8,.72)}
  #choice-prompt{font-size:24px;margin-bottom:8px;text-align:center;max-width:70%}
  .choice{min-width:46%;max-width:78%;padding:16px 22px;font-size:20px;color:var(--ink);
    background:rgba(30,34,44,.92);border:1px solid var(--line);border-radius:10px;cursor:pointer;
    text-align:left;transition:transform .1s,border-color .15s,background .15s}
  .choice:hover{border-color:var(--accent);background:rgba(40,48,64,.95);transform:translateY(-2px)}
  .choice .key{color:var(--accent);margin-right:10px;font-size:17px}
  #title,#ending{position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;
    justify-content:center;gap:18px;background:rgba(4,5,8,.9);text-align:center;padding:6%}
  #title h1{font-size:52px;margin:0;letter-spacing:8px}
  #title p{color:var(--dim);margin:0;line-height:1.8;font-size:16px}
  .btn{padding:13px 30px;font-size:18px;color:var(--ink);background:rgba(30,34,44,.9);
    border:1px solid var(--line);border-radius:10px;cursor:pointer}
  .btn:hover{border-color:var(--accent)}
  #ending h2{font-size:34px;margin:0 0 4px}
  #ending .body{white-space:pre-wrap;line-height:1.9;font-size:19px;max-width:76ch;color:#e6e8ee}
  #ending .meta{color:var(--dim);font-size:15px;white-space:pre-wrap}
  #dev{position:absolute;left:14px;bottom:14px;max-height:70%;overflow:auto;display:none;
    background:rgba(0,0,0,.86);border:1px solid var(--line);border-radius:10px;padding:12px 14px;
    font-family:Consolas,Menlo,monospace;font-size:13px;line-height:1.6;color:#cfe3ff;max-width:46%}
  #dev h4{margin:0 0 8px;font-size:13px;color:var(--accent);letter-spacing:1px}
  #dev a{color:#9fd0ff;text-decoration:none}
  #dev .row{color:#9aa4b4}
  .shake{animation:shake .5s linear}
  @keyframes shake{0%,100%{transform:translate(0,0)}20%{transform:translate(-14px,6px)}
    40%{transform:translate(12px,-8px)}60%{transform:translate(-9px,-5px)}80%{transform:translate(8px,7px)}}
  #toast{position:absolute;top:70px;left:50%;transform:translateX(-50%);padding:10px 18px;
    background:rgba(10,12,18,.9);border:1px solid var(--line);border-radius:8px;font-size:15px;
    opacity:0;transition:opacity .25s;pointer-events:none}
</style>
</head>
<body>
<div id="app">
  <div id="stage-wrap"><div id="stage">
    <div class="layer" id="bg"><div id="bg-fallback">背景</div></div>
    <div class="layer" id="cglayer"></div>
    <div class="layer" id="portrait"></div>
    <div class="layer" id="tint"></div>
    <div class="layer" id="grain"></div>
    <div class="layer" id="vignette"></div>
    <div class="layer" id="flash"></div>
    <div class="layer" id="hud"><div><b id="hud-chapter"></b>　<span id="hud-title"></span></div><div id="hud-progress"></div></div>
    <div id="dialog"><div id="speaker"></div><div id="body"></div><div id="next-hint">点击 / 空格 继续</div></div>
    <div id="choices"><div id="choice-prompt"></div><div id="choice-list" style="display:flex;flex-direction:column;gap:14px;align-items:center;width:100%"></div></div>
    <div id="toast"></div>
    <div id="dev"></div>

    <div id="title">
      <h1>时光之痕</h1>
      <p>剧本试玩预览 (preview)<br>
         <span id="title-meta"></span></p>
      <div style="display:flex;gap:14px;flex-wrap:wrap;justify-content:center">
        <button class="btn" id="btn-new">开始新游戏</button>
        <button class="btn" id="btn-continue">继续上次</button>
      </div>
      <p style="font-size:14px;opacity:.75">操作：空格/回车/点击 = 继续　1–4 = 选项　S = 存档　L = 读档<br>
        F1 = 调试面板（场景号、行号、失眠值、标记）</p>
    </div>

    <div id="ending" style="display:none">
      <h2 id="ending-title"></h2>
      <div class="body" id="ending-body"></div>
      <div class="meta" id="ending-meta"></div>
      <button class="btn" id="btn-again">回到标题</button>
    </div>
  </div></div>
</div>
<script>
const DATA = ${json};
const ART = '../Assets/Resources/Art/';
const ART_DATA = ${artJson};
const ENDINGS = ${endingsJson};
const CAST_ORDER = ${castJson};
const parseJump = ${parseJump.toString()};
const SAVE_KEY = 'shiguang_zhithen_preview_v1';

const $ = (id) => document.getElementById(id);
const el = {
  stage: $('stage'), bg: $('bg'), cg: $('cglayer'), portrait: $('portrait'),
  tint: $('tint'), grain: $('grain'), flash: $('flash'),
  hudChapter: $('hud-chapter'), hudTitle: $('hud-title'), hudProgress: $('hud-progress'),
  dialog: $('dialog'), speaker: $('speaker'), body: $('body'), nextHint: $('next-hint'),
  choices: $('choices'), choicePrompt: $('choice-prompt'), choiceList: $('choice-list'),
  title: $('title'), ending: $('ending'), endingTitle: $('ending-title'),
  endingBody: $('ending-body'), endingMeta: $('ending-meta'),
  toast: $('toast'), dev: $('dev')
};

const state = {
  sceneId: '', lineIndex: 0, insomnia: 0, flags: [], affection: {},
  playSeconds: 0, chapter: '', visited: [], history: [],
  endingId: '', endingRule: -1
};

let typing = false;      // 正在逐字显示
let typingDone = false;  // 一行是否已显示完
let typeTimer = null;
let pendingChoice = null; // 当前 choice 行
let sceneObj = null;
let devVisible = false;
let tickTimer = null;

/* ---------- 小工具 ---------- */
function toast(msg) {
  el.toast.textContent = msg;
  el.toast.style.opacity = '1';
  clearTimeout(el.toast._t);
  el.toast._t = setTimeout(() => (el.toast.style.opacity = '0'), 1100);
}
function toastErr(msg) {
  el.toast.textContent = msg;
  el.toast.style.opacity = '1';
  el.toast.style.borderColor = '#ff6b6b';
  clearTimeout(el.toast._t);
  el.toast._t = setTimeout(() => {
    el.toast.style.opacity = '0';
    el.toast.style.borderColor = 'rgba(255,255,255,.14)';
  }, 1600);
}
function artUrl(kind, id) { return ART_DATA[kind + '/' + id] || (ART + kind + '/' + id + '.png'); }
function setImage(node, src, fallbackText) {
  node.innerHTML = '';
  if (!src) { const d = document.createElement('div'); d.className = 'layer'; d.id = 'bg-fallback';
    d.textContent = fallbackText || ''; node.appendChild(d); return; }
  const img = new Image();
  img.onerror = () => { const d = document.createElement('div');
    d.style.cssText = 'position:absolute;inset:0;display:flex;align-items:center;justify-content:center;font-size:24px;color:rgba(255,255,255,.4)';
    d.textContent = fallbackText || ''; node.innerHTML = ''; node.appendChild(d); };
  img.src = src;
  node.appendChild(img);
}
function insomniaTier() {
  if (state.insomnia < 25) return 0;
  if (state.insomnia < 55) return 1;
  if (state.insomnia < 80) return 2;
  return 3;
}
function applyInsomniaVisual() {
  const t = insomniaTier();
  el.tint.style.opacity = [0, .35, .65, 1][t];
  el.grain.style.opacity = [0, .25, .45, .7][t];
}
function hidden() { return el.title.style.display !== 'none' || el.ending.style.display === 'flex'; }

/* ---------- 存档 ---------- */
function save(slot) {
  const key = SAVE_KEY + '_slot' + slot;
  localStorage.setItem(key, JSON.stringify(state));
  toast(slot === 0 ? '已自动存档' : '已存到存档 ' + slot);
}
function load(slot) {
  const raw = localStorage.getItem(SAVE_KEY + '_slot' + slot);
  if (!raw) return false;
  Object.assign(state, JSON.parse(raw));
  return true;
}
function hasAnySave() {
  for (let i = 0; i <= 3; i++) if (localStorage.getItem(SAVE_KEY + '_slot' + i)) return true;
  return false;
}

/* ---------- v2：label 前向跳 + 结局规则表 ---------- */
let endingRouteTaken = false; // 顶层 next "@ending" 只解析并跳转一次，规则表自指时不会打转

function labelIndexOf(scene, label) {
  const lines = (scene && scene.lines) || [];
  for (let i = 0; i < lines.length; i++) {
    const ln = lines[i];
    if (ln && ln.label === label) return i;
  }
  return -1;
}
/** 跳到某场景的 label 行（#lNN / sceneId#lNN 的落地）。 */
function gotoSceneLabel(id, label) {
  const s = DATA.scenes[id];
  if (!s) { toastErr('场景 ' + id + ' 不存在'); return; }
  gotoScene(id);
  const idx = labelIndexOf(s, label);
  if (idx > 0) { state.lineIndex = idx; playLine(); }
}
function flagsHas(f) { return state.flags.indexOf(f) >= 0; }
/** 好感最高的人物：并列时按 cast.json 的人物顺序取先者（与 Unity/check-endings.js 同一口径）。 */
function topAffection() {
  let best = CAST_ORDER[0] || '';
  let bestV = -Infinity;
  for (let i = 0; i < CAST_ORDER.length; i++) {
    const v = Number(state.affection[CAST_ORDER[i]] || 0);
    if (v > bestV) { bestV = v; best = CAST_ORDER[i]; }
  }
  return { who: best, value: bestV === -Infinity ? 0 : bestV };
}
/** §8.3 when 判定；数值 0 = 未给出该约束（与 Unity JsonUtility 口径一致）。 */
function whenSatisfied(w) {
  if (!w || typeof w !== 'object') return true;
  if (Array.isArray(w.flags)) { for (const f of w.flags) if (!flagsHas(f)) return false; }
  if (Array.isArray(w.notFlags)) { for (const f of w.notFlags) if (flagsHas(f)) return false; }
  if (typeof w.insomniaMin === 'number' && w.insomniaMin !== 0 && state.insomnia < w.insomniaMin) return false;
  if (typeof w.insomniaMax === 'number' && w.insomniaMax !== 0 && state.insomnia > w.insomniaMax) return false;
  const shards = Array.isArray(state.memoryShards) ? state.memoryShards.length : 0; // 预览暂无碎片状态
  if (typeof w.shardsMin === 'number' && w.shardsMin !== 0 && shards < w.shardsMin) return false;
  if (typeof w.flagCountMin === 'number' && w.flagCountMin !== 0 && state.flags.length < w.flagCountMin) return false;
  const top = topAffection();
  if (typeof w.topAffection === 'string' && w.topAffection !== '' && top.who !== w.topAffection) return false;
  if (typeof w.affectionMin === 'number' && w.affectionMin !== 0 && top.value < w.affectionMin) return false;
  return true;
}
/** 规则表里该结局场景的标题（F1 面板与结局画面都用它）。 */
function ruleTitleFor(sceneId) {
  if (!ENDINGS || !Array.isArray(ENDINGS.rules)) return '';
  for (const r of ENDINGS.rules) {
    if (r && r.scene === sceneId && typeof r.title === 'string' && r.title) return r.title;
  }
  return '';
}
/** 自上而下第一条全部满足的规则胜出；都不满足用 fallback。返回 null = 没有规则表（走老路径）。 */
function resolveEnding() {
  if (!ENDINGS || !Array.isArray(ENDINGS.rules)) return null;
  for (let i = 0; i < ENDINGS.rules.length; i++) {
    const r = ENDINGS.rules[i];
    if (r && whenSatisfied(r.when)) return { sceneId: r.scene, title: r.title || '', index: i };
  }
  const fb = typeof ENDINGS.fallback === 'string' ? ENDINGS.fallback : '';
  return fb ? { sceneId: fb, title: ruleTitleFor(fb), index: -1 } : null;
}

/* ---------- 主流程 ---------- */
function newGame() {
  endingRouteTaken = false;
  Object.assign(state, { sceneId: DATA.start, lineIndex: 0, insomnia: 0, flags: [], affection: {},
    playSeconds: 0, chapter: '', visited: [], history: [], endingId: '', endingRule: -1 });
  el.title.style.display = 'none';
  el.ending.style.display = 'none';
  el.cg.style.display = 'none';
  applyInsomniaVisual();
  gotoScene(DATA.start);
}
function gotoScene(id) {
  const s = DATA.scenes[id];
  if (!s) { toastErr('场景 ' + id + ' 不存在'); return; }
  sceneObj = s;
  state.sceneId = id;
  state.lineIndex = 0;
  state.chapter = s.chapter || '';
  if (!state.visited.includes(id)) state.visited.push(id);
  if (s.background) setImage(el.bg, artUrl('Bg', s.background), '背景 ' + s.background);
  el.hudChapter.textContent = s.chapter || '';
  el.hudTitle.textContent = '第 ' + id + ' 场 · ' + (s.title || '');
  el.cg.style.display = 'none';
  playLine();
}

function applyLineOverrides(line) {
  if (line.background) setImage(el.bg, artUrl('Bg', line.background), '背景 ' + line.background);
  if (line.bgm === 'stop') { /* 预览无音频 */ }
}

function playLine() {
  if (pendingChoice) return;
  const lines = sceneObj.lines || [];
  if (state.lineIndex >= lines.length) return endScene();
  const line = lines[state.lineIndex];
  applyLineOverrides(line);

  switch (line.type) {
    case 'choice': {
      showChoices(line);
      break;
    }
    case 'fx': {
      playFx(line.fx);
      // fx 行不显示文本，短暂停顿后自动前进
      typing = false; typingDone = true;
      el.dialog.style.display = 'none';
      setTimeout(() => { if (pendingChoice) return; state.lineIndex++; playLine(); }, line.fx === 'wait' ? 500 : 380);
      break;
    }
    case 'cg': {
      el.cg.style.display = 'block';
      setImage(el.cg, artUrl('Cg', line.background), 'CG ' + line.background);
      el.portrait.innerHTML = '';
      el.speaker.textContent = '';
      el.dialog.style.display = line.text ? 'block' : 'none';
      startTyping(line.text || '');
      break;
    }
    case 'dialogue': {
      el.cg.style.display = 'none';
      el.speaker.textContent = line.speaker || '';
      if (line.sprite) setImage(el.portrait, artUrl('Portrait', line.sprite), '');
      else if (line.sprite === '') el.portrait.innerHTML = '';
      el.dialog.style.display = 'block';
      startTyping(line.text || '');
      break;
    }
    default: { // narration
      el.cg.style.display = 'none';
      el.speaker.textContent = '';
      el.portrait.innerHTML = '';
      el.dialog.style.display = 'block';
      startTyping(line.text || '');
      break;
    }
  }
  renderDev();
}

function startTyping(text) {
  clearTimeout(typeTimer);
  el.nextHint.style.opacity = '0';
  if (!text) { el.body.textContent = ''; typing = false; typingDone = true; el.nextHint.style.opacity = '1'; return; }
  typing = true; typingDone = false;
  el.body.textContent = '';
  let i = 0;
  const step = () => {
    if (!typing) return;
    el.body.textContent = text.slice(0, ++i);
    if (i >= text.length) { typing = false; typingDone = true; el.nextHint.style.opacity = '1'; return; }
    typeTimer = setTimeout(step, 22);
  };
  step();
}
function completeTyping() {
  if (!typing) return;
  typing = false;
  clearTimeout(typeTimer);
  const line = (sceneObj.lines || [])[state.lineIndex];
  el.body.textContent = (line && line.text) || '';
  typingDone = true;
  el.nextHint.style.opacity = '1';
}

function endScene() {
  const nxt = sceneObj.next || '';
  const jump = parseJump(nxt);
  if (jump.kind === 'ending') {
    // 顶层 next "@ending"：按结局规则表解析目标场景；只跳一次，规则表自指时直接收尾
    const pick = resolveEnding();
    if (!pick || endingRouteTaken) { showEnding(pick); return; }
    if (!DATA.scenes[pick.sceneId]) {
      toastErr('结局场景 ' + pick.sceneId + ' 还没写（剧本缺口）');
      showGapNotice(pick.sceneId);
      return;
    }
    endingRouteTaken = true;
    state.endingId = pick.sceneId;
    state.endingRule = typeof pick.index === 'number' ? pick.index : -1;
    save(0);
    gotoScene(pick.sceneId);
    return;
  }
  if (jump.kind === 'label') { gotoSceneLabel(state.sceneId, jump.label); return; }
  if (jump.kind === 'sceneLabel') { gotoSceneLabel(jump.sceneId, jump.label); return; }
  if (jump.kind === 'illegal') { toastErr('场景 next(' + nxt + ') 写法非法'); showEnding(null); return; }
  if (jump.kind === 'scene') {
    if (!DATA.scenes[jump.sceneId]) { toastErr('下一场景 ' + jump.sceneId + ' 还没写（剧本缺口）'); showGapNotice(jump.sceneId); return; }
    save(0);
    gotoScene(jump.sceneId);
    return;
  }
  showEnding(null);
}

/** 剧本写到一半时，next 指向的场景可能还不存在 —— 明确说出来，不要假装通关。 */
function showGapNotice(missingId) {
  el.dialog.style.display = 'block';
  el.speaker.textContent = '（剧本缺口）';
  el.body.textContent = '本场景已结束，下一场 ' + missingId + ' 还没有写。\\n\\n这是一份边写边试玩的预览，不是成品。';
  el.nextHint.style.opacity = '0';
  typing = false; typingDone = true;
  pendingChoice = { gap: true, missingId: missingId };
}

function playFx(fx) {
  switch (fx) {
    case 'shake': el.stage.classList.add('shake'); setTimeout(() => el.stage.classList.remove('shake'), 520); break;
    case 'flash':
      el.flash.style.transition = 'none'; el.flash.style.opacity = '.9';
      requestAnimationFrame(() => { el.flash.style.transition = 'opacity .45s'; el.flash.style.opacity = '0'; });
      break;
    case 'insomnia':
      if (state.insomnia < 25) state.insomnia = 25;
      applyInsomniaVisual(); renderDev();
      break;
    case 'timeline':
      el.tint.style.opacity = '.8';
      setTimeout(() => applyInsomniaVisual(), 420);
      break;
    case 'wait': break;
  }
}

function showChoices(line) {
  pendingChoice = line;
  el.dialog.style.display = 'none';
  el.choices.style.display = 'flex';
  el.choicePrompt.textContent = line.prompt || '';
  el.choiceList.innerHTML = '';
  (line.options || []).forEach((opt, i) => {
    const b = document.createElement('button');
    b.className = 'choice';
    b.innerHTML = '<span class="key">' + (i + 1) + '</span>' + opt.text;
    b.onclick = () => chooseOption(opt);
    el.choiceList.appendChild(b);
  });
}

function chooseOption(opt) {
  el.choices.style.display = 'none';
  pendingChoice = null;
  if (opt.affection && opt.affectionWho) {
    state.affection[opt.affectionWho] = (state.affection[opt.affectionWho] || 0) + opt.affection;
  }
  if (opt.insomnia) {
    state.insomnia = Math.max(0, Math.min(100, state.insomnia + opt.insomnia));
    applyInsomniaVisual();
  }
  if (opt.flag && !state.flags.includes(opt.flag)) state.flags.push(opt.flag);
  if (opt.next) {
    const jump = parseJump(opt.next);
    if (jump.kind === 'label') { save(0); gotoSceneLabel(state.sceneId, jump.label); return; }
    if (jump.kind === 'sceneLabel') { save(0); gotoSceneLabel(jump.sceneId, jump.label); return; }
    if (jump.kind === 'ending') { toastErr('选项 next 不能用 @ending（只有场景顶层 next 可以写 @ending）'); return; }
    if (jump.kind === 'illegal') { toastErr('分支 ' + opt.next + ' 写法非法'); showGapNotice(opt.next); return; }
    if (!DATA.scenes[jump.sceneId]) { toastErr('分支 ' + jump.sceneId + ' 还没写（剧本缺口）'); showGapNotice(jump.sceneId); return; }
    save(0);
    gotoScene(jump.sceneId);
    return;
  }
  state.lineIndex++;
  playLine();
}

function advance() {
  if (hidden()) return;
  if (pendingChoice) return;
  if (typing) { completeTyping(); return; }
  state.lineIndex++;
  playLine();
}

function showEnding(pick) {
  let title, body;
  const has = (f) => state.flags.includes(f);
  if (has('ending_miracle')) { title = '结局 · 奇迹';
    body = '所有碎片在同一瞬间安静下来。\\n你没有回到过去, 也没有抹掉任何一道伤痕——你只是终于愿意带着它们继续往前走。\\n时间没有原谅谁, 但它停止了继续索要。'; }
  else if (state.insomnia >= 80) { title = '结局 · 失眠';
    body = '你已经分不清自己是在醒来还是在睡去。\\n那条链还在向前延伸, 只是再没有人替它念出下一个词。\\n窗外的天亮了, 而你不知道这是第几个天亮。'; }
  else if (has('ending_hero')) { title = '结局 · 英雄';
    body = '你把最后一块碎片按回了它该在的位置。\\n代价被记在了别人看不见的地方, 但这一次, 所有人都活着走出了那栋楼。'; }
  else if (has('ending_tragic')) { title = '结局 · 诀别';
    body = '你救下了能救的一切, 除了那个一直站在你身边的人。\\n时间合拢的速度比你说出那句话更快。'; }
  else if (has('ending_open')) { title = '结局 · 未完成';
    body = '链断了。\\n没有人知道断口那边是什么, 也没有人再提起那个名字。\\n故事在这里停了下来, 像一句被刻意咽回去的话。'; }
  else { title = '结局 · 归途';
    body = '碎片的答案不在过去, 而在你决定怎么过完接下来的每一天。\\n你合上了那本借来的书, 把它放回原来的位置。'; }

  el.endingTitle.textContent = title;
  el.endingBody.textContent = body;
  const aff = Object.entries(state.affection)
    .map(([who, v]) => who + ' ' + (v > 0 ? '+' : '') + v).join('　');
  el.endingMeta.textContent = '—— 本次游玩 ——\\n场景 ' + state.visited.length + ' 个 / 已写 ' + DATA.order.length +
    ' 个　失眠值 ' + state.insomnia + '/100 (档 ' + insomniaTier() + ')' +
    (aff ? '\\n好感 ' + aff : '') + '\\n标记 ' + state.flags.length + ' 个';
  el.dialog.style.display = 'none';
  el.ending.style.display = 'flex';
}

function renderDev() {
  if (!devVisible) return;
  const aff = Object.entries(state.affection).map(([k, v]) => k + '=' + v).join(' ');
  el.dev.innerHTML = '<h4>调试 (F1 关闭)</h4>' +
    '<div class="row">场景 ' + state.sceneId + '　行 ' + (state.lineIndex + 1) + '/' + ((sceneObj && sceneObj.lines || []).length) + '</div>' +
    '<div class="row">失眠 ' + state.insomnia + ' (档 ' + insomniaTier() + ')　标记 ' + state.flags.length + ' 个</div>' +
    '<div class="row">好感 ' + (aff || '—') + '</div>' +
    '<div class="row">已访问 ' + state.visited.length + ' / ' + DATA.order.length + ' 场</div>' +
    '<div class="row">全剧 ' + DATA.totalChars + ' 字　构建 ' + DATA.builtAt + '</div>' +
    '<div class="row" style="margin-top:6px;color:#8fa4bd">场景列表（点开跳转）</div>' +
    DATA.order.map((id) => '<a href="#" data-jump="' + id + '" style="margin-right:8px">' + id + '</a>').join('');
  el.dev.querySelectorAll('[data-jump]').forEach((a) => {
    a.onclick = (ev) => {
      ev.preventDefault();
      state.lineIndex = 0; save(0); gotoScene(a.dataset.jump);
    };
  });
}

/* ---------- 事件 ---------- */
document.addEventListener('keydown', (ev) => {
  if (ev.key === 'F1') { devVisible = !devVisible; el.dev.style.display = devVisible ? 'block' : 'none';
    if (devVisible) renderDev(); ev.preventDefault(); return; }
  if (ev.key === 's' || ev.key === 'S') { save(1); return; }
  if (ev.key === 'l' || ev.key === 'L') { if (load(1)) { el.title.style.display = 'none'; el.ending.style.display = 'none'; gotoScene(state.sceneId); toast('已读档'); } else toastErr('没有存档'); return; }
  if (pendingChoice && !pendingChoice.gap && /^[1-4]$/.test(ev.key)) {
    const opt = (pendingChoice.options || [])[Number(ev.key) - 1];
    if (opt) chooseOption(opt);
    return;
  }
  if (ev.key === ' ' || ev.key === 'Enter') { ev.preventDefault(); advance(); }
});
el.stage.addEventListener('click', (ev) => { if (ev.target.classList.contains('choice')) return; advance(); });
$('btn-new').onclick = newGame;
$('btn-continue').onclick = () => {
  if (!load(0)) { toastErr('还没有自动存档'); return; }
  el.title.style.display = 'none';
  el.ending.style.display = 'none';
  applyInsomniaVisual();
  gotoScene(state.sceneId);
};
$('btn-again').onclick = () => { el.ending.style.display = 'none'; el.title.style.display = 'flex'; };

/* ---------- 启动 ---------- */
(function boot() {
  const acts = DATA.order.reduce((m, id) => { const a = id.split('-')[0]; m[a] = (m[a] || 0) + 1; return m; }, {});
  $('title-meta').textContent = '已写 ' + DATA.order.length + ' 场（第一幕 ' + (acts['1'] || 0) +
    ' / 第二幕 ' + (acts['2'] || 0) + ' / 第三幕 ' + (acts['3'] || 0) + ' 场）· ' + DATA.totalChars + ' 字 / 目标 100000';
  $('btn-continue').disabled = !hasAnySave();
  $('btn-continue').style.opacity = hasAnySave() ? '1' : '.5';
  tickTimer = setInterval(() => { state.playSeconds += 1; }, 1000);
})();
</script>
</body>
</html>
`;
}

function main() {
  const args = parseArgs(process.argv.slice(2));
  const { scenes } = readScenes();
  const ids = Object.keys(scenes);
  if (ids.length === 0) { console.error('没有找到任何剧本（' + STORY_DIR + '）'); process.exit(1); }

  const problems = audit(scenes);
  const totalChars = ids.reduce((sum, id) => sum + sceneChars(scenes[id]), 0);
  let artData = {};
  if (args.standalone) {
    const inlined = inlineArt(scenes);
    artData = inlined.map;
    console.log('内嵌美术 ' + Object.keys(artData).length + ' 张 (' +
      (inlined.bytes / 1024 / 1024).toFixed(2) + ' MB 原始)') +
      (inlined.missing ? '　缺图 ' + inlined.missing + ' 张（运行时显示占位文字）' : '');
  }
  const html = buildHtml(scenes, artData);
  fs.mkdirSync(path.dirname(args.out), { recursive: true });
  fs.writeFileSync(args.out, html, 'utf8');

  console.log('预览已生成: ' + path.relative(ROOT, args.out) +
    '  (' + (Buffer.byteLength(html) / 1024 / 1024).toFixed(2) + ' MB)');
  console.log('场景 ' + ids.length + ' 个 / ' + totalChars + ' 字 / 目标 100000 (' +
    (totalChars / 100000 * 100).toFixed(1) + '%)');
  if (problems.length) {
    console.log('\n剧本缺口 ' + problems.length + ' 处（预览里会显示「剧本缺口」而不是假装通关）:');
    for (const p of problems) console.log('  - ' + p);
  }
  console.log(args.standalone
    ? '\n这是自包含单文件：美术已内嵌，拷到任何机器上双击就能玩（不需要 Unity，也不需要仓库其它文件）。'
    : '\n双击打开该文件即可试玩（美术走 Assets/Resources/Art 的相对路径，需保留仓库目录结构）。');
}

main();