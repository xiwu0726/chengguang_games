# 《时光之痕》时间线切换系统实现文档

## 概述

本文档描述了《时光之痕》视觉小说项目中的时间线切换机制实现。该系统支持4个时间线之间的切换，包括主时间线（2150年）、过去时间线（1950年代）、未来时间线（2200年）和梦境世界，并提供视觉和音频转换效果。

## 系统架构

### 核心组件

1. **TimeLineType** - 时间线类型枚举
   - 定义4个时间线类型
   - 定义切换方式枚举

2. **TimeLineConfig** - 时间线配置类
   - 每个时间线的详细配置
   - 视觉、音频、交互参数

3. **TimeLineTransitionEffect** - 切换效果控制器
   - 处理切换时的视觉和音频效果
   - 支持颜色过渡、故障效果、暗角等

4. **TimeLineManager** - 时间线管理器
   - 管理当前时间线和配置
   - 处理切换逻辑和冷却管理

5. **TimeLineSwitcher** - 切换器
   - 执行具体切换流程
   - 检查切换条件和限制

6. **TimeLineUI** - UI管理器
   - 时间线选择界面
   - 信息显示和状态反馈

7. **MemoryShardSystem** - 记忆碎片系统
   - 管理记忆碎片的收集
   - 记忆查看功能

8. **TimeLineController** - 主控制器
   - 协调所有子系统
   - 提供统一接口

## 时间线类型

### 1. 主时间线（2150年）
- **时间特征**：正常时间流逝
- **地点特征**：新东京
- **人物特征**：正常状态
- **环境特征**：现代科技感
- **事件特征**：正常的日常事件

### 2. 过去时间线（1950年代）
- **时间特征**：1950年代
- **地点特征**：历史图书馆、城市街道
- **人物特征**：1950年代的人物
- **环境特征**：复古风格
- **事件特征**：被掩盖的历史事件

### 3. 未来时间线（2200年）
- **时间特征**：2200年
- **地点特征**：废墟城市、时空裂缝中心
- **人物特征**：变异的人类
- **环境特征**：荒凉、废墟
- **事件特征**：时空崩溃后的景象

### 4. 梦境世界
- **时间特征**：非线性时间
- **地点特征**：抽象的空间
- **人物特征**：记忆实体
- **环境特征**：梦幻、美丽
- **事件特征**：潜意识的具象化

## 切换方式

### 1. 时间碎片切换
- 基本操作：选择使用时间碎片 -> 选择目标时间线 -> 确认切换 -> 进入目标时间线
- 限制：需要冷却时间、不同时间线需要不同等级的时间碎片、过度使用可能导致严重失眠

### 2. 时空裂缝切换
- 基本操作：接近时空裂缝 -> 选择穿越方式 -> 确认穿越 -> 进入目标时间线
- 限制：时空裂缝不稳定、穿越有风险、可能迷失

### 3. 记忆实体切换
- 基本操作：与记忆实体对话 -> 选择共享记忆 -> 确认共享 -> 进入记忆世界
- 限制：需要信任记忆实体、可能受到情感冲击、记忆可能不完整

## 切换效果

### 视觉效果

#### 主时间线到过去时间线
- 颜色变化：从现代色彩变为复古色彩
- 场景变化：从现代场景变为复古场景
- 时间线流动：从稳定变为流动
- 空间扭曲：从正常变为扭曲

#### 主时间线到未来时间线
- 颜色变化：从现代色彩变为暗淡色彩
- 场景变化：从现代场景变为废墟场景
- 时间线流动：从稳定变为混乱
- 空间扭曲：从正常变为严重扭曲

#### 主时间线到梦境世界
- 颜色变化：从现实色彩变为梦幻色彩
- 场景变化：从现实场景变为抽象场景
- 时间线流动：从稳定变为流动
- 空间扭曲：从正常变为扭曲

### 音效效果

#### 主时间线到过去时间线
- 音效变化：从现代音效变为复古音效
- 时间流动声：从稳定变为流动
- 环境音：从现代环境音变为复古环境音

#### 主时间线到未来时间线
- 音效变化：从现代音效变为低沉音效
- 时间流动声：从稳定变为混乱
- 环境音：从现代环境音变为废墟环境音

#### 主时间线到梦境世界
- 音效变化：从现实音效变为梦幻音效
- 时间流动声：从稳定变为流动
- 环境音：从现实环境音变为梦幻环境音

## 使用方法

### 1. 在场景中添加组件

1. 创建一个空GameObject命名为"TimeLineController"
2. 添加TimeLineController组件
3. 配置引用（可选，会自动创建默认实例）

### 2. 基本使用

```csharp
// 获取TimeLineController实例
TimeLineController timeLineController = FindObjectOfType<TimeLineController>();

// 切换到过去时间线
timeLineController.SwitchToTimeLine(TimeLineType.Past);

// 切换到未来时间线
timeLineController.SwitchToTimeLine(TimeLineType.Future);

// 切换到梦境世界
timeLineController.SwitchToTimeLine(TimeLineType.Dream);

// 切换到主时间线
timeLineController.SwitchToTimeLine(TimeLineType.Main);

// 打开时间线选择面板
timeLineController.OpenTimeLineSelector();

// 关闭时间线选择面板
timeLineController.CloseTimeLineSelector();

// 获取当前时间线
TimeLineType currentTimeline = timeLineController.GetCurrentTimeline();

// 获取当前年份
int currentYear = timeLineController.GetCurrentYear();

// 检查是否可以切换到指定时间线
bool canSwitch = timeLineController.CanSwitchTo(TimeLineType.Past);

// 获取当前冷却时间
float cooldown = timeLineController.GetCurrentCooldownTime();
```

### 3. 使用切换器直接操作

```csharp
// 获取TimeLineSwitcher实例
TimeLineSwitcher switcher = FindObjectOfType<TimeLineSwitcher>();

// 切换到过去时间线（使用时间碎片）
switcher.SwitchToPast(TimeLineSwitchMethod.TimeFragment);

// 切换到未来时间线（使用时空裂缝）
switcher.SwitchToFuture(TimeLineSwitchMethod.SpaceTimeCrack);

// 切换到梦境世界（使用记忆实体）
switcher.SwitchToDream(TimeLineSwitchMethod.MemoryEntity);

// 检查是否可以切换到指定时间线
bool canSwitchToPast = switcher.CanSwitchTo(TimeLineType.Past);

// 设置切换方式权限
switcher.SetAllowTimeFragmentSwitch(true);
switcher.SetAllowCrackSwitch(true);
switcher.SetAllowMemoryEntitySwitch(true);
```

### 4. 使用记忆碎片系统

```csharp
// 获取MemoryShardSystem实例
MemoryShardSystem memoryShardSystem = FindObjectOfType<MemoryShardSystem>();

// 收集记忆碎片
MemoryShard shard = new MemoryShard("id1", "记忆标题", "记忆内容", MemoryShardType.Personal, "2150", "新东京", "林晓");
memoryShardSystem.CollectMemoryShard(shard);

// 检查是否已收集
bool hasShard = memoryShardSystem.HasMemoryShard(shard);

// 获取所有收集的记忆碎片
List<MemoryShard> allShards = memoryShardSystem.GetAllCollectedShards();

// 根据类型获取记忆碎片
List<MemoryShard> personalShards = memoryShardSystem.GetMemoryShardsByType(MemoryShardType.Personal);

// 显示记忆碎片
memoryShardSystem.ShowMemoryShard(shard);

// 获取收集数量
int collectedCount = memoryShardSystem.GetCollectedCount();
int totalCount = memoryShardSystem.GetTotalCount();
```

### 5. 使用UI管理器

```csharp
// 获取TimeLineUI实例
TimeLineUI timeLineUI = FindObjectOfType<TimeLineUI>();

// 打开时间线选择面板
timeLineUI.OpenPanel();

// 关闭时间线选择面板
timeLineUI.ClosePanel();

// 检查面板是否打开
bool isOpen = timeLineUI.IsPanelOpen();

// 设置是否显示冷却倒计时
timeLineUI.SetShowCooldown(true);

// 重置UI
timeLineUI.Reset();
```

## 自定义配置

### 修改时间线配置

可以在TimeLineManager中修改时间线参数：

```csharp
// 获取时间线配置
TimeLineConfig config = timeLineManager.GetConfig(TimeLineType.Past);

// 修改配置
config.distortionIntensity = 0.7f;
config.transitionDuration = 2.0f;
config.cooldownTime = 45f;
```

### 添加自定义切换方式

```csharp
// 在TimeLineSwitcher中添加新的切换方法
public bool SwitchToTimeLine(TimeLineType targetTimeline, TimeLineSwitchMethod method)
{
    // 实现自定义切换逻辑
    switch (method)
    {
        case TimeLineSwitchMethod.CustomMethod:
            // 自定义切换逻辑
            break;
    }
}
```

### 添加自定义记忆碎片

```csharp
// 创建新的记忆碎片
MemoryShard newShard = new MemoryShard(
    "custom-id",
    "自定义记忆标题",
    "自定义记忆内容",
    MemoryShardType.Historical,
    "2150",
    "新东京",
    "林晓"
);

// 收集记忆碎片
memoryShardSystem.CollectMemoryShard(newShard);
```

## 性能优化

1. **自动更新**：系统默认每0.5秒更新一次，可以调整updateInterval来平衡性能和响应速度
2. **对象池**：记忆碎片系统使用列表管理，避免频繁创建销毁
3. **事件系统**：使用System.Action减少直接调用
4. **缓存配置**：时间线配置在初始化时创建，运行时只读取

## 注意事项

1. **冷却时间**：每次切换后会有冷却时间，需要等待冷却结束
2. **能力要求**：某些时间线需要特殊能力或更高等级的时间碎片
3. **失眠影响**：切换时间线会增加失眠值，过度切换可能导致严重失眠
4. **跨场景**：TimeLineController设置为DontDestroyOnLoad，确保跨场景保持状态
5. **UI引用**：确保所有UI引用正确设置，否则UI可能无法显示

## 扩展建议

1. **更多视觉效果**：添加屏幕闪烁、色差、粒子效果等
2. **更多音效**：添加环境音、背景音乐变化等
3. **更多切换方式**：添加更多切换方法（如剧情触发、特殊物品等）
4. **记忆查看器UI**：添加更丰富的记忆查看界面
5. **成就系统**：根据收集的记忆碎片解锁成就
6. **时间线限制**：根据剧情限制某些时间线的访问

## 文件结构

```
Assets/Scripts/TimeLine/
├── Enums/
│   └── TimeLineType.cs
├── Effects/
│   └── TimeLineTransitionEffect.cs
├── Managers/
│   ├── TimeLineManager.cs
│   ├── TimeLineSwitcher.cs
│   └── MemoryShardSystem.cs
├── UI/
│   └── TimeLineUI.cs
├── TimeLineController.cs
└── README.md
```

## 版本历史

- v1.0 (2024) - 初始版本
  - 实现4个时间线类型
  - 实现切换效果系统
  - 实现切换管理器
  - 实现UI管理器
  - 实现记忆碎片系统
