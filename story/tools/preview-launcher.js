#!/usr/bin/env node
/**
 * preview-launcher.js —— 被 SEA 打包进 exe 的入口。
 *
 * 双击 exe 时：把内嵌的单文件试玩版写到固定路径的临时文件，再用默认浏览器打开它。
 * 路径固定（不是随机名）是刻意的 —— 浏览器按 file:// 路径隔离 localStorage，
 * 固定路径才能让「继续上次 / 存档」跨次保留。
 *
 * 支持 --selftest：只校验内嵌资源是否完好，不开浏览器（给构建脚本做验证用）。
 */
'use strict';

const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');

const ASSET = 'game.html';
const HTML_NAME = 'shiguang-zhihen-preview.html';

function readAsset() {
  try {
    const sea = require('node:sea');
    if (sea && typeof sea.isSea === 'function' && sea.isSea()) {
      return { html: sea.getAsset(ASSET, 'utf8'), source: 'embedded' };
    }
  } catch (err) { /* 不是 exe 运行时就走回退 */ }
  const sibling = path.join(__dirname, '时光之痕-试玩.html');
  if (fs.existsSync(sibling)) return { html: fs.readFileSync(sibling, 'utf8'), source: 'sibling' };
  return { html: null, source: 'none' };
}

const { html, source } = readAsset();
const bytes = html ? Buffer.byteLength(html, 'utf8') : 0;

if (process.argv.includes('--selftest')) {
  console.log('source=' + source + ' bytes=' + bytes);
  const scenes = html ? (html.match(/"sceneId"/g) || []).length : 0;
  console.log('sceneId 出现次数=' + scenes);
  if (!html || bytes < 100000 || scenes === 0) {
    console.error('[selftest] 内嵌资源不完整');
    process.exit(2);
  }
  console.log('[selftest] OK');
  process.exit(0);
}

if (!html) {
  console.error('读取不到内嵌的试玩页面（请用 story/tools/build-exe.js 重新打包）。');
  process.exit(1);
}

const out = path.join(os.tmpdir(), HTML_NAME);
fs.writeFileSync(out, html, 'utf8');

const { spawn } = require('node:child_process');
const child = spawn('cmd', ['/c', 'start', '', out], { detached: true, stdio: 'ignore', windowsHide: true });
child.unref();

console.log('《时光之痕》试玩版已在默认浏览器中打开。');
console.log('页面: ' + out);
console.log('存档保存在浏览器本地（同一个文件路径才会共用存档），关掉浏览器即可，本窗口可以关掉。');
console.log('提示: 网页版立绘/背景目前是程序生成的占位图，美术资源补进 Assets/Resources/Art 后重新打包即可替换。');