# 《时光之痕》资源清单 (ASSETS)

本文件是**资源 id 的登记处**。`story/tools/count-story.js` 会扫描本文件里所有反引号包裹的
小写 id；剧本里用到但没登记在这里的 id 一律判为不合格（SCHEMA §6）。

美术/音频还没生产时，游戏会用**占位图 / 静音**继续跑（`ArtRegistry` / `AudioRegistry`），
所以可以「先写剧本、先能玩，再补资源」。补资源时按本清单的 id 命名放进：

- 背景 → `Assets/Resources/Art/Bg/<id>.png`
- 立绘 → `Assets/Resources/Art/Portrait/<id>.png`
- 整屏 CG → `Assets/Resources/Art/Cg/<id>.png`
- BGM → `Assets/Resources/Audio/Bgm/<id>.ogg`
- 音效 → `Assets/Resources/Audio/Sfx/<id>.wav|ogg`

尺寸建议：背景 1920×1080、CG 1920×1080、立绘 900×1300（透明 PNG）。

---

## 1. 背景 `bg_*`

| id | 用途 |
|---|---|
| `bg_title` | 标题画面 |
| `bg_black` | 纯黑过渡 |
| `bg_library_night` | 历史图书馆·夜（开场） |
| `bg_library_day` | 历史图书馆·日 |
| `bg_library_basement` | 图书馆地下书库 |
| `bg_institute_day` | 时空异常研究所·日 |
| `bg_institute_night` | 时空异常研究所·夜 |
| `bg_lab` | 研究所实验舱 |
| `bg_dorm_night` | 林晓的宿舍·夜（失眠） |
| `bg_dorm_dawn` | 林晓的宿舍·黎明 |
| `bg_cafe_evening` | 街角咖啡馆·黄昏 |
| `bg_street_night` | 新东京街道·夜 |
| `bg_street_rain` | 新东京街道·雨 |
| `bg_rooftop` | 研究所天台 |
| `bg_hospital` | 医院走廊 |
| `bg_train` | 城际列车车厢 |
| `bg_base` | 守望者地下基地 |
| `bg_base_room` | 基地作战室 |
| `bg_ruins` | 时空裂缝区的废墟 |
| `bg_corridor` | 裂缝内部的走廊 |
| `bg_crack` | 时空裂缝本体 |
| `bg_memory_white` | 记忆空间·白 |
| `bg_memory_school` | 记忆空间·旧校舍 |
| `bg_alt_city` | 平行世界的新东京 |
| `bg_lord_hall` | 时间领主的领域 |
| `bg_time_core` | 时间核心 |
| `bg_timeline` | 时间线具象化 |
| `bg_final_arena` | 最终决战之地 |
| `bg_dawn_city` | 决战后的黎明城市 |
| `bg_ending_void` | 结局用·空白空间 |

## 2. 立绘 `*_表情`

| 角色 | id |
|---|---|
| 林晓 | `linxiao_normal` `linxiao_tired` `linxiao_shock` `linxiao_resolve` `linxiao_sleep` `linxiao_smile` |
| 艾莉丝 | `alice_calm` `alice_cold` `alice_worried` `alice_smile` `alice_cry` `alice_tired` |
| 雷恩 | `raine_alert` `raine_grin` `raine_serious` `raine_hurt` `raine_tired` |
| 小雅 | `xya_smile` `xya_curious` `xya_scared` `xya_determined` `xya_cry` |
| 时间领主 | `lord_calm` `lord_amused` `lord_angry` `lord_broken` |
| 老周 | `zhou_kind` `zhou_grim` `zhou_worried` |
| 维拉 | `vera_cold` `vera_smile` `vera_angry` |

## 3. 整屏 CG `cg_*`

| id | 用途 |
|---|---|
| `cg_first_fragment` | 林晓第一次触到时间碎片 |
| `cg_awaken_eye` | 失眠觉醒·瞳孔变蓝的特写 |
| `cg_crack_open` | 时空裂缝张开 |
| `cg_raine_rescue` | 雷恩在雨中救人 |
| `cg_team_first` | 四人第一次同框 |
| `cg_base_establish` | 基地建立 |
| `cg_alice_lab` | 艾莉丝在实验舱 |
| `cg_alice_past` | 艾莉丝的过去（失去的亲人） |
| `cg_raine_secret` | 雷恩的秘密（守望者烙印） |
| `cg_xya_awaken` | 小雅的能力觉醒 |
| `cg_truth_reveal` | 时间真相揭露 |
| `cg_betrayal` | 背叛揭露 |
| `cg_collapse` | 时空崩溃 |
| `cg_final_choice` | 最终抉择 |
| `cg_trade_ledger` | 最终交易账页（四行名字并成一行） |
| `cg_lord_face` | 时间领主的真面目 |
| `cg_miracle` | 奇迹瞬间 |
| `cg_ending_normal` | 正常结局 |
| `cg_ending_hero` | 英雄结局 |
| `cg_ending_tragic` | 悲剧结局 |
| `cg_ending_open` | 开放结局 |
| `cg_ending_miracle` | 奇迹结局 |
| `cg_ending_e01` | 结局·归途 |
| `cg_ending_e02` | 结局·两个人的研究所 |
| `cg_ending_e03` | 结局·重新学睡 |
| `cg_ending_e04` | 结局·没人记得的门 |
| `cg_ending_e05` | 结局·写进页脚的名字 |
| `cg_ending_e06` | 结局·被纪念的陌生人 |
| `cg_ending_e07` | 结局·图书馆的新管理员 |
| `cg_ending_e08` | 结局·雕像下面的空位 |
| `cg_ending_e09` | 结局·钥匙在锁孔里 |
| `cg_ending_e10` | 结局·留在井底的那个人 |
| `cg_ending_e11` | 结局·雷小满 |
| `cg_ending_e12` | 结局·账没有平 |
| `cg_ending_e13` | 结局·门留着一条缝 |
| `cg_ending_e14` | 结局·那页纸还在口袋里 |
| `cg_ending_e15` | 结局·每个月丢一件东西 |
| `cg_ending_e16` | 结局·一个都没少 |
| `cg_ending_e17` | 结局·十一个名字都清了 |
| `cg_ending_e18` | 结局·钟重新走起来 |
| `cg_ending_e19` | 结局·醒着的城市 |
| `cg_ending_e20` | 结局·把借来的记忆当成自己的 |

## 4. BGM `bgm_*`

`bgm_title` `bgm_ambient` `bgm_mystery` `bgm_warm` `bgm_tension` `bgm_battle` `bgm_sad`
`bgm_hope` `bgm_insomnia` `bgm_lord` `bgm_ending` `bgm_silence`

## 5. 音效 `sfx_*`

`sfx_door` `sfx_glass` `sfx_thunder` `sfx_rain` `sfx_clock` `sfx_heartbeat` `sfx_crack`
`sfx_footstep` `sfx_alarm` `sfx_whoosh` `sfx_breath` `sfx_click`

---

## 6. 登记规则（改剧本时注意）

1. 新增任何 `bg_` / `cg_` / 立绘 / `bgm_` / `sfx_` id，**先在本文件补一行**再写进剧本。
2. `bgm` 可以写 `""`（沿用上一场景）或 `"stop"`（停乐）；这两个值不需要登记。
3. `sprite` 写 `""` 表示本行不显示立绘，不需要登记。
4. 立绘 id 的前缀必须是 `story/cast.json` 里该角色的 `pinyin`。
