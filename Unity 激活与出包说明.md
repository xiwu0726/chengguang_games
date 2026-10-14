# 《时光之痕》Unity 激活与出包说明

这份文档只讲一件事：**把仓库里这个 Unity 工程变成原生 exe 需要人工做哪一步、之后怎么出包**。

## 现状（2026-10-13）

| 项 | 状态 |
| --- | --- |
| Unity 编辑器 | 已装：`C:\Program Files\Unity 2021.3.45f2\Editor\Unity.exe`（winget 包 `Unity.Unity.2021`，版本 2021.3.45f2，changeset `88f88f591b2e`） |
| Unity Hub | 已装：MSIX 版 `UnityTechnologies.UnityHub 3.22.2.65535`，路径 `C:\Program Files\WindowsApps\UnityTechnologies.UnityHub_3.22.2.65535_x64__2vrhnee42bhxm` |
| 工程文件 | 齐：`ProjectSettings/ProjectVersion.txt` = 2021.3.45f2、`Packages/manifest.json`（只依赖 `com.unity.ugui`，无第三方包） |
| 剧本 | `Assets/Resources/Story/**/*.json` |
| 占位美术 | `Assets/Resources/Art/{Bg,Portrait,Cg}` 共 83 张 |
| **许可证** | **未激活 —— 这是唯一的人工阻塞项** |

已经能玩的东西：`dist\时光之痕-试玩.exe`（90 MB，双击即玩，内嵌剧本与美术，不需要 Unity）。它是 Node SEA 打包的网页播放器，用来在 Unity 不能出包时也能试玩；Unity 版出包后是原生窗口版，画面与音频表现更好。

## 第一步：人工激活许可证（只有你能做）

Unity 必须先登录账号激活一次（Personal 免费版即可）。凭据不要发在对话里，请在图形界面自己完成：

1. 开始菜单搜索 **Unity Hub** 并打开（首次启动会要求登录）。
2. 右上角头像 → **Sign in**，用你的 Unity ID 登录。
3. 左侧 **Licenses（许可证）** → **Add** → **Get a free personal license** → 确认。
4. 看到 Personal 许可证变成 **Active** 即完成。这一步本机需要联网。

> 编辑器也可以命令行激活（`Unity.exe -batchmode -quit -username ... -password ... -serial ...`），但 Personal 版通常走 Hub 登录更省事；命令行的密码/序列号同样不要交给助手。

## 第二步：把工程加进 Hub

1. Unity Hub → **Projects** → **Add** → 选择仓库根目录 `C:\Users\xiwu0\source\repos\chengguang_games`。
2. 点开时 Hub 会让你选编辑器版本：选 **2021.3.45f2**（`ProjectVersion.txt` 里已经钉了这个版本，正常不会被要求升级）。

## 第三步：首次导入会编译脚本

工程打开后，Unity 会编译 `Assets/Scripts` 下的 60 个脚本（视觉小说运行时 + 剧情系统 + 结局/失眠/回忆碎片等模块）。

- 编译通过：状态栏没有红色报错，菜单栏会出现 **时光之痕**。
- 编译失败：**把 Console 里的报错原文贴出来**，我来修。构建会在有编译错误时直接失败。

## 第四步：出包

### 方式 A：编辑器菜单（推荐）

1. 菜单 **时光之痕 → 构建 Windows 可执行文件**。
2. 产物：`Build\Windows\时光之痕.exe`（窗口 1600×900，可调大小、可后台运行）。
3. 想顺手跑起来：**时光之痕 → 构建并运行**。

### 方式 B：命令行（无界面，适合批处理）

```powershell
& "C:\Program Files\Unity 2021.3.45f2\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "C:\Users\xiwu0\source\repos\chengguang_games" `
  -executeMethod BuildScript.BuildWindowsBatch `
  -logFile -
```

成功退出码 0、失败 1。

## 第五步：不用出包也能先试玩

菜单 **时光之痕 → 在编辑器里试玩（Play）**。

**任意场景按 Play 都能开局**：工程是「零场景资源」设计，`StoryBootstrap` 用 `[RuntimeInitializeOnLoadMethod]` 在运行时自动创建相机、Canvas 和全部 UI，不需要预制体、也不用你手动搭场景。构建时会由 `Assets/Editor/BuildScript.cs` 现场生成一个空场景 `Assets/Scenes/MainScene.unity` 当载体。

## 设计约定（改代码前先看）

- **UI 全部由代码在运行时创建**，不手写 `.unity` / `.prefab`（避免手搓 GUID 与 YAML）。
- **中文用 legacy uGUI `Text` + 系统动态字体**（微软雅黑 / SimHei / SimSun 依次尝试，兜底 `LegacyRuntime.ttf`），不用 TextMeshPro——绕开 CJK 字形图集烘焙。
- 美术按「整屏 CG + 矩形立绘框」设计，缺图时 `ArtRegistry` 给占位图而不是报错。
- 剧本一场一个 JSON，规范见 `story/BIBLE.md` 与 `story/SCHEMA.md`；写完后用 `node story/tools/count-story.js` 自查。

## 出包之后

`Build\Windows\时光之痕.exe` 与 `dist\时光之痕-试玩.exe` 是两条并行的交付路径，内容同源（同一批剧本 JSON、同一批占位美术）。美术补进 `Assets/Resources/Art` 后重新出包即自动替换。
