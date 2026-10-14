# 多结局系统文档

## 概述

多结局系统是《时光之痕》视觉小说的核心系统之一，为玩家提供了丰富的结局选择，包括正常结局、英雄结局、悲剧结局、开放结局、奇迹结局和隐藏结局。系统支持复杂的结局条件检查、结局触发和结局展示。

## 系统架构

```
EndingController (核心控制器)
├── EndingManager (结局管理器)
│   ├── EndingData (结局数据)
│   └── EndingCondition (结局条件)
├── EndingTrigger (结局触发器)
│   └── EndingTriggerData (触发器数据)
└── EndingUI (结局UI)
    ├── 结局面板
    ├── 结局CG显示
    └── 结局信息展示
```

## 核心组件

### 1. EndType (结局类型枚举)

#### 结局类型

1. **Normal (正常结局)**
   - 描述：主角成功解决时空裂缝问题，世界恢复正常
   - 评级：B级
   - 难度：普通
   - 可重复：是

2. **Heroic (英雄结局)**
   - 描述：主角牺牲自己拯救世界，成为传奇英雄
   - 评级：S级
   - 难度：困难
   - 可重复：否

3. **Tragic (悲剧结局)**
   - 描述：主角失败，世界被毁灭，或主角失去一切
   - 评级：F级
   - 难度：极难
   - 可重复：否

4. **Open (开放结局)**
   - 描述：主角选择留下，继续探索时空，或离开世界
   - 评级：A级
   - 难度：普通
   - 可重复：是

5. **Miracle (奇迹结局)**
   - 描述：主角创造奇迹，拯救世界并获得完美结局
   - 评级：S级
   - 难度：极难
   - 可重复：否

6. **Hidden (隐藏结局)**
   - 描述：需要满足特定条件才能解锁
   - 评级：S级
   - 难度：隐藏
   - 可重复：否

### 2. EndingData (结局数据)

#### 数据结构

```csharp
public class EndingData
{
    public string endingId;           // 结局ID
    public string endingName;         // 结局名称
    public EndType endType;           // 结局类型
    public EndingRating rating;       // 结局评级
    public EndingDifficulty difficulty; // 结局难度
    public string description;        // 结局描述
    public string detail;             // 结局详细信息
    public List<EndingCondition> conditions; // 结局触发条件
    public List<Sprite> cgCollection; // 结局CG集合
    public string backgroundMusic;    // 结局音乐
    public List<string> soundEffects; // 结局音效
    public bool canRepeat;            // 是否可重复达成
    public bool isAchieved;           // 是否已达成
    public int achieveCount;          // 达成次数
    public List<string> unlockConditions; // 解锁条件
    public List<DialogueLine> triggerDialogue; // 触发对话
    public System.Func<bool> triggerCondition; // 触发条件
}
```

#### 结局条件

```csharp
public class EndingCondition
{
    public ConditionType conditionType;  // 条件类型
    public string conditionId;           // 条件ID
    public int targetValue;              // 目标值
    public ConditionOperator operatorType; // 操作符
    public System.Func<bool> checkCondition; // 检查条件
}

public enum ConditionType
{
    Unknown,
    Affection,        // 角色好感度
    Insomnia,         // 失眠值
    MemoryShard,      // 记忆碎片数量
    Branch,           // 剧情分支
    TimeLine,         // 时间线
    Item,             // 物品收集
    Achievement,      // 成就
    Choice,           // 选择
    None              // 无条件
}

public enum ConditionOperator
{
    Equal,            // 等于
    NotEqual,         // 不等于
    Greater,          // 大于
    GreaterOrEqual,   // 大于等于
    Less,             // 小于
    LessOrEqual,      // 小于等于
    Contains          // 包含
}
```

### 3. EndingManager (结局管理器)

#### 功能

- 管理所有结局的配置和状态
- 触发结局并检查条件
- 记录已达成的结局
- 保存和加载结局进度

#### 使用方法

```csharp
// 触发结局
EndingManager.Instance.TriggerEnding("normal_ending");

// 获取结局
EndingData ending = EndingManager.Instance.GetEnding("normal_ending");

// 检查是否已达成
bool achieved = EndingManager.Instance.IsEndingAchieved("normal_ending");

// 获取已达成的结局列表
List<EndingData> achieved = EndingManager.Instance.GetAchievedEndings();
```

### 4. EndingTrigger (结局触发器)

#### 功能

- 自动检查结局触发条件
- 管理结局触发器列表
- 支持手动触发结局检查

#### 使用方法

```csharp
// 添加结局触发器
EndingTriggerData trigger = new EndingTriggerData("normal_ending", "FinalScene", () =>
{
    // 检查条件
    return CheckNormalEndingConditions();
});

EndingTrigger.Instance.AddEndingTrigger(trigger);

// 手动检查特定结局
EndingTrigger.Instance.CheckEnding("normal_ending");

// 检查所有结局
EndingTrigger.Instance.CheckAllEndings();
```

### 5. EndingUI (结局UI)

#### 功能

- 显示结局面板
- 展示结局信息（名称、描述、评级、类型）
- 显示结局CG
- 提供继续、重玩、新游戏、选择等按钮

#### 使用方法

```csharp
// 显示结局
EndingData ending = EndingManager.Instance.GetEnding("normal_ending");
EndingUI.Instance.ShowEnding(ending);

// 隐藏结局
EndingUI.Instance.HideEnding();

// 检查是否显示结局
bool showing = EndingUI.Instance.IsShowing();
```

## 结局条件设计

### 1. 正常结局条件

```csharp
private bool CheckNormalEndingConditions()
{
    // 检查所有角色是否存活
    bool allCompanionsAlive = affectionManager.IsCompanionAlive("艾莉丝") &&
                             affectionManager.IsCompanionAlive("雷恩") &&
                             affectionManager.IsCompanionAlive("小雅");

    // 检查记忆碎片收集
    bool allMemoryShardsCollected = memoryShardSystem.GetMemoryShardCount() >= 50;

    return allCompanionsAlive && allMemoryShardsCollected;
}
```

### 2. 英雄结局条件

```csharp
private bool CheckHeroicEndingConditions()
{
    // 检查所有角色是否存活
    bool allCompanionsAlive = affectionManager.IsCompanionAlive("艾莉丝") &&
                             affectionManager.IsCompanionAlive("雷恩") &&
                             affectionManager.IsCompanionAlive("小雅");

    // 检查是否选择了牺牲
    bool sacrificeChosen = CheckSacrificeChoice();

    return allCompanionsAlive && sacrificeChosen;
}
```

### 3. 悲剧结局条件

```csharp
private bool CheckTragicEndingConditions()
{
    // 检查是否失去了重要角色
    bool companionDied = !affectionManager.IsCompanionAlive("艾莉丝") ||
                       !affectionManager.IsCompanionAlive("雷恩") ||
                       !affectionManager.IsCompanionAlive("小雅");

    // 检查是否做出了错误选择
    bool wrongChoice = CheckWrongChoice();

    return companionDied && wrongChoice;
}
```

### 4. 开放结局条件

```csharp
private bool CheckOpenEndingConditions()
{
    // 检查所有角色是否存活
    bool allCompanionsAlive = affectionManager.IsCompanionAlive("艾莉丝") &&
                             affectionManager.IsCompanionAlive("雷恩") &&
                             affectionManager.IsCompanionAlive("小雅");

    // 检查是否选择了离开
    bool chooseToLeave = CheckLeaveChoice();

    return allCompanionsAlive && chooseToLeave;
}
```

### 5. 奇迹结局条件

```csharp
private bool CheckMiracleEndingConditions()
{
    // 检查所有角色是否存活
    bool allCompanionsAlive = affectionManager.IsCompanionAlive("艾莉丝") &&
                             affectionManager.IsCompanionAlive("雷恩") &&
                             affectionManager.IsCompanionAlive("小雅");

    // 检查是否收集了所有特殊物品
    bool allItemsCollected = CheckAllItems();

    // 检查是否做出了完美选择
    bool perfectChoices = CheckPerfectChoices();

    return allCompanionsAlive && allItemsCollected && perfectChoices;
}
```

### 6. 隐藏结局条件

```csharp
private bool CheckHiddenEndingConditions()
{
    // 检查是否收集了所有记忆碎片
    bool allMemoryShardsCollected = memoryShardSystem.GetMemoryShardCount() >= 100;

    // 检查是否与所有角色好感度达到100
    bool allAffection100 = affectionManager.GetAffection("艾莉丝") >= 100 &&
                          affectionManager.GetAffection("雷恩") >= 100 &&
                          affectionManager.GetAffection("小雅") >= 100;

    return allMemoryShardsCollected && allAffection100;
}
```

## 结局触发流程

```
1. 玩家完成所有剧情内容
2. 触发最终场景
3. 检查结局触发条件
4. 如果条件满足：
   - 标记结局为已达成
   - 播放结局音效
   - 显示结局面板
   - 展示结局CG和信息
   - 保存成就
5. 如果条件不满足：
   - 不触发结局
```

## 结局展示UI

### 结局面板元素

1. **标题**：显示结局名称
2. **描述**：显示结局简要描述
3. **评级**：显示结局评级（S/A/B/C/D/F）
4. **类型**：显示结局类型（正常/英雄/悲剧/开放/奇迹/隐藏）
5. **详细信息**：显示结局详细描述
6. **CG展示**：显示结局CG图片
7. **按钮**：
   - 继续按钮：继续游戏或查看其他内容
   - 重玩按钮：重玩游戏
   - 新游戏按钮：开始新游戏
   - 选择按钮：选择其他内容

### 按钮状态

- **正常结局**：显示继续按钮
- **英雄结局**：显示新游戏和选择按钮
- **悲剧结局**：显示重玩、新游戏和选择按钮
- **开放结局**：显示继续、新游戏和选择按钮
- **奇迹结局**：显示新游戏和选择按钮
- **隐藏结局**：显示选择按钮

## 最佳实践

### 1. 结局设计

- 确保每个结局都有独特的描述和CG
- 合理设置结局难度
- 提供清晰的结局触发条件
- 考虑结局的可重复性

### 2. 条件设计

- 条件应该清晰明确
- 避免过于复杂的条件组合
- 考虑不同玩家的选择
- 提供多种达成路径

### 3. UI设计

- 结局面板应该美观大方
- CG展示应该有动画效果
- 按钮功能应该明确
- 提供多种操作选项

### 4. 保存和加载

- 自动保存达成状态
- 支持读取存档
- 提供进度追踪

## 性能优化

- 使用对象池管理结局数据
- 优化CG加载和显示
- 减少不必要的计算
- 使用异步加载技术

## 扩展性

多结局系统设计为高度可扩展，可以通过以下方式扩展：

1. **添加新结局类型**
2. **自定义结局条件**
3. **添加自定义CG**
4. **自定义结局音乐**
5. **添加结局成就系统**

## 示例代码

完整的示例代码请参考 `EndingExample.cs` 文件，其中包含：

1. 6种结局的配置
2. 6种结局的触发条件
3. 结局触发器的使用
4. 结局UI的展示

## 注意事项

1. 确保所有引用的脚本都已正确配置
2. 结局CG应该有合适的尺寸和分辨率
3. 结局音乐和音效应该符合氛围
4. 结局描述应该清晰易懂
5. 按钮功能应该明确

## 故障排除

### 结局不触发

- 检查结局触发条件是否正确
- 确认结局数据已正确配置
- 检查触发器是否已正确添加

### 结局UI不显示

- 检查EndingUI引用是否正确
- 确认结局面板已启用
- 检查CG资源是否存在

### 结局无法重复达成

- 检查结局的canRepeat设置
- 确认结局状态已正确重置

## 更新日志

### v1.0.0 (2025-06)
- 初始版本发布
- 实现6种结局类型
- 实现结局触发系统
- 实现结局UI系统
- 实现结局条件检查系统
