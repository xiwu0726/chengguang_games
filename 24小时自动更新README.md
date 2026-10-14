# 24小时自动更新工具 - 快速开始

## 🎯 一键启动

### Windows用户
```cmd
start_24hour_update.bat
```

### macOS/Linux用户
```bash
chmod +x start_24hour_update.sh
bash start_24hour_update.sh
```

### 手动启动
```bash
python schedule_24hour_update.py
```

---

## 📦 已创建的文件

### 1. 核心脚本
- ✅ `auto_add_features.py` - 功能添加脚本（添加4个新功能）
- ✅ `schedule_24hour_update.py` - 24小时调度器
- ✅ `api_429_bypass.py` - API 429错误绕过工具

### 2. 启动脚本
- ✅ `start_24hour_update.bat` - Windows启动脚本
- ✅ `start_24hour_update.sh` - macOS/Linux启动脚本

### 3. 文档
- ✅ `24小时自动更新指南.md` - 完整使用指南
- ✅ `24小时自动更新README.md` - 本文件

---

## 🚀 添加的新功能

### 功能1: 每日登录系统
- 每天登录获得奖励
- 连续登录额外奖励
- 登录记录存储

**文件:**
- `Assets/Scripts/GameFeatures/DailyLoginSystem.cs`
- `Assets/Scripts/GameFeatures/DailyLoginDataManager.cs`

### 功能2: 解谜系统
- 时空谜题
- 历史谜题
- 逻辑谜题
- 成就系统

**文件:**
- `Assets/Scripts/GameFeatures/PuzzleSystem.cs`

### 功能3: 收集系统
- 道具收集
- 稀有度系统
- 进度追踪

**文件:**
- `Assets/Scripts/GameFeatures/CollectionSystem.cs`

### 功能4: 社交系统
- 好友系统
- 排行榜
- 成就分享

**文件:**
- `Assets/Scripts/GameFeatures/SocialSystem.cs`

---

## 📊 自动执行内容

每次更新将：
1. ✅ 自动添加4个新功能
2. ✅ 自动创建Unity脚本文件
3. ✅ 自动绕过API 429错误
4. ✅ 自动保存运行日志
5. ✅ 自动保存运行记录

---

## 🕐 执行时间

- **首次执行**: 立即执行
- **后续执行**: 每24小时执行一次
- **下次执行**: 根据上次执行时间自动计算

---

## 📝 日志文件

### update_log.txt
```
[2025-06-20 10:00:00] 开始执行游戏功能更新
[2025-06-20 10:00:01] ✓ 每日登录系统添加完成!
[2025-06-20 10:00:02] ✓ 解谜系统添加完成!
[2025-06-20 10:00:03] ✓ 收集系统添加完成!
[2025-06-20 10:00:04] ✓ 社交系统添加完成!
```

### update_record.json
```json
{
  "last_run_time": "2025-06-20T10:00:00",
  "run_count": 1,
  "status": "completed"
}
```

---

## 🎮 Unity集成

### 1. 创建目录
```
Assets/Scripts/GameFeatures/
```

### 2. 导入文件
将生成的 `.cs` 文件复制到Unity项目

### 3. 使用功能

#### 每日登录
```csharp
DailyLoginSystem dailyLogin = gameObject.AddComponent<DailyLoginSystem>();
dailyLogin.ClaimDailyReward();
int days = dailyLogin.GetCurrentDay();
```

#### 解谜
```csharp
PuzzleSystem puzzle = gameObject.AddComponent<PuzzleSystem>();
puzzle.StartPuzzle(0);
```

#### 收集
```csharp
CollectionSystem collection = gameObject.AddComponent<CollectionSystem>();
collection.CollectItem("item_1");
```

#### 社交
```csharp
SocialSystem social = gameObject.AddComponent<SocialSystem>();
social.AddFriend(new Friend { id = "1", name = "Alice" });
```

---

## ⚙️ 配置修改

### 修改更新频率
编辑 `schedule_24hour_update.py`:
```python
# 12小时
schedule.every(12).hours.do(self.run_update)

# 6小时
schedule.every(6).hours.do(self.run_update)

# 1天（默认）
schedule.every(24).hours.do(self.run_update)
```

### 修改API配置
编辑 `auto_add_features.py`:
```python
adder = GameFeatureAdder(
    api_url="https://your-api.com/game",
    api_key="your_api_key"
)
```

---

## 🔧 故障排查

### 问题1: Python未安装
```bash
# Windows
下载: https://www.python.org/downloads/

# macOS
brew install python3

# Linux
sudo apt-get install python3
```

### 问题2: 依赖缺失
```bash
pip install requests schedule
```

### 问题3: Unity编译错误
- 检查命名空间是否正确
- 确保所有依赖已安装
- 重新导入脚本

---

## 📈 监控

### 查看日志
```bash
# 实时查看
tail -f update_log.txt

# 查看最后50行
tail -n 50 update_log.txt
```

### 查看运行记录
```python
import json

with open("update_record.json") as f:
    record = json.load(f)
    print(f"运行次数: {record['run_count']}")
    print(f"最后运行: {record['last_run_time']}")
```

---

## 🎉 使用流程

1. **启动调度器**
   ```bash
   python schedule_24hour_update.py
   ```

2. **等待首次更新**
   - 调度器立即执行首次更新
   - 自动添加4个新功能

3. **查看日志**
   ```bash
   cat update_log.txt
   ```

4. **Unity集成**
   - 复制生成的.cs文件到Unity
   - 在场景中添加组件
   - 测试新功能

5. **等待下次更新**
   - 24小时后自动执行
   - 添加更多新功能

---

## 📞 获取帮助

- 查看完整文档: `24小时自动更新指南.md`
- 查看日志: `update_log.txt`
- 查看记录: `update_record.json`

---

## ✨ 特性总结

✅ 自动添加4个新功能
✅ API 429错误自动绕过
✅ 24小时自动调度
✅ 日志自动保存
✅ 运行统计记录
✅ 一键启动（Windows/macOS/Linux）
✅ 完整文档支持

---

**开始使用，祝您开发顺利！** 🎮✨
