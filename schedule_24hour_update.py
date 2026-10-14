#!/usr/bin/env python3
"""
24小时自动更新调度脚本
自动执行游戏功能更新，并在明天运行
"""

import os
import time
import json
import schedule
import threading
from datetime import datetime, timedelta
from api_429_bypass import API429Bypass, RateLimiter

class UpdateScheduler:
    """24小时更新调度器"""

    def __init__(self, update_script_path="auto_add_features.py"):
        """
        初始化更新调度器

        Args:
            update_script_path: 更新脚本路径
        """
        self.update_script_path = update_script_path
        self.scheduler_thread = None
        self.is_running = False
        self.last_run_time = None
        self.run_count = 0

        # 创建API绕过处理器
        self.bypass = API429Bypass(
            max_retries=10,
            initial_delay=1.0,
            max_delay=60.0,
            backoff_factor=2.0
        )

        # 创建请求限流器
        self.rate_limiter = RateLimiter(max_requests=50, period=60)

    def log(self, message):
        """记录日志"""
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        print(f"[{timestamp}] {message}")
        self._save_log(message)

    def _save_log(self, message):
        """保存日志到文件"""
        log_file = "update_log.txt"

        with open(log_file, 'a', encoding='utf-8') as f:
            timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
            f.write(f"[{timestamp}] {message}\n")

    def run_update(self):
        """执行更新脚本"""
        self.log("=" * 60)
        self.log("开始执行游戏功能更新")
        self.log("=" * 60)

        try:
            # 检查更新脚本是否存在
            if not os.path.exists(self.update_script_path):
                self.log(f"错误: 更新脚本不存在 - {self.update_script_path}")
                return False

            # 执行更新脚本
            self.log(f"执行更新脚本: {self.update_script_path}")

            # 使用subprocess执行脚本
            import subprocess
            result = subprocess.run(
                ["python", self.update_script_path],
                capture_output=True,
                text=True,
                timeout=300  # 5分钟超时
            )

            if result.returncode == 0:
                self.log("✓ 更新脚本执行成功")
                self.log(f"输出:\n{result.stdout}")
                self.last_run_time = datetime.now()
                self.run_count += 1
                self._save_run_record()
                return True
            else:
                self.log(f"✗ 更新脚本执行失败")
                self.log(f"错误输出:\n{result.stderr}")
                return False

        except subprocess.TimeoutExpired:
            self.log("错误: 更新脚本执行超时")
            return False
        except Exception as e:
            self.log(f"错误: 执行更新脚本时发生异常 - {e}")
            return False

    def _save_run_record(self):
        """保存运行记录"""
        record = {
            "last_run_time": self.last_run_time.isoformat() if self.last_run_time else None,
            "run_count": self.run_count,
            "status": "completed"
        }

        with open("update_record.json", 'w', encoding='utf-8') as f:
            json.dump(record, f, indent=2, ensure_ascii=False)

    def _load_run_record(self):
        """加载运行记录"""
        if os.path.exists("update_record.json"):
            try:
                with open("update_record.json", 'r', encoding='utf-8') as f:
                    record = json.load(f)
                    self.last_run_time = datetime.fromisoformat(record.get("last_run_time", ""))
                    self.run_count = record.get("run_count", 0)
                    return True
            except Exception as e:
                print(f"加载运行记录失败: {e}")
        return False

    def check_next_run_time(self):
        """检查下一次运行时间"""
        if self.last_run_time is None:
            # 如果没有运行记录，立即运行
            return datetime.now() + timedelta(seconds=10)

        # 计算下次运行时间（24小时后）
        next_run = self.last_run_time + timedelta(hours=24)

        # 如果已经过了24小时，立即运行
        if datetime.now() >= next_run:
            return datetime.now() + timedelta(seconds=10)

        return next_run

    def start_scheduler(self):
        """启动调度器"""
        if self.is_running:
            self.log("调度器已经在运行中")
            return

        self.is_running = True
        self.log("启动24小时自动更新调度器")

        # 加载运行记录
        self._load_run_record()

        # 立即运行一次
        self.log("立即执行首次更新...")
        self.run_update()

        # 设置定时任务
        schedule.every(24).hours.do(self.run_update)

        # 启动调度线程
        self.scheduler_thread = threading.Thread(target=self._scheduler_loop, daemon=True)
        self.scheduler_thread.start()

        self.log("调度器已启动，将每24小时自动执行一次更新")

    def _scheduler_loop(self):
        """调度器主循环"""
        while self.is_running:
            try:
                # 检查是否有待执行的任务
                if schedule.idle_seconds() < 0:
                    self.run_update()
                    schedule.clear()  # 清除所有任务

                # 等待1小时
                time.sleep(3600)

            except KeyboardInterrupt:
                self.log("收到停止信号")
                break
            except Exception as e:
                self.log(f"调度器错误: {e}")
                time.sleep(60)  # 出错后等待1分钟再试

    def stop_scheduler(self):
        """停止调度器"""
        if not self.is_running:
            return

        self.log("停止调度器")
        self.is_running = False

        if self.scheduler_thread:
            self.scheduler_thread.join(timeout=5)

        schedule.clear()
        self.log("调度器已停止")

    def get_status(self):
        """获取调度器状态"""
        return {
            "is_running": self.is_running,
            "last_run_time": self.last_run_time.isoformat() if self.last_run_time else None,
            "run_count": self.run_count,
            "next_run_time": self.check_next_run_time().isoformat() if not self.is_running else "运行中"
        }


def main():
    """主函数"""
    import sys
    import io

    # 设置标准输出编码
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

    print("=" * 60)
    print("《时光之痕》24小时自动更新调度器")
    print("=" * 60)
    print()

    # 创建调度器
    scheduler = UpdateScheduler(update_script_path="auto_add_features.py")

    # 显示当前状态
    print("当前状态:")
    status = scheduler.get_status()
    print(f"  运行中: {'是' if status['is_running'] else '否'}")
    print(f"  最后运行: {status['last_run_time'] if status['last_run_time'] else '从未'}")
    print(f"  运行次数: {status['run_count']}")
    print(f"  下次运行: {status['next_run_time']}")
    print()

    # 启动调度器
    print("启动调度器...")
    scheduler.start_scheduler()

    print()
    print("=" * 60)
    print("调度器已启动！")
    print("=" * 60)
    print()
    print("功能:")
    print("  ✓ 每24小时自动执行一次更新")
    print("  ✓ 自动添加新功能到游戏")
    print("  ✓ 记录所有运行日志")
    print("  ✓ 保存运行记录")
    print()
    print("查看日志: update_log.txt")
    print("查看记录: update_record.json")
    print()

    # 保持运行
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        print("\n停止调度器...")
        scheduler.stop_scheduler()
        print("调度器已停止")


if __name__ == "__main__":
    main()
