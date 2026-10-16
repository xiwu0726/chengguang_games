#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""《时光之痕》美术审图表生成器。

把一批 PNG 拼成带 id 标注的网格图，供人工逐组审图（正式出图后先看这张表，
再决定替换 Assets/Resources/Art 里的占位图）。

用法：
    python story/tools/art-review.py --dir art/out/Bg --out art/out/_review/bg.png
    python story/tools/art-review.py --dir art/out/Portrait --filter "alice_*" --out art/out/_review/alice.png

退出码：0 成功 / 2 参数或依赖问题 / 3 目录里没有匹配的图。
"""

import argparse
import fnmatch
import glob
import os
import sys

try:
    from PIL import Image, ImageDraw, ImageFont
except ImportError:  # pragma: no cover - 环境问题，不是逻辑分支
    sys.stderr.write("错误：未安装 Pillow，请先 pip install Pillow\n")
    sys.exit(2)

# Windows 控制台默认 cp1252，打印中文会 UnicodeEncodeError，先把输出流固定成 UTF-8
for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        _stream.reconfigure(encoding="utf-8", errors="replace")

DEFAULT_COLS = 5
CELL_W = 368
CELL_H = 260
LABEL_H = 20
PAD = 8
SHEET_BG = (26, 30, 44)          # 深夜蓝底，透明立绘在这上面才看得出边缘
ALPHA_FILL = (26, 30, 44)          # RGBA 图先垫这层再缩放
LABEL_FG = (216, 184, 120)         # 与播放器金色一致
FONT_CANDIDATES = [
    r"C:\Windows\Fonts\msyh.ttc",
    r"C:\Windows\Fonts\segoeui.ttf",
]
FONT_SIZE = 14


def parse_args():
    ap = argparse.ArgumentParser(description="把一批 PNG 拼成带 id 标注的审图表")
    ap.add_argument("--dir", required=True, help="要扫描的 PNG 目录")
    ap.add_argument("--out", required=True, help="审图表输出路径（PNG）")
    ap.add_argument("--filter", default="", help="只收文件名匹配该 glob 的图，例如 alice_*")
    ap.add_argument("--cols", type=int, default=DEFAULT_COLS, help=f"每行列数（默认 {DEFAULT_COLS}）")
    ap.add_argument("--cell-w", type=int, default=CELL_W, help=f"单元格宽（默认 {CELL_W}）")
    ap.add_argument("--cell-h", type=int, default=CELL_H, help=f"单元格高（默认 {CELL_H}）")
    ap.add_argument("--title", default="", help="表头标题文字")
    return ap.parse_args()


def load_font(size):
    for path in FONT_CANDIDATES:
        if os.path.exists(path):
            try:
                return ImageFont.truetype(path, size)
            except OSError:
                continue
    return ImageFont.load_default()


def collect(dir_path, pattern):
    files = sorted(glob.glob(os.path.join(dir_path, "*.png")))
    if pattern:
        files = [f for f in files if fnmatch.fnmatch(os.path.basename(f), pattern)]
    return files


def compose_cell(im, cell_w, cell_h):
    """等比缩放进单元格，垂直居中，横向留白由底色填充。"""
    if im.mode == "P":  # 调色板图（占位图重量化后常见）先转 RGBA 再处理透明
        im = im.convert("RGBA")
    src = im.convert("RGBA") if im.mode == "RGBA" else im.convert("RGB")
    if src.mode == "RGBA":
        base = Image.new("RGB", src.size, ALPHA_FILL)
        base.paste(src, mask=src.split()[-1])
        src = base
    scale = min(cell_w / src.width, cell_h / src.height)
    size = (max(1, round(src.width * scale)), max(1, round(src.height * scale)))
    return src.resize(size, Image.LANCZOS)


def build_sheet(files, out_path, cols, cell_w, cell_h, title):
    font = load_font(FONT_SIZE)
    head_h = LABEL_H * 2 if title else 0
    rows = (len(files) + cols - 1) // cols
    width = cols * cell_w + (cols + 1) * PAD
    height = head_h + rows * (cell_h + LABEL_H) + (rows + 1) * PAD
    sheet = Image.new("RGB", (width, height), SHEET_BG)
    draw = ImageDraw.Draw(sheet)
    y = PAD
    if title:
        draw.text((PAD, y), title, font=font, fill=LABEL_FG)
        y += LABEL_H * 2
    for idx, path in enumerate(files):
        r, c = divmod(idx, cols)
        x = PAD + c * (cell_w + PAD)
        top = y + r * (cell_h + LABEL_H + PAD)
        try:
            cell = compose_cell(Image.open(path), cell_w, cell_h)
        except OSError as exc:
            draw.text((x, top), f"读取失败 {os.path.basename(path)}", font=font, fill=(220, 90, 90))
            sys.stderr.write(f"警告：{os.path.basename(path)} 读不出来（{exc}）\n")
            continue
        ox = x + (cell_w - cell.width) // 2
        sheet.paste(cell, (ox, top))
        name = os.path.splitext(os.path.basename(path))[0]
        draw.text((x + 2, top + cell_h + 2), name, font=font, fill=LABEL_FG)
    os.makedirs(os.path.dirname(os.path.abspath(out_path)) or ".", exist_ok=True)
    sheet.save(out_path)
    return sheet.size


def main():
    args = parse_args()
    if not os.path.isdir(args.dir):
        sys.stderr.write(f"错误：目录不存在 {args.dir}\n")
        return 2
    files = collect(args.dir, args.filter)
    if not files:
        sys.stderr.write(f"错误：{args.dir} 里没有匹配 {args.filter or '*.png'} 的图片\n")
        return 3
    size = build_sheet(files, args.out, max(1, args.cols), args.cell_w, args.cell_h, args.title)
    print(f"审图表：{args.out}（{size[0]}x{size[1]}，{len(files)} 张，每行 {max(1, args.cols)} 列）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
