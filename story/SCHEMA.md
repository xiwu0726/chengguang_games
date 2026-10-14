# 《时光之痕》剧本文本格式规范 (SCHEMA v2)

本文件是**剧本数据的唯一契约**。所有剧本 JSON、Unity 运行时 (`Assets/Scripts/Story/`)、
字数统计工具 (`story/tools/count-story.js`) 都以本文件为准。改动格式必须三处同步。

设定与人物口径见 `story/BIBLE.md`（圣经）。本文件只管**格式**。

---

## 1. 目录与文件

- 剧本文件放在 `Assets/Resources/Story/` 下，按幕分目录：`act1/`、`act2/`、`act3/`。
- 一个文件 = 一个场景，文件名必须等于 `sceneId` 并加 `.json`，例如 `1-1.json`。
- 只允许 UTF-8 **无 BOM**、LF 换行、2 空格缩进。
- `Resources/` 下的所有内容都会打进包，所以剧本必须放在这里才能被 `Resources.Load` 读到。

## 2. 顶层结构（一个场景）

```json
{
  "sceneId": "1-1",
  "chapter": "第一幕·异变初现",
  "title": "历史图书馆",
  "background": "bg_library_night",
  "bgm": "bgm_mystery",
  "next": "1-2",
  "lines": [ ... ]
}
```

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `sceneId` | string | 是 | 与文件名一致。幕-序号，如 `1-1`、`2-7`、`3-4` |
| `chapter` | string | 是 | 章节标题，同一幕内保持一致 |
| `title` | string | 是 | 场景标题，显示在顶栏与存档里 |
| `background` | string | 是 | 背景资源 id，见 §6 |
| `bgm` | string | 是 | 背景乐 id；没有就写 `""` |
| `next` | string | 是 | 本场景结束后进入的 sceneId；写 `"@ending"` 表示按结局规则表进入某个结局场景（见 §8.3）；终幕结局场景自身写 `""`（进入结局画面） |
| `lines` | array | 是 | 内容行，按播放顺序，至少 20 行 |
| `dense` | bool | 否 | 写 `true` 表示本场景接受 §8.2 的 200 字分支密度门禁（迁移中的旧场景缺省视为 `false`） |
| `kind` | string | 否 | 写 `"ending"` 表示这是一个结局场景：字数硬下限降为 800 字、行数下限降为 10 行（见 §5） |

## 3. 内容行 `lines[]`

每行必须有 `type`，取值 `narration` / `dialogue` / `choice` / `cg` / `fx`。

### 3.1 `narration` 旁白 / 心理描写

```json
{ "type": "narration", "text": "雨点砸在图书馆的穹顶上，像有人在敲一面很远很远的鼓。" }
```

- 无人物头像，文本左对齐显示在下框。
- 主角的**内心独白**也用 narration，但文本要写成第一人称口语（"我想……"）。

### 3.2 `dialogue` 对白

```json
{ "type": "dialogue", "speaker": "艾莉丝", "sprite": "alice_worried", "text": "你看起来很不对劲。" }
```

- `speaker` 必须是 `BIBLE.md` 里的人物名（**精确匹配**，不要加括号或头衔）。
- `sprite` 是立绘 id，见 §6；说话人没有立绘就写 `""`。
- 一行只放一句话或一个完整语气的短句；不要把整段对话塞进一行。

### 3.3 `choice` 选择

```json
{
  "type": "choice",
  "text": "",
  "prompt": "你要说真话吗？",
  "options": [
    { "text": "把看到的一切告诉她", "affectionWho": "alice", "affection": 8,
      "insomnia": -2, "flag": "told_alice_truth", "next": "1-1a" },
    { "text": "先含糊过去", "affectionWho": "alice", "affection": -5,
      "insomnia": 3, "flag": "hid_from_alice", "next": "1-1b" }
  ]
}
```

- `options` 每项 2–4 个（**全剧固定 2–4 个**，不要出现 5 个以上）。
- `text`：**所有行都必须有 `text` 字段，`choice` 也一样**。校验器按行检查这个字段，缺了会报
  `缺少 text`；运行时 `choice` 只显示 `prompt`，所以这里固定写 `""`（写非空内容也不会被显示）。
- `prompt`：选择框标题，写问题本身（一句话，不超过 40 字）。
- `affectionWho`：受影响人物名（`BIBLE.md` 中的名字）；不需要影响谁就写 `""`。
- `affection`/`insomnia`：整数，没变化写 `0`。`insomnia` 是失眠值（0–100），正数为恶化。
- `flag`：本选项写入的剧情标记名（小写下划线）；不需要就写 `""`。
- `next`：**选了这项之后跳到哪**，四种写法（见 §8.1）：
  - `""` — 继续播放本场景剩下的行（收敛型选择）；
  - `"1-2"` — 跳到另一个场景的第一行（旧写法，仍然支持）；
  - `"#l07"` — 跳到**本场景**里 `label` 为 `l07` 的那一行（scene 内分支，新）；
  - `"1-2#l07"` — 跳到场景 `1-2` 的 `l07` 行（跨场景带标签，新）。
  - **只允许前向跳转**：scene 内跳转的目标行必须排在 choice 行之后（否则不合格，见 §8.1）。
- 分支文件（`1-1a.json`）与普通场景格式完全相同，用 `next` 收回主线。

### 3.4 `cg` 整屏插图

```json
{ "type": "cg", "background": "cg_first_fragment", "text": "时间碎片在她掌心裂开——" }
```

- 切整屏图，`text` 可为 `""`（纯图停留，玩家点击继续）。

### 3.5 `fx` 演出指令

```json
{ "type": "fx", "fx": "shake", "text": "" }
```

- `fx` 取值：`shake`（震屏）、`flash`（闪白）、`insomnia`（进入失眠视觉）、
  `timeline`（时间线具象化）、`wait`（停顿半秒）。`text` 忽略。

## 4. 每行的可选覆盖字段

任意行都可以额外带以下字段，用于让演出跟着文本走（没写就不变）：

| 字段 | 说明 |
|---|---|
| `background` | 本行开始换背景（id 见 §6） |
| `bgm` | 本行开始换 BGM；`"stop"` 表示停乐 |
| `sfx` | 播放一次音效 id，如 `sfx_door`；没有就省略 |
| `sprite` | （dialogue 专用）本行的立绘 id |

## 5. 字数口径（**硬指标**）

- 只统计 **CJK 汉字**（`\u4e00-\u9fff`），标点、数字、拉丁字母、空白都不算。
- `text` 与 `prompt`、`options[].text`、`speaker` 全部计入；但 `speaker` 只算一次不重复计。
- **单场景目标：≥ 2500 字，硬下限 2200 字**（`lines` 少于 20 行或字数不够都算不合格）。
- **结局场景（`kind: "ending"`）**：目标 ≥ 1200 字，硬下限 **800 字**、行数下限 **10 行**。
- 全剧目标：**≥ 100,000 字**，分布为 第一幕 ≥ 30,000 / 第二幕 ≥ 35,000 / 第三幕 ≥ 35,000。
- 统计命令：`node story/tools/count-story.js`（会打印每场景字数、每幕小计、全剧总计，
  并校验 schema；有任何不合格场景时退出码非 0）。

## 6. 资源 id 命名

- 背景：`bg_<地点>_<时段>`，如 `bg_library_night`、`bg_institute_day`、`bg_cafe_evening`。
- 立绘：`<角色拼音>_<表情>`，如 `alice_worried`、linxiao_tired`、`raine_alert`、`xya_smile`、
  `lord_calm`；角色拼音见 `BIBLE.md`。
- 整屏 CG：`cg_<内容>`，如 `cg_first_fragment`。
- BGM：`bgm_<情绪>`，如 `bgm_mystery`、`bgm_warm`、`bgm_tension`。
- 音效：`sfx_<事件>`，如 `sfx_door`、`sfx_glass`。
- **所有 id 只能用小写字母、数字、下划线**。用到的每个 id 必须在
  `story/ASSETS.md` 里登记（缺少登记视为不合格），美术/音频资源按该清单生产。

## 7. 写作口径（不合格的常见原因）

1. **不要写剧本格式**：不要 `【场景】`、不要 `画外音：`、不要 markdown 加粗，
   一切演出都落进上面的字段里。
2. **不要出现选项后无后果**：每个 `choice` 至少要在后续 3 行内体现差异
   （不同的对白内容、或不同的 `affection`/`insomnia`/`flag` 影响）。
3. **人名必须精确**：写错名字（如"爱丽丝"）会被工具判为拼写漂移，直接不合格。
4. **不许复读**：同一场景内不得出现两次完全相同的 `text`；跨场景复读超过 3 句不合格。
5. **字数密度**：不要用长段的景物描写堆字数；每 300 字内必须有情节推进或人物互动。
6. **时间线一致**：严格按 `BIBLE.md` 的时间线（2150 年、第 N 天）写，不得出现
   与已写场景矛盾的时间/地点/事实。
7. **敏感内容**：不写真实的现实品牌、真实政治人物、真实可执行的危险操作；
   暴力与情感描写按 `BIBLE.md` 的尺度上限（不超过影视剧普遍可播出的强度）。

---

## 8. 分支密度与结局路由（v2，2026-10-13 起）

目标（用户口径）：**每 200 字进入一次分支**，全剧**至少 20 种不同结局**。

### 8.1 行标签与前向跳转

- 任意行都可以加 `"label": "l07"`（`^l[0-9]{2,}$`），**同一场景内必须唯一**。
- `option.next` 四种写法（见 §3.3）：`""` 收敛 / `"1-2"` 整场景跳 / `"#l07"` 本场景内跳 / `"1-2#l07"` 跨场景带标签跳。
- **前向约束（硬）**：`"#l07"` 的目标行索引必须**大于**该 `choice` 行的索引；
  跨场景跳转不受此限。运行时因此不需要死循环保护，校验器直接判不合格。
- **真支线要求（硬）**：每个 `dense` 场景至少有 **2 个选项**使用 `#label` 跳转
  （即至少两个真正的岔路口，不是全收敛）。

### 8.2 分支密度门禁（每 200 字一个分支点）

- 「正文段」= 两个分支点之间的连续非 `choice` 行，含「场景开头 → 第一个 choice」
  与「最后一个 choice → 场景结尾」。
- 规则：**每个正文段的 CJK 字数 ≤ 200**（`DENSITY_MAX_CHARS = 200`）。
  也就是说 2500 字的场景至少要有 13 个 `choice` 行。
- 不合格报错：`x <scene> 分支点间隔 N 字 > 200（第 a 行到第 b 行）`。
- 分支点只算 `choice` 行；`choice` 的 `prompt` 要写成真正的岔路口（问句或决定），选项 2–3 个。
- 迁移期：只有写了 `"dense": true` 的场景受此门禁约束；迁移完成后默认全部开启。

### 8.3 结局规则表 `Assets/Resources/StoryConfig/endings.json`

```json
{
  "version": 2,
  "minEndings": 20,
  "rules": [
    { "scene": "3-e01", "title": "结局·归途",
      "when": { "flags": ["ending_normal"], "insomniaMax": 54, "topAffection": "艾莉丝", "affectionMin": 60 } }
  ],
  "fallback": "3-e20"
}
```

- `when` 字段全部可选，出现几个就要求同时满足几个：
  | 字段 | 含义 |
  |---|---|
  | `flags` | 这些标记**全部**必须存在 |
  | `notFlags` | 这些标记必须**都不**存在 |
  | `insomniaMin` / `insomniaMax` | 失眠值区间（含端点） |
  | `shardsMin` | 记忆碎片数下限 |
  | `flagCountMin` | 已获得标记总数下限（用来区分「做了很多选择」的玩家） |
  | `topAffection` | 好感最高的人物名（并列时按 `BIBLE.md` 的人物顺序取先者） |
  | `affectionMin` | 该人物好感下限 |
- 解析：**自上而下第一条全部满足的规则胜出**；都不满足用 `fallback`。
- 规则表必须满足：`scene` 文件存在且 `kind: "ending"`；规则条数 ≥ `minEndings`；
  任意两条规则的 `when` 不相同；**每条规则都能被真实路径走到**（工具用蒙特卡洛走图验证）。
- 场景顶层 `"next": "@ending"` = 本场景结束后按规则表进入结局场景；
  结局场景自身 `next` 必须为 `""`（演完进入结局画面，标题取规则里的 `title`）。

### 8.4 结局场景

- 文件 `act3/3-e01.json` … `3-e20.json`，`sceneId` 同名、`kind: "ending"`、`next: ""`。
- 每个结局一张专属 CG `cg_ending_e01` … `cg_ending_e20`，必须登记进 `story/ASSETS.md`。
- 标题一律 `结局·<两到四字>`；正文要能独立看懂（玩家可能只看到这一个结局）。
