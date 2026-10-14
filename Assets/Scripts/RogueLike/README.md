# Rogue Like元素系统文档

## 概述

Rogue Like元素系统为《时光之痕》视觉小说添加了随机性和重玩价值，包括随机事件、随机选择、随机奖励、难度递增、多周目体验、隐藏要素和随机挑战。

## 系统架构

```
RogueLikeController (核心控制器)
├── RogueLikeEventManager (随机事件管理器)
├── RogueLikeChoiceSystem (随机选择系统)
├── RogueLikeRewardSystem (随机奖励系统)
├── RogueLikeDifficultySystem (难度递增系统)
├── RogueLikeNewGamePlusSystem (多周目重玩性系统)
├── RogueLikeHiddenElementSystem (隐藏要素系统)
└── RogueLikeChallengeSystem (随机挑战系统)
```

## 核心组件

### 1. RogueLikeEventManager (随机事件管理器)

#### 功能
- 管理所有随机事件的配置和触发
- 检查事件触发条件
- 处理事件效果和应用
- 记录事件触发历史

#### 使用方法

```csharp
// 添加随机事件
RogueLikeEventData event = new RogueLikeEventData("event1", "偶遇", RogueLikeEventType.Normal);
event.triggerProbability = 15f;
event.canRepeat = true;
RogueLikeEventManager.Instance.AddEvent(event);

// 触发事件
RogueLikeEventManager.Instance.TriggerEvent(event);

// 获取可触发事件
List<RogueLikeEventData> triggerableEvents = RogueLikeEventManager.Instance.GetTriggerableEvents();
```

#### 事件类型

- **Normal**：正常事件，对剧情有轻微影响
- **Important**：重要事件，对剧情有重大影响
- **Random**：随机事件，完全随机的事件
- **Dangerous**：危险事件，可能带来负面影响
- **Beneficial**：有益事件，明显的正面效果
- **Special**：特殊事件，特殊的剧情事件
- **Hidden**：隐藏事件，需要特定条件触发

#### 事件稀有度

- **Common**：常见事件，触发概率高
- **Rare**：稀有事件，触发概率中等
- **Legendary**：传奇事件，触发概率低
- **Mythic**：传说事件，触发概率极低

### 2. RogueLikeChoiceSystem (随机选择系统)

#### 功能
- 管理随机选择的影响
- 应用选择效果
- 处理选择后的剧情变化

#### 使用方法

```csharp
// 应用选择效果
RogueLikeChoiceSystem.Instance.ApplyChoiceEffects(eventData.choices);
```

#### 选择效果

- **InsomniaIncrease**：增加失眠值
- **InsomniaDecrease**：减少失眠值
- **MemoryShardUnlock**：解锁记忆碎片
- **TimeShardUnlock**：解锁时间碎片
- **AffinityIncrease**：增加好感度
- **AffinityDecrease**：减少好感度
- **BranchUnlock**：解锁剧情分支
- **TimeLineUnlock**：解锁时间线
- **ItemUnlock**：解锁物品
- **AchievementUnlock**：解锁成就
- **HiddenEventUnlock**：解锁隐藏事件

### 3. RogueLikeRewardSystem (随机奖励系统)

#### 功能
- 管理随机奖励的发放
- 生成随机奖励列表
- 应用奖励效果

#### 使用方法

```csharp
// 发放奖励
RogueLikeRewardSystem.Instance.GrantReward(eventData.effects);

// 生成随机奖励
List<RogueLikeEffect> rewards = RogueLikeRewardSystem.Instance.GenerateRandomRewards(3);
```

### 4. RogueLikeDifficultySystem (难度递增系统)

#### 功能
- 管理游戏难度的递增
- 根据游戏进度调整难度
- 提供难度描述

#### 难度等级

- **Easy**：简单
- **Normal**：正常
- **Hard**：困难
- **VeryHard**：极难
- **Insane**：疯狂

#### 使用方法

```csharp
// 增加难度
RogueLikeDifficultySystem.Instance.IncreaseDifficulty();

// 获取当前难度
RogueLikeDifficultySystem.DifficultyLevel current = RogueLikeDifficultySystem.Instance.GetCurrentDifficulty();

// 获取难度描述
string desc = RogueLikeDifficultySystem.Instance.GetDifficultyDescription();

// 重置难度
RogueLikeDifficultySystem.Instance.ResetDifficulty();
```

### 5. RogueLikeNewGamePlusSystem (多周目重玩性系统)

#### 功能
- 管理多周目体验
- 解锁周目专属内容
- 调整难度倍率

#### 使用方法

```csharp
// 进入新周目
RogueLikeNewGamePlusSystem.Instance.EnterNewRun();

// 获取当前周目
int run = RogueLikeNewGamePlusSystem.Instance.GetCurrentRun();

// 获取难度倍率
float multiplier = RogueLikeNewGamePlusSystem.Instance.GetDifficultyMultiplier();

// 解锁内容
RogueLikeNewGamePlusSystem.Instance.UnlockContent("secret_event");

// 检查内容是否解锁
bool unlocked = RogueLikeNewGamePlusSystem.Instance.IsContentUnlocked("secret_event");
```

### 6. RogueLikeHiddenElementSystem (隐藏要素系统)

#### 功能
- 管理隐藏剧情和彩蛋
- 跟踪隐藏要素发现状态
- 提供隐藏要素解锁条件

#### 使用方法

```csharp
// 发现隐藏要素
RogueLikeHiddenElementSystem.Instance.DiscoverHiddenElement("hidden_event_1");

// 检查隐藏要素是否已发现
bool discovered = RogueLikeHiddenElementSystem.Instance.IsHiddenElementDiscovered("hidden_event_1");

// 获取所有已发现的隐藏要素
List<RogueLikeHiddenElement> discovered = RogueLikeHiddenElementSystem.Instance.GetDiscoveredElements();

// 添加隐藏要素
RogueLikeHiddenElement element = new RogueLikeHiddenElement
{
    elementId = "hidden_event_1",
    elementName = "隐藏事件1",
    description = "这是一个隐藏事件",
    unlockConditions = new List<string> { "收集所有记忆碎片" }
};
RogueLikeHiddenElementSystem.Instance.AddHiddenElement(element);
```

### 7. RogueLikeChallengeSystem (随机挑战系统)

#### 功能
- 随机触发小挑战
- 管理挑战完成状态
- 提供挑战奖励

#### 挑战类型

- **SpeedChallenge**：速度挑战
- **AccuracyChallenge**：精度挑战
- **MemoryChallenge**：记忆挑战
- **ChoiceChallenge**：选择挑战
- **SurvivalChallenge**：存活挑战

#### 使用方法

```csharp
// 随机触发挑战
RogueLikeChallengeSystem.Instance.TriggerRandomChallenge();

// 完成挑战
RogueLikeChallengeSystem.Instance.CompleteChallenge("challenge_1");

// 获取未完成的挑战
List<RogueLikeChallenge> active = RogueLikeChallengeSystem.Instance.GetActiveChallenges();

// 添加挑战
RogueLikeChallenge challenge = new RogueLikeChallenge
{
    challengeId = "challenge_1",
    challengeName = "快速反应挑战",
    description = "在5秒内做出选择",
    challengeType = RogueLikeChallengeSystem.ChallengeType.SpeedChallenge,
    reward = "获得时间碎片x5",
    onCompleted = () => Debug.Log("挑战完成！")
};
RogueLikeChallengeSystem.Instance.AddChallenge(challenge);
```

## 实现文件

- `Assets/Scripts/RogueLike/Enums/RogueLikeEventType.cs`
- `Assets/Scripts/RogueLike/Data/RogueLikeEventData.cs`
- `Assets/Scripts/RogueLike/Managers/RogueLikeEventManager.cs`
- `Assets/Scripts/RogueLike/Systems/RogueLikeChoiceSystem.cs`
- `Assets/Scripts/RogueLike/Systems/RogueLikeRewardSystem.cs`
- `Assets/Scripts/RogueLike/Systems/RogueLikeDifficultySystem.cs`
- `Assets/Scripts/RogueLike/Systems/RogueLikeNewGamePlusSystem.cs`
- `Assets/Scripts/RogueLike/Systems/RogueLikeHiddenElementSystem.cs`
- `Assets/Scripts/RogueLike/Systems/RogueLikeChallengeSystem.cs`

## 系统特色

1. **随机性**：随机事件、随机选择、随机奖励
2. **重玩价值**：多周目体验，每次游玩不同
3. **难度递增**：随着游戏进行难度逐渐增加
4. **隐藏要素**：隐藏剧情和彩蛋增加探索乐趣
5. **随机挑战**：小挑战增加游戏趣味性
6. **灵活性**：易于添加新事件和挑战
7. **可扩展性**：支持自定义事件和挑战类型

## 最佳实践

### 1. 事件设计

- 合理设置触发概率
- 设计有意义的选项
- 确保事件效果平衡
- 提供多样化的选项

### 2. 奖励设计

- 奖励应该有价值
- 奖励应该有吸引力
- 奖励应该有风险回报
- 奖励应该有持续性

### 3. 隐藏要素

- 设计有趣的隐藏内容
- 提供合理的解锁条件
- 增加探索乐趣
- 避免过度隐藏

### 4. 难度设计

- 难度递增要平滑
- 难度调整要合理
- 难度应该有反馈
- 难度应该有选择

## 性能优化

- 使用对象池管理事件和挑战
- 优化随机算法
- 减少不必要的计算
- 合理管理内存

## 扩展性

Rogue Like元素系统设计为高度可扩展：

1. **添加新事件类型**
2. **添加新挑战类型**
3. **自定义事件效果**
4. **自定义奖励类型**
5. **自定义难度曲线**

## 注意事项

1. 确保所有引用的脚本都已正确配置
2. 事件触发概率要合理设置
3. 奖励效果要平衡
4. 隐藏要素不要太难发现
5. 难度递增不要太快

## 更新日志

### v1.0.0 (2025年6月)
- 初始版本发布
- 实现7个核心系统
- 完整的文档和示例代码
