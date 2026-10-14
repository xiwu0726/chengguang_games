@echo off
chcp 65001 >nul
cls

echo ============================================================
echo 《时光之痕》24小时自动更新工具
echo ============================================================
echo.

REM 检查Python是否安装
python --version >nul 2>&1
if errorlevel 1 (
    echo 错误: 未找到Python，请先安装Python
    echo 下载地址: https://www.python.org/downloads/
    pause
    exit /b 1
)

echo ✓ Python已安装
echo 版本:
python --version
echo.

REM 检查依赖
echo 检查依赖...
python -c "import requests" >nul 2>&1
if errorlevel 1 (
    echo   正在安装 requests...
    pip install requests
)

python -c "import schedule" >nul 2>&1
if errorlevel 1 (
    echo   正在安装 schedule...
    pip install schedule
)

echo ✓ 依赖检查完成
echo.

REM 检查脚本文件
echo 检查脚本文件...
if not exist "schedule_24hour_update.py" (
    echo 错误: 未找到 schedule_24hour_update.py
    pause
    exit /b 1
)

if not exist "auto_add_features.py" (
    echo 错误: 未找到 auto_add_features.py
    pause
    exit /b 1
)

echo ✓ 脚本文件检查完成
echo.

REM 显示信息
echo ============================================================
echo 启动24小时自动更新调度器
echo ============================================================
echo.
echo 功能:
echo   ✓ 每24小时自动执行一次更新
echo   ✓ 自动添加新功能到游戏
echo   ✓ API 429错误自动绕过
echo   ✓ 自动保存运行日志
echo.
echo 日志文件:
echo   - update_log.txt
echo.
echo 运行记录:
echo   - update_record.json
echo.
echo 按 Ctrl+C 停止调度器
echo.

REM 启动调度器
python schedule_24hour_update.py

pause
