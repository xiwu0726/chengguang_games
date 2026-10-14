# 《时光之痕》失眠状态系统实现文档

## 概述

本文档描述了《时光之痕》视觉小说项目中的失眠状态系统实现。该系统实现了4个失眠等级，包括视觉扭曲、时间线可视化、音效变化、意识流对话和梦境场景等核心功能。

## 系统架构

### 核心组件

1. **InsomniaStateManager** - 失眠状态管理器
   - 管理失眠值（0-100%）
   - 处理状态转换（轻度 -> 中度 -> 重度 -> 临界）
   - 提供状态查询接口

2. **SpaceDistortionEffect** - 空间扭曲效果控制器
   - 实现场景空间扭曲
   - 支持动态强度和速度调整
   - 基于顶点位移的扭曲算法

3. **TimeLineVisualizer** - 时间线可视化系统
   - 显示4条时间线（主时间线、过去、未来、梦境）
   - 实现时间流动动画
   - 支持可见性和颜色偏移控制

4. **InsomniaAudioManager** - 失眠音效管理器
   - 播放脑波声、时间流动声等音效
   - 根据失眠等级动态调整音量
   - 支持多种音效类型

5. **StreamOfConsciousnessDialogue** - 意识流对话系统
   - 处理对话跳跃效果
   - 实现意识流文本生成
   - 支持文本故障效果

6. **DreamSceneManager** - 梦境场景管理器
   - 管理梦境场景的激活/停用
   - 控制记忆实体的生成和销毁
   - 实现实体浮动效果

7. **InsomniaController** - 主控制器
   - 协调所有子系统
   - 自动更新效果参数
   - 提供统一接口

## 失眠等级

### 1. 轻度失眠 (0-30%)
- **视觉**：轻微扭曲，颜色略微变化
- **音效**：脑波声轻微，环境音效轻微失真
- **对话**：正常对话，正常选择
- **交互**：可以正常对话和场景切换

### 2. 中度失眠 (30-60%)
- **视觉**：视野重影，时间线可见，空间扭曲
- **音效**：脑波声明显，时间流动声，记忆闪回声
- **对话**：对话开始跳跃，出现意识流对话
- **交互**：选择框出现重影，场景切换延迟

### 3. 重度失眠 (60-90%)
- **视觉**：多重时间线交织，严重扭曲，记忆实体出现
- **音效**：脑波声强烈，时间流动声急促，记忆实体对话声
- **对话**：完全意识流对话，选择框极不稳定
- **交互**：可以穿越时间线，可以与记忆实体互动

### 4. 临界失眠 (90-100%)
- **视觉**：时间线崩塌，空间彻底扭曲，记忆实体聚集
- **音效**：脑波声极度强烈，时间流动声混乱，多重声音交织
- **对话**：完全意识流对话，选择框几乎消失
- **交互**：可以穿越到任何时间线，可以与所有记忆实体互动

## 使用方法

### 1. 在场景中添加组件

1. 创建一个空GameObject命名为"InsomniaController"
2. 添加InsomniaController组件
3. 配置引用（可选，会自动创建默认实例）

### 2. 基本使用

```csharp
// 获取InsomniaController实例
InsomniaController insomniaController = FindObjectOfType<InsomniaController>();

// 增加失眠值
insomniaController.IncreaseInsomnia(10, "使用时间碎片");

// 减少失眠值
insomniaController.DecreaseInsomnia(10, "休息");

// 设置失眠值
insomniaController.SetInsomniaValue(50, "剧情事件");

// 重置失眠状态
insomniaController.ResetInsomnia();

// 获取当前状态
InsomniaState currentState = insomniaController.GetCurrentState();
int insomniaValue = insomniaController.GetCurrentInsomniaValue();
InsomniaStateConfig config = insomniaController.GetCurrentConfig();
```

### 3. 检查状态权限

```csharp
// 检查是否可以正常对话
if (insomniaController.CanNormalDialogue())
{
    // 执行正常对话逻辑
}

// 检查是否可以穿越时间线
if (insomniaController.CanTravelTimeLines())
{
    // 允许时间线切换
}

// 检查是否可以与记忆实体互动
if (insomniaController.CanInteractWithMemoryEntities())
{
    // 允许实体互动
}
```

### 4. 剧情触发示例

```csharp
// 在剧情事件中增加失眠值
public void OnUseTimeFragment()
{
    insomniaController.IncreaseInsomnia(15, "使用时间碎片");
}

// 在休息场景中减少失眠值
public void OnRestScene()
{
    insomniaController.DecreaseInsomnia(20, "休息");
}

// 在梦境场景中
public void OnDreamScene()
{
    if (insomniaController.IsMediumInsomnia())
    {
        // 显示轻度梦境效果
        dreamSceneManager.ActivateDreamScene();
    }
    else if (insomniaController.IsHeavyInsomnia())
    {
        // 显示重度梦境效果
        dreamSceneManager.ActivateDreamScene();
        // 可以与记忆实体互动
        if (insomniaController.CanInteractWithMemoryEntities())
        {
            InteractWithMemoryEntity();
        }
    }
}
```

### 5. 对话系统示例

```csharp
// 在对话开始时检查状态
public void OnDialogueStart()
{
    if (insomniaController.IsMediumInsomnia())
    {
        // 进入意识流对话模式
        dialogueSystem.StartStreamOfConsciousnessMode();
    }
    else
    {
        dialogueSystem.StopStreamOfConsciousnessMode();
    }
}

// 在对话更新时根据状态调整对话
public void OnDialogueUpdate()
{
    float insomniaLevel = insomniaController.GetCurrentInsomniaValue() / 100f;
    dialogueSystem.UpdateDialogueBasedOnInsomnia(insomniaLevel);
}
```

## 自定义配置

### 修改状态配置

可以在InsomniaStateManager中修改状态参数：

```csharp
// 修改轻度失眠的扭曲强度
stateManager.stateConfigs[0].distortionIntensity = 0.3f;

// 修改中度失眠的对话跳跃概率
stateManager.stateConfigs[1].dialogueJumpProbability = 0.3f;

// 修改重度失眠的实体互动权限
stateManager.stateConfigs[2].canInteractWithMemoryEntities = true;
```

### 添加自定义对话

```csharp
// 添加到正常对话池
dialogueSystem.AddDialogueToPool(dialogueSystem.normalDialoguePool, "我的记忆...");

// 添加到意识流对话池
dialogueSystem.AddDialogueToPool(dialogueSystem.streamOfConsciousnessPool, "时间...在扭曲...");

// 添加到记忆对话池
dialogueSystem.AddDialogueToPool(dialogueSystem.memoryDialoguePool, "我记得...");
```

### 自定义视觉效果

```csharp
// 调整空间扭曲效果
spaceDistortionEffect.UpdateIntensity(0.8f);
spaceDistortionEffect.UpdateSpeed(2.0f);

// 调整时间线可视化
timeLineVisualizer.UpdateVisibility(0.7f);
timeLineVisualizer.UpdateFlowSpeed(1.5f);

// 调整梦境场景
dreamSceneManager.UpdateDreamSceneOpacity(0.6f);
dreamSceneManager.UpdateInteractionPermission(true);
```

## 性能优化

1. **自动更新**：系统默认每0.5秒更新一次效果参数，可以调整updateInterval来平衡性能和响应速度
2. **对象池**：记忆实体使用简单的对象池管理
3. **粒子系统**：使用Unity的ParticleSystem进行高效渲染
4. **材质复用**：共享材质资源，减少GPU开销

## 注意事项

1. **音频资源**：需要准备相应的音频文件（脑波声、时间流动声等）
2. **视觉效果**：需要创建相应的Shader和材质
3. **内存管理**：长时间运行时注意内存使用情况
4. **跨场景**：InsomniaController设置为DontDestroyOnLoad，确保跨场景保持状态

## 扩展建议

1. **更多视觉效果**：添加屏幕闪烁、色差效果等
2. **更多音效**：添加环境音、背景音乐变化等
3. **更多交互**：添加时间线切换动画、记忆收集等
4. **成就系统**：根据失眠等级解锁成就
5. **UI反馈**：添加失眠值UI显示、状态指示器等

## 文件结构

```
Assets/Scripts/Insomnia/
├── Enums/
│   └── InsomniaState.cs
├── Managers/
│   └── InsomniaStateManager.cs
├── VisualEffects/
│   ├── SpaceDistortionEffect.cs
│   └── TimeLineVisualizer.cs
├── Audio/
│   └── InsomniaAudioManager.cs
├── Interactions/
│   ├── StreamOfConsciousnessDialogue.cs
│   └── DreamSceneManager.cs
└── InsomniaController.cs
```

## 版本历史

- v1.0 (2024) - 初始版本
  - 实现4个失眠等级
  - 实现空间扭曲效果
  - 实现时间线可视化
  - 实现音效系统
  - 实现意识流对话
  - 实现梦境场景管理
