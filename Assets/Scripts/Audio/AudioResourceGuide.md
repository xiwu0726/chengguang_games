# 《时光之痕》音频资源开发指南

## 文档信息
- **项目名称**: 《时光之痕》
- **文档版本**: 1.0
- **创建日期**: 2024年
- **负责模块**: 音频设计

---

## 1. 音频资源分类

### 1.1 失眠状态音效
**文件命名格式**: `Insomnia_[类型]_[等级].wav`
**存放位置**: `Assets/Audio/Insomnia/`

| 音效类型 | 描述 | 等级 |
|---------|------|------|
| `Brainwave_Light` | 轻度脑波声，轻微波动 | 0-30% |
| `Brainwave_Medium` | 中度脑波声，明显波动 | 30-60% |
| `Brainwave_Heavy` | 重度脑波声，强烈波动 | 60-90% |
| `Brainwave_Critical` | 临界脑波声，极度强烈 | 90-100% |
| `TimeFlow_Light` | 轻度时间流动声，像流水 | 0-30% |
| `TimeFlow_Medium` | 中度时间流动声，明显 | 30-60% |
| `TimeFlow_Heavy` | 重度时间流动声，急促 | 60-90% |
| `TimeFlow_Critical` | 临界时间流动声，混乱 | 90-100% |
| `SpaceDistortion_Light` | 轻度空间扭曲声，轻微嘶嘶声 | 0-30% |
| `SpaceDistortion_Medium` | 中度空间扭曲声，明显嘶嘶声 | 30-60% |
| `SpaceDistortion_Heavy` | 重度空间扭曲声，强烈嘶嘶声 | 60-90% |
| `SpaceDistortion_Critical` | 临界空间扭曲声，极度强烈 | 90-100% |
| `MemoryEntity_Light` | 轻度记忆实体声，断续对话 | 0-30% |
| `MemoryEntity_Medium` | 中度记忆实体声，明显对话 | 30-60% |
| `MemoryEntity_Heavy` | 重度记忆实体声，强烈对话 | 60-90% |
| `MemoryEntity_Critical` | 临界记忆实体声，集体对话 | 90-100% |
| `Crackling_Light` | 轻度裂缝撕裂声，轻微 | 0-30% |
| `Crackling_Medium` | 中度裂缝撕裂声，明显 | 30-60% |
| `Crackling_Heavy` | 重度裂缝撕裂声，强烈 | 60-90% |
| `Crackling_Critical` | 临界裂缝撕裂声，极度强烈 | 90-100% |

**音频规格要求**:
- 格式: WAV (16-bit, 44.1kHz)
- 时长: 10-30秒循环
- 音量: 根据等级动态调整
- 特效: 添加轻微的混响和延迟效果

---

### 1.2 场景背景音乐
**文件命名格式**: `Scene_[场景名称].wav`
**存放位置**: `Assets/Audio/BGM/`

| 场景名称 | 描述 | 风格 |
|---------|------|------|
| `Scene_MainTheme` | 主旋律，贯穿整个游戏 | 史诗感 + 神秘感 |
| `Scene_NewTokyo` | 新东京，现代都市 | 未来科技感，轻快 |
| `Scene_TimeResearchLab` | 时空研究所，高科技 | 未来感，神秘 |
| `Scene_HistoricalLibrary` | 历史图书馆，复古风格 | 怀旧，安静 |
| `Scene_FutureRuins` | 未来废墟，荒凉 | 低沉，空灵 |
| `Scene_DreamWorld` | 梦境世界，梦幻 | 空灵，美丽 |
| `Scene_Cafe` | 咖啡馆，温馨 | 温暖，轻松 |
| `Scene_MemoryFlashback` | 记忆闪回 | 怀旧，柔和 |

**音频规格要求**:
- 格式: WAV (16-bit, 44.1kHz)
- 时长: 2-4分钟循环
- 混音: 主旋律 + 场景变奏
- 特效: 添加环境音和轻微混响

---

### 1.3 情感音乐
**文件命名格式**: `Emotional_[情感类型].wav`
**存放位置**: `Assets/Audio/Emotional/`

| 情感类型 | 描述 | 应用场景 |
|---------|------|---------|
| `Emotional_Joy` | 喜悦音乐 | 角色开心、成功时刻 |
| `Emotional_Sadness` | 悲伤音乐 | 角色悲伤、失去时刻 |
| `Emotional_Tension` | 紧张音乐 | 冲突、对抗时刻 |
| `Emotional_Nostalgia` | 怀旧音乐 | 回忆、反思时刻 |
| `Emotional_Hope` | 希望音乐 | 坚定、希望时刻 |
| `Emotional_Fear` | 恐惧音乐 | 恐怖、危险时刻 |

**音频规格要求**:
- 格式: WAV (16-bit, 44.1kHz)
- 时长: 1.5-3分钟
- 混音: 根据情感强度调整
- 特效: 添加情感化音效

---

### 1.4 特殊音乐
**文件命名格式**: `Special_[类型].wav`
**存放位置**: `Assets/Audio/Special/`

| 特殊类型 | 描述 | 应用场景 |
|---------|------|---------|
| `Special_TimeTravel` | 时空穿越音乐 | 时间线切换、穿越时空 |
| `Special_MemoryFlashback` | 记忆闪回音乐 | 记忆实体对话、记忆世界 |
| `Special_Climax` | 高潮时刻音乐 | 最终决战、关键选择 |
| `Special_Victory` | 胜利音乐 | 成功、拯救世界 |
| `Special_Defeat` | 失败音乐 | 失败、悲剧结局 |

**音频规格要求**:
- 格式: WAV (16-bit, 44.1kHz)
- 时长: 1-2分钟
- 混音: 紧张、史诗感
- 特效: 添加空间特效、混响

---

### 1.5 音效
**文件命名格式**: `SFX_[类型].wav`
**存放位置**: `Assets/Audio/SFX/`

| 音效类型 | 描述 | 应用场景 |
|---------|------|---------|
| `SFX_UIClick` | UI点击音效 | 按钮点击、选项选择 |
| `SFX_UIHover` | UI悬停音效 | 鼠标悬停、光标移动 |
| `SFX_DoorOpen` | 门打开音效 | 场景切换、进入新区域 |
| `SFX_BookOpen` | 书打开音效 | 阅读古籍、查看资料 |
| `SFX_TimeFragmentUse` | 使用时间碎片音效 | 使用能力、切换时间线 |
| `SFX_TimeJump` | 时间跳跃音效 | 穿越时空 |
| `SFX_MemoryCollect` | 收集记忆碎片音效 | 获得记忆 |
| `SFX_CrystalCollect` | 收集水晶音效 | 获得物品 |
| `SFX_Miss` | 错过音效 | 错过选择、失败 |
| `SFX_Yes` | 确认音效 | 确认选择 |
| `SFX_No` | 拒绝音效 | 拒绝选择 |

**音频规格要求**:
- 格式: WAV (16-bit, 44.1kHz)
- 时长: 0.3-1秒
- 音量: 短促有力
- 特效: 清晰的音效

---

### 1.6 语音配音
**文件命名格式**: `Voice_[角色]_[场景/对话].wav`
**存放位置**: `Assets/Audio/Voices/`

#### 主要角色配音

**主角林晓**:
- `Voice_LinXiao_Intro` - 开场介绍
- `Voice_LinXiao_Normal` - 正常对话
- `Voice_LinXiao_Surprise` - 惊讶
- `Voice_LinXiao_Sad` - 悲伤
- `Voice_LinXiao_Tension` - 紧张
- `Voice_LinXiao_Hope` - 希望
- `Voice_LinXiao_Fear` - 恐惧

**艾莉丝**:
- `Voice_Alice_Intro` - 开场介绍
- `Voice_Alice_Normal` - 正常对话
- `Voice_Alice_Surprise` - 惊讶
- `Voice_Alice_Sad` - 悲伤
- `Voice_Alice_Tension` - 紧张
- `Voice_Alice_Hope` - 希望
- `Voice_Alice_Fear` - 恐惧

**雷恩**:
- `Voice_Rain_Intro` - 开场介绍
- `Voice_Rain_Normal` - 正常对话
- `Voice_Rain_Surprise` - 惊讶
- `Voice_Rain_Sad` - 悲伤
- `Voice_Rain_Tension` - 紧张
- `Voice_Rain_Hope` - 希望
- `Voice_Rain_Fear` - 恐惧

**小雅**:
- `Voice_Xiaoya_Intro` - 开场介绍
- `Voice_Xiaoya_Normal` - 正常对话
- `Voice_Xiaoya_Surprise` - 惊讶
- `Voice_Xiaoya_Sad` - 悲伤
- `Voice_Xiaoya_Tension` - 紧张
- `Voice_Xiaoya_Hope` - 希望
- `Voice_Xiaoya_Fear` - 恐惧

**时间领主**:
- `Voice_TimeLord_Intro` - 开场介绍
- `Voice_TimeLord_Normal` - 正常对话
- `Voice_TimeLord_Surprise` - 惊讶
- `Voice_TimeLord_Sad` - 悲伤
- `Voice_TimeLord_Tension` - 紧张
- `Voice_TimeLord_Hope` - 希望
- `Voice_TimeLord_Fear` - 恐惧

**记忆实体**:
- `Voice_MemoryEntity_Father` - 父亲的记忆实体
- `Voice_MemoryEntity_Mother` - 母亲的记忆实体
- `Voice_MemoryEntity_Alice_Family` - 艾莉丝家族记忆
- `Voice_MemoryEntity_Rain_Past` - 雷恩过去记忆

**音频规格要求**:
- 格式: WAV (16-bit, 44.1kHz)
- 时长: 根据对话长度调整 (2-10秒)
- 音量: 统一标准
- 声音: 清晰、自然
- 配音演员: 专业声优

---

## 2. 音频组织结构

```
Assets/
├── Audio/
│   ├── BGM/                    # 背景音乐
│   │   ├── Scene_MainTheme.wav
│   │   ├── Scene_NewTokyo.wav
│   │   ├── Scene_TimeResearchLab.wav
│   │   ├── Scene_HistoricalLibrary.wav
│   │   ├── Scene_FutureRuins.wav
│   │   └── Scene_DreamWorld.wav
│   ├── Emotional/              # 情感音乐
│   │   ├── Emotional_Joy.wav
│   │   ├── Emotional_Sadness.wav
│   │   ├── Emotional_Tension.wav
│   │   ├── Emotional_Nostalgia.wav
│   │   ├── Emotional_Hope.wav
│   │   └── Emotional_Fear.wav
│   ├── Special/                # 特殊音乐
│   │   ├── Special_TimeTravel.wav
│   │   ├── Special_MemoryFlashback.wav
│   │   ├── Special_Climax.wav
│   │   ├── Special_Victory.wav
│   │   └── Special_Defeat.wav
│   ├── SFX/                    # 音效
│   │   ├── SFX_UIClick.wav
│   │   ├── SFX_UIHover.wav
│   │   ├── SFX_DoorOpen.wav
│   │   ├── SFX_BookOpen.wav
│   │   ├── SFX_TimeFragmentUse.wav
│   │   ├── SFX_TimeJump.wav
│   │   └── SFX_MemoryCollect.wav
│   ├── Voices/                 # 语音配音
│   │   ├── Voice_LinXiao_Intro.wav
│   │   ├── Voice_Alice_Normal.wav
│   │   ├── Voice_Rain_Surprise.wav
│   │   ├── Voice_Xiaoya_Tension.wav
│   │   ├── Voice_TimeLord_Hope.wav
│   │   └── Voice_MemoryEntity_Father.wav
│   └── Insomnia/               # 失眠音效
│       ├── Insomnia_Brainwave_Light.wav
│       ├── Insomnia_Brainwave_Medium.wav
│       ├── Insomnia_TimeFlow_Heavy.wav
│       ├── Insomnia_SpaceDistortion_Critical.wav
│       ├── Insomnia_MemoryEntity_Heavy.wav
│       └── Insomnia_Crackling_Heavy.wav
```

---

## 3. 音频制作建议

### 3.1 背景音乐制作

**主旋律 (Main Theme)**:
- 建议使用钢琴、弦乐、合成器
- 结构: 主歌-副歌-主歌-副歌-桥段-副歌
- 情感: 史诗感 + 神秘感
- 时长: 2-4分钟

**场景音乐变奏**:
- 每个场景音乐应有变奏版本
- 根据场景氛围调整速度和强度
- 与主旋律保持一致性

### 3.2 情感音乐制作

**喜悦音乐**:
- 速度: 120-140 BPM
- 乐器: 钢琴、弦乐、轻打击乐
- 情感: 明亮、欢快

**悲伤音乐**:
- 速度: 60-80 BPM
- 乐器: 钢琴、大提琴、弦乐
- 情感: 低沉、柔和

**紧张音乐**:
- 速度: 100-120 BPM
- 乐器: 打击乐、合成器、弦乐
- 情感: 紧迫、不安

**怀旧音乐**:
- 速度: 70-90 BPM
- 乐器: 钢琴、大提琴、手风琴
- 情感: 温暖、回忆

### 3.3 特殊音乐制作

**时空穿越音乐**:
- 使用快速变化的合成器音色
- 添加空间特效和混响
- 情感: 神秘、震撼

**记忆闪回音乐**:
- 使用柔和的钢琴和弦乐
- 添加轻微的延迟和混响
- 情感: 怀旧、温柔

**高潮时刻音乐**:
- 使用完整的管弦乐队
- 强烈的情感表达
- 情感: 史诗、震撼

### 3.4 失眠音效制作

**脑波声**:
- 使用低频合成器
- 添加轻微的失真效果
- 根据失眠等级调整频率和强度

**时间流动声**:
- 使用流水声效果
- 添加轻微的混响
- 根据失眠等级调整速度

**空间扭曲声**:
- 使用尖锐的嘶嘶声
- 添加轻微的失真效果
- 根据失眠等级调整强度

**裂缝撕裂声**:
- 使用不规则的噪音
- 添加轻微的回声
- 根据失眠等级调整强度

### 3.5 语音配音制作

**录音环境**:
- 安静的录音棚
- 隔音处理
- 专业录音设备

**录音标准**:
- 采样率: 44.1kHz
- 位数: 16-bit
- 声道: 单声道或立体声
- 声音: 清晰、自然、有情感

**配音演员**:
- 专业声优
- 有视觉小说配音经验
- 能准确表达角色情感

**后期处理**:
- 混音: 统一音量
- 均衡: 调整频率
- 压缩: 控制动态范围
- 混响: 添加空间感

---

## 4. 音频技术规格

### 4.1 通用规格
- 格式: WAV (未压缩)
- 采样率: 44.1kHz
- 位数: 16-bit
- 声道: 单声道 (音效) / 立体声 (BGM、语音)
- 时长: 根据需要

### 4.2 Unity集成规格
- 资源压缩: WAV格式，Unity会自动优化
- 音频导入设置:
  - Load Type: Compressed In Memory
  - Compression Format: PCM
  - Sample Rate Setting: Preserve Sample Rate
- 音频混合:
  - 使用Audio Mixer进行精细控制
  - 分组管理: BGM、SFX、Voice、Insomnia

---

## 5. 音频使用示例

### 5.1 播放背景音乐
```csharp
AudioManager.Instance.PlayMainTheme();
AudioManager.Instance.PlaySceneMusic(SceneType.NewTokyo);
```

### 5.2 切换情感音乐
```csharp
EmotionalMusicManager.Instance.TransitionToEmotionalMusic(EmotionalState.Tension);
```

### 5.3 播放特殊音乐
```csharp
SpecialMusicManager.Instance.PlayTimeTravelMusic();
SpecialMusicManager.Instance.PlayClimaxMusic();
```

### 5.4 播放音效
```csharp
AudioManager.Instance.PlayUIClick();
AudioManager.Instance.PlayTimeFragmentUse();
```

### 5.5 播放语音
```csharp
VoiceRecordingManager.Instance.PlayCharacterVoice("LinXiao_Normal");
VoiceRecordingManager.Instance.PlayCharacterVoiceWithFadeOut("Alice_Sad");
```

### 5.6 失眠音效
```csharp
InsomniaAudioManager.Instance.StartInsomniaAudio(50f); // 中度失眠
InsomniaAudioManager.Instance.UpdateVolumes(75f); // 更新音量
InsomniaAudioManager.Instance.StopInsomniaAudio();
```

---

## 6. 开发进度跟踪

### 6.1 已完成
- [x] 音频管理器系统设计
- [x] 情感音乐管理器
- [x] 特殊音乐管理器
- [x] 语音录制管理器
- [x] 音频资源组织结构

### 6.2 进行中
- [ ] 创建音频资源目录结构
- [ ] 制作失眠状态音效
- [ ] 制作场景背景音乐
- [ ] 制作情感音乐
- [ ] 制作特殊音乐
- [ ] 制作音效
- [ ] 录制语音配音

### 6.3 待完成
- [ ] Audio Mixer设置
- [ ] 音频测试和优化
- [ ] 音频文档完善
- [ ] 最终验收

---

## 7. 注意事项

### 7.1 音量平衡
- 确保各音频类别音量平衡
- 使用Audio Mixer进行精细控制
- 避免音量突然变化

### 7.2 音频优化
- 优化音频资源大小
- 使用适当的压缩格式
- 避免过度使用音频特效

### 7.3 兼容性
- 确保音频在所有平台兼容
- 测试不同音频设备的输出
- 检查音频延迟和同步

### 7.4 版权
- 确保所有音频资源有版权或使用许可
- 避免使用受版权保护的素材
- 考虑使用原创音乐

---

## 8. 联系方式

如有问题或需要协助，请联系音频设计团队成员。

---

**文档结束**
