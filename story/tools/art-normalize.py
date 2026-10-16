#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""《时光之痕》出图归一化工具（订阅通道配套）。

DSH 的订阅图像通道由助手逐张驱动，产物文件名不受我们控制；本工具把它们按
art/manifest.json 的 id 与尺寸红线归一化，落到 art/out/{Bg,Cg,Portrait}/<id>.png，
再用 art-review.py 出审图表。

约定：原始图放在 --in 目录里，文件名必须是 <manifest id>.png（例如 bg_library_day.png）。

用法：
    python story/tools/art-normalize.py --in art/out/raw --out art/out
    python story/tools/art-normalize.py --in art/out/raw --out art/out --only alice --report

退出码：0 全部成功 / 2 参数、依赖或 manifest 问题 / 3 有文件没归一化成功。
"""

import argparse
import fnmatch
import json
import os
import sys

try:
    from PIL import Image, ImageChops, ImageFilter
except ImportError:  # pragma: no cover - 环境问题，不是逻辑分支
    sys.stderr.write("错误：未安装 Pillow，请先 pip install Pillow\n")
    sys.exit(2)

for _stream in (sys.stdout, sys.stderr):
    if hasattr(_stream, "reconfigure"):
        _stream.reconfigure(encoding="utf-8", errors="replace")

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
KIND_DIR = {"bg": "Bg", "cg": "Cg", "portrait": "Portrait"}
TARGET_SIZE = {"bg": (1920, 1080), "cg": (1920, 1080), "portrait": (900, 1300)}
CHROMA_TOLERANCE = 40          # 与 gen-art.js 保持一致的抠底容差
CORNER_SAMPLE = 8              # 四角各取 8x8 均值当背景色
MASK_BLUR = 1                  # 掩码羽化半径


def parse_args():
    ap = argparse.ArgumentParser(description="按 manifest 把原始出图归一化成红线尺寸")
    ap.add_argument("--in", dest="src", required=True, help="原始图目录（文件名 = <id>.png）")
    ap.add_argument("--out", dest="dst", required=True, help="输出根目录（下分 Bg/Cg/Portrait）")
    ap.add_argument("--manifest", default=os.path.join(ROOT, "art", "manifest.json"))
    ap.add_argument("--only", default="", help="只处理 kind / 角色 / id（与 gen-art.js 口径一致）")
    ap.add_argument("--force", action="store_true", help="覆盖已存在的目标文件")
    ap.add_argument("--report", action="store_true", help="逐条打印处理结果")
    return ap.parse_args()


def load_manifest(path):
    if not os.path.isfile(path):
        sys.stderr.write(f"错误：manifest 不存在 {path}\n")
        sys.exit(2)
    with open(path, "r", encoding="utf-8") as fh:
        data = json.load(fh)
    entries = data.get("entries") or []
    if not entries:
        sys.stderr.write("错误：manifest 的 entries 为空\n")
        sys.exit(2)
    return {e["id"]: e for e in entries}, data


def match(entry, only):
    if not only:
        return True
    return entry["kind"] == only or entry.get("character") == only or entry["id"] == only


def cover_crop(im, w, h):
    """等比放大后中心裁切到 w x h（背景与 CG 用）。"""
    src = im.convert("RGB")
    scale = max(w / src.width, h / src.height)
    src = src.resize((round(src.width * scale), round(src.height * scale)), Image.LANCZOS)
    left, top = (src.width - w) // 2, (src.height - h) // 2
    return src.crop((left, top, left + w, top + h))


def chroma_portrait(im, w, h, tol=CHROMA_TOLERANCE):
    """四角采样背景色抠掉转透明，等比放入 w x h 透明画布（立绘用）。"""
    src = im.convert("RGB")
    n = CORNER_SAMPLE
    spots = [(0, 0), (src.width - n, 0), (0, src.height - n), (src.width - n, src.height - n)]
    corners = [src.crop((x, y, x + n, y + n)).resize((1, 1), Image.LANCZOS).getpixel((0, 0)) for x, y in spots]
    bgc = tuple(sum(c[i] for c in corners) // len(corners) for i in range(3))
    diff = ImageChops.difference(src, Image.new("RGB", src.size, bgc)).convert("L")
    mask = diff.point(lambda p: 0 if p < tol else 255).filter(ImageFilter.GaussianBlur(MASK_BLUR))
    scale = min(w / src.width, h / src.height)
    src = src.resize((round(src.width * scale), round(src.height * scale)), Image.LANCZOS)
    mask = mask.resize(src.size, Image.LANCZOS)
    canvas = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    canvas.paste(src, ((w - src.width) // 2, (h - src.height) // 2), mask)
    return canvas


def process_one(src_path, entry, dst_root, force, verbose):
    stem = os.path.splitext(os.path.basename(src_path))[0]
    kind = entry["kind"]
    w, h = TARGET_SIZE[kind]
    dst_dir = os.path.join(dst_root, KIND_DIR[kind])
    dst_path = os.path.join(dst_dir, f"{stem}.png")
    if os.path.exists(dst_path) and not force:
        return ("skip", dst_path)
    os.makedirs(dst_dir, exist_ok=True)
    with Image.open(src_path) as im:
        out = chroma_portrait(im, w, h) if kind == "portrait" else cover_crop(im, w, h)
    out.save(dst_path)
    if verbose:
        print(f"  {stem}: {kind} -> {os.path.relpath(dst_path, ROOT)}（{w}x{h}）")
    return ("ok", dst_path)


def main():
    args = parse_args()
    if not os.path.isdir(args.src):
        sys.stderr.write(f"错误：原始图目录不存在 {args.src}\n")
        return 2
    index, manifest = load_manifest(args.manifest)
    files = sorted(fnmatch.filter(os.listdir(args.src), "*.png"))
    if not files:
        sys.stderr.write(f"错误：{args.src} 里没有 PNG（文件名必须等于 manifest 的 id）\n")
        return 3

    done, skipped, missing, failed = 0, 0, [], []
    for name in files:
        stem = os.path.splitext(name)[0]
        entry = index.get(stem)
        if entry is None:
            missing.append(stem)
            continue
        if not match(entry, args.only):
            skipped += 1
            continue
        try:
            state, _ = process_one(os.path.join(args.src, name), entry, args.dst, args.force, args.report)
            done += 1 if state == "ok" else 0
            skipped += 1 if state == "skip" else 0
        except (OSError, ValueError) as exc:
            failed.append(f"{stem}（{exc}）")

    print(f"归一化完成：成功 {done} 张 / 跳过 {skipped} 张（已存在或不属 --only 范围）")
    if missing:
        print(f"  manifest 里没有这些 id：{', '.join(missing)}")
    if failed:
        print(f"  处理失败：{', '.join(failed)}")
    print(f"  输出根目录：{os.path.relpath(args.dst, ROOT)}，接着用 art-review.py 出审图表")
    return 3 if (missing or failed) else 0


if __name__ == "__main__":
    sys.exit(main())
