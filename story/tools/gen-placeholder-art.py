#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""按 story/ASSETS.md 生成占位美术资源（背景 / 立绘 / CG）。

用途：剧本写完以后游戏要「看得见」。真正的美术还没生产，先用程序生成的
氛围占位图把 Assets/Resources/Art/** 填满，Unity 里 Resources.Load 就能拿到
真实贴图，画面比例和构图位置也提前定死。

和旧版的区别：默认**不再把资源 id 烙在图上**（旧版那行白字会透过游戏里的
压角遮罩隐约可读，很出戏）。现在按 id 与用途描述里的关键词决定色板与构图
母题（书架 / 天际线 / 雨丝 / 光池 / 裂缝…），看起来像一张「风格稿」而不是
一张标签卡。需要把 id 印回图上做对照表时加 --labels。

- 背景 / CG：1920x1080，对应整屏
- 立绘：520x900，带透明底，脚部（底部 12%）透明，方便站在对话框后面

体积红线：网页版要把这些图内嵌成 base64，所以统一量化到 128 色 PNG-8，
单张控制在几十 KB。别在这里加逐像素噪点——噪点会让 PNG 膨胀十倍。

可重复运行：已存在且非空的文件默认跳过（想重画加 --force）。

用法：
    python story/tools/gen-placeholder-art.py
    python story/tools/gen-placeholder-art.py --force
    python story/tools/gen-placeholder-art.py --force --labels
"""
import argparse
import hashlib
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

PNG_COLORS = 128          # 量化色数：够平滑，又能把体积压住
VIGNETTE_STRENGTH = 0.42  # 四角压暗比例
VIGNETTE_RINGS = 24       # 压暗环数：越多越平滑，但每张多几十毫秒
LIGHT_POOL_ALPHA = 96     # 光池最大不透明度

CJK_FONT_CANDIDATES = [
    r"C:\Windows\Fonts\msyh.ttc",
    r"C:\Windows\Fonts\msyhbd.ttc",
    r"C:\Windows\Fonts\simhei.ttf",
    r"C:\Windows\Fonts\simsun.ttc",
]

# Pillow 10 把这两个枚举挪了家，仓库里两种版本都可能遇到
try:
    QUANT_METHOD = Image.Quantize.MEDIANCUT
    QUANT_METHOD_RGBA = Image.Quantize.FASTOCTREE   # RGBA 只接受 FastOctree / libimagequant
    DITHER_NONE = Image.Dither.NONE
except AttributeError:
    QUANT_METHOD = Image.MEDIANCUT
    QUANT_METHOD_RGBA = Image.FASTOCTREE
    DITHER_NONE = Image.NONE

# 关键词 -> (上/中/下三段色板, 构图母题)。命中第一条即采用；顺序有意义，
# 越具体的放前面（"雨" 比 "街道" 更该决定画面）。
MOODS = [
    (("纯黑", "black"), (10, 10, 14), (6, 6, 9), (2, 2, 4), "none"),
    (("雨",), (26, 34, 52), (38, 50, 70), (16, 22, 34), "rain"),
    (("黎明", "dawn"), (58, 52, 92), (140, 110, 118), (214, 168, 128), "horizon"),
    (("黄昏", "evening"), (48, 34, 58), (122, 74, 70), (206, 140, 92), "horizon"),
    (("夜", "night"), (12, 16, 34), (24, 32, 62), (8, 10, 22), "windows"),
    (("日", "day"), (74, 96, 128), (132, 152, 176), (58, 72, 96), "windows"),
    (("记忆", "白"), (214, 218, 226), (238, 240, 244), (186, 190, 202), "haze"),
    (("裂缝", "crack"), (18, 12, 34), (46, 26, 78), (10, 6, 20), "slit"),
    (("核心", "core"), (10, 26, 40), (26, 74, 96), (6, 16, 26), "core"),
    (("领主", "hall"), (26, 14, 42), (58, 30, 74), (12, 6, 22), "arches"),
    (("时间线", "timeline"), (10, 14, 32), (28, 44, 84), (6, 8, 18), "stream"),
    (("废墟", "ruins"), (40, 36, 34), (66, 58, 52), (22, 20, 18), "rubble"),
    (("基地", "作战", "base"), (18, 26, 30), (34, 50, 56), (10, 16, 18), "panels"),
    (("医院", "hospital"), (176, 186, 188), (206, 214, 214), (140, 150, 152), "corridor"),
    (("列车", "train"), (26, 24, 40), (52, 44, 66), (14, 12, 22), "windowstrip"),
    (("天台", "rooftop"), (30, 34, 66), (86, 78, 108), (18, 20, 34), "horizon"),
    (("校舍", "school"), (56, 44, 34), (96, 76, 56), (30, 24, 18), "arches"),
    (("咖啡馆", "cafe"), (44, 28, 26), (96, 58, 42), (24, 14, 14), "lamp"),
    (("图书馆", "library"), (28, 22, 34), (58, 44, 40), (14, 12, 18), "shelves"),
    (("街道", "城市", "city", "street"), (18, 22, 42), (40, 48, 76), (10, 12, 24), "skyline"),
    (("走廊", "corridor"), (22, 24, 34), (48, 52, 66), (12, 12, 18), "corridor"),
    (("研究所", "institute", "实验", "lab"), (16, 26, 38), (38, 62, 82), (8, 14, 22), "panels"),
    (("宿舍", "dorm"), (24, 22, 38), (52, 44, 62), (12, 12, 20), "lamp"),
    (("平行", "alt"), (22, 18, 44), (54, 40, 88), (12, 10, 26), "skyline"),
    (("结局", "空白", "void"), (14, 14, 20), (26, 26, 36), (6, 6, 10), "haze"),
]
DEFAULT_MOOD = ((20, 24, 40), (44, 52, 76), (12, 14, 24), "horizon")

# 强调色：光池 / 母题高光，同样按关键词命中
ACCENTS = [
    (("雨", "夜", "night"), (120, 160, 220)),
    (("黎明", "黄昏", "evening", "dawn"), (236, 176, 116)),
    (("裂缝", "crack", "领主", "hall"), (176, 120, 236)),
    (("核心", "core", "时间线", "timeline"), (110, 226, 226)),
    (("记忆", "白", "校舍", "school"), (246, 240, 220)),
    (("医院", "走廊", "corridor"), (214, 236, 236)),
    (("图书馆", "library", "咖啡馆", "cafe", "宿舍", "dorm", "列车", "train"), (232, 176, 108)),
    (("基地", "base", "研究所", "institute", "实验", "lab"), (128, 200, 210)),
    (("废墟", "ruins"), (206, 168, 132)),
]
DEFAULT_ACCENT = (150, 170, 210)


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

    out = {}
    for kind, items in sections.items():
        seen, cleaned = set(), []
        for it in items:
            sid, label = it if isinstance(it, tuple) else (it, it)
            if sid in seen:
                continue
            seen.add(sid)
            cleaned.append((sid, label))
        out[kind] = cleaned
    return out


def id_hash(sid):
    """稳定散列：同一 id 永远同一张图，跨机器、跨次运行都不跳。"""
    return int(hashlib.sha256(sid.encode("utf-8")).hexdigest()[:8], 16)


def color_from_id(sid):
    """与运行时 AssetRegistry.ColorFromId 同一套哈希思路，保证占位色一致。"""
    h = 17
    for ch in sid:
        h = (h * 31 + ord(ch)) & 0xFFFFFFFF
    r = 46 + int((h & 0xFF) / 255 * 56)
    g = 46 + int(((h >> 8) & 0xFF) / 255 * 56)
    b = 61 + int(((h >> 16) & 0xFF) / 255 * 66)
    return r, g, b


def mood_of(sid, label):
    """按 id 与用途描述里的关键词挑色板与母题。"""
    hay = sid + " " + (label or "")
    for keys, top, mid, bottom, motif in MOODS:
        if any(k in hay for k in keys):
            return top, mid, bottom, motif
    return DEFAULT_MOOD[0], DEFAULT_MOOD[1], DEFAULT_MOOD[2], DEFAULT_MOOD[3]


def accent_of(sid, label):
    hay = sid + " " + (label or "")
    for keys, color in ACCENTS:
        if any(k in hay for k in keys):
            return color
    return DEFAULT_ACCENT


def three_stop_gradient(size, top, mid, bottom):
    """上/中/下三段线性渐变，比旧版两段更像打了光的场景。"""
    img = Image.new("RGB", size, top)
    draw = ImageDraw.Draw(img)
    w, h = size
    half = h // 2
    for y in range(h):
        if y < half:
            t = y / max(half - 1, 1)
            a, b = top, mid
        else:
            t = (y - half) / max(h - half - 1, 1)
            a, b = mid, bottom
        color = tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))
        draw.line([(0, y), (w, y)], fill=color)
    return img


def add_light_pool(img, accent, cx, cy, radius, alpha):
    """一个软光池：同心圆由透明叠到 accent，模拟场景里的主光源。"""
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(layer)
    steps = 26
    for i in range(steps, 0, -1):
        t = i / steps
        a = int(alpha * (1 - t) ** 2)
        r = int(radius * t)
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], fill=accent + (a,))
    img.paste(Image.alpha_composite(img.convert("RGBA"), layer).convert("RGB"), (0, 0))


def motif_shelves(draw, w, h, accent, seed):
    """图书馆：一排排竖向书脊 + 横向层板。"""
    x = 40
    band = h // 3
    while x < w - 60:
        bw = 26 + (seed + x) % 46
        shade = 18 + (seed + x * 7) % 26
        draw.rectangle([x, band, x + bw, h - 120], fill=(shade, shade - 2, shade + 6))
        for row in range(4):
            yy = band + 40 + row * ((h - band - 160) // 4)
            draw.line([(x, yy), (x + bw, yy)], fill=(0, 0, 0), width=3)
        x += bw + 8
    draw.rectangle([0, band - 10, w, band + 10], fill=(30, 24, 20))


def motif_skyline(draw, w, h, accent, seed):
    """城市：远近两层楼群剪影。"""
    for layer, base_y, tone in ((0, int(h * 0.62), 22), (1, int(h * 0.74), 12)):
        x = -40
        while x < w:
            bw = 90 + (seed + x * (layer + 3)) % 150
            bh = 90 + (seed + x * (layer + 7)) % (260 - layer * 90)
            draw.rectangle([x, base_y - bh, x + bw, h], fill=(tone, tone + 2, tone + 10))
            for wy in range(base_y - bh + 24, base_y - 20, 42):
                for wx in range(x + 16, x + bw - 16, 34):
                    if (wx * 31 + wy * 17 + seed) % 7 < 2:
                        draw.rectangle([wx, wy, wx + 9, wy + 14], fill=accent + (150,))
            x += bw + 14


def motif_windows(draw, w, h, accent, seed):
    """室内夜/日：一面分成格子的窗，格子里透出光。"""
    fx, fy, fw, fh = int(w * 0.08), int(h * 0.16), int(w * 0.30), int(h * 0.52)
    draw.rectangle([fx, fy, fx + fw, fy + fh], fill=(12, 14, 22))
    cols, rows = 3, 4
    for c in range(cols):
        for r in range(rows):
            x0 = fx + c * (fw // cols) + 10
            y0 = fy + r * (fh // rows) + 10
            x1 = fx + (c + 1) * (fw // cols) - 10
            y1 = fy + (r + 1) * (fh // rows) - 10
            lit = (c * 7 + r * 13 + seed) % 5 < 2
            draw.rectangle([x0, y0, x1, y1], fill=accent if lit else (26, 30, 44))
    draw.rectangle([fx, fy, fx + fw, fy + fh], outline=(6, 6, 10), width=14)


def motif_rain(draw, w, h, accent, seed):
    """雨：斜落的高透明线条 + 地面积水反光。"""
    for i in range(240):
        x = (seed * (i + 11) * 37) % (w + 200) - 100
        y = (seed * (i + 5) * 61) % h
        ln = 40 + (i % 5) * 16
        draw.line([(x, y), (x - 14, y + ln)], fill=(200, 214, 236), width=1)
    draw.rectangle([0, int(h * 0.82), w, h], fill=(14, 18, 28))
    for i in range(24):
        x = (seed + i * 83) % w
        draw.line([(x, int(h * 0.86)), (x + 60, int(h * 0.86))], fill=(40, 50, 70), width=2)


def motif_slit(draw, w, h, accent, seed):
    """时空裂缝：画面中央一道竖裂口，边缘发亮。"""
    cx = w // 2 + (seed % 120) - 60
    for half in (140, 96, 58, 30):
        draw.polygon([(cx - half, 0), (cx + half, int(h * 0.1)),
                      (cx + half * 0.6, h), (cx - half * 0.7, h)], fill=accent)
    draw.polygon([(cx - 10, 0), (cx + 10, int(h * 0.1)), (cx + 6, h), (cx - 6, h)],
                 fill=(250, 250, 255))


def motif_core(draw, w, h, accent, seed):
    """时间核心：中心亮核 + 环状轨道。"""
    cx, cy = w // 2, int(h * 0.46)
    for r in (360, 260, 170, 96):
        draw.ellipse([cx - r, cy - r // 2, cx + r, cy + r // 2], outline=accent, width=3)
    draw.ellipse([cx - 60, cy - 60, cx + 60, cy + 60], fill=(245, 250, 250))


def motif_arches(draw, w, h, accent, seed):
    """大厅 / 旧校舍：连续拱门透视收缩。"""
    for i in range(6):
        t = i / 6
        aw = int(w * (0.16 - t * 0.02))
        ah = int(h * (0.62 - t * 0.06))
        cx = w // 2 + int((t - 0.5) * w * 0.62)
        shade = int(26 - i * 3)
        draw.rectangle([cx - aw // 2, h - ah, cx + aw // 2, h], fill=(shade, shade, shade + 8))
        draw.ellipse([cx - aw // 2, h - ah - aw // 2, cx + aw // 2, h - ah + aw // 2],
                     fill=(shade, shade, shade + 8))
    draw.ellipse([w // 2 - 90, int(h * 0.2), w // 2 + 90, int(h * 0.2) + 180], fill=accent)


def motif_panels(draw, w, h, accent, seed):
    """基地 / 研究所：金属壁板与一条横向灯带。"""
    for i in range(9):
        x = i * (w // 9)
        draw.rectangle([x + 8, int(h * 0.18), x + w // 9 - 8, int(h * 0.78)],
                       fill=(28 + (i % 3) * 4, 38 + (i % 3) * 4, 44 + (i % 3) * 4))
    draw.rectangle([0, int(h * 0.16), w, int(h * 0.175)], fill=accent)
    draw.rectangle([0, int(h * 0.79), w, int(h * 0.805)], fill=(10, 14, 18))


def motif_corridor(draw, w, h, accent, seed):
    """走廊 / 医院：向灭点收缩的地面与天花。"""
    vx, vy = w // 2, int(h * 0.5)
    for i in range(10):
        t = i / 10
        shade = int(150 - t * 90) if i % 2 == 0 else int(120 - t * 80)
        draw.polygon([(0, h), (w, h), (vx + (1 - t) * w // 2, vy), (vx - (1 - t) * w // 2, vy)],
                     fill=(shade, shade + 4, shade + 6))
    draw.rectangle([vx - 60, vy - 120, vx + 60, vy + 120], fill=accent)


def motif_windowstrip(draw, w, h, accent, seed):
    """列车：一长条车窗，窗外是掠过的光。"""
    for i in range(6):
        x = 60 + i * (w // 6)
        draw.rounded_rectangle([x, int(h * 0.26), x + w // 6 - 90, int(h * 0.62)],
                               radius=24, fill=accent if i % 2 else (30, 34, 48))
    draw.rectangle([0, int(h * 0.66), w, h], fill=(16, 14, 22))


def motif_lamp(draw, w, h, accent, seed):
    """咖啡馆 / 宿舍：几盏暖灯把桌面照亮。"""
    for i in range(3):
        cx = int(w * (0.24 + i * 0.26)) + (seed % 40)
        draw.ellipse([cx - 26, int(h * 0.24), cx + 26, int(h * 0.24) + 52], fill=accent)
        draw.line([(cx, 0), (cx, int(h * 0.24))], fill=(20, 16, 14), width=6)
    draw.rectangle([0, int(h * 0.7), w, h], fill=(34, 24, 20))


def motif_horizon(draw, w, h, accent, seed):
    """天台 / 黎明 / 黄昏：一条地平线加远处低矮轮廓。"""
    hy = int(h * 0.6)
    draw.rectangle([0, hy, w, h], fill=(20, 18, 26))
    for i in range(14):
        x = i * (w // 14)
        bh = 20 + (seed + i * 53) % 90
        draw.rectangle([x, hy - bh, x + w // 14 - 12, hy], fill=(26, 24, 34))
    draw.line([(0, hy), (w, hy)], fill=accent, width=4)


def motif_stream(draw, w, h, accent, seed):
    """时间线：横向流光的带子。"""
    for i in range(16):
        y = int(h * (0.12 + i * 0.05)) + (seed % 30)
        draw.line([(0, y), (w, y + 40 - i * 4)], fill=accent, width=2 if i % 3 else 5)


def motif_haze(draw, w, h, accent, seed):
    """记忆空间 / 结局空白：一层一层横着的薄雾。"""
    for i in range(8):
        yy = int(h * (0.2 + i * 0.09))
        draw.rectangle([0, yy, w, yy + 60], fill=accent)


def motif_rubble(draw, w, h, accent, seed):
    """废墟：倒塌的块体与扬尘。"""
    for i in range(22):
        x = (seed + i * 137) % w
        y = int(h * 0.55) + (i * 41) % int(h * 0.4)
        s = 40 + (seed + i * 29) % 160
        draw.polygon([(x, y), (x + s, y - s // 3), (x + s, y + s // 2), (x - s // 4, y + s // 2)],
                     fill=(46, 40, 36) if i % 2 else (34, 30, 28))


MOTIFS = {
    "shelves": motif_shelves, "skyline": motif_skyline, "windows": motif_windows,
    "rain": motif_rain, "slit": motif_slit, "core": motif_core, "arches": motif_arches,
    "panels": motif_panels, "corridor": motif_corridor, "windowstrip": motif_windowstrip,
    "lamp": motif_lamp, "horizon": motif_horizon, "stream": motif_stream,
    "haze": motif_haze, "rubble": motif_rubble, "none": None,
}


def add_vignette(img, strength):
    """四角压暗：一圈圈逐渐变浅的矩形环近似径向遮罩，不引入逐像素噪点。"""
    w, h = img.size
    steps = VIGNETTE_RINGS
    layer = Image.new("L", (w, h), 0)
    draw = ImageDraw.Draw(layer)
    # 环宽按两轴里较大的间距取，保证相邻环重叠无缝；由外向内画，值递减
    ring = int(max(w, h) * 0.5 / (steps - 1)) + 2
    for i in range(steps):
        t = i / (steps - 1)              # 0 = 最外圈（最暗），1 = 中心
        mx, my = int(w * 0.5 * t), int(h * 0.5 * t)
        if w - mx <= mx or h - my <= my:
            break
        draw.rectangle([mx, my, w - mx, h - my],
                       outline=int(255 * strength * (1 - t)), width=ring)
    dark = Image.new("RGB", (w, h), (0, 0, 0))
    return Image.composite(dark, img, layer)


def draw_labels(img, sid, label, kind):
    """--labels 才走到这里：把 id 印回图上，方便做美术对照表。"""
    draw = ImageDraw.Draw(img)
    w, h = img.size
    title_font = load_font(72 if kind != "portrait" else 40)
    small_font = load_font(40 if kind != "portrait" else 26)
    bbox = draw.textbbox((0, 0), sid, font=title_font)
    draw.text(((w - (bbox[2] - bbox[0])) / 2, h * 0.06), sid, font=title_font, fill=(255, 255, 255))
    if label and label != sid:
        bbox2 = draw.textbbox((0, 0), label, font=small_font)
        draw.text(((w - (bbox2[2] - bbox2[0])) / 2, h * 0.06 + (bbox[3] - bbox[1]) + 16),
                  label, font=small_font, fill=(230, 230, 230))


def gen_background(sid, label, kind):
    top, mid, bottom, motif = mood_of(sid, label)
    accent = accent_of(sid, label)
    size = CG_SIZE if kind == "cg" else BG_SIZE
    seed = id_hash(sid)
    img = three_stop_gradient(size, top, mid, bottom)

    overlay = Image.new("RGBA", size, (0, 0, 0, 0))
    od = ImageDraw.Draw(overlay)
    fn = MOTIFS.get(motif)
    if fn:
        fn(od, size[0], size[1], accent, seed)
    img = Image.alpha_composite(img.convert("RGBA"), overlay).convert("RGB")

    pool_alpha = LIGHT_POOL_ALPHA + (30 if kind == "cg" else 0)
    add_light_pool(img, accent,
                   int(size[0] * (0.5 if kind == "cg" else 0.28 + (seed % 40) / 100.0)),
                   int(size[1] * (0.42 if kind == "cg" else 0.3)),
                   int(size[0] * (0.34 if kind == "cg" else 0.22)), pool_alpha)
    img = add_vignette(img, VIGNETTE_STRENGTH)
    return img.quantize(colors=PNG_COLORS, method=QUANT_METHOD)


def gen_portrait(sid, label):
    """立绘：带轮廓光的剪影，脚部透明，不写任何文字。"""
    base = color_from_id(sid)
    accent = accent_of(sid, label)
    w, h = PORTRAIT_SIZE
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    body_top, foot = int(h * 0.30), int(h * 0.88)
    body = [(int(w * 0.30), body_top), (int(w * 0.70), body_top),
            (int(w * 0.86), foot), (int(w * 0.14), foot)]
    draw.polygon(body, fill=(base[0], base[1], base[2], 235))
    # 轮廓光：沿左侧描一条 accent 亮边，剪影立刻不那么「纸片」
    draw.line([(int(w * 0.30), body_top), (int(w * 0.14), foot)], fill=accent + (200,), width=7)

    head_r = int(w * 0.19)
    cx, cy = w // 2, int(h * 0.20)
    draw.ellipse([cx - head_r, cy - head_r, cx + head_r, cy + head_r],
                 fill=(base[0] + 14, base[1] + 12, base[2] + 10, 245))
    draw.arc([cx - head_r, cy - head_r, cx + head_r, cy + head_r], 120, 260,
             fill=accent + (220,), width=6)
    # 脚下投影，让角色「站」在场景里
    draw.ellipse([int(w * 0.16), foot - 12, int(w * 0.84), foot + 34], fill=(0, 0, 0, 90))
    return img.quantize(colors=PNG_COLORS, method=QUANT_METHOD_RGBA, dither=DITHER_NONE)


def save(img, path):
    img.save(path, optimize=True)


def main():
    # Windows 控制台默认不是 UTF-8，先兜住，免得中文 print 直接把脚本炸在半路
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except (AttributeError, ValueError):
            pass

    ap = argparse.ArgumentParser()
    ap.add_argument("--force", action="store_true", help="重画已存在的文件")
    ap.add_argument("--labels", action="store_true",
                    help="把资源 id 印回图上（做美术对照表用，默认不印）")
    args = ap.parse_args()

    assets = parse_assets()
    plan = [("bg", "Bg"), ("portrait", "Portrait"), ("cg", "Cg")]

    made = skipped = 0
    total_bytes = 0
    for kind, folder in plan:
        items = assets.get(kind, [])
        if not items:
            print("警告：%s 在 ASSETS.md 里没解析到条目" % kind)
            continue
        out_dir = os.path.join(ART_ROOT, folder)
        os.makedirs(out_dir, exist_ok=True)
        for sid, label in items:
            path = os.path.join(out_dir, sid + ".png")
            if os.path.exists(path) and os.path.getsize(path) > 0 and not args.force:
                skipped += 1
                continue
            img = gen_portrait(sid, label) if kind == "portrait" else gen_background(sid, label, kind)
            if args.labels:
                img = img.convert("RGB")
                draw_labels(img, sid, label, kind)
                img = img.quantize(colors=PNG_COLORS, method=QUANT_METHOD)
            save(img, path)
            total_bytes += os.path.getsize(path)
            made += 1
        print("%-9s -> %s (%d 项)" % (folder, os.path.relpath(out_dir, ROOT), len(items)))

    print("\n生成 %d 张, 跳过 %d 张, 新图合计 %.2f MB" % (made, skipped, total_bytes / 1048576.0))
    print("注意：立绘是带轮廓光的占位剪影；BGM/SFX 未生成（运行时静音，缺资源会被 "
          "ArtRegistry/AudioRegistry 记为 missing）。正式美术见 art/manifest.json。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
