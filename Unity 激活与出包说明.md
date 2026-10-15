# 《时光之痕》Unity 激活与出包说明

这份文档只讲一件事：**把仓库里这个 Unity 工程变成原生 exe 需要人工做哪一步、之后怎么出包**。

## 现状（2026-10-15）

| 项 | 状态 |
| --- | --- |
| Unity 编辑器 | 已装：`C:\Program Files\Unity 2021.3.45f2\Editor\Unity.exe`（winget 包 `Unity.Unity.2021`，版本 2021.3.45f2，changeset `88f88f591b2e`，与 `ProjectSettings/ProjectVersion.txt` 钉的版本一致） |
| Unity Hub | 已装：MSIX 版 `UnityTechnologies.UnityHub 3.22.2.65535`，路径 `C:\Program Files\WindowsApps\UnityTechnologies.UnityHub_3.22.2.65535_x64__2vrhnee42bhxm` |
| **许可证** | **未激活 —— 这是唯一的人工阻塞项**，见下一步 |
| 构建模块 | 已装 Windows Standalone（`Editor\Data\PlaybackEngines\windowsstandalonesupport`）。Variations 只有 `win64_player_*_mono` 系列，**没有 IL2CPP**，所以出包走 **Mono2x**（工程默认即是，无需改动） |
| 工程文件 | 齐：`ProjectSettings/ProjectVersion.txt`、`Packages/manifest.json`（只依赖 `com.unity.ugui`，无第三方包） |
| 剧本 | `Assets/Resources/Story/**/*.json` 共 54 场 / 170648 字；结局规则表 `Assets/Resources/StoryConfig/endings.json`（20 条 + 兜底 `3-e20`） |
| 占位美术 | `Assets/Resources/Art/{Bg,Cg,Portrait}` 共 **104 张**（30 / 42 / 32，合计 1.77 MB），由 `story/tools/gen-placeholder-art.py` 按资源 id 的语义生成氛围图，**画面上不再烙资源名** |
| 脚本编译 | **已离线验证 0 错误**（用 Unity 自带的 `UnityEngine.*Module.dll` + ugui 包源码在 .NET SDK 里编译整个 `Assets`，见下方「已验证」一节） |

已经能玩的东西：`dist\时光之痕-试玩.exe`（约 92 MB，双击即玩，内嵌剧本与美术，不需要 Unity）。它是 Node SEA 打包的网页播放器，用来在 Unity 不能出包时也能试玩；Unity 版出包后是原生窗口版，画面与音频表现更好。

## 第一步：人工激活许可证（只有你能做）

Unity 必须先登录账号激活一次（Personal 免费版即可）。凭据不要发在对话里，请在图形界面自己完成：

1. 开始菜单搜索 **Unity Hub** 并打开（首次启动会要求登录）。
2. 右上角头像 → **Sign in**，用你的 Unity ID 登录。
3. 左侧 **Licenses（许可证）** → **Add** → **Get a free personal license** → 确认。
4. 看到 Personal 许可证变成 **Active** 即完成。这一步本机需要联网。

> 编辑器也可以命令行激活（`Unity.exe -batchmode -quit -username ... -password ... -serial ...`），但 Personal 版通常走 Hub 登录更省事；命令行的密码/序列号同样不要交给助手。

未激活时构建会失败，本机日志里的原文长这样（`Logs\unity-build-*.log`）：

```
[Licensing::Client] Error: Code 500 while processing request (status: Unable to update licenses. Errors: No ULF license found.,Token not found in cache)
No valid Unity Editor license found. Please activate your license.
Exiting without the bug reporter. Application will terminate with return code 1
```

`scripts\build-unity.ps1` 认到这一行就会直接告诉你「许可证未激活」并给出上面这四步，不会让你去翻日志。

## 第二步：把工程加进 Hub

1. Unity Hub → **Projects** → **Add** → 选择仓库根目录 `C:\Users\xiwu0\source\repos\chengguang_games`。
2. 点开时 Hub 会让你选编辑器版本：选 **2021.3.45f2**（`ProjectVersion.txt` 里已经钉了这个版本，正常不会被要求升级）。

## 第三步：脚本编译（已提前验证过）

工程打开后，Unity 会编译 `Assets/Scripts` 下的脚本（视觉小说运行时 + 剧情系统 + 结局/失眠/回忆碎片等模块）。**这一步我已经脱离编辑器先验证过了**，用的就是把 `Assets\Scripts\**\*.cs` + `Assets\Editor\**\*.cs` 对着 Unity 自带程序集编译的离线校验台：

- **运行时链路零错误**：`Assets/Scripts/Story/`（含 `StoryBootstrap`、`StoryDirector`、`VNView`、`VNUiKit`、`ArtRegistry`、`EndingResolver`）与 `Assets/Editor/BuildScript.cs` 全部编译通过。
- **61 个早期脚手架文件被 `#if CG_LEGACY` 整体屏蔽**：`Audio/Dialogue/Ending/GameFeatures/Insomnia/RogueLike/Test/TimeLine` 这 8 个目录是历史生成的示例代码，跨命名空间互相引用却不 `using`，本身编译不过，而运行时链路**完全不引用它们**。屏蔽（而不是删除）保证出包可编译，同时保留全部代码与 git 历史；想恢复就在 `Project Settings → Player → Script Compilation → Assembly Defined Symbols` 加 `CG_LEGACY` 再逐个修。
- 顺手修掉的真实缺陷：`VNView.cs` 两处 `VNUiKit.AddImage(VNUiKit.Solid(...), color)` —— `Solid` 已经返回 `Image`，再套一层 `AddImage` 是类型错误（CS1503），Unity 里同样编译不过；`TestDemoScene.cs` / `API429BypassExample.cs` 的 `using TMPro;` 改成本项目统一口径的 `UnityEngine.UI.Text`（工程没装 TextMeshPro 包）。
- **结局路由已实现**：`Assets/Scripts/Story/EndingResolver.cs`（`namespace Shiguang.Story`，零 `UnityEngine` 依赖）读取 `StoryConfig/endings.json`，按「第一条命中即胜出、全不命中走兜底」分流，与网页播放器同一套语义。测试在 `tools/ending-resolver-tests/`：C# 侧 91 个用例全过，`node tools/ending-resolver-tests/cross-check.js` 与 JS 实现交叉比对 61 个用例全部一致。

所以正常情况下你打开工程不会看到红色报错，菜单栏直接出现 **时光之痕**。万一还是报错：**把 Console 里的报错原文贴出来**，我来修。

## 第四步：出包

### 方式 A：一键脚本（推荐，激活完直接跑）

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-unity.ps1            # 出包
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-unity.ps1 -Run       # 出完直接跑
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\build-unity.ps1 -CheckOnly # 只让 Unity 编译一遍脚本
```

脚本会自动定位编辑器（找不到就提示用 `-UnityExe` 指定）、比对 `ProjectVersion.txt` 的版本、把日志写到 `Logs\unity-build-<时间戳>.log`、识别许可证未激活并给出激活步骤。退出码：`0` 成功、`1` 构建失败（会打印日志里的报错行）、`2` 前置条件不满足。

> 注意：`.ps1` 必须存成 **UTF-8 with BOM**，否则 Windows PowerShell 5.1 会按 ANSI 解析，中文注释直接把语法炸掉。

### 方式 B：编辑器菜单

1. 菜单 **时光之痕 → 构建 Windows 可执行文件**。
2. 产物：`Build\Windows\时光之痕.exe`（窗口 1600×900，可调大小、可后台运行）。
3. 想顺手跑起来：**时光之痕 → 构建并运行**。

### 方式 C：命令行（无界面，适合批处理）

```powershell
& "C:\Program Files\Unity 2021.3.45f2\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "C:\Users\xiwu0\source\repos\chengguang_games" `
  -executeMethod BuildScript.BuildWindowsBatch `
  -logFile -
```

成功退出码 0、失败 1。`Unity.exe` 是 GUI 子系统程序，直接 `&` 调用**不会等待**（`$LASTEXITCODE` 是空的），要等就用 `Start-Process -Wait -PassThru`。

## 第五步：不用出包也能先试玩

菜单 **时光之痕 → 在编辑器里试玩（Play）**。

**任意场景按 Play 都能开局**：工程是「零场景资源」设计，`StoryBootstrap` 用 `[RuntimeInitializeOnLoadMethod]` 在运行时自动创建相机、Canvas 和全部 UI，不需要预制体、也不用你手动搭场景。构建时会由 `Assets/Editor/BuildScript.cs` 现场生成一个空场景 `Assets/Scenes/MainScene.unity` 当载体。

## 设计约定（改代码前先看）

- **UI 全部由代码在运行时创建**，不手写 `.unity` / `.prefab`（避免手搓 GUID 与 YAML）。
- **中文用 legacy uGUI `Text` + 系统动态字体**（微软雅黑 / SimHei / SimSun 依次尝试，兜底 `LegacyRuntime.ttf`），不用 TextMeshPro——绕开 CJK 字形图集烘焙。
- 美术按「整屏 CG + 矩形立绘框」设计，缺图时 `ArtRegistry` 给占位图而不是报错。
- 剧本一场一个 JSON，规范见 `story/BIBLE.md` 与 `story/SCHEMA.md`；写完后用 `node story/tools/count-story.js` 自查。

## 出包之后

`Build\Windows\时光之痕.exe` 与 `dist\时光之痕-试玩.exe` 是两条并行的交付路径，内容同源（同一批剧本 JSON、同一批美术）。

补正式美术的流水线在 `art/manifest.json`（104 条，含 prompt / 尺寸 / 依据行号 / 立绘身份一致性方案）+ `story/tools/gen-art.js`（默认 `--dry-run` 只校验不出图；真出图需要 API key，见 `art/出图流水线.md`）。图放进 `Assets/Resources/Art` 后，网页版跑 `node story/tools/build-preview.js --standalone` 重新内嵌、Unity 版重新出包，两边都自动替换。
