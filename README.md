# 《时光之痕》Unity项目

## 项目概述

《时光之痕》是一款科幻奇幻视觉小说游戏，结合了时间线穿梭、失眠状态系统、记忆碎片收集等创新机制。

## 项目状态

✅ **代码已完成**：45个C#脚本文件，约11300行代码
✅ **文档已完成**：32个详细设计文档
⏳ **Unity项目**：正在创建中...

## 核心系统

1. **对话系统**：支持多分支剧情、角色立绘、选择对话框
2. **失眠状态系统**：4级失眠状态，影响视觉效果和对话方式
3. **时间线切换系统**：4个时间线（主时间线、过去、未来、梦境）
4. **记忆碎片收集系统**：3种记忆类型，收集记忆解锁剧情
5. **多结局系统**：6种结局类型
6. **音频系统**：BGM、音效、语音配音管理
7. **Rogue Like元素**：随机事件、随机奖励、多周目重玩性

## 快速开始

### 方法1：使用Unity Hub（推荐）

1. 打开Unity Hub
2. 创建新项目，选择"3D"模板
3. 将本项目中的 `Assets/Scripts/` 文件夹复制到Unity项目的 `Assets/` 目录下
4. 打开项目，创建一个新场景
5. 将所有脚本拖入场景中
6. 运行项目

### 方法2：等待自动创建

子代理正在自动创建Unity项目，完成后会提供项目文件。

## 项目结构

```
TimeOfFragments/
├── Assets/
│   ├── Scripts/
│   │   ├── Dialogue/          # 对话系统
│   │   ├── Insomnia/          # 失眠状态系统
│   │   ├── TimeLine/          # 时间线系统
│   │   ├── Ending/            # 多结局系统
│   │   ├── Audio/             # 音频系统
│   │   ├── RogueLike/         # Rogue Like系统
│   │   └── Test/              # 测试系统
│   └── Scenes/
│       └── MainScene.unity    # 主场景
├── ProjectSettings/
└── README.md
```

## 系统架构

### 对话系统
- `DialogueManager`：统一管理对话
- `DialogueSystem`：对话文本显示
- `CharacterSpriteSystem`：角色立绘显示
- `ChoiceSystem`：选择对话框
- `KeyChoiceManager`：关键选择点
- `BranchingStorySystem`：分支剧情

### 失眠状态系统
- `InsomniaController`：主控制器
- `InsomniaStateManager`：状态管理
- `SpaceDistortionEffect`：空间扭曲效果
- `TimeLineVisualizer`：时间线可视化
- `InsomniaAudioManager`：失眠音效
- `StreamOfConsciousnessDialogue`：意识流对话

### 时间线系统
- `TimeLineController`：主控制器
- `TimeLineManager`：时间线管理
- `TimeLineSwitcher`：切换机制
- `TimeLineTransitionEffect`：切换效果

### 多结局系统
- `EndingController`：主控制器
- `EndingManager`：结局管理
- `EndingTrigger`：结局触发

## 开发计划

1. ✅ 完成核心设计文档
2. ✅ 实现所有核心系统
3. ⏳ 创建Unity项目
4. ⏳ 制作美术资源
5. ⏳ 制作音频资源
6. ⏳ 编写完整剧情
7. ⏳ 测试和优化
8. ⏳ 发布

## 技术栈

- **引擎**：Unity 3D
- **语言**：C# (C# 8.0+)
- **平台**：PC、移动端（iOS、Android）

## 许可证

本项目仅供学习和研究使用。

## 联系方式

如有问题或建议，请通过项目仓库提交Issue。

---

**最后更新**：2025年6月
**项目状态**：开发中
