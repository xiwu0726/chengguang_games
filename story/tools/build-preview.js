#!/usr/bin/env node
/**
 * build-preview.js —— 把剧本 JSON 打成一个**双击就能玩**的单文件网页。
 *
 * 为什么存在：Unity 编辑器必须先由人工登录账号激活许可证才能跑，而那一步我代替不了。
 * 这个预览器读的是**和 Unity 运行时完全相同的** Assets/Resources/Story/**.json 与
 * Assets/Resources/Art/**  占位图，因此：剧本一写出来就能立刻试玩、走分支、看结局，
 * 不必等编辑器、也不必等美术。它不是游戏本体，是**剧本的可玩验收面**。
 *
 * 页面本体不在本文件里：CSS 与 HTML 在 story/tools/player-template.html，
 * 本脚本只负责把剧本/美术/结局规则填进它的 5 个占位符。改界面请改模板。
 *
 * 用法:
 *   node story/tools/build-preview.js            # 输出 preview/index.html
 *   node story/tools/build-preview.js --out x.html
 *   node story/tools/build-preview.js --standalone
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

  // 页面本体是独立模板（真 HTML 文件才好维护）：先把数据填进占位符，再整体返回。
  const templatePath = path.join(__dirname, 'player-template.html');
  let tpl;
  try {
    tpl = fs.readFileSync(templatePath, 'utf8');
  } catch (err) {
    console.error('读不到播放器模板 ' + path.relative(ROOT, templatePath) + ' -> ' + err.message);
    process.exit(1);
  }
  /** 占位符缺一个都不许静默通过：模板被改坏时宁可报错，也别产出一个空白页面。 */
  const fill = (token, value) => {
    if (tpl.indexOf(token) < 0) {
      console.error('播放器模板缺少占位符 ' + token + '（story/tools/player-template.html 被改坏了？）');
      process.exit(1);
    }
    tpl = tpl.split(token).join(value);
  };
  fill('/*__DATA__*/null', json);
  fill('/*__ART__*/{}', artJson);
  fill('/*__ENDINGS__*/null', endingsJson);
  fill('/*__CAST__*/[]', castJson);
  // parseJump 依赖模块级的 LABEL_RE，注入时必须带上，否则页面一跳到 label 就 ReferenceError。
  fill('/*__PARSE_JUMP__*/null', 'const LABEL_RE = ' + LABEL_RE + ';\n' + parseJump.toString());
  return tpl;
}

function main() {
  const args = parseArgs(process.argv.slice(2));
  const { scenes } = readScenes();
  const ids = Object.keys(scenes);
  if (ids.length === 0) { console.error('没有找到任何剧本（' + STORY_DIR + '）'); process.exit(1); }

  // 结局规则表必须真的传进页面：否则 @ending 只能落到兜底文案，20 个结局场景一次都播不到。
  const endings = readEndings();
  const problems = audit(scenes);
  const missingEnd = missingEndingScenes(scenes, endings.table);
  const totalChars = ids.reduce((sum, id) => sum + sceneChars(scenes[id]), 0);
  let artData = {};
  if (args.standalone) {
    const inlined = inlineArt(scenes);
    artData = inlined.map;
    console.log('内嵌美术 ' + Object.keys(artData).length + ' 张 (' +
      (inlined.bytes / 1024 / 1024).toFixed(2) + ' MB 原始)') +
      (inlined.missing ? '　缺图 ' + inlined.missing + ' 张（运行时显示占位文字）' : '');
  }
  const html = buildHtml(scenes, artData, endings);
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
  if (endings.table) {
    console.log('结局规则 ' + endings.table.rules.length + ' 条已接入页面（兜底场景 ' +
      (endings.table.fallback || '无') + '）' + (missingEnd.length
        ? '，但有 ' + missingEnd.length + ' 个规则/兜底指向未创建场景: ' + missingEnd.join('、')
        : '，引用场景齐全'));
  } else {
    console.log('警告: 没读到结局规则表，@ending 只会走兜底文案');
  }
  console.log(args.standalone
    ? '\n这是自包含单文件：美术已内嵌，拷到任何机器上双击就能玩（不需要 Unity，也不需要仓库其它文件）。'
    : '\n双击打开该文件即可试玩（美术走 Assets/Resources/Art 的相对路径，需保留仓库目录结构）。');
}

main();