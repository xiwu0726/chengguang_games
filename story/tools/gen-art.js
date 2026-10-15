#!/usr/bin/env node
// 《时光之痕》正式美术批量出图工具。
// 默认 --dry-run：只校验 art/manifest.json，不联网、不写图。
// --generate：按环境变量提供的图像生成 key 真实出图；缺 key 时中文报错并退出码非 0。
// 用法、命名与尺寸红线、后处理依赖见 art/出图流水线.md。
'use strict';
const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');

const ROOT = path.resolve(__dirname, '..', '..');
const KIND_DIR = { bg: 'Bg', cg: 'Cg', portrait: 'Portrait' };
const EXPECTED_SIZE = { bg: '1920x1080', cg: '1920x1080', portrait: '900x1300' };
const KNOWN_CHARACTERS = ['linxiao', 'alice', 'raine', 'xya', 'lord', 'zhou', 'vera'];
const PROVIDERS = {
  google: { keyEnv: 'GOOGLE_GEMINI_API_KEY', defaultModel: 'gemini-2.0-flash-preview-image-generation' },
  openai: { keyEnv: 'OPENAI_API_KEY', defaultModel: 'gpt-image-1' },
};
const RETRY_MAX = 3;
const BACKOFF_BASE_MS = 2000;
const CHROMA_TOLERANCE = 40;
const ERROR_BODY_LIMIT = 300;
const HEX_COLOR = /^#[0-9a-fA-F]{6}$/;

// ---------- 命令行 ----------
function parseArgs(argv) {
  const opts = { mode: 'dry-run', only: '', out: '', manifest: path.join(ROOT, 'art', 'manifest.json'), force: false, usedOnly: false, help: false };
  for (let i = 0; i < argv.length; i += 1) {
    const a = argv[i];
    if (a === '--dry-run') opts.mode = 'dry-run';
    else if (a === '--generate') opts.mode = 'generate';
    else if (a === '--only') opts.only = argv[++i] || '';
    else if (a === '--out') opts.out = argv[++i] || '';
    else if (a === '--manifest') opts.manifest = path.resolve(ROOT, argv[++i] || '');
    else if (a === '--force') opts.force = true;
    else if (a === '--used-only') opts.usedOnly = true;
    else if (a === '--help' || a === '-h') opts.help = true;
    else fail(`未知参数：${a}（用 --help 查看用法）`);
  }
  return opts;
}

function printHelp() {
  console.log(`用法：node story/tools/gen-art.js [选项]
  --dry-run          校验 manifest 并打印统计（默认，不联网、不写图）
  --generate         真实出图（需环境变量 DSH_IMAGE_PROVIDER=google|openai 与对应 key）
  --only <值>        只处理 kind（bg/cg/portrait）、角色（如 linxiao）或单个 id
  --out <目录>       输出根目录（默认 Assets/Resources/Art/，下分 Bg/Cg/Portrait）
  --manifest <路径>  指定 manifest 文件（默认 art/manifest.json）
  --used-only        只处理剧本实际引用的条目（used: true）
  --force            覆盖已存在的目标文件
key 环境变量：google → GOOGLE_GEMINI_API_KEY；openai → OPENAI_API_KEY；模型可用 DSH_IMAGE_MODEL 覆盖。`);
}

function fail(message, code = 1) {
  console.error(`错误：${message}`);
  process.exit(code);
}

// ---------- manifest 校验 ----------
function validateManifest(manifest) {
  const errors = [];
  if (!manifest || !Array.isArray(manifest.entries) || manifest.entries.length === 0) {
    return ['manifest 缺少非空 entries 数组'];
  }
  const seen = new Set();
  for (const e of manifest.entries) errors.push(...validateEntry(e, seen));
  return errors;
}

function validateEntry(e, seen) {
  const errors = [];
  const where = e && e.id ? `条目 ${e.id}` : '条目 ' + JSON.stringify(e).slice(0, 40);
  if (!e || typeof e !== 'object') return [`${where} 不是对象`];
  if (typeof e.id !== 'string' || !/^[a-z0-9_]+$/.test(e.id)) errors.push(`${where}：id 缺失或含非法字符`);
  else if (seen.has(e.id)) errors.push(`${where}：id 重复`);
  else seen.add(e.id);
  if (!KIND_DIR[e.kind]) errors.push(`${where}：kind 必须是 bg/cg/portrait，当前为 ${JSON.stringify(e.kind)}`);
  if (EXPECTED_SIZE[e.kind] && e.size !== EXPECTED_SIZE[e.kind]) {
    errors.push(`${where}：size 应为 ${EXPECTED_SIZE[e.kind]}，当前为 ${JSON.stringify(e.size)}`);
  }
  if (e.kind && e.id && e.target !== `Assets/Resources/Art/${KIND_DIR[e.kind]}/${e.id}.png`) {
    errors.push(`${where}：target 必须落在 Art/${KIND_DIR[e.kind]}/ 下且文件名为 <id>.png，当前为 ${JSON.stringify(e.target)}`);
  }
  if (typeof e.prompt !== 'string' || !e.prompt.trim()) errors.push(`${where}：prompt 为空`);
  if (typeof e.negative !== 'string' || !e.negative.trim()) errors.push(`${where}：negative 为空`);
  if (!Array.isArray(e.palette) || e.palette.some((c) => !HEX_COLOR.test(c))) errors.push(`${where}：palette 必须是非空十六进制色值数组`);
  if (e.kind === 'portrait') {
    if (e.transparent !== true) errors.push(`${where}：立绘必须 transparent: true`);
    if (!KNOWN_CHARACTERS.includes(e.character)) errors.push(`${where}：立绘 character 必须是 ${KNOWN_CHARACTERS.join('/')} 之一`);
    if (typeof e.consistency !== 'string' || !e.consistency.trim()) errors.push(`${where}：立绘缺少 consistency 一致性说明`);
  }
  return errors;
}

// ---------- 过滤与统计 ----------
function filterEntries(entries, only) {
  if (!only) return entries;
  const hit = entries.filter((e) => e.kind === only || e.character === only || e.id === only);
  if (hit.length === 0) fail(`--only ${only} 没有匹配到任何条目（可用 kind/角色名/id）`);
  return hit;
}

function groupCount(entries, keyFn) {
  const map = new Map();
  for (const e of entries) map.set(keyFn(e), (map.get(keyFn(e)) || 0) + 1);
  return [...map.entries()].sort((a, b) => b[1] - a[1]);
}

function printDryRun(entries, manifest, outBase) {
  const used = entries.filter((e) => e.used);
  console.log(`manifest 校验通过：${path.relative(ROOT, manifest.__file)} 共 ${entries.length} 条`);
  console.log(`  剧本引用 ${used.length} 条 / 未引用 ${entries.length - used.length} 条`);
  for (const [kind, n] of groupCount(entries, (e) => e.kind)) console.log(`  kind=${kind}: ${n} 张`);
  console.log('  按角色（立绘）：');
  for (const [ch, n] of groupCount(entries.filter((e) => e.kind === 'portrait'), (e) => e.character)) {
    const name = (manifest.characters[ch] && manifest.characters[ch].name) || ch;
    console.log(`    ${name}(${ch}): ${n} 张`);
  }
  const noSource = entries.filter((e) => !e.source);
  console.log(`  缺依据（source 为空，均为补写）：${noSource.length} 条`);
  const solid = entries.filter((e) => e.solidColor);
  if (solid.length) console.log(`  本地纯色生成（不耗 API）：${solid.map((e) => e.id).join(', ')}`);
  console.log(`  输出根目录：${path.relative(ROOT, outBase) || outBase}`);
  const tools = detectPostProcess();
  console.log(`  后处理依赖：${tools.summary}`);
  if (!tools.pillow && used.length) {
    console.log('  警告：未探测到 Pillow，--generate 的后处理将不可用（详见 art/出图流水线.md）');
  }
}

function resolveOutBase(opts) {
  return opts.out ? path.resolve(ROOT, opts.out) : path.join(ROOT, 'Assets', 'Resources', 'Art');
}

function outDirFor(outBase, kind) {
  return path.join(outBase, KIND_DIR[kind]);
}

// ---------- 后处理依赖探测 ----------
function detectPostProcess() {
  const python = probePython();
  const magickProbe = spawnSync('magick', ['--version'], { encoding: 'utf8' });
  const magickOk = magickProbe.status === 0 && (magickProbe.stdout || '').includes('ImageMagick');
  const parts = [];
  if (python) parts.push(`Pillow 可用（${python} + PIL）`);
  if (magickOk) parts.push('ImageMagick 可用');
  return { pillow: Boolean(python), python, magick: magickOk, summary: parts.join('；') || '无（Pillow 与 ImageMagick 均未探测到）' };
}

function probePython() {
  for (const exe of ['python', 'py']) {
    const version = spawnSync(exe, ['--version'], { encoding: 'utf8' });
    if (version.status !== 0) continue;
    const pil = spawnSync(exe, ['-c', 'import PIL'], { encoding: 'utf8' });
    if (pil.status === 0) return exe;
  }
  return null;
}

// ---------- Pillow 后处理（cover 裁切 / 立绘抠底转透明） ----------
const PY_POSTPROCESS = `
import sys
from PIL import Image, ImageChops, ImageFilter
mode, src, dst, w, h = sys.argv[1], sys.argv[2], sys.argv[3], int(sys.argv[4]), int(sys.argv[5])
if mode == 'solid':
    Image.new('RGB', (w, h), sys.argv[6]).save(dst)
elif mode == 'cover':
    im = Image.open(src).convert('RGB')
    s = max(w / im.width, h / im.height)
    im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)
    left, top = (im.width - w) // 2, (im.height - h) // 2
    im.crop((left, top, left + w, top + h)).save(dst)
elif mode == 'portrait':
    tol = int(sys.argv[6])
    im = Image.open(src).convert('RGB')
    corners = []
    n = 8
    for x, y in [(0, 0), (im.width - n, 0), (0, im.height - n), (im.width - n, im.height - n)]:
        patch = im.crop((x, y, x + n, y + n)).resize((1, 1), Image.LANCZOS).getpixel((0, 0))
        corners.append(patch)
    bgc = tuple(sum(c[i] for c in corners) // len(corners) for i in range(3))
    diff = ImageChops.difference(im, Image.new('RGB', im.size, bgc)).convert('L')
    mask = diff.point(lambda p: 0 if p < tol else 255).filter(ImageFilter.GaussianBlur(1))
    s = min(w / im.width, h / im.height)
    im = im.resize((round(im.width * s), round(im.height * s)), Image.LANCZOS)
    mask = mask.resize(im.size, Image.LANCZOS)
    canvas = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    canvas.paste(im, ((w - im.width) // 2, (h - im.height) // 2), mask)
    canvas.save(dst)
else:
    sys.exit('unknown mode')
`;

function runPostProcess(python, args) {
  const run = spawnSync(python, ['-c', PY_POSTPROCESS, ...args], { encoding: 'utf8' });
  if (run.status !== 0) {
    fail(`后处理失败（${args[0]} ${args[1]}）：${(run.stderr || run.stdout).trim().slice(0, ERROR_BODY_LIMIT)}`);
  }
}

// ---------- 真实出图：provider 调用 ----------
function requireProvider() {
  const name = process.env.DSH_IMAGE_PROVIDER || '';
  if (!PROVIDERS[name]) fail('未设置 DSH_IMAGE_PROVIDER（可选 google / openai），真实出图无法进行', 2);
  const key = process.env[PROVIDERS[name].keyEnv] || '';
  if (!key) fail(`缺少环境变量 ${PROVIDERS[name].keyEnv}，真实出图无法进行（本工具不硬编码、不代存任何 key）`, 2);
  const model = process.env.DSH_IMAGE_MODEL || PROVIDERS[name].defaultModel;
  return { name, key, model };
}

function sanitize(text, key) {
  return String(text).split(key).join('***').slice(0, ERROR_BODY_LIMIT);
}

async function fetchWithRetry(url, init, key) {
  let lastError = '未知错误';
  for (let attempt = 1; attempt <= RETRY_MAX; attempt += 1) {
    try {
      const res = await fetch(url, init);
      if (res.ok) return res;
      const body = sanitize(await res.text(), key);
      if (res.status !== 429 && res.status < 500) throw new Error(`HTTP ${res.status}：${body}`);
      lastError = `HTTP ${res.status}：${body}`;
    } catch (err) {
      if (err instanceof Error && err.message.startsWith('HTTP ')) throw err;
      lastError = String(err.message || err);
    }
    if (attempt < RETRY_MAX) await sleep(BACKOFF_BASE_MS * 2 ** (attempt - 1));
  }
  throw new Error(`重试 ${RETRY_MAX} 次后仍失败：${lastError}`);
}

function sleep(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function generateImage(provider, entry, refPath) {
  const fullPrompt = `${entry.prompt}。画面中不得出现以下内容：${entry.negative}`;
  if (provider.name === 'google') return generateGoogle(provider, fullPrompt, entry, refPath);
  return generateOpenai(provider, fullPrompt, entry, refPath);
}

async function generateGoogle(provider, fullPrompt, entry, refPath) {
  const parts = [{ text: fullPrompt }];
  if (refPath) {
    parts.push({ inlineData: { mimeType: 'image/png', data: fs.readFileSync(refPath).toString('base64') } });
    parts.push({ text: '请以参考图中的同一名角色为准：保持同一张脸、发型发色、瞳色、服装与体型完全一致，只改变表情与姿态细节。' });
  }
  const endpoint = new URL(`https://generativelanguage.googleapis.com/v1beta/models/${provider.model}:generateContent`);
  endpoint.searchParams.set('key', provider.key);
  const res = await fetchWithRetry(endpoint.toString(), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ contents: [{ parts }], generationConfig: { responseModalities: ['TEXT', 'IMAGE'] } }),
  }, provider.key);
  const json = await res.json();
  const image = (json.candidates?.[0]?.content?.parts || []).find((p) => p.inlineData?.data);
  if (!image) throw new Error(`响应中没有图片：${sanitize(JSON.stringify(json).slice(-200), provider.key)}`);
  return Buffer.from(image.inlineData.data, 'base64');
}

function openaiSize(entry) {
  return entry.kind === 'portrait' ? '1024x1536' : '1536x1024';
}

async function generateOpenai(provider, fullPrompt, entry, refPath) {
  const headers = { Authorization: `Bearer ${provider.key}` };
  let res;
  if (refPath) {
    const form = new FormData();
    form.append('model', provider.model);
    form.append('prompt', `${fullPrompt}。以参考图为同一名角色，保持脸部、发型发色、瞳色、服装完全一致，只改表情。`);
    form.append('size', openaiSize(entry));
    form.append('image', new Blob([fs.readFileSync(refPath)], { type: 'image/png' }), 'base.png');
    res = await fetchWithRetry('https://api.openai.com/v1/images/edits', { method: 'POST', headers, body: form }, provider.key);
  } else {
    res = await fetchWithRetry('https://api.openai.com/v1/images/generations', {
      method: 'POST',
      headers: { ...headers, 'Content-Type': 'application/json' },
      body: JSON.stringify({ model: provider.model, prompt: fullPrompt, size: openaiSize(entry) }),
    }, provider.key);
  }
  const json = await res.json();
  const b64 = json.data?.[0]?.b64_json;
  if (!b64) throw new Error(`响应中没有图片：${sanitize(JSON.stringify(json).slice(-200), provider.key)}`);
  return Buffer.from(b64, 'base64');
}

// ---------- 真实出图主流程 ----------
function portraitOrder(entries, manifest) {
  const baseIds = new Set(Object.values(manifest.characters || {}).map((c) => c.basePortrait));
  const bases = entries.filter((e) => e.kind !== 'portrait' || baseIds.has(e.id));
  const variants = entries.filter((e) => e.kind === 'portrait' && !baseIds.has(e.id));
  return [...bases, ...variants];
}

async function runGenerate(entries, manifest, outBase, force) {
  const tools = detectPostProcess();
  if (!tools.pillow) fail('后处理依赖 Pillow 未安装（python -c "import PIL" 失败），无法保证尺寸与抠底红线，已中止', 2);
  const provider = requireProvider();
  const tmpDir = path.join(ROOT, 'art', '.tmp-gen');
  fs.mkdirSync(tmpDir, { recursive: true });
  fs.mkdirSync(outBase, { recursive: true });
  const failures = [];
  let done = 0;
  let skipped = 0;
  for (const entry of portraitOrder(entries, manifest)) {
    const finalPath = path.join(outDirFor(outBase, entry.kind), `${entry.id}.png`);
    fs.mkdirSync(path.dirname(finalPath), { recursive: true });
    if (fs.existsSync(finalPath) && !force) {
      skipped += 1;
      continue;
    }
    try {
      await produceOne(entry, manifest, provider, tools, tmpDir, finalPath);
      done += 1;
      console.log(`完成 ${entry.id} → ${path.relative(ROOT, finalPath)}`);
    } catch (err) {
      failures.push(`${entry.id}: ${sanitize(err.message, provider.key)}`);
      console.error(`失败 ${entry.id}: ${sanitize(err.message, provider.key)}`);
    }
  }
  fs.rmSync(tmpDir, { recursive: true, force: true });
  reportGenerate(done, skipped, failures);
}

async function produceOne(entry, manifest, provider, tools, tmpDir, finalPath) {
  const [w, h] = entry.size.split('x').map(Number);
  if (entry.solidColor) {
    runPostProcess(tools.python, ['solid', '-', finalPath, String(w), String(h), entry.solidColor]);
    return;
  }
  const refPath = resolveReference(entry, manifest, finalPath);
  const raw = await generateImage(provider, entry, refPath);
  const rawPath = path.join(tmpDir, `${entry.id}.raw.png`);
  fs.writeFileSync(rawPath, raw);
  if (entry.kind === 'portrait') {
    runPostProcess(tools.python, ['portrait', rawPath, finalPath, String(w), String(h), String(CHROMA_TOLERANCE)]);
  } else {
    runPostProcess(tools.python, ['cover', rawPath, finalPath, String(w), String(h)]);
  }
  fs.rmSync(rawPath, { force: true });
}

// 立绘变体必须以该角色基准图为参考图（同一张脸）；基准图本身不带参考。
function resolveReference(entry, manifest, finalPath) {
  if (entry.kind !== 'portrait') return null;
  const ch = manifest.characters && manifest.characters[entry.character];
  if (!ch) throw new Error(`立绘 ${entry.id} 的 character=${entry.character} 不在 manifest.characters 中`);
  if (ch.basePortrait === entry.id) return null;
  const basePath = path.join(path.dirname(finalPath), `${ch.basePortrait}.png`);
  if (!fs.existsSync(basePath)) {
    throw new Error(`找不到角色基准图 ${path.relative(ROOT, basePath)}；请先出基准图，或用 --only ${entry.character} 整组生成`);
  }
  return basePath;
}

function reportGenerate(done, skipped, failures) {
  console.log(`\n生成 ${done} 张，跳过已存在 ${skipped} 张（--force 可覆盖），失败 ${failures.length} 张`);
  for (const f of failures) console.error(`  - ${f}`);
  if (failures.length) process.exit(3);
}

// ---------- 入口 ----------
async function main() {
  const opts = parseArgs(process.argv.slice(2));
  if (opts.help) {
    printHelp();
    return;
  }
  if (!fs.existsSync(opts.manifest)) fail(`找不到 manifest：${path.relative(ROOT, opts.manifest)}`);
  let manifest;
  try {
    manifest = JSON.parse(fs.readFileSync(opts.manifest, 'utf8'));
  } catch (err) {
    fail(`manifest 不是合法 JSON：${err.message}`);
  }
  manifest.__file = opts.manifest;
  const errors = validateManifest(manifest);
  if (errors.length) {
    console.error(`manifest 校验失败，共 ${errors.length} 处：`);
    for (const e of errors) console.error(`  - ${e}`);
    process.exit(1);
  }
  let entries = filterEntries(manifest.entries, opts.only);
  if (opts.usedOnly) entries = entries.filter((e) => e.used);
  if (opts.usedOnly && entries.length === 0) fail('--used-only 没有匹配到任何条目');
  const outBase = resolveOutBase(opts);
  if (opts.mode === 'generate') {
    await runGenerate(entries, manifest, outBase, opts.force);
  } else {
    printDryRun(entries, manifest, outBase);
  }
}

main().catch((err) => fail(err.stack || String(err)));
