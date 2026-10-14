#!/usr/bin/env node
'use strict';

/**
 * check-endings.js —— SCHEMA v2 §8.3 结局规则可达性验证（蒙特卡洛走图）
 *
 * 和 Unity 运行时（StoryDirector + EndingRules.cs）用同一套语义走剧本图：
 *   - choice 行：随机取一个选项，把 affection / insomnia / flag 施加到状态上
 *   - 选项 next："" 继续本场景 / "#lNN" 同场景前向跳 / "sceneId" 换场 / "sceneId#lNN" 换场+跳行
 *   - 场景顶层 next：sceneId 换场；"@ending" 按结局规则表解析；"" 收尾（进结局画面）
 *   - kind == "ending" 的场景：next 必须为 ""，演完即收尾（标题由规则表给）
 *
 * 每个结局规则被命中的次数为 0 = 该结局不可达 → 不合格。兜底命中次数、走通的结局种类数
 * 也一并输出；种类数 < minEndings 同样不合格。
 *
 * 用法：
 *   node story/tools/check-endings.js
 *   node story/tools/check-endings.js --story-dir <dir> --endings <file> --runs 5000 --seed 20261013
 *
 * 退出码：0 = 全部规则可达且种类数足够；1 = 有规则不可达 / 种类数不足 / 规则表读不了。
 */

const fs = require('fs');
const path = require('path');

const ROOT = path.resolve(__dirname, '..', '..');
const DEFAULT_STORY_DIR = path.join(ROOT, 'Assets', 'Resources', 'Story');
const DEFAULT_ENDINGS_FILE = path.join(ROOT, 'Assets', 'Resources', 'StoryConfig', 'endings.json');
const DEFAULT_RUNS = 5000;
const DEFAULT_SEED = 20261013; // 固定种子：同样的剧本 + 规则表 → 可复现的命中次数
const DEFAULT_MIN_ENDINGS = 20;
const DEFAULT_START_SCENE = '1-1';

const LABEL_RE = /^l[0-9]{2,}$/;
const MAX_STEPS = 400000; // 单次走图的行访问上限（防死循环）
const MAX_HOPS = 2000; // 单次走图的换场上限

const WHEN_FIELDS = new Set([
  'flags',
  'notFlags',
  'insomniaMin',
  'insomniaMax',
  'shardsMin',
  'flagCountMin',
  'topAffection',
  'affectionMin',
]);

function rel(p) {
  const r = path.relative(ROOT, p);
  return r === '' ? '.' : r.split(path.sep).join('/');
}

function walkJson(dir) {
  const out = [];
  const stack = [dir];
  while (stack.length) {
    const cur = stack.pop();
    let entries;
    try {
      entries = fs.readdirSync(cur, { withFileTypes: true });
    } catch {
      continue;
    }
    for (const e of entries) {
      const full = path.join(cur, e.name);
      if (e.isDirectory()) stack.push(full);
      else if (e.isFile() && e.name.toLowerCase().endsWith('.json')) out.push(full);
    }
  }
  return out.sort();
}

function loadCastOrder() {
  const file = path.join(ROOT, 'story', 'cast.json');
  try {
    const data = JSON.parse(fs.readFileSync(file, 'utf8'));
    const list = Array.isArray(data.characters) ? data.characters : [];
    return list.map((c) => (c && typeof c.name === 'string' ? c.name : '')).filter(Boolean);
  } catch {
    return [];
  }
}

// mulberry32：小、快、可复现
function makeRng(seed) {
  let a = seed >>> 0;
  return function rng() {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function parseJump(next) {
  if (next === '') return { kind: 'none' };
  if (next.charAt(0) === '@') {
    return next === '@ending' ? { kind: 'ending' } : { kind: 'illegal' };
  }
  const hash = next.indexOf('#');
  if (hash < 0) return { kind: 'scene', sceneId: next };
  const scenePart = next.slice(0, hash);
  const label = next.slice(hash + 1);
  if (!LABEL_RE.test(label)) return { kind: 'illegal' };
  if (scenePart === '') return { kind: 'label', label };
  return { kind: 'sceneLabel', sceneId: scenePart, label };
}

function loadScenes(dir) {
  const scenes = new Map();
  const problems = [];
  for (const file of walkJson(dir)) {
    let data;
    try {
      data = JSON.parse(fs.readFileSync(file, 'utf8'));
    } catch (e) {
      problems.push(`${rel(file)}: JSON 解析失败 - ${e.message}`);
      continue;
    }
    if (!data || typeof data !== 'object' || Array.isArray(data)) {
      problems.push(`${rel(file)}: 顶层必须是对象`);
      continue;
    }
    const sceneId = typeof data.sceneId === 'string' && data.sceneId !== ''
      ? data.sceneId
      : path.basename(file, '.json');
    const labels = new Map();
    const lines = Array.isArray(data.lines) ? data.lines : [];
    lines.forEach((line, i) => {
      if (!line || typeof line !== 'object') return;
      if (typeof line.label !== 'string' || line.label === '') return;
      if (!labels.has(line.label)) labels.set(line.label, i); // 重复 label 由 count-story.js 报，这里取第一处
    });
    if (scenes.has(sceneId)) problems.push(`sceneId ${sceneId} 重复（${rel(file)}）`);
    scenes.set(sceneId, { file, data, labels, lines });
  }
  return { scenes, problems };
}

function loadTable(file) {
  const text = fs.readFileSync(file, 'utf8');
  const raw = JSON.parse(text);
  if (!raw || typeof raw !== 'object' || Array.isArray(raw)) throw new Error('顶层必须是对象');
  const rules = Array.isArray(raw.rules) ? raw.rules : [];
  const minEndings = Number.isInteger(raw.minEndings) ? raw.minEndings : DEFAULT_MIN_ENDINGS;
  const fallback = typeof raw.fallback === 'string' ? raw.fallback : '';
  return { file, raw, rules, minEndings, fallback };
}

function newState() {
  return { insomnia: 0, flags: new Set(), shards: new Set(), affection: new Map() };
}

function applyOption(state, opt) {
  if (!opt || typeof opt !== 'object') return;
  const who = typeof opt.affectionWho === 'string' ? opt.affectionWho : '';
  const affection = Number.isInteger(opt.affection) ? opt.affection : 0;
  if (who !== '' && affection !== 0) {
    state.affection.set(who, (state.affection.get(who) || 0) + affection);
  }
  const insomnia = Number.isInteger(opt.insomnia) ? opt.insomnia : 0;
  if (insomnia !== 0) {
    state.insomnia = Math.max(0, Math.min(100, state.insomnia + insomnia));
  }
  if (typeof opt.flag === 'string' && opt.flag !== '') state.flags.add(opt.flag);
}

/** 好感最高的人物：并列时按 story/cast.json 的人物顺序取先者（§8.3）。 */
function topAffection(state, castOrder) {
  const names = [];
  for (const name of castOrder) names.push(name);
  for (const name of state.affection.keys()) {
    if (!names.includes(name)) names.push(name);
  }
  let best = '';
  let bestValue = 0;
  let first = true;
  for (const name of names) {
    const v = state.affection.get(name) || 0;
    if (first || v > bestValue) {
      best = name;
      bestValue = v;
      first = false;
    }
  }
  return { who: best, value: bestValue };
}

function whenSatisfied(when, state, castOrder) {
  if (!when || typeof when !== 'object' || Array.isArray(when)) return false;
  for (const key of Object.keys(when)) {
    if (!WHEN_FIELDS.has(key)) return false; // 未定义字段：count-story.js 会报错，这里视为永不命中
  }
  if (Array.isArray(when.flags)) {
    for (const f of when.flags) if (!state.flags.has(f)) return false;
  }
  if (Array.isArray(when.notFlags)) {
    for (const f of when.notFlags) if (state.flags.has(f)) return false;
  }
  // 数值 0 一律当作「未给出该约束」：Unity 用 JsonUtility 反序列化时无法区分「字段缺失」与 0，
  // 三处实现（校验器 / 网页播放器 / Unity）统一按这个口径走；endings.json 里也没有 0 值约束。
  if (Number.isInteger(when.insomniaMin) && when.insomniaMin !== 0 && state.insomnia < when.insomniaMin) return false;
  if (Number.isInteger(when.insomniaMax) && when.insomniaMax !== 0 && state.insomnia > when.insomniaMax) return false;
  if (Number.isInteger(when.shardsMin) && when.shardsMin !== 0 && state.shards.size < when.shardsMin) return false;
  if (Number.isInteger(when.flagCountMin) && when.flagCountMin !== 0 && state.flags.size < when.flagCountMin) return false;
  const top = topAffection(state, castOrder);
  if (typeof when.topAffection === 'string' && when.topAffection !== '' && top.who !== when.topAffection) return false;
  if (Number.isInteger(when.affectionMin) && when.affectionMin !== 0 && top.value < when.affectionMin) return false;
  return true;
}

/** 自上而下第一条全部满足的规则胜出；都不满足用 fallback。-1 = 兜底。 */
function resolveRule(table, state, castOrder) {
  for (let i = 0; i < table.rules.length; i++) {
    const rule = table.rules[i];
    if (!rule || typeof rule !== 'object') continue;
    if (whenSatisfied(rule.when, state, castOrder)) {
      return { index: i, scene: typeof rule.scene === 'string' ? rule.scene : '', rule };
    }
  }
  return { index: -1, scene: table.fallback, rule: null };
}

function main() {
  const args = process.argv.slice(2);
  let storyDir = DEFAULT_STORY_DIR;
  const dirIdx = args.indexOf('--story-dir');
  if (dirIdx >= 0) {
    const d = args[dirIdx + 1];
    if (!d) {
      console.error('--story-dir 缺少路径参数');
      process.exit(2);
    }
    storyDir = path.resolve(ROOT, d);
    if (!fs.existsSync(storyDir)) {
      console.error(`--story-dir 不存在: ${d}`);
      process.exit(2);
    }
  }
  const endingsIdx = args.indexOf('--endings');
  const endingsFile = endingsIdx >= 0
    ? path.resolve(ROOT, args[endingsIdx + 1] || '')
    : DEFAULT_ENDINGS_FILE;
  const runsIdx = args.indexOf('--runs');
  const runs = runsIdx >= 0 ? Number.parseInt(args[runsIdx + 1], 10) : DEFAULT_RUNS;
  if (!Number.isInteger(runs) || runs <= 0) {
    console.error('--runs 必须是正整数');
    process.exit(2);
  }
  const seedIdx = args.indexOf('--seed');
  const seed = seedIdx >= 0 ? Number.parseInt(args[seedIdx + 1], 10) : DEFAULT_SEED;
  if (!Number.isInteger(seed)) {
    console.error('--seed 必须是整数');
    process.exit(2);
  }

  const errors = [];
  if (!fs.existsSync(endingsFile)) {
    errors.push(`结局规则表不存在: ${rel(endingsFile)}`);
    console.error(`=== check-endings: 结局可达性走图 ===`);
    console.error(`  x ${errors[0]}`);
    process.exit(1);
  }

  const { scenes, problems } = loadScenes(storyDir);
  let table;
  try {
    table = loadTable(endingsFile);
  } catch (e) {
    console.error(`=== check-endings: 结局可达性走图 ===`);
    console.error(`  x ${rel(endingsFile)}: 解析失败 - ${e.message}`);
    process.exit(1);
  }
  const castOrder = loadCastOrder();

  let startScene = DEFAULT_START_SCENE;
  if (!scenes.has(startScene)) {
    const first = [...scenes.keys()].sort()[0];
    if (first === undefined) {
      console.error('=== check-endings: 结局可达性走图 ===');
      console.error(`  x ${rel(storyDir)} 下没有任何剧本场景`);
      process.exit(1);
    }
    startScene = first;
  }

  const ruleHits = table.rules.map(() => 0); // 规则胜出次数
  const enteredHits = new Map(); // 直接走进某个结局场景的次数（不经 @ending 解析）
  const counters = {
    fallback: 0,
    legacyEnd: 0, // next == "" 的普通场景收尾（老行为的硬编码结局画面）
    endingSceneEnd: 0, // 结局场景演完收尾
    deadEnd: 0, // 走进不存在的场景
    badJump: 0, // 非法 / 失效的跳转
    stuck: 0, // 超出步数/换场上限
    missingResolve: 0, // 规则解析出的场景不存在
  };

  const rng = makeRng(seed);

  // 定向偏置采样：纯随机游走只能证明「大概率可达」，对低概率组合（特异 flag × 失眠档位 ×
  // 好感最高者）几乎采不到。这里按规则表逐条生成目标状态，在 choice 处对选项打分取最高
  // （平局按原顺序取先者）：flags +1000 / notFlags 排除项 -1000 / topAffection +500 /
  // 失眠档位 low(≤max)=负值+400、mid[min,max]=向区间中点靠拢、high(≥min)=正值+300。
  // 定向轮证明「可达性」，随机轮保留分布统计。
  // 互斥回避: 剧本里若干 flag 是「同组选一」关系(命 flag 四选一; 表/账/页 各两选一)。
  // 定向游走若只按规则自身 when 打分, flags+1000 会把所有游走都拖进「带 ledger_balanced
  // 的奇迹路」这类高价值组合, 遮蔽同组其他规则。这里按组把「规则没点名的成员」并入回避项。
  // 未点名的组只回避「高价值成员」：逼游走去点负向孪生（keep_the_watch 等）反而会把失眠抬进兜底档。
  const PREMIUM_FLAGS = new Set(['ledger_balanced', 'returned_watch', 'kept_the_page']);
  const EXCLUSIVE_GROUPS = [
    ['ending_miracle', 'ending_normal', 'ending_hero', 'ending_tragic', 'ending_open'],
    ['returned_watch', 'keep_the_watch'],
    ['ledger_balanced', 'ledger_left_open'],
    ['kept_the_page', 'page_returned_to_ledger'],
  ];
  const biasWalks = [];
  for (let i = 0; i < table.rules.length; i++) {
    const when = (table.rules[i] && table.rules[i].when) || {};
    const want = new Set(Array.isArray(when.flags) ? when.flags : []);
    const avoid = new Set(Array.isArray(when.notFlags) ? when.notFlags : []);
    for (const g of EXCLUSIVE_GROUPS) {
      const named = g.some((f) => want.has(f));
      for (const f of g) {
        if (want.has(f)) continue;
        if (named) avoid.add(f);
        else if (PREMIUM_FLAGS.has(f)) avoid.add(f);
      }
    }
    const hasMin = Number.isInteger(when.insomniaMin) && when.insomniaMin !== 0;
    const hasMax = Number.isInteger(when.insomniaMax) && when.insomniaMax !== 0;
    const target = hasMin && hasMax ? 'mid' : hasMin ? 'high' : hasMax ? 'low'
      : (Array.from(want).some((f) => !/^ending_/.test(f)) ? 'under80' : 'any');
    biasWalks.push({
      target,
      flags: Array.from(want),
      notFlags: Array.from(avoid),
      top: typeof when.topAffection === 'string' ? when.topAffection : '',
      insMin: Number.isInteger(when.insomniaMin) && when.insomniaMin !== 0 ? when.insomniaMin : null,
      insMax: Number.isInteger(when.insomniaMax) && when.insomniaMax !== 0 ? when.insomniaMax : null,
    });
  }
  function biasScore(b, opt, state) {
    let s = 0;
    const f = typeof opt.flag === 'string' ? opt.flag : '';
    if (f !== '' && b.flags.includes(f)) s += 1000;
    if (f !== '' && b.notFlags.includes(f)) s -= 1000;
    const who = typeof opt.affectionWho === 'string' ? opt.affectionWho : '';
    if (b.top !== '' && who === b.top) s += 500;
    const ins = Number.isInteger(opt.insomnia) ? opt.insomnia : 0;
    if (b.target === 'low') { // 压到 max 以下(det2 口径: 无条件重罚大正增量)
      if (ins < 0) s += 400; else if (ins > 10) s -= 300;
    } else if (b.target === 'mid') { // 向 55-79 区间靠拢
      if (state.insomnia < 50) { if (ins > 0) s += 300; else s -= 400; }
      else if (state.insomnia > 72) { if (ins < 0) s += 300; else s -= 400; }
      else if (ins === 0) s += 100;
    } else if (b.target === 'high') {
      if (ins > 0) s += 300; else s -= 400;
    } else if (b.target === 'under80') { // 避开 #5 e20{insomniaMin80} 截胡
      if (ins < 0) s += 250; else if (ins > 8) s -= 350;
    }
    return s;
  }
  for (let phase = 0; phase < 2; phase++) {
    const biasRuns = phase === 0 ? biasWalks.length : 1; // 定向：每条规则一次；随机：runs 次
    for (let rr = 0; rr < (phase === 0 ? biasRuns : runs); rr++) {
      const b = phase === 0 ? biasWalks[rr] : null;
    const state = newState();
    const seen = new Set(); // 本次走图已"进入过"的结局场景
    let sceneId = startScene;
    let idx = 0;
    let steps = 0;
    let hops = 0;
    let finished = false;

    while (!finished) {
      const scene = scenes.get(sceneId);
      if (!scene) {
        counters.deadEnd += 1;
        break;
      }
      if (scene.data.kind === 'ending' && !seen.has(sceneId)) {
        seen.add(sceneId);
        enteredHits.set(sceneId, (enteredHits.get(sceneId) || 0) + 1);
      }

      if (idx >= scene.lines.length) {
        const nx = typeof scene.data.next === 'string' ? scene.data.next : '';
        const jump = parseJump(nx);
        if (jump.kind === 'ending') {
          const picked = resolveRule(table, state, castOrder);
          if (picked.index < 0) counters.fallback += 1;
          else ruleHits[picked.index] += 1;
          if (typeof picked.scene !== 'string' || picked.scene === '' || !scenes.has(picked.scene)) {
            counters.missingResolve += 1;
            break;
          }
          sceneId = picked.scene;
          idx = 0;
          hops += 1;
        } else if (jump.kind === 'none') {
          if (scene.data.kind === 'ending') counters.endingSceneEnd += 1;
          else counters.legacyEnd += 1;
          break;
        } else if (jump.kind === 'scene') {
          if (!scenes.has(jump.sceneId)) {
            counters.deadEnd += 1;
            break;
          }
          sceneId = jump.sceneId;
          idx = 0;
          hops += 1;
        } else {
          counters.badJump += 1;
          break;
        }
        if (hops > MAX_HOPS) {
          counters.stuck += 1;
          break;
        }
        continue;
      }

      steps += 1;
      if (steps > MAX_STEPS) {
        counters.stuck += 1;
        break;
      }

      const line = scene.lines[idx];
      if (!line || typeof line !== 'object' || line.type !== 'choice' ||
          !Array.isArray(line.options) || line.options.length === 0) {
        idx += 1;
        continue;
      }

      const opt = b
        ? line.options.reduce((best, o, i2) => {
            const sc = biasScore(b, o, state) - i2 / 1000; // 平局取先出现者
            return sc > best.sc ? { sc, o } : best;
          }, { sc: -Infinity, o: line.options[0] }).o
        : line.options[Math.floor(rng() * line.options.length)];
      applyOption(state, opt);
      const next = opt && typeof opt.next === 'string' ? opt.next : '';
      const jump = parseJump(next);

      if (jump.kind === 'none') {
        idx += 1;
        continue;
      }
      if (jump.kind === 'illegal' || jump.kind === 'ending') {
        counters.badJump += 1;
        break;
      }
      if (jump.kind === 'label' || jump.kind === 'sceneLabel') {
        const targetSceneId = jump.kind === 'label' ? sceneId : jump.sceneId;
        const target = scenes.get(targetSceneId);
        const targetIdx = target ? target.labels.get(jump.label) : undefined;
        if (targetIdx === undefined || (targetSceneId === sceneId && targetIdx <= idx)) {
          counters.badJump += 1;
          break;
        }
        sceneId = targetSceneId;
        idx = targetIdx;
        hops += 1;
      } else {
        if (!scenes.has(jump.sceneId)) {
          counters.deadEnd += 1;
          break;
        }
        sceneId = jump.sceneId;
        idx = 0;
        hops += 1;
      }
      if (hops > MAX_HOPS) {
        counters.stuck += 1;
        break;
      }
    }
    }
  }
  // 走通的结局种类：本局真正"进入过"的 kind="ending" 场景（直接跳进或经 @ending 规则解析进来都算），
  // 外加老式收尾（next == "" 的普通场景，运行时会落到硬编码结局画面）。
  const kinds = new Set();
  for (const [sceneId, n] of enteredHits) {
    if (n > 0) kinds.add(sceneId);
  }
  if (counters.legacyEnd > 0) kinds.add('legacy-end');

  const unreachable = [];
  const lines = [];
  let hitTotal = 0;
  for (let i = 0; i < table.rules.length; i++) {
    const rule = table.rules[i] || {};
    const entered = enteredHits.get(rule.scene) || 0;
    const won = ruleHits[i];
    if (won + entered === 0) unreachable.push({ index: i, rule, won, entered });
    hitTotal += won + entered;
  }

  const out = [];
  out.push('=== check-endings: 结局可达性走图 ===');
  out.push(`  剧本目录: ${rel(storyDir)}（${scenes.size} 个场景，起点 ${startScene}）`);
  out.push(`  规则表:   ${rel(table.file)}（${table.rules.length} 条规则, minEndings=${table.minEndings}, fallback=${table.fallback || '(缺失)'}）`);
  out.push(`  随机走图: runs=${runs} seed=${seed}${castOrder.length ? '' : '（未读到 story/cast.json，并列取先者退化为插入顺序）'}`);
  out.push('');
  out.push('  #   命中   规则胜利  直接进入  场景      标题');
  for (let i = 0; i < table.rules.length; i++) {
    const rule = table.rules[i] || {};
    const entered = enteredHits.get(rule.scene) || 0;
    out.push(`  ${String(i + 1).padStart(2)}  ${String(ruleHits[i] + entered).padStart(6)}  ${String(ruleHits[i]).padStart(8)}  ${String(entered).padStart(8)}  ${String(rule.scene || '?').padEnd(8)}  ${rule.title || ''}`);
  }
  out.push('');
  out.push('--- 汇总');
  out.push(`  规则命中合计: ${hitTotal} 次`);
  out.push(`  兜底命中:     ${counters.fallback} 次（fallback = ${table.fallback || '(缺失)'}）`);
  out.push(`  结局场景演完收尾: ${counters.endingSceneEnd} 次`);
  out.push(`  普通场景收尾(next: ""): ${counters.legacyEnd} 次`);
  out.push(`  走通的结局种类: ${kinds.size} / 至少需要 ${table.minEndings}（${[...kinds].sort().join(', ')}）`);
  if (counters.deadEnd || counters.badJump || counters.stuck || counters.missingResolve) {
    out.push(`  异常走图: 空场景 ${counters.deadEnd} / 非法跳转 ${counters.badJump} / 未收敛 ${counters.stuck} / 规则场景缺失 ${counters.missingResolve}`);
  }
  for (const p of problems) out.push(`  x ${p}`);

  if (unreachable.length) {
    out.push('');
    out.push(`--- 不可达结局 ${unreachable.length} 条`);
    for (const u of unreachable) {
      out.push(`  x #${u.index + 1} 规则 scene=${u.rule.scene || '?'} title=${u.rule.title || '?'}：${runs} 次走图命中 0 次（从 ${startScene} 出发走不到）`);
    }
  }
  if (problems.length) {
    out.push('');
    out.push(`--- 剧本读取问题 ${problems.length} 条`);
  }

  const ok = unreachable.length === 0 && problems.length === 0 && kinds.size >= table.minEndings;
  if (ok) {
    out.push('');
    out.push(`全部合格：${table.rules.length} 条规则都可达，走通 ${kinds.size} 种结局 ≥ ${table.minEndings}。`);
  } else {
    out.push('');
    const why = [];
    if (problems.length) why.push(`${problems.length} 处剧本读取问题`);
    if (unreachable.length) why.push(`${unreachable.length} 条规则命中 0 次`);
    if (kinds.size < table.minEndings) why.push(`结局种类 ${kinds.size} < minEndings ${table.minEndings}`);
    out.push(`不合格：${why.join('；')}。`);
  }

  console.log(out.join('\n'));
  process.exit(ok ? 0 : 1);
}

main();