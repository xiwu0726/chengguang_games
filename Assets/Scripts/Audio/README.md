# 音频系统模块

## 概述
《时光之痕》音频系统负责管理游戏中的所有音频资源，包括背景音乐、音效、情感音乐、特殊音乐和语音配音。

## 主要组件

### 1. AudioManager - 音频管理器
- 统一管理所有音频资源
- 负责背景音乐播放和切换
- 提供音效播放接口
- 支持淡入淡出效果

**主要功能**:
- 播放主旋律和场景音乐
- 跨场景BGM切换（淡入淡出）
- UI音效播放
- 音量控制

**使用示例**:
```csharp
// 播放主旋律
AudioManager.Instance.PlayMainTheme();

// 播放场景音乐
AudioManager.Instance.PlaySceneMusic(SceneType.NewTokyo);

// 切换BGM
AudioManager.Instance.CrossFadeBGM(newClip, 2f);

// 播放音效
AudioManager.Instance.PlayUIClick();
AudioManager.Instance.PlayTimeFragmentUse();
```

### 2. EmotionalMusicManager - 情感音乐管理器
- 管理不同情感类型的背景音乐
- 支持情感音乐切换
- 提供淡入淡出效果

**主要功能**:
- 播放喜悦、悲伤、紧张等情感音乐
- 自动切换情感音乐
- 情感音乐淡入淡出

**使用示例**:
```csharp
// 播放喜悦音乐
EmotionalMusicManager.Instance.PlayJoyMusic();

// 切换到紧张音乐
EmotionalMusicManager.Instance.TransitionToEmotionalMusic(EmotionalState.Tension);

// 停止情感音乐
EmotionalMusicManager.Instance.StopEmotionalMusic();
```

### 3. SpecialMusicManager - 特殊音乐管理器
- 管理特殊场景的音乐
- 支持时空穿越、记忆闪回等特殊音乐
- 提供淡入淡出效果

**主要功能**:
- 播放时空穿越音乐
- 播放记忆闪回音乐
- 播放高潮时刻音乐
- 播放胜利/失败音乐

**使用示例**:
```csharp
// 播放时空穿越音乐
SpecialMusicManager.Instance.PlayTimeTravelMusic();

// 播放记忆闪回音乐
SpecialMusicManager.Instance.PlayMemoryFlashbackMusic();

// 播放高潮时刻音乐
SpecialMusicManager.Instance.PlayClimaxMusic();

// 停止特殊音乐
SpecialMusicManager.Instance.StopSpecialMusic();
```

### 4. VoiceRecordingManager - 语音录制管理器
- 管理主要角色的配音
- 支持语音淡入淡出
- 提供语音加载和管理接口

**主要功能**:
- 播放角色语音
- 语音淡入淡出效果
- 语音加载和管理
- 未加载语音检测

**使用示例**:
```csharp
// 播放林晓的正常对话
VoiceRecordingManager.Instance.PlayCharacterVoice("LinXiao_Normal");

// 播放艾莉丝的悲伤对话（带淡出）
VoiceRecordingManager.Instance.PlayCharacterVoiceWithFadeOut("Alice_Sad");

// 加载语音文件
AudioClip voiceClip = Resources.Load<AudioClip>("Voices/LinXiao_Intro");
VoiceRecordingManager.Instance.LoadVoiceFile("LinXiao_Intro", voiceClip);

// 检查语音是否已加载
if (VoiceRecordingManager.Instance.IsVoiceLoaded("LinXiao_Normal"))
{
    VoiceRecordingManager.Instance.PlayCharacterVoice("LinXiao_Normal");
}

// 获取未加载的语音列表
List<string> unloadedVoices = VoiceRecordingManager.Instance.GetUnloadedVoices();
```

### 5. InsomniaAudioManager - 失眠音效管理器
- 管理失眠状态相关的音效
- 根据失眠等级动态调整音效
- 支持脑波声、时间流动声、裂缝声等

**主要功能**:
- 播放失眠状态音效
- 根据失眠等级调整音量
- 播放记忆实体对话
- 播放裂缝撕裂声

**使用示例**:
```csharp
// 开始播放轻度失眠音效
InsomniaAudioManager.Instance.StartInsomniaAudio(30f);

// 更新到重度失眠音效
InsomniaAudioManager.Instance.UpdateVolumes(75f);

// 播放记忆实体对话
InsomniaAudioManager.Instance.PlayMemoryEntityDialogue("林晓...你是谁？");

// 播放裂缝撕裂声
InsomniaAudioManager.Instance.PlayCrackleSound();

// 停止失眠音效
InsomniaAudioManager.Instance.StopInsomniaAudio();
```

## 音频资源组织

### 目录结构
```
Assets/
├── Audio/
│   ├── BGM/                    # 背景音乐
│   ├── Emotional/              # 情感音乐
│   ├── Special/                # 特殊音乐
│   ├── SFX/                    # 音效
│   ├── Voices/                 # 语音配音
│   └── Insomnia/               # 失眠音效
```

### 资源命名规范
- **背景音乐**: `Scene_[场景名称].wav`
- **情感音乐**: `Emotional_[情感类型].wav`
- **特殊音乐**: `Special_[类型].wav`
- **音效**: `SFX_[类型].wav`
- **语音**: `Voice_[角色]_[场景/对话].wav`
- **失眠音效**: `Insomnia_[类型]_[等级].wav`

详细说明请参考 `AudioResourceGuide.md`

## 技术规格

### 音频格式
- 格式: WAV (16-bit, 44.1kHz)
- 压缩: Unity自动优化
- 声道: 单声道(SFX) / 立体声(BGM、语音)

### 音频混合
- 使用Audio Mixer进行精细控制
- 分组: BGM、SFX、Voice、Insomnia
- 支持实时调整

## 开发进度

- [x] AudioManager - 音频管理器
- [x] EmotionalMusicManager - 情感音乐管理器
- [x] SpecialMusicManager - 特殊音乐管理器
- [x] VoiceRecordingManager - 语音录制管理器
- [x] InsomniaAudioManager - 失眠音效管理器
- [x] 音频资源组织结构
- [x] AudioResourceGuide.md - 音频资源开发指南
- [ ] 音频资源制作（待音频设计师完成）
- [ ] Audio Mixer设置
- [ ] 音频测试和优化

## 注意事项

1. **音频资源**: 需要音频设计师根据AudioResourceGuide.md制作音频资源
2. **语音配音**: 需要专业声优录制语音
3. **音量平衡**: 使用Audio Mixer确保各音频类别音量平衡
4. **性能优化**: 避免过度使用音频特效，优化音频资源大小
5. **兼容性**: 确保音频在所有平台兼容

## 参考资料

- [AudioResourceGuide.md](./AudioResourceGuide.md) - 详细的音频资源开发指南
- [Unity官方文档](https://docs.unity3d.com/Manual/Audio.html)

---

**最后更新**: 2024年
**维护者**: 音频设计团队
