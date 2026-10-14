#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""按 story/ASSETS.md 生成占位美术资源（背景 / 立绘 / CG）。

用途：剧本写完以后游戏要「看得见」。真正的美术还没生产，先用程序生成的
渐变占位图把 Assets/Resources/Art/**  填满，Unity 里 Resources.Load 就能
拿到真实贴图（而不是运行时那张 64x64 色块），画面比例和构图位置也提前定死。

- 背景 / CG：1920x1080，对应整屏
- 立绘：520x900，带透明底，脚部（底部 12%）透明，方便站在对话框后面

可重复运行：已存在且非空的文件默认跳过（想重画加 --force）。

用法：
    python story/tools/gen-placeholder-art.py
    python story/tools/gen-placeholder-art.py --force
"""
import argparse
import os
import re
import sys

from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
ASSETS_MD = os.path.join(ROOT, "story", "ASSETS.md")
ART_ROOT = os.path.join(ROOT, "Assets", "Resources", "Art")

BG_SIZE = (1920, 1080)
CG_SIZE = (1920, 1080)
PORTRAIT_SIZE = (520, 900)

CJK_FONT_CANDIDATES = [
    r"C:\Windows\Fonts\msyh.ttc",
    r"C:\Windows\Fonts\msyhbd.ttc",
    r"C:\Windows\Fonts\simhei.ttf",
    r"C:\Windows\Fonts\simsun.ttc",
]


def load_font(size):
    for path in CJK_FONT_CANDIDATES:
        if os.path.exists(path):
            try:
                return ImageFont.truetype(path, size)
            except OSError:
                continue
    return ImageFont.load_default()


def parse_assets():
    """返回 {section_kind: [(id, label), ...]}，kind 取 bg / portrait / cg。"""
    with open(ASSETS_MD, encoding="utf-8") as fh:
        text = fh.read()

    sections = {"bg": [], "portrait": [], "cg": []}
    current = None
    for line in text.splitlines():
        if line.startswith("## "):
            if "背景" in line:
                current = "bg"
            elif "立绘" in line:
                current = "portrait"
            elif "CG" in line:
                current = "cg"
            else:
                current = None
            continue
        if current is None:
            continue
        for token in re.findall(r"`([^`]+)`", line):
            if token == current:
                continue
            # 第 2 节是「| 林晓 | `linxiao_normal` `linxiao_tired` … |」这种多列写法
            if re.fullmatch(r"[a-z0-9_]+", token) and not token.endswith("_*"):
                sections[current].append(token)
                # 有用途描述时取描述当标签（表格第二列）
                if current != "portrait" and line.strip().startswith("|"):
                    cells = [c.strip() for c in line.strip().strip("|").split("|")]
                    if len(cells) >= 2 and token in cells[0]:
                        sections[current][-1] = (token, cells[1])

    # 立绘那节只取到 id，标签用 id 本身
    out = {}
    for kind, items in sections.items():
        seen = set()
        cleaned = []
        for it in items:
            if isinstance(it, tuple):
                sid, label = it
            else:
                sid, label = it, it
            if sid in seen:
                continue
            seen.add(sid)
            cleaned.append((sid, label))
        out[kind] = cleaned
    return out


def color_from_id(sid):
    """与运行时 AssetRegistry.ColorFromId 同一套哈希思路，保证占位色一致。"""
    h = 17
    for ch in sid:
        h = (h * 31 + ord(ch)) & 0xFFFFFFFF
    r = 46 + int((h & 0xFF) / 255 * 56)
    g = 46 + int(((h >> 8) & 0xFF) / 255 * 56)
    b = 61 + int(((h >> 16) & 0xFF) / 255 * 66)
    return r, g, b


def vertical_gradient(size, top, bottom):
    img = Image.new("RGB", size, top)
    draw = ImageDraw.Draw(img)
    w, h = size
    for y in range(h):
        t = y / max(h - 1, 1)
        color = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
        draw.line([(0, y), (w, y)], fill=color)
    return img


def draw_labels(img, sid, label, kind):
    draw = ImageDraw.Draw(img)
    w, h = img.size
    title_font = load_font(72 if kind != "portrait" else 40)
    small_font = load_font(40 if kind != "portrait" else 26)

    # 中央十字 + 边框，明确这是占位图而不是成品
    draw.rectangle([8, 8, w - 9, h - 9], outline=(255, 255, 255), width=3)
    cx, cy = w // 2, h // 2
    draw.line([(cx - 60, cy), (cx + 60, cy)], fill=(255, 255, 255), width=2)
    draw.line([(cx, cy - 60), (cx, cy + 60)], fill=(255, 255, 255), width=2)

    text = sid
    bbox = draw.textbbox((0, 0), text, font=title_font)
    draw.text(((w - (bbox[2] - bbox[0])) / 2, h * 0.30), text, font=title_font, fill=(255, 255, 255))

    if label and label != sid:
        bbox2 = draw.textbbox((0, 0), label, font=small_font)
        draw.text(((w - (bbox2[2] - bbox2[0])) / 2, h * 0.30 + (bbox[3] - bbox[1]) + 24),
                  label, font=small_font, fill=(230, 230, 230))

    tag = "占位图 PLACEHOLDER"
    bbox3 = draw.textbbox((0, 0), tag, font=small_font)
    draw.text(((w - (bbox3[2] - bbox3[0])) / 2, h * 0.86), tag, font=small_font, fill=(255, 220, 160))


def gen_background(sid, label, kind):
    base = color_from_id(sid)
    top = tuple(min(255, int(c * 0.55)) for c in base)
    bottom = tuple(min(255, int(c * 1.35)) for c in base)
    img = vertical_gradient(BG_SIZE, top, bottom)
    draw_labels(img, sid, label, kind)
    return img


def gen_portrait(sid, label):
    base = color_from_id(sid)
    size = PORTRAIT_SIZE
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    # 身体（梯形剪影）+ 头部圆
    draw = ImageDraw.Draw(img)
    w, h = size
    body_top = int(h * 0.30)
    foot = int(h * 0.88)
    draw.polygon(
        [(int(w * 0.30), body_top), (int(w * 0.70), body_top),
         (int(w * 0.86), foot), (int(w * 0.14), foot)],
        fill=(base[0], base[1], base[2], 235),
    )
    head_r = int(w * 0.19)
    cx, cy = w // 2, int(h * 0.20)
    draw.ellipse([cx - head_r, cy - head_r, cx + head_r, cy + head_r],
                 fill=(base[0], base[1], base[2], 245))
    draw.rectangle([8, 8, w - 9, h - 9], outline=(255, 255, 255, 200), width=3)
    draw_labels(img.convert("RGB"), sid, label, "portrait")
    return img.convert("RGBA")


def main():
    # Windows 控制台默认不是 UTF-8，先兜住，免得中文 print 直接把脚本炸在半路
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

    ap = argparse.ArgumentParser()
    ap.add_argument("--force", action="store_true", help="重画已存在的文件")
    args = ap.parse_args()

    assets = parse_assets()
    plan = [("bg", "Bg", gen_background), ("portrait", "Portrait", None), ("cg", "Cg", gen_background)]

    made = skipped = 0
    for kind, folder, _ in plan:
        items = assets.get(kind, [])
        if not items:
            print("[warn] ASSETS.md 里没解析到 %s 条目" % kind)
            continue
        out_dir = os.path.join(ART_ROOT, folder)
        os.makedirs(out_dir, exist_ok=True)
        for sid, label in items:
            path = os.path.join(out_dir, sid + ".png")
            if os.path.exists(path) and os.path.getsize(path) > 0 and not args.force:
                skipped += 1
                continue
            if kind == "portrait":
                img = gen_portrait(sid, label)
            else:
                img = gen_background(sid, label, kind)
            img.save(path)
            made += 1
        print("%-9s -> %s (%d 项)" % (folder, os.path.relpath(out_dir, ROOT), len(items)))

    print("\n生成 %d 张, 跳过 %d 张" % (made, skipped))
    print("注意：立绘是纯占位剪影；BGM/SFX 未生成（运行时静音，缺资源会被 ArtRegistry/AudioRegistry 记为 missing）。")
    return 0


if __name__ == "__main__":
    sys.exit(main())