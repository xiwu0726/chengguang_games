#!/bin/bash

# 《时光之痕》24小时自动更新启动脚本
# 使用方法: bash start_24hour_update.sh

echo "============================================================"
echo "《时光之痕》24小时自动更新工具"
echo "============================================================"
echo ""

# 检查Python是否安装
if ! command -v python3 &> /dev/null; then
    echo "错误: 未找到Python3，请先安装Python3"
    echo "安装命令: sudo apt-get install python3"
    exit 1
fi

echo "✓ Python3已安装"
echo "版本: $(python3 --version)"
echo ""

# 检查依赖
echo "检查依赖..."
python3 -c "import requests" 2>/dev/null
if [ $? -ne 0 ]; then
    echo "  正在安装 requests..."
    pip3 install requests
fi

python3 -c "import schedule" 2>/dev/null
if [ $? -ne 0 ]; then
    echo "  正在安装 schedule..."
    pip3 install schedule
fi

echo "✓ 依赖检查完成"
echo ""

# 检查脚本文件
echo "检查脚本文件..."
if [ ! -f "schedule_24hour_update.py" ]; then
    echo "错误: 未找到 schedule_24hour_update.py"
    exit 1
fi

if [ ! -f "auto_add_features.py" ]; then
    echo "错误: 未找到 auto_add_features.py"
    exit 1
fi

echo "✓ 脚本文件检查完成"
echo ""

# 显示信息
echo "============================================================"
echo "启动24小时自动更新调度器"
echo "============================================================"
echo ""
echo "功能:"
echo "  ✓ 每24小时自动执行一次更新"
echo "  ✓ 自动添加新功能到游戏"
echo "  ✓ API 429错误自动绕过"
echo "  ✓ 自动保存运行日志"
echo ""
echo "日志文件:"
echo "  - update_log.txt"
echo ""
echo "运行记录:"
echo "  - update_record.json"
echo ""
echo "按 Ctrl+C 停止调度器"
echo ""

# 启动调度器
python3 schedule_24hour_update.py
