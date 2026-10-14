#!/usr/bin/env node
/**
 * 《时光之痕》剧本字数统计 + schema 校验
 *
 * 用法:  node story/tools/count-story.js [--json] [--only <路径,...>]
 *        [--story-dir <dir>] [--endings <file>] [--require-dense]
 * 退出码: 0 = 全部合格; 1 = 有不合格场景或校验错误
 *
 * 契约来源: story/SCHEMA.md (§2 场景结构 / §3 内容行 / §5 字数口径 / §6 资源 id /
 *           §8 分支密度、label 前向跳转、结局规则表)
 * 人物表来源: story/cast.json
 * 资源登记来源: story/ASSETS.md
 */
'use strict';

const fs = require('node:fs');
const path = require('node:path');

const ROOT = path.resolve(__dirname, '..', '..');
const DEFAULT_STORY_DIR = path.join(ROOT, 'Assets', 'Resources', 'Story');
const DEFAULT_ENDINGS_FILE = path.join(ROOT, 'Assets', 'Resources', 'StoryConfig', 'endings.json');
let STORY_DIR = DEFAULT_STORY_DIR; // 可被 --story-dir 覆盖
const CAST_FILE = path.join(ROOT, 'story', 'cast.json');
const ASSETS_FILE = path.join(ROOT, 'story', 'ASSETS.md');

const MIN_SCENE_CHARS = 2200; // SCHEMA §5 硬下限
const TARGET_SCENE_CHARS = 2500;
const MIN_LINES = 20;
const ENDING_MIN_CHARS = 800; // SCHEMA §5 结局场景硬下限
const ENDING_TARGET_CHARS = 1200;
const ENDING_MIN_LINES = 10;
const DENSITY_MAX_CHARS = 200; // SCHEMA §8.2 分支密度门禁
const MIN_LABEL_JUMPS = 2; // SCHEMA §8.1 dense 场景真支线要求
const TOTAL_TARGET = 100000;
const ACT_TARGETS = { 1: 30000, 2: 35000, 3: 35000 };
const ACT_MIN = { 1: 20000, 2: 25000, 3: 25000 };

const LINE_TYPES = new Set(['narration', 'dialogue', 'choice', 'cg', 'fx']);
const FX_TYPES = new Set(['shake', 'flash', 'insomnia', 'timeline', 'wait']);
const ID_RE = /^[a-z0-9_]+$/;
const LABEL_RE = /^l[0-9]{2,}$/; // SCHEMA §8.1
const SCENE_ID_RE = /^[0-9]+-[0-9a-zA-Z]+$/;

/** SCHEMA §8.3 `when` 允许的字段，其它字段一律报错。 */
const WHEN_FIELDS = new Set([
  'flags', 'notFlags', 'insomniaMin', 'insomniaMax',
  'shardsMin', 'flagCountMin', 'topAffection', 'affectionMin',
]);

function cjkCount(s) {
  if (typeof s !== 'string') return 0;
  const m = s.match(/[\u4e00-\u9fff]/g);
  return m ? m.length : 0;
}

function readJson(file) {
  return JSON.parse(fs.readFileSync(file, 'utf8'));
}

function walkJson(dir) {
  if (!fs.existsSync(dir)) return [];
  const out = [];
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const p = path.join(dir, entry.name);
    if (entry.isDirectory()) out.push(...walkJson(p));
    else if (entry.isFile() && entry.name.toLowerCase().endsWith('.json')) out.push(p);
  }
  return out.sort();
}

function loadCast() {
  if (!fs.existsSync(CAST_FILE)) return new Set();
  const cast = readJson(CAST_FILE);
  const names = new Set();
  for (const c of cast.characters || []) {
    if (c && typeof c.name === 'string') names.add(c.name);
  }
  return names;
}

function loadRegisteredIds() {
  if (!fs.existsSync(ASSETS_FILE)) return new Set();
  const text = fs.readFileSync(ASSETS_FILE, 'utf8');
  const ids = new Set();
  for (const m of text.matchAll(/`([a-z0-9_]+)`/g)) ids.add(m[1]);
  return ids;
}

function rel(p) {
  return path.relative(ROOT, p).replace(/\\/g, '/');
}

/**
 * 解析跳转写法（SCHEMA §8.1）：
 *   ""         继续本场景      "1-2"      跳场景
 *   "#l07"     本场景内前向跳  "1-2#l07"  跳到场景 1-2 的 label
 *   "@ending"  顶层路由到结局规则表（只有场景顶层 next 可以写）
 */
function parseJump(next) {
  if (typeof next !== 'string' || next === '') return { kind: 'none' };
  if (next === '@ending') return { kind: 'ending' };
  if (next.charAt(0) === '@') return { kind: 'illegal', raw: next };
  const hash = next.indexOf('#');
  if (hash < 0) {
    if (!SCENE_ID_RE.test(next)) return { kind: 'illegal', raw: next };
    return { kind: 'scene', sceneId: next };
  }
  const scenePart = next.slice(0, hash);
  const labelPart = next.slice(hash + 1);
  if (!LABEL_RE.test(labelPart)) return { kind: 'illegal', raw: next };
  if (scenePart === '') return { kind: 'label', label: labelPart };
  if (!SCENE_ID_RE.test(scenePart)) return { kind: 'illegal', raw: next };
  return { kind: 'sceneLabel', sceneId: scenePart, label: labelPart };
}

/** 把一个场景文件登记进 label 索引 / sceneId 集合 / kind 表（已登记的不覆盖）。 */
function indexSceneFile(file, labelIndex, sceneIds, sceneKinds) {
  let d;
  try { d = readJson(file); } catch { return; }
  if (!d || typeof d !== 'object' || typeof d.sceneId !== 'string') return;
  sceneIds.add(d.sceneId);
  sceneKinds.set(d.sceneId, typeof d.kind === 'string' ? d.kind : '');
  if (!labelIndex.has(d.sceneId)) labelIndex.set(d.sceneId, new Map());
  const m = labelIndex.get(d.sceneId);
  const list = Array.isArray(d.lines) ? d.lines : [];
  list.forEach((line, i) => {
    if (line && typeof line.label === 'string' && line.label !== '' && !m.has(line.label)) m.set(line.label, i);
  });
}

function buildSceneIndex(files) {
  const labelIndex = new Map();
  const sceneIds = new Set();
  const sceneKinds = new Map();
  for (const f of files) indexSceneFile(f, labelIndex, sceneIds, sceneKinds);
  return { labelIndex, sceneIds, sceneKinds };
}

function normalizeWhen(when) {
  return JSON.stringify(when, Object.keys(when).sort());
}

/** 校验结局规则表（SCHEMA §8.3 / §8.4）。未见过的结局场景只记提示，不算错误（迁移期）。 */
function validateEndings(file, ctx) {
  const errors = [];
  const warns = [];
  const notes = [];
  const result = {
    file: rel(file),
    ruleCount: 0,
    minEndings: 0,
    fallback: null,
    referenced: 0,
    created: 0,
    ok: false,
  };
  if (!fs.existsSync(file)) {
    errors.push(`结局规则表不存在: ${rel(file)}`);
    return { errors, warns, notes, result };
  }
  let table;
  try {
    table = readJson(file);
  } catch (e) {
    errors.push(`${rel(file)}: JSON 解析失败 - ${e.message}`);
    return { errors, warns, notes, result };
  }
  if (!table || typeof table !== 'object' || Array.isArray(table)) {
    errors.push(`${rel(file)}: 顶层必须是对象`);
    return { errors, warns, notes, result };
  }
  const minEndings = Number.isInteger(table.minEndings) && table.minEndings > 0 ? table.minEndings : 20;
  const rules = Array.isArray(table.rules) ? table.rules : [];
  result.minEndings = minEndings;
  result.ruleCount = rules.length;
  result.fallback = typeof table.fallback === 'string' && table.fallback !== '' ? table.fallback : null;

  if (!Array.isArray(table.rules)) errors.push(`${rel(file)}: rules 必须是数组`);
  if (rules.length < minEndings) {
    errors.push(`${rel(file)}: 规则只有 ${rules.length} 条，至少需要 ${minEndings} 条`);
  }

  const seenWhen = new Map();
  const referenced = [];
  rules.forEach((rule, i) => {
    const at = `${rel(file)}#rules[${i}]`;
    if (!rule || typeof rule !== 'object') { errors.push(`${at}: 不是对象`); return; }
    if (typeof rule.scene !== 'string' || rule.scene === '') errors.push(`${at}: 缺少 scene`);
    if (typeof rule.title !== 'string' || rule.title === '') errors.push(`${at}: 缺少 title`);
    const when = rule.when === undefined || rule.when === null ? {} : rule.when;
    if (typeof when !== 'object' || Array.isArray(when)) {
      errors.push(`${at}: when 必须是对象`);
    } else {
      for (const k of Object.keys(when)) {
        if (!WHEN_FIELDS.has(k)) errors.push(`${at}: when 出现 §8.3 未定义字段 ${k}`);
      }
      for (const k of ['flags', 'notFlags']) {
        if (when[k] !== undefined && (!Array.isArray(when[k]) || !when[k].every((x) => typeof x === 'string'))) {
          errors.push(`${at}: when.${k} 必须是字符串数组`);
        }
      }
      for (const k of ['insomniaMin', 'insomniaMax', 'shardsMin', 'flagCountMin', 'affectionMin']) {
        if (when[k] !== undefined && !Number.isInteger(when[k])) errors.push(`${at}: when.${k} 必须是整数`);
        // Unity 用 JsonUtility 反序列化，无法区分「字段缺失」与 0；三处实现统一把 0 当作「未给出该约束」
        else if (when[k] === 0) warns.push(`${at}: when.${k} 写了 0，运行时按「未给出该约束」处理（写 0 不生效）`);
      }
      if (when.topAffection !== undefined && typeof when.topAffection !== 'string') {
        errors.push(`${at}: when.topAffection 必须是字符串`);
      }
      const key = normalizeWhen(when);
      if (seenWhen.has(key)) errors.push(`${at}: when 与 #rules[${seenWhen.get(key)}] 完全相同`);
      else seenWhen.set(key, i);
      if (Array.isArray(when.flags) && Array.isArray(when.notFlags)) {
        const both = when.flags.filter((f) => when.notFlags.includes(f));
        if (both.length) errors.push(`${at}: when 自相矛盾（flags 与 notFlags 同时含 ${both.join(',')}）`);
      }
      if (Number.isInteger(when.insomniaMin) && Number.isInteger(when.insomniaMax)
        && when.insomniaMin > when.insomniaMax) {
        errors.push(`${at}: when.insomniaMin > when.insomniaMax，永远不会命中`);
      }
    }
    if (typeof rule.scene === 'string' && rule.scene !== '') referenced.push({ scene: rule.scene, at });
  });

  if (!result.fallback) errors.push(`${rel(file)}: 缺少 fallback（必须有兜底结局）`);

  const targets = [...referenced];
  if (result.fallback) targets.push({ scene: result.fallback, at: `${rel(file)}#fallback` });
  result.referenced = new Set(targets.map((t) => t.scene)).size;
  const created = new Set();
  for (const { scene, at } of targets) {
    if (!ctx.sceneIds.has(scene)) {
      notes.push(`${at}: 结局场景 ${scene} 尚未创建（check-endings.js 会判为该规则不可达）`);
      continue;
    }
    created.add(scene);
    const kind = ctx.sceneKinds.get(scene) || '';
    if (kind !== 'ending') {
      errors.push(`${at}: 场景 ${scene} 的 kind 是 ${kind || '(空)'}，结局规则只能指向 kind="ending" 的场景`);
    }
  }
  result.created = created.size;
  result.ok = errors.length === 0;
  return { errors, warns, notes, result };
}

function validateScene(file, data, ctx) {
  const errors = [];
  const warns = [];
  const where = rel(file);
  const need = (cond, msg) => { if (!cond) errors.push(msg); };

  if (!data || typeof data !== 'object' || Array.isArray(data)) {
    return { errors: [`${where}: 顶层必须是对象`], warns, chars: 0, lines: 0, sceneId: null, act: 0 };
  }

  for (const f of ['sceneId', 'chapter', 'title', 'background', 'bgm', 'next']) {
    need(typeof data[f] === 'string', `${where}: 缺少或类型错误的字段 ${f}`);
  }
  const sceneId = typeof data.sceneId === 'string' ? data.sceneId : '';
  const base = path.basename(file, '.json');
  need(sceneId === base, `${where}: sceneId(${sceneId}) 与文件名(${base}) 不一致`);

  const act = Number.parseInt(String(sceneId).split('-')[0], 10);
  need(act === 1 || act === 2 || act === 3, `${where}: sceneId 必须以 1-/2-/3- 开头`);

  for (const [f, v] of [['background', data.background], ['bgm', data.bgm]]) {
    if (typeof v === 'string' && v !== '' && v !== 'stop') {
      need(ID_RE.test(v), `${where}: ${f}(${v}) 只能用小写字母/数字/下划线`);
      if (!ctx.ids.has(v)) errors.push(`${where}: ${f}(${v}) 未登记在 story/ASSETS.md`);
    }
  }

  // §8.4 结局场景：kind = "ending"，字数/行数下限更低，next 必须为 ""
  need(data.kind === undefined || data.kind === '' || data.kind === 'ending',
    `${where}: kind(${data.kind}) 非法（只允许省略或 "ending"）`);
  const isEnding = data.kind === 'ending';
  need(data.dense === undefined || typeof data.dense === 'boolean', `${where}: dense 必须是布尔值`);
  const isDense = data.dense === true || ctx.requireDense === true; // §8.1 dense 密度门禁

  // §2 顶层 next 只允许 sceneId 或 "@ending"（"#label" 只写在选项里）
  if (typeof data.next === 'string' && data.next !== '') {
    const nj = parseJump(data.next);
    if (nj.kind === 'illegal') {
      errors.push(`${where}: next(${data.next}) 写法非法（顶层只能是 sceneId 或 "@ending"）`);
    } else if (nj.kind === 'label' || nj.kind === 'sceneLabel') {
      errors.push(`${where}: 顶层 next(${data.next}) 不能用 #label（前向跳转只写在选项的 next 上）`);
    } else if (nj.kind !== 'ending') {
      // "@ending" 是合法顶层写法：运行时按结局规则表解析，不算悬空引用
      ctx.pendingNext.push({ from: sceneId, to: data.next, where });
    }
  }
  if (isEnding && typeof data.next === 'string' && data.next !== '') {
    errors.push(`${where}: 结局场景的 next 必须为 ""（实际 ${data.next}）`);
  }

  const lines = data.lines;
  need(Array.isArray(lines), `${where}: lines 必须是数组`);
  const list = Array.isArray(lines) ? lines : [];
  const minLines = isEnding ? ENDING_MIN_LINES : MIN_LINES;
  need(list.length >= minLines, `${where}: 只有 ${list.length} 行，至少需要 ${minLines} 行`);

  // §8.1 行 label：^l[0-9]{2,}$ 且同场景内唯一
  const labelPos = new Map();
  list.forEach((line, i) => {
    if (!line || typeof line !== 'object' || line.label === undefined || line.label === null) return;
    const at = `${where}#L${i}`;
    if (typeof line.label !== 'string' || !LABEL_RE.test(line.label)) {
      errors.push(`${at}: label(${line.label}) 必须匹配 ^l[0-9]{2,}$`);
      return;
    }
    if (labelPos.has(line.label)) {
      errors.push(`${at}: label(${line.label}) 与第 ${labelPos.get(line.label) + 1} 行重复`);
      return;
    }
    labelPos.set(line.label, i);
  });

  let chars = 0;
  const seenText = new Map();
  let dialogueCount = 0;
  let choiceCount = 0;
  let labelJumps = 0; // §8.1 dense 场景要求的 #label 前向跳转选项个数

  list.forEach((line, i) => {
    const at = `${where}#L${i}`;
    if (!line || typeof line !== 'object') { errors.push(`${at}: 不是对象`); return; }
    need(LINE_TYPES.has(line.type), `${at}: type(${line.type}) 非法`);
    need(typeof line.text === 'string', `${at}: 缺少 text`);

    if (typeof line.speaker === 'string' && line.speaker !== '') {
      need(ctx.cast.has(line.speaker), `${at}: 人物名(${line.speaker}) 不在 story/cast.json`);
    }

    if (line.type === 'dialogue') {
      dialogueCount += 1;
      need(typeof line.speaker === 'string' && line.speaker !== '', `${at}: dialogue 必须有 speaker`);
      need(typeof line.sprite === 'string', `${at}: dialogue 必须有 sprite（没有立绘写 ""）`);
    }
    if (line.type === 'fx') {
      need(FX_TYPES.has(line.fx), `${at}: fx(${line.fx}) 非法`);
    }
    if (line.type === 'cg') {
      need(typeof line.background === 'string' && line.background !== '', `${at}: cg 必须有 background`);
    }

    if (typeof line.sprite === 'string' && line.sprite !== '') {
      need(ID_RE.test(line.sprite), `${at}: sprite(${line.sprite}) 命名非法`);
      if (!ctx.ids.has(line.sprite)) errors.push(`${at}: sprite(${line.sprite}) 未登记在 story/ASSETS.md`);
    }

    if (line.type === 'choice') {
      choiceCount += 1;
      need(typeof line.prompt === 'string' && line.prompt !== '', `${at}: choice 必须有 prompt`);
      need(Array.isArray(line.options), `${at}: choice 必须有 options 数组`);
      const opts = Array.isArray(line.options) ? line.options : [];
      need(opts.length >= 2 && opts.length <= 4, `${at}: options 必须是 2-4 个（实际 ${opts.length}）`);
      const seenOpt = new Set();
      opts.forEach((o, j) => {
        const oat = `${at}.options[${j}]`;
        if (!o || typeof o !== 'object') { errors.push(`${oat}: 不是对象`); return; }
        need(typeof o.text === 'string' && o.text !== '', `${oat}: 缺少 text`);
        if (seenOpt.has(o.text)) errors.push(`${oat}: 选项文本重复`);
        seenOpt.add(o.text);
        need(typeof o.affectionWho === 'string', `${oat}: 缺少 affectionWho（不影响谁写 ""）`);
        if (typeof o.affectionWho === 'string' && o.affectionWho !== '') {
          need(ctx.cast.has(o.affectionWho), `${oat}: affectionWho(${o.affectionWho}) 不在 cast.json`);
        }
        need(Number.isInteger(o.affection), `${oat}: affection 必须是整数（无变化写 0）`);
        need(Number.isInteger(o.insomnia), `${oat}: insomnia 必须是整数（无变化写 0）`);
        need(typeof o.flag === 'string', `${oat}: 缺少 flag（不需要写 ""）`);
        if (typeof o.flag === 'string' && o.flag !== '') {
          need(ID_RE.test(o.flag), `${oat}: flag(${o.flag}) 命名非法`);
        }
        need(typeof o.next === 'string', `${oat}: 缺少 next（继续本场景写 ""）`);
        if (typeof o.next === 'string' && o.next !== '') {
          const oj = parseJump(o.next);
          if (oj.kind === 'illegal') {
            errors.push(`${oat}: next(${o.next}) 写法非法（可用 sceneId / "#lNN" / "sceneId#lNN"）`);
          } else if (oj.kind === 'ending') {
            errors.push(`${oat}: 选项 next 不能用 @ending（只有场景顶层 next 可以写 @ending）`);
          } else if (oj.kind === 'label') {
            // §8.1 同场景前向跳转：#lNN 的目标行必须在本 choice 行之后
            const target = labelPos.get(oj.label);
            if (target === undefined) {
              errors.push(`${oat}: next(${o.next}) 在本场景找不到 label ${oj.label}（未写 label 的场景不能用 #label）`);
            } else if (target <= i) {
              errors.push(`${oat}: next(${o.next}) 回跳到第 ${target + 1} 行（本行是第 ${i + 1} 行），只允许前向跳转`);
            } else {
              labelJumps += 1;
            }
          } else if (oj.kind === 'sceneLabel') {
            const m = ctx.labelIndex.get(oj.sceneId);
            if (m === undefined) {
              errors.push(`${oat}: next(${o.next}) 指向不存在的场景 ${oj.sceneId}`);
            } else if (!m.has(oj.label)) {
              errors.push(`${oat}: next(${o.next}) 在场景 ${oj.sceneId} 找不到 label ${oj.label}`);
            } else {
              labelJumps += 1;
            }
          } else {
            ctx.pendingNext.push({ from: `${sceneId}/choice${i}`, to: oj.sceneId, where: oat });
          }
        }
        chars += cjkCount(o.text);
      });
      chars += cjkCount(line.prompt);
    } else {
      chars += cjkCount(line.text);
    }

    if (typeof line.text === 'string' && line.text !== '') {
      if (seenText.has(line.text)) {
        errors.push(`${at}: 与第 ${seenText.get(line.text)} 行文本完全重复`);
      } else {
        seenText.set(line.text, i);
      }
    }
  });

  // §8.2 dense 密度门禁：相邻分支点之间的正文 CJK 字数 ≤ 200
  if (isDense) {
    const points = [];
    list.forEach((line, i) => {
      if (line && typeof line === 'object' && line.type === 'choice') points.push(i);
    });
    let start = 0;
    for (let k = 0; k <= points.length; k++) {
      const active = k < points.length;
      const end = active ? points[k] - 1 : list.length - 1;
      let n = 0;
      for (let j = start; j <= end; j++) {
        const ln = list[j];
        if (ln && typeof ln === 'object') n += cjkCount(ln.text);
      }
      if (n > DENSITY_MAX_CHARS) {
        errors.push(`${where}: 分支点间隔 ${n} 字 > ${DENSITY_MAX_CHARS}（第 ${start + 1} 行到第 ${end + 1} 行）`);
      }
      start = active ? points[k] + 1 : list.length;
    }
    if (labelJumps < MIN_LABEL_JUMPS) {
      errors.push(`${where}: dense 场景至少需要 ${MIN_LABEL_JUMPS} 个选项带 #label 前向跳转（实际 ${labelJumps} 个）`);
    }
  }

  const minChars = isEnding ? ENDING_MIN_CHARS : MIN_SCENE_CHARS;
  const targetChars = isEnding ? ENDING_TARGET_CHARS : TARGET_SCENE_CHARS;
  if (chars < minChars) {
    errors.push(`${where}: 只有 ${chars} 字，硬下限 ${minChars}`);
  } else if (chars < targetChars) {
    warns.push(`${where}: ${chars} 字，略低于目标 ${targetChars}`);
  }

  return {
    errors,
    warns,
    chars,
    lines: list.length,
    sceneId,
    act,
    dialogueCount,
    choiceCount,
    dense: isDense,
    kind: isEnding ? 'ending' : '',
    labelJumps,
  };
}

function main() {
  const args = process.argv.slice(2);
  const asJson = args.includes('--json');
  const requireDense = args.includes('--require-dense');
  const onlyIdx = args.indexOf('--only');
  const onlyPath = onlyIdx >= 0 ? args[onlyIdx + 1] : null;
  const dirIdx = args.indexOf('--story-dir');
  if (dirIdx >= 0) {
    const d = args[dirIdx + 1];
    if (!d) { console.error('--story-dir 缺少路径参数'); process.exit(2); }
    STORY_DIR = path.resolve(ROOT, d);
    if (!fs.existsSync(STORY_DIR)) { console.error(`--story-dir 不存在: ${d}`); process.exit(2); }
  }
  const endingsIdx = args.indexOf('--endings');
  const endingsFile = endingsIdx >= 0 ? path.resolve(ROOT, args[endingsIdx + 1] || '') : DEFAULT_ENDINGS_FILE;
  const cast = loadCast();
  const ids = loadRegisteredIds();
  const allFiles = walkJson(STORY_DIR);
  const sceneIndex = buildSceneIndex(allFiles);
  let files = allFiles;
  let scoped = false;
  let scopeLabel = null;
  const outsideIds = new Set();

  if (onlyPath) {
    const requested = onlyPath.split(',').map((s) => s.trim()).filter(Boolean);
    const picked = [];
    const labels = [];
    for (const item of requested) {
      const abs = path.resolve(ROOT, item);
      const target = fs.existsSync(abs) ? abs : path.resolve(STORY_DIR, item);
      if (!fs.existsSync(target)) {
        console.error(`--only 路径不存在: ${item}`);
        process.exit(2);
      }
      labels.push(rel(target));
      if (fs.statSync(target).isDirectory()) picked.push(...walkJson(target));
      else picked.push(target);
    }
    scoped = true;
    scopeLabel = labels.join(', ');
    files = [...new Set(picked)];
    for (const f of allFiles) {
      if (files.includes(f)) continue;
      try {
        const d = readJson(f);
        if (d && typeof d.sceneId === 'string') outsideIds.add(d.sceneId);
      } catch {
        // 范围外的文件坏掉不在这里报, 全量跑时才报
      }
    }
  }

  // --only 可能指向 story-dir 之外的文件，一并登记进 label/scene 索引
  for (const f of files) indexSceneFile(f, sceneIndex.labelIndex, sceneIndex.sceneIds, sceneIndex.sceneKinds);

  const ctx = {
    cast,
    ids,
    pendingNext: [],
    labelIndex: sceneIndex.labelIndex,
    sceneIds: sceneIndex.sceneIds,
    sceneKinds: sceneIndex.sceneKinds,
    requireDense,
  };

  const scenes = [];
  const errors = [];
  const warns = [];
  const infos = [];
  const seenIds = new Set();

  for (const file of files) {
    let data;
    try {
      data = readJson(file);
    } catch (e) {
      errors.push(`${rel(file)}: JSON 解析失败 - ${e.message}`);
      continue;
    }
    const r = validateScene(file, data, ctx);
    errors.push(...r.errors);
    warns.push(...r.warns);
    if (r.sceneId) {
      if (seenIds.has(r.sceneId)) errors.push(`${rel(file)}: sceneId ${r.sceneId} 重复`);
      seenIds.add(r.sceneId);
    }
    scenes.push({ file: rel(file), ...r });
  }

  for (const p of ctx.pendingNext) {
    if (seenIds.has(p.to)) continue;
    if (scoped) {
      const why = outsideIds.has(p.to) ? '在本次校验范围之外' : '尚未创建';
      infos.push(`${p.where}: next(${p.to}) ${why}（全量校验时检查）`);
      continue;
    }
    errors.push(`${p.where}: next(${p.to}) 指向不存在的场景`);
  }

  // 唯一文本跨场景复读
  const textOwners = new Map();
  for (const file of files) {
    let data;
    try { data = readJson(file); } catch { continue; }
    const texts = [];
    for (const line of data.lines || []) {
      if (typeof line.text === 'string' && line.text !== '') texts.push(line.text);
      for (const o of line.options || []) if (typeof o.text === 'string') texts.push(o.text);
    }
    for (const t of texts) {
      if (textOwners.has(t) && textOwners.get(t) !== data.sceneId) {
        warns.push(`跨场景复读: "${t.slice(0, 18)}…" 同时出现在 ${textOwners.get(t)} 与 ${data.sceneId}`);
      } else {
        textOwners.set(t, data.sceneId);
      }
    }
  }

  // §8.3 结局规则表（endings.json）
  const endings = validateEndings(endingsFile, ctx);
  errors.push(...endings.errors);
  warns.push(...endings.warns);
  const endingNotes = endings.notes;

  const actStats = { 1: { chars: 0, scenes: 0 }, 2: { chars: 0, scenes: 0 }, 3: { chars: 0, scenes: 0 } };
  let total = 0;
  let totalLines = 0;
  for (const s of scenes) {
    total += s.chars;
    totalLines += s.lines || 0;
    if (actStats[s.act]) {
      actStats[s.act].chars += s.chars;
      actStats[s.act].scenes += 1;
    }
  }

  if (!scoped) {
    for (const act of [1, 2, 3]) {
      const got = actStats[act].chars;
      if (actStats[act].scenes > 0 && got < ACT_MIN[act]) {
        errors.push(`第${act}幕只有 ${got} 字，硬下限 ${ACT_MIN[act]}（目标 ${ACT_TARGETS[act]}）`);
      }
    }
    if (total < TOTAL_TARGET && scenes.length > 0) {
      errors.push(`全剧只有 ${total} 字，目标 ${TOTAL_TARGET}`);
    }
  }

  const report = {
    totalChars: total,
    totalLines,
    sceneCount: scenes.length,
    acts: [1, 2, 3].map((a) => ({
      act: a,
      scenes: actStats[a].scenes,
      chars: actStats[a].chars,
      target: ACT_TARGETS[a],
    })),
    scenes: scenes.map((s) => ({
      sceneId: s.sceneId,
      file: s.file,
      chars: s.chars,
      lines: s.lines,
      dialogue: s.dialogueCount || 0,
      choices: s.choiceCount || 0,
      dense: s.dense === true,
      kind: s.kind || '',
      labelJumps: s.labelJumps || 0,
    })),
    endings: { ...endings.result, notes: endingNotes },
    errors,
    warns,
    infos,
    scope: scopeLabel,
    requireDense,
  };

  if (asJson) {
    console.log(JSON.stringify(report, null, 2));
  } else {
    console.log('=== 《时光之痕》剧本统计 ===');
    for (const s of report.scenes) {
      const target = s.kind === 'ending' ? ENDING_TARGET_CHARS : TARGET_SCENE_CHARS;
      const floor = s.kind === 'ending' ? ENDING_MIN_CHARS : MIN_SCENE_CHARS;
      const flag = s.chars >= target ? 'OK ' : s.chars >= floor ? 'LOW' : 'BAD';
      const tags = `${s.kind === 'ending' ? ' [ending]' : ''}${s.dense ? ' [dense]' : ''}`;
      console.log(`  [${flag}] ${String(s.sceneId).padEnd(6)} ${String(s.chars).padStart(6)} 字  ${String(s.lines).padStart(4)} 行  对白 ${String(s.dialogue).padStart(3)}  选择 ${s.choices}${tags}`);
    }
    console.log('---');
    for (const a of report.acts) {
      const pct = a.target ? ((a.chars / a.target) * 100).toFixed(1) : '0.0';
      console.log(`  第${a.act}幕: ${a.scenes} 个场景, ${a.chars} 字 / 目标 ${a.target} (${pct}%)`);
    }
    console.log(`  合计: ${report.sceneCount} 个场景, ${total} 字 / 目标 ${TOTAL_TARGET} (${((total / TOTAL_TARGET) * 100).toFixed(1)}%), ${totalLines} 行`);
    if (scoped) {
      console.log(`  （--only ${scopeLabel}: 只校验 ${files.length} 个场景, 本幕/全剧总量门禁已跳过）`);
    }
    console.log(`\n--- 结局路由 ${report.endings.file}`);
    console.log(`  规则 ${report.endings.ruleCount} 条 / 至少需要 ${report.endings.minEndings} 条：${report.endings.ok ? '合格' : '不合格'}`);
    console.log(`  规则+兜底引用结局场景 ${report.endings.referenced} 个，已创建 ${report.endings.created} 个；兜底 fallback = ${report.endings.fallback || '(缺失)'}`);
    for (const n of endingNotes.slice(0, 10)) console.log(`  i ${n}`);
    if (endingNotes.length > 10) console.log(`  ... 其余 ${endingNotes.length - 10} 条省略`);
    if (infos.length) {
      console.log(`\n--- 范围外引用 ${infos.length} 条（不算错误）`);
      for (const i of infos.slice(0, 20)) console.log(`  i ${i}`);
      if (infos.length > 20) console.log(`  ... 其余 ${infos.length - 20} 条省略`);
    }
    if (warns.length) {
      console.log(`\n--- 警告 ${warns.length} 条`);
      for (const w of warns.slice(0, 40)) console.log(`  ! ${w}`);
      if (warns.length > 40) console.log(`  ... 其余 ${warns.length - 40} 条省略`);
    }
    if (errors.length) {
      console.log(`\n--- 错误 ${errors.length} 条`);
      for (const e of errors.slice(0, 60)) console.log(`  x ${e}`);
      if (errors.length > 60) console.log(`  ... 其余 ${errors.length - 60} 条省略`);
    } else if (scenes.length > 0) {
      console.log('\n全部合格。');
    }
  }

  process.exit(errors.length > 0 ? 1 : 0);
}

main();
