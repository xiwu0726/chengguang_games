#!/usr/bin/env node
/**
 * build-exe.js —— 把「单文件试玩版」打成**一个双击就能玩的 Windows exe**（不需要 Unity、不需要装 Node）。
 *
 * 原理：Node 的 Single Executable Application。把试玩页面作为 SEA asset 内嵌进一份 node.exe 的副本，
 * 用 postject 写入 blob。产物是 `dist/时光之痕-试玩.exe`，双击后用默认浏览器打开游戏。
 *
 * 为什么先做这个：Unity 必须由人工登录账号激活许可证才能出包，那一步我代替不了；
 * 这个 exe 让「打开就能玩」现在就能成立，剧本变多以后重跑一次即可更新。
 *
 * 用法: node story/tools/build-exe.js
 * 依赖: 本机 node（>=20）与能联网的 npx（用于取 postject，首次会下载）。
 */
'use strict';

const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawnSync } = require('node:child_process');

const ROOT = path.join(__dirname, '..', '..');
const PREVIEW_HTML = path.join(ROOT, 'preview', '时光之痕-试玩.html');
const LAUNCHER = path.join(__dirname, 'preview-launcher.js');
const OUT_DIR = path.join(ROOT, 'dist');
const OUT_EXE = path.join(OUT_DIR, '时光之痕-试玩.exe');
const BUILD_DIR = path.join(os.tmpdir(), 'sgt-sea-build');
const SENTINEL = 'NODE_SEA_FUSE_fce680ab2cc467b6e072b8b5df1996b2';

function run(cmd, args, opts) {
  const res = spawnSync(cmd, args, Object.assign({ stdio: 'inherit', shell: false }, opts || {}));
  if (res.error) throw res.error;
  return res.status;
}

/** Windows 上 .cmd（npx.cmd）不能直接 spawn（Node 24 的 EINVAL 防护），必须过一层 shell。 */
function runShell(cmdline) {
  const res = spawnSync(cmdline, [], { stdio: 'inherit', shell: true });
  if (res.error) throw res.error;
  return res.status;
}

function main() {
  if (process.platform !== 'win32') {
    console.error('这个打包脚本目前只按 Windows 写（postject 的 PE 流程）。');
    process.exit(1);
  }

  // 1) 先保证试玩页面是最新的
  console.log('[1/5] 重新生成单文件试玩版…');
  if (run(process.execPath, [path.join(__dirname, 'build-preview.js'), '--standalone']) !== 0) {
    console.error('生成 preview 失败'); process.exit(1);
  }
  if (!fs.existsSync(PREVIEW_HTML)) { console.error('找不到 ' + PREVIEW_HTML); process.exit(1); }
  const htmlBytes = fs.statSync(PREVIEW_HTML).size;

  // 2) 准备构建目录（launcher + sea 配置 + blob）
  console.log('[2/5] 准备 SEA 构建目录…');
  fs.mkdirSync(BUILD_DIR, { recursive: true });
  fs.mkdirSync(OUT_DIR, { recursive: true });
  const blob = path.join(BUILD_DIR, 'sea-prep.blob');
  const cfgPath = path.join(BUILD_DIR, 'sea-config.json');
  const cfg = {
    main: LAUNCHER,
    output: blob,
    disableExperimentalSEAWarning: true,
    useSnapshot: false,
    useCodeCache: false,
    assets: { 'game.html': PREVIEW_HTML }
  };
  fs.writeFileSync(cfgPath, JSON.stringify(cfg, null, 2), 'utf8');

  // 3) 生成 blob
  console.log('[3/5] 生成 SEA blob…');
  if (run(process.execPath, ['--experimental-sea-config', cfgPath]) !== 0) {
    console.error('生成 blob 失败'); process.exit(1);
  }
  if (!fs.existsSync(blob)) { console.error('blob 没生成'); process.exit(1); }

  // 4) 复制 node.exe 并注入
  console.log('[4/5] 复制运行时并注入 (postject 首次会联网下载)…');
  fs.copyFileSync(process.execPath, OUT_EXE);
  const postjectArgs = [OUT_EXE, 'NODE_SEA_BLOB', blob, '--sentinel-fuse', SENTINEL];
  const quote = (s) => '"' + s + '"';
  const cmdline = ['npx', '--yes', 'postject'].concat(postjectArgs.map(quote)).join(' ');
  const code = runShell(cmdline);
  if (code !== 0) { console.error('postject 注入失败（退出码 ' + code + '）'); process.exit(1); }

  // 5) 自检：不打开浏览器的前提下确认内嵌资源可用
  console.log('[5/5] 自检 exe…');
  const check = spawnSync(OUT_EXE, ['--selftest'], { encoding: 'utf8' });
  const out = (check.stdout || '') + (check.stderr || '');
  console.log(out.trim());
  const ok = check.status === 0 && /\[selftest\] OK/.test(out);
  const mb = (fs.statSync(OUT_EXE).size / 1024 / 1024).toFixed(0);
  console.log('\n产物: ' + path.relative(ROOT, OUT_EXE) + '  (' + mb + ' MB, 内嵌页面 ' +
    (htmlBytes / 1024 / 1024).toFixed(2) + ' MB)');
  console.log(ok ? '自检通过：双击即可在浏览器里试玩。' : '⚠ 自检未通过，请检查上面的输出。');
  process.exit(ok ? 0 : 1);
}

main();