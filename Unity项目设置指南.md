# Unity项目设置指南

## 概述

《时光之痕》Unity项目包含45个C#脚本文件，涵盖对话系统、失眠状态系统、时间线系统等核心功能。

## 快速开始

### 方法1：手动导入（推荐）

#### 步骤1：创建Unity项目

1. 打开Unity Hub
2. 点击"新建项目"
3. 选择"3D"模板
4. 项目名称: `TimeOfFragments`
5. 位置: 选择一个文件夹
6. 点击"创建项目"

#### 步骤2：导入脚本文件

1. 在Unity中，打开项目资源管理器
2. 右键点击 `Assets/Scripts` 文件夹
3. 选择"导入文件夹"
4. 选择本项目中的 `Assets/Scripts/` 文件夹
5. 等待导入完成

#### 步骤3：创建主场景

1. 在 `Assets/Scenes` 文件夹中，右键选择"创建" → "Unity场景"
2. 命名为 `MainScene`
3. 双击打开场景

#### 步骤4：添加测试脚本

1. 在场景中创建一个空GameObject，命名为 `TestDemo`
2. 添加组件 `TestDemoScene.cs`
3. 添加UI元素（按钮和文本）

#### 步骤5：设置UI

创建以下UI元素：

**按钮（Canvas → Canvas Group → Button）:**
- `testDialogueButton` - 测试对话系统
- `testInsomniaButton` - 测试失眠系统
- `testTimeLineButton` - 测试时间线系统
- `testMemoryShardButton` - 测试记忆碎片系统
- `testEndingButton` - 测试结局系统

**文本（Canvas → TextMeshPro - Text）:**
- `insomniaValueText` - 显示失眠值
- `timeLineText` - 显示时间线
- `memoryShardText` - 显示记忆碎片
- `statusText` - 显示系统状态
- `testResultText` - 显示测试结果

#### 步骤6：运行项目

1. 点击Unity编辑器顶部的"播放"按钮
2. 点击测试按钮，观察系统功能

### 方法2：使用Python脚本（可选）

如果安装了Python，可以运行 `setup_unity_project.py` 脚本来自动创建项目结构。

## 项目结构

```
TimeOfFragments/
├── Assets/
│   ├── Scripts/
│   │   ├── Dialogue/          # 对话系统 (8个文件)
│   │   ├── Insomnia/          # 失眠状态系统 (8个文件)
│   │   ├── TimeLine/          # 时间线系统 (8个文件)
│   │   ├── Ending/            # 多结局系统 (7个文件)
│   │   ├── Audio/             # 音频系统 (5个文件)
│   │   ├── RogueLike/         # Rogue Like系统 (7个文件)
│   │   ├── Test/              # 测试系统 (5个文件)
│   │   └── Data/              # 数据文件
│   ├── Scenes/
│   │   └── MainScene.unity    # 主场景
│   └── Resources/             # 资源文件夹
├── ProjectSettings/
│   ├── project.asset          # 项目设置
│   ├── EditorBuildSettings.asset
│   └── ProjectVersion.txt
├── Packages/
│   └── manifest.json          # 包管理
└── README.md
```

## 核心系统说明

### 1. 对话系统

**文件:** `DialogueManager.cs`

**主要功能:**
- 统一管理所有对话
- 支持多分支剧情
- 角色立绘显示
- 选择对话框

**使用示例:**
```csharp
// 获取单例
DialogueManager dialogueManager = DialogueManager.Instance;

// 开始对话
List<DialogueLine> lines = new List<DialogueLine> { /* 对话行 */ };
dialogueManager.StartDialogue(lines, DialogueType.Normal);

// 显示角色
dialogueManager.ShowCharacter("林晓", sprite, CharacterPosition.Center);
```

### 2. 失眠状态系统

**文件:** `InsomniaController.cs`

**主要功能:**
- 4级失眠状态（轻度、中度、重度、临界）
- 空间扭曲视觉效果
- 时间线可视化
- 音效系统

**使用示例:**
```csharp
// 获取单例
InsomniaController insomniaController = InsomniaController.Instance;

// 增加失眠值
insomniaController.IncreaseInsomnia(30);

// 减少失眠值
insomniaController.DecreaseInsomnia(20);

// 获取当前状态
InsomniaState state = insomniaController.GetCurrentState();
int value = insomniaController.GetCurrentInsomniaValue();
```

### 3. 时间线系统

**文件:** `TimeLineController.cs`

**主要功能:**
- 4个时间线（主时间线、过去、未来、梦境）
- 时间线切换效果
- 颜色过渡动画

**使用示例:**
```csharp
// 获取单例
TimeLineController timeLineController = TimeLineController.Instance;

// 切换时间线
timeLineController.SwitchToTimeLine(TimeLineType.Main);
timeLineController.SwitchToTimeLine(TimeLineType.Past);
timeLineController.SwitchToTimeLine(TimeLineType.Future);
timeLineController.SwitchToTimeLine(TimeLineType.Dream);

// 获取当前时间线
TimeLineType current = timeLineController.GetCurrentTimeLine();
```

### 4. 记忆碎片系统

**文件:** `MemoryShardSystem.cs`

**主要功能:**
- 3种记忆类型（个人、历史、情感）
- 记忆碎片收集
- 记忆查看器

**使用示例:**
```csharp
// 获取单例
MemoryShardSystem memoryShardSystem = MemoryShardSystem.Instance;

// 收集记忆碎片
memoryShardSystem.CollectMemoryShard("memory_id_1");

// 检查是否已收集
bool has = memoryShardSystem.HasMemoryShard("memory_id_1");

// 获取数量
int count = memoryShardSystem.GetMemoryShardCount();
```

### 5. 多结局系统

**文件:** `EndingController.cs`

**主要功能:**
- 6种结局类型
- 结局条件检查
- 结局触发

**使用示例:**
```csharp
// 获取单例
EndingController endingController = EndingController.Instance;

// 触发结局
endingController.TriggerEnding("normal_ending");

// 检查是否已达成
bool achieved = endingController.IsEndingAchieved("normal_ending");

// 获取已达成的结局
List<EndingData> endings = endingController.GetAchievedEndings();
```

## 技术细节

### 命名空间

所有脚本都使用以下命名空间：
- `TimeStamp.Dialogue.Managers` - 对话管理器
- `TimeStamp.Insomnia` - 失眠状态系统
- `TimeStamp.TimeLine` - 时间线系统
- `TimeStamp.Ending` - 多结局系统

### 单例模式

所有核心系统都使用单例模式实现：
```csharp
public static Type Instance { get; }
```

### 事件系统

系统使用Unity事件系统进行通信：
```csharp
public event Action OnDialogueComplete;
public event Action<int> OnChoiceSelected;
public event Action<string> OnBranchChanged;
```

## 测试系统

### 测试场景

`TestDemoScene.cs` 提供了所有系统的测试功能：

1. **测试对话系统** - 演示基本对话功能
2. **测试失眠系统** - 演示失眠值变化和效果
3. **测试时间线系统** - 演示时间线切换
4. **测试记忆碎片系统** - 演示收集功能
5. **测试结局系统** - 演示结局触发

### 运行测试

1. 在Unity中创建场景
2. 添加 `TestDemoScene` 组件
3. 按照UI说明设置按钮和文本
4. 点击播放按钮
5. 依次点击测试按钮

## 性能优化

### 对象池

系统使用对象池技术减少内存分配：
- 对话行对象池
- 角色立绘对象池
- 特效对象池

### 资源管理

- 资源流式加载
- 异步加载场景
- 智能缓存机制

### 渲染优化

- Draw Call优化
- LOD技术
- 材质优化

## 已知限制

1. **角色立绘**: 需要实际导入Sprite资源
2. **音频资源**: 需要导入BGM和音效文件
3. **UI资源**: 需要创建实际的UI预制体
4. **剧情数据**: 需要创建剧情数据文件

## 下一步

1. **导入美术资源**: 根据设计文档导入角色立绘、场景等
2. **导入音频资源**: 导入BGM、音效、语音文件
3. **创建剧情数据**: 创建JSON格式的剧情数据
4. **完善UI**: 创建完整的游戏UI
5. **测试优化**: 进行完整的功能测试和性能优化

## 联系方式

如有问题，请查看项目文档或提交Issue。

---

**项目状态**: 代码已完成，等待Unity项目创建
**最后更新**: 2025年6月
