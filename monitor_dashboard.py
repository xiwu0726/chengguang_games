#!/usr/bin/env python3
"""
66RPG Monitoring Dashboard
Real-time monitoring and visualization of the analysis process
"""

import json
import time
import logging
from datetime import datetime
from typing import Dict, List
import threading
from collections import defaultdict
import random

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class MonitoringDashboard:
    """Real-time monitoring dashboard for 66RPG analysis"""
    
    def __init__(self):
        self.stats = {
            "total_requests": 0,
            "successful_requests": 0,
            "failed_requests": 0,
            "features_added": 0,
            "games_scanned": 0,
            "pages_fetched": 0,
            "current_ranking": None,
            "last_update": None
        }
        
        self.feature_breakdown = defaultdict(int)
        self.game_feature_count = defaultdict(int)
        self.request_history = []
        
        self.running = True
        self.lock = threading.Lock()
    
    def update_stats(self, **kwargs):
        """Update statistics"""
        with self.lock:
            for key, value in kwargs.items():
                if key in self.stats:
                    self.stats[key] = value
            self.stats["last_update"] = datetime.now().isoformat()
    
    def record_request(self, success: bool):
        """Record a request"""
        with self.lock:
            self.stats["total_requests"] += 1
            if success:
                self.stats["successful_requests"] += 1
            else:
                self.stats["failed_requests"] += 1
            
            self.request_history.append({
                "timestamp": datetime.now().isoformat(),
                "success": success
            })
            
            # Keep only last 100 requests
            if len(self.request_history) > 100:
                self.request_history = self.request_history[-100:]
    
    def record_feature_added(self, feature_data: Dict):
        """Record a feature being added"""
        with self.lock:
            category = feature_data.get("category", "未知")
            self.feature_breakdown[category] += 1
            game_id = feature_data.get("game_id", 0)
            self.game_feature_count[game_id] += 1
            self.stats["features_added"] += 1
    
    def record_game_scanned(self, game_id: int):
        """Record a game being scanned"""
        with self.lock:
            self.stats["games_scanned"] += 1
    
    def record_page_fetched(self, page_info: Dict):
        """Record a page being fetched"""
        with self.lock:
            self.stats["pages_fetched"] += 1
            self.stats["current_ranking"] = page_info.get("ranking", "未知")
    
    def generate_report(self) -> Dict:
        """Generate a comprehensive report"""
        with self.lock:
            report = {
                "timestamp": datetime.now().isoformat(),
                "overall_stats": self.stats.copy(),
                "feature_breakdown": dict(self.feature_breakdown),
                "top_games": sorted(
                    self.game_feature_count.items(),
                    key=lambda x: x[1],
                    reverse=True
                )[:10],
                "request_success_rate": 0
            }
            
            if report["overall_stats"]["total_requests"] > 0:
                report["request_success_rate"] = (
                    report["overall_stats"]["successful_requests"] /
                    report["overall_stats"]["total_requests"] * 100
                )
            
            return report
    
    def print_dashboard(self):
        """Print current dashboard to console"""
        with self.lock:
            now = datetime.now()
            elapsed = (now - datetime.fromisoformat(self.stats["last_update"])).total_seconds()
            
            print("\n" + "="*60)
            print("66RPG ANALYSIS DASHBOARD")
            print("="*60)
            print(f"Time: {now.strftime('%Y-%m-%d %H:%M:%S')}")
            print(f"Elapsed: {elapsed:.1f} seconds")
            print()
            
            print("OVERALL STATISTICS:")
            print(f"  Total Requests: {self.stats['total_requests']}")
            print(f"  Successful: {self.stats['successful_requests']}")
            print(f"  Failed: {self.stats['failed_requests']}")
            print(f"  Success Rate: {self.stats.get('request_success_rate', 0):.1f}%")
            print(f"  Features Added: {self.stats['features_added']}")
            print(f"  Games Scanned: {self.stats['games_scanned']}")
            print(f"  Pages Fetched: {self.stats['pages_fetched']}")
            print(f"  Current Ranking: {self.stats.get('current_ranking', 'N/A')}")
            print()
            
            print("FEATURE BREAKDOWN:")
            for category, count in sorted(self.feature_breakdown.items(), key=lambda x: x[1], reverse=True):
                print(f"  {category}: {count}")
            print()
            
            print("TOP GAMES BY FEATURES:")
            for game_id, count in self.top_games[:5]:
                print(f"  Game {game_id}: {count} features")
            print()
            
            print("="*60)
    
    def save_report(self, report: Dict, filename: str = "monitor_report.json"):
        """Save report to file"""
        try:
            with open(filename, 'w', encoding='utf-8') as f:
                json.dump(report, f, ensure_ascii=False, indent=2)
            logger.info(f"Report saved to {filename}")
        except Exception as e:
            logger.error(f"Error saving report: {e}")


def simulate_monitoring(dashboard: MonitoringDashboard, duration_seconds: int = 300):
    """Simulate monitoring activity for testing"""
    print(f"Starting simulation for {duration_seconds} seconds...")
    
    start_time = time.time()
    
    while time.time() - start_time < duration_seconds:
        # Simulate random activity
        activity = random.choice([
            ("request_success", None),
            ("request_fail", None),
            ("feature_added", {"category": random.choice(["剧情扩展", "角色系统", "战斗机制"])}),
            ("game_scanned", random.randint(1000000, 9999999)),
            ("page_fetched", {"ranking": random.choice(["热门", "新作", "流行"])})
        ])
        
        if activity[0] == "request_success":
            dashboard.record_request(True)
        elif activity[0] == "request_fail":
            dashboard.record_request(False)
        elif activity[0] == "feature_added":
            dashboard.record_feature_added(activity[1])
        elif activity[0] == "game_scanned":
            dashboard.record_game_scanned(activity[1])
        elif activity[0] == "page_fetched":
            dashboard.record_page_fetched(activity[1])
        
        # Update dashboard
        dashboard.update_stats(
            total_requests=dashboard.stats["total_requests"],
            successful_requests=dashboard.stats["successful_requests"],
            failed_requests=dashboard.stats["failed_requests"],
            features_added=dashboard.stats["features_added"],
            games_scanned=dashboard.stats["games_scanned"],
            pages_fetched=dashboard.stats["pages_fetched"]
        )
        
        # Print dashboard every 5 seconds
        if int(time.time() - start_time) % 5 == 0:
            dashboard.print_dashboard()
        
        time.sleep(0.5)
    
    print("\nSimulation complete!")
    return dashboard.generate_report()


def main():
    """Main function"""
    dashboard = MonitoringDashboard()
    
    # Run simulation
    report = simulate_monitoring(dashboard, duration_seconds=30)
    
    # Print final report
    dashboard.print_dashboard()
    
    # Save report
    dashboard.save_report(report)
    
    print("\nFinal report generated and saved!")


if __name__ == "__main__":
    main()
