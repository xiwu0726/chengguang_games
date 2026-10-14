# 对话系统文档

## 概述

对话系统是《时光之痕》视觉小说的核心交互系统，提供了完整的对话、选择、分支剧情支持。系统包含以下核心模块：

1. **对话文本系统** (DialogueSystem) - 管理对话的显示和打字效果
2. **角色立绘显示系统** (CharacterSpriteSystem) - 管理角色立绘的显示和动画
3. **选择对话框系统** (ChoiceSystem) - 管理玩家选择的显示和处理
4. **关键选择点管理器** (KeyChoiceManager) - 管理所有关键选择点的配置和触发
5. **分支剧情逻辑系统** (BranchingStorySystem) - 管理多分支剧情的执行和状态

## 系统架构

```
DialogueManager (核心控制器)
├── DialogueSystem (对话文本系统)
├── CharacterSpriteSystem (角色立绘显示系统)
├── ChoiceSystem (选择对话框系统)
├── KeyChoiceManager (关键选择点管理器)
└── BranchingStorySystem (分支剧情逻辑系统)
```

## 核心组件

### 1. DialogueSystem (对话文本系统)

#### 功能
- 显示对话文本，支持打字效果
- 支持多种对话类型（普通、选择、意识流、梦境、信息、战斗）
- 自动处理对话行切换
- 支持自动继续功能

#### 使用方法

```csharp
// 创建对话行列表
List<DialogueLine> lines = new List<DialogueLine>
{
    new DialogueLine("角色名", 角色立绘, "对话文本"),
    new DialogueLine("角色名", 角色立绘, "更多对话文本")
};

// 开始对话
DialogueManager.Instance.StartDialogue(lines, DialogueType.Normal);
```

#### 对话类型

- **Normal**: 普通对话
- **Choice**: 选择对话
- **StreamOfConsciousness**: 意识流对话（失眠状态）
- **Dream**: 梦境对话（记忆实体）
- **Info**: 信息提示（系统消息）
- **Combat**: 战斗对话

### 2. CharacterSpriteSystem (角色立绘显示系统)

#### 功能
- 显示/隐藏角色立绘
- 支持多种位置设置（左、中、右）
- 支持缩放动画
- 支持淡入淡出动画
- 支持滑动动画

#### 使用方法

```csharp
// 显示角色立绘
CharacterSpriteSystem.CharacterPosition position = CharacterSpriteSystem.CharacterPosition.Left;
DialogueManager.Instance.ShowCharacter("艾莉丝", characterSprite, position);

// 隐藏角色立绘
DialogueManager.Instance.HideCharacter();

// 设置角色位置
DialogueManager.Instance.SetCharacterPosition(CharacterSpriteSystem.CharacterPosition.Right);

// 缩放动画
DialogueManager.Instance.ShowCharacter("艾莉丝", characterSprite, CharacterSpriteSystem.CharacterPosition.Center);
// 自动播放缩放动画
```

#### 角色位置

- **Left**: 左侧位置
- **Center**: 中间位置
- **Right**: 右侧位置

### 3. ChoiceSystem (选择对话框系统)

#### 功能
- 显示选择对话框
- 处理玩家选择
- 支持选择动画
- 支持自动关闭选择对话框

#### 使用方法

```csharp
// 在对话行中设置选择
List<DialogueLine> lines = new List<DialogueLine>
{
    new DialogueLine("艾莉丝", characterSprite, "我们有两个选择：", true)
    {
        choices = new List<Choice>
        {
            new Choice("选项1", new ChoiceEffect { affectionChange = 10 }),
            new Choice("选项2", new ChoiceEffect { insomniaChange = -10f }),
            new Choice("选项3", new ChoiceEffect { triggerBranch = true, branchId = "branch1" })
        }
    }
};

DialogueManager.Instance.StartDialogue(lines);
```

#### 选择效果 (ChoiceEffect)

- **changeAffection**: 改变角色好感度
- **changeInsomnia**: 改变失眠值
- **unlockMemoryShard**: 解锁记忆碎片
- **changeTimeLine**: 改变时间线
- **triggerBranch**: 触发剧情分支
- **triggerDialogueEvent**: 触发对话事件

### 4. KeyChoiceManager (关键选择点管理器)

#### 功能
- 管理所有关键选择点的配置
- 自动执行选择效果
- 管理剧情分支状态
- 记录选择历史

#### 使用方法

```csharp
// 配置关键选择点
KeyChoiceManager.KeyChoice keyChoice = new KeyChoiceManager.KeyChoice
{
    choiceId = "first_meeting",
    choiceName = "初次见面",
    branchId = "alice_good",
    dialogueLineIndex = 1,
    choices = new List<Choice>
    {
        new Choice("礼貌打招呼", new ChoiceEffect
        {
            affectionChange = 10,
            triggerBranch = true,
            branchId = "alice_good"
        }),
        new Choice("保持警惕", new ChoiceEffect
        {
            affectionChange = -10,
            triggerBranch = true,
            branchId = "alice_bad"
        })
    }
};

// 设置关键选择点
keyChoiceManager.keyChoices = new List<KeyChoiceManager.KeyChoice> { keyChoice };

// 触发关键选择
keyChoiceManager.TriggerKeyChoice("first_meeting", 0, () =>
{
    Debug.Log("选择完成");
});
```

### 5. BranchingStorySystem (分支剧情逻辑系统)

#### 功能
- 管理多分支剧情
- 支持分支嵌套
- 记录分支历史
- 支持分支保存和加载

#### 使用方法

```csharp
// 进入分支
DialogueManager.Instance.EnterBranch("default");

// 添加对话行
DialogueLine line = new DialogueLine("艾莉丝", characterSprite, "我们需要调查时空裂缝。");
DialogueManager.Instance.AddDialogueLineToCurrentBranch(line);

// 添加分支选择
BranchChoice choice = new BranchChoice
{
    text = "先去图书馆",
    targetBranchId = "library",
    effect = new ChoiceEffect { unlockMemoryShard = true, memoryShardId = "library_1" }
};
DialogueManager.Instance.AddBranchChoice(choice);

// 进入下一个分支
DialogueManager.Instance.EnterBranch("library");

// 切换分支
DialogueManager.Instance.SwitchToBranch("research_institute");

// 退出当前分支
DialogueManager.Instance.ExitBranch();
```

#### 预定义分支

- **default**: 默认主线
- **alice_good**: 艾莉丝好感线
- **alice_bad**: 艾莉丝负面线
- **rain_good**: 雷恩好感线
- **rain_bad**: 雷恩负面线
- **xiaoya_good**: 小雅好感线
- **xiaoya_bad**: 小雅负面线
- **true_end**: 真结局
- **bad_end_1**: 坏结局1
- **bad_end_2**: 坏结局2

## 对话事件系统

对话系统支持对话事件系统，可以在特定对话事件中触发特定的行为。

```csharp
// 触发对话事件
DialogueManager.Instance.TriggerDialogueEvent("research_institute_event");

// 保存对话进度
DialogueManager.Instance.SaveDialogueProgress();

// 加载对话进度
DialogueManager.Instance.LoadDialogueProgress();
```

## 最佳实践

### 1. 对话行设计

- 保持对话简洁明了
- 使用适当的对话类型
- 合理设置选择效果
- 确保对话逻辑连贯

### 2. 角色立绘使用

- 根据场景选择合适的位置
- 使用动画增强视觉效果
- 合理控制立绘的显示和隐藏
- 避免频繁切换立绘

### 3. 选择设计

- 提供有意义的选项
- 确保选择效果合理
- 避免过于复杂的分支
- 保持剧情连贯性

### 4. 分支管理

- 合理规划分支结构
- 避免过度嵌套
- 记录分支历史
- 支持分支保存和加载

## 性能优化

- 使用对象池管理对话行和选择
- 合理控制动画效果
- 优化UI渲染
- 减少不必要的对象创建

## 扩展性

对话系统设计为高度可扩展，可以通过继承和组合来实现自定义功能：

- 自定义对话类型
- 自定义动画效果
- 自定义选择效果
- 自定义分支逻辑

## 示例代码

完整的示例代码请参考 `DialogueExample.cs` 文件，其中包含：

1. 简单对话示例
2. 选择对话示例
3. 关键选择点示例
4. 分支剧情示例
5. 意识流对话示例
6. 梦境对话示例
7. 战斗对话示例
8. 信息提示示例
9. 完整剧情流程示例

## 注意事项

1. 确保所有引用的脚本都已正确配置
2. 对话文本应使用UTF-8编码
3. 角色立绘应使用合适的尺寸和分辨率
4. 选择对话框应保持简洁易用
5. 分支剧情应避免过于复杂

## 故障排除

### 对话不显示

- 检查DialogueSystem是否正确配置
- 确保UI引用已正确设置
- 检查对话行数据是否正确

### 角色立绘不显示

- 检查CharacterSpriteSystem引用
- 确保角色立绘Sprite已正确设置
- 检查动画设置是否正确

### 选择对话框不显示

- 检查ChoiceSystem引用
- 确保选择对话框Prefab已正确配置
- 检查选择按钮是否正确绑定

### 分支剧情不工作

- 检查BranchingStorySystem引用
- 确保分支ID正确
- 检查分支逻辑是否正确实现

## 更新日志

### v1.0.0 (2025-06)
- 初始版本发布
- 实现基本对话系统
- 实现角色立绘显示
- 实现选择对话框
- 实现关键选择点管理
- 实现分支剧情逻辑
