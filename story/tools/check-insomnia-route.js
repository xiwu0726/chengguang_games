#!/usr/bin/env node
/**
 * check-insomnia-route.js —— 只读的失眠路径验证器（前向不动点版）。
 *
 * 为什么需要它：结局结算优先级是
 *   ending_miracle > insomnia >= 80（强制「结局·失眠」）> ending_hero > ending_tragic > ending_open > 默认
 * （见 Assets/Scripts/Story/StoryDirector.cs 的 ShowEnding 与 story/BIBLE.md）。
 * 所以只要走到某场后日谈时失眠值已经 >= 80，玩家在 3-7 选的那一项就被整段盖掉。
 * 本脚本算出每一场的「入口失眠值」最小 / 最大范围，用来证明：
 *   1) 3-8 / 3-9 / 3-10 / 3-11 / 3-12 在后日谈入口都 < 80（五个结局都走得到）；
 *   2) 完全不休息地玩时能到 >= 80（「结局·失眠」不是死内容）。
 *
 * 语义对齐 Assets/Scripts/Story/StoryDirector.cs：
 *   - 走到 choice 行时必须选一项，选中时结算 option.insomnia（缺省 0）；
 *   - option.next 非空 → 跳到该场景第 0 行；为空 → 继续本场景下一行；
 *   - 本场景行读完后走顶层 scene.next；next 为空 → 终局（后日谈本身 next 都是 ""）；
 *   - 每一步都 clamp 到 0..100（与 StoryData.GameState.AddInsomnia 一致）。
 * 因为「加值 + clamp」是单调不减映射，最小值与最大值可以各自独立传播，所以用前向不动点即可，
 * 不需要枚举路径（枚举是指数级的，本项目 38 场会跑不完）。
 *
 * 用法：node story/tools/check-insomnia-route.js   （只读，不改任何文件）
 * 有问题时 exit 1；读不到场景 exit 2；全绿 exit 0。
 */

'use strict';

const fs = require('node:fs');
const path = require('node:path');

const ROOT = path.join(__dirname, '..', '..');
const STORY_DIR = path.join(ROOT, 'Assets', 'Resources', 'Story');
const ENDING_SCENES = ['3-8', '3-9', '3-10', '3-11', '3-12'];
const KEY_NODES = ['3-7', '3-6a'];
const START_SCENE = '1-1';
const MAX_PASSES = 200;

const clamp = (value) => Math.max(0, Math.min(100, value));

function loadScenes() {
  const scenes = new Map();
  for (const act of ['act1', 'act2', 'act3']) {
    const dir = path.join(STORY_DIR, act);
    if (!fs.existsSync(dir)) continue;
    for (const file of fs.readdirSync(dir)) {
      if (!file.endsWith('.json')) continue;
      let scene;
      try {
        scene = JSON.parse(fs.readFileSync(path.join(dir, file), 'utf8'));
      } catch (err) {
        console.error(`x ${act}/${file}: JSON 解析失败 —— ${err.message}`);
        process.exit(2);
      }
      scenes.set(scene.sceneId || file.replace(/\.json$/, ''), scene);
    }
  }
  return scenes;
}

/** 把一段「起点入睡眠值 → 出口」的传播结果并入某个场景的入口区间。 */
function relax(entry, sceneId, min, max) {
  if (!entry.has(sceneId)) return false;
  const current = entry.get(sceneId);
  let changed = false;
  if (min < current.min) {
    current.min = min;
    changed = true;
  }
  if (max > current.max) {
    current.max = max;
    changed = true;
  }
  return changed;
}

/**
 * 处理一个场景：从传入的入口值出发，沿行读完，把可达的下一场入口区间并进 entry。
 * 返回是否有任何场景入口区间被更新。
 */
function propagate(scenes, entry, sceneId, startMin, startMax) {
  const scene = scenes.get(sceneId);
  if (!scene) return false;

  const lines = scene.lines || [];
  // 每行一个 [min,max]；choice 会有多个分支，逐个分支继续向下传。
  let states = [{ index: 0, min: startMin, max: startMax }];
  const seen = new Set();
  let changed = false;
  let guard = 0;

  while (states.length > 0) {
    if (++guard > 10000) {
      console.error(`x ${sceneId}: 行内传播没有收敛，疑似循环引用`);
      process.exit(2);
    }
    const state = states.pop();
    const key = `${state.index}|${state.min}|${state.max}`;
    if (seen.has(key)) continue;
    seen.add(key);

    if (state.index >= lines.length) {
      const next = scene.next;
      if (next && relax(entry, next, state.min, state.max)) changed = true;
      continue;
    }

    const line = lines[state.index];
    if (line.type === 'choice' && Array.isArray(line.options) && line.options.length > 0) {
      for (const option of line.options) {
        const delta = Number.isFinite(option.insomnia) ? option.insomnia : 0;
        const min = clamp(state.min + delta);
        const max = clamp(state.max + delta);
        if (option.next) {
          // 选项自己跳到另一个场景：直接并进那一场的入口区间。
          if (scenes.has(option.next)) {
            if (relax(entry, option.next, min, max)) changed = true;
          }
        } else {
          states.push({ index: state.index + 1, min, max });
        }
      }
      continue;
    }

    states.push({ index: state.index + 1, min: state.min, max: state.max });
  }

  return changed;
}

function solve(scenes) {
  // 入口区间：未知场景为 +∞ / -∞，便于取 min / max。
  const entry = new Map();
  for (const id of scenes.keys()) entry.set(id, { min: Infinity, max: -Infinity });

  entry.set(START_SCENE, { min: 0, max: 0 });

  for (let pass = 0; pass < MAX_PASSES; pass += 1) {
    let changed = false;
    for (const [id, range] of entry) {
      if (!Number.isFinite(range.min) && !Number.isFinite(range.max)) continue;
      if (range.min === Infinity) continue;
      if (propagate(scenes, entry, id, range.min, range.max)) changed = true;
    }
    if (!changed) return entry;
  }

  console.error('x 不动点未收敛（超过 200 轮），检查场景 next 是否成环');
  process.exit(2);
}

function main() {
  for (const stream of [process.stdout, process.stderr]) {
    try {
      stream.reconfigure({ encoding: 'utf8', errors: 'replace' });
    } catch (err) {
      /* 老 Node 不支持 reconfigure，忽略 */
    }
  }

  const scenes = loadScenes();
  if (scenes.size === 0) {
    console.error('x 没读到任何场景，检查 Assets/Resources/Story/');
    process.exit(2);
  }

  const entry = solve(scenes);
  const problems = [];
  const unreachable = [];

  // 起点自身固定为 0；「最高能到多少」要取所有场景入口区间的上界。
  let reachMax = 0;
  for (const range of entry.values()) {
    if (Number.isFinite(range.max) && range.max > reachMax) reachMax = range.max;
  }

  console.log(`场景总数: ${scenes.size}`);
  console.log(`起点 ${START_SCENE} 入口固定 0；全局最高入口失眠值 ${reachMax}（clamp 上限 100）`);
  console.log('');

  console.log('--- 后日谈入口失眠值（要求最低路径 < 80，否则这一场永远被「结局·失眠」盖掉）');
  for (const id of ENDING_SCENES) {
    if (!scenes.has(id)) {
      problems.push(`${id} 场景文件不存在`);
      console.log(`x ${id}: 场景文件不存在`);
      continue;
    }
    const range = entry.get(id);
    if (range.min === Infinity && range.max === -Infinity) {
      unreachable.push(id);
      problems.push(`${id} 从 ${START_SCENE} 走不到`);
      console.log(`x ${id}: 走不到（没有任何路径进入）`);
      continue;
    }
    // 判定标准是「存在一条不休息的走法」：只要最低路径 < 80，该结局就看得见；
    // 最高路径故意允许 >= 80（那是「结局·失眠」该出现的时候）。
    const ok = range.min < 80;
    if (!ok) problems.push(`${id} 入口失眠值最低 ${range.min} >= 80，被「结局·失眠」盖掉`);
    console.log(`${ok ? 'o' : 'x'} ${id}: 入口 ${range.min} ~ ${range.max}（最低路径 ${ok ? '可见' : '被盖掉'}）`);
  }

  console.log('');
  console.log('--- 关键节点入口失眠值');
  for (const id of KEY_NODES) {
    if (!scenes.has(id)) {
      console.log(`- ${id}: 不存在`);
      continue;
    }
    const range = entry.get(id);
    console.log(`- ${id}: ${range.min} ~ ${range.max}`);
  }

  console.log('');
  const capped = reachMax >= 80;
  if (!capped) problems.push(`完全不休息也到不了 80（全局最高 ${reachMax}），「结局·失眠」成为死内容`);
  console.log(`${capped ? 'o' : 'x'} 「结局·失眠」可达（全局最高入口失眠值 ${reachMax} >= 80）`);

  console.log('');
  if (problems.length > 0) {
    console.log(`共 ${problems.length} 条问题:`);
    for (const problem of problems) console.log(`  x ${problem}`);
    console.log('EXIT=1');
    process.exit(1);
  }
  console.log('五个后日谈的最低路径入口全部 < 80，且「结局·失眠」仍可达。');
  console.log('EXIT=0');
}

main();