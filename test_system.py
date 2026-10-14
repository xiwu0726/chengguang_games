#!/usr/bin/env python3
"""
Test script for 66RPG system
Verifies all components work correctly
"""

import sys
import json
import logging

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


def test_imports():
    """Test if all required modules can be imported"""
    try:
        import requests
        import time
        import json
        import random
        from datetime import datetime, timedelta
        from typing import List, Dict, Optional
        import threading
        logger.info("✓ All imports successful")
        return True
    except ImportError as e:
        logger.error(f"✗ Import failed: {e}")
        return False


def test_config():
    """Test configuration file"""
    try:
        with open('66rpg_config.json', 'r', encoding='utf-8') as f:
            config = json.load(f)
        
        # Verify required sections exist
        required_sections = ['rate_limiter', 'monitoring', 'features', 'target_games', 'rate_limit_bypass', 'output']
        for section in required_sections:
            if section not in config:
                logger.error(f"✗ Missing section in config: {section}")
                return False
        
        logger.info("✓ Configuration file valid")
        return True
    except FileNotFoundError:
        logger.error("✗ Config file not found")
        return False
    except json.JSONDecodeError as e:
        logger.error(f"✗ Invalid JSON in config: {e}")
        return False


def test_feature_generator():
    """Test feature generator"""
    try:
        from feature_generator import FeatureGenerator
        
        generator = FeatureGenerator()
        
        # Test simple feature generation
        feature = generator.generate_feature(1000001)
        assert 'game_id' in feature
        assert 'feature_name' in feature
        assert 'category' in feature
        assert 'priority' in feature
        logger.info(f"✓ Simple feature generation works: {feature['feature_name']}")
        
        # Test complex feature generation
        complex_feature = generator.generate_complex_feature(1000002)
        assert 'complexity' in complex_feature
        assert 'estimated_effort' in complex_feature
        logger.info(f"✓ Complex feature generation works: {complex_feature['feature_name']} (Complexity: {complex_feature['complexity']})")
        
        # Test batch generation
        game_ids = [1000003, 1000004, 1000005]
        batch = generator.generate_batch(game_ids, batch_size=3)
        assert len(batch) == 3
        logger.info(f"✓ Batch generation works: {len(batch)} features generated")
        
        # Test statistics
        stats = generator.get_statistics(batch)
        assert 'total_features' in stats
        assert 'by_category' in stats
        logger.info(f"✓ Statistics generation works")
        
        logger.info("✓ Feature generator working correctly")
        return True
    except Exception as e:
        logger.error(f"✗ Feature generator test failed: {e}")
        return False


def test_rate_limiter():
    """Test rate limiter"""
    try:
        import sys
        sys.path.insert(0, '.')
        import importlib.util
        spec = importlib.util.spec_from_file_location("rate_limiter", "66rpg_analyzer.py")
        rate_limiter_module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(rate_limiter_module)
        RateLimiter = rate_limiter_module.RateLimiter
        import time
        
        limiter = RateLimiter(min_delay=0.1, max_delay=0.2)
        
        # Test user agent rotation
        ua1 = limiter.get_random_user_agent()
        ua2 = limiter.get_random_user_agent()
        assert ua1 != ua2
        logger.info(f"✓ User agent rotation works")
        
        # Test delay
        start = time.time()
        limiter.wait_if_needed()
        elapsed = time.time() - start
        assert 0.1 <= elapsed <= 0.3  # Within expected range
        logger.info(f"✓ Delay mechanism works (delay: {elapsed:.3f}s)")
        
        logger.info("✓ Rate limiter working correctly")
        return True
    except Exception as e:
        logger.error(f"Rate limiter test failed: {e}")
        import traceback
        traceback.print_exc()
        return False


def test_dashboard():
    """Test monitoring dashboard"""
    try:
        from monitor_dashboard import MonitoringDashboard
        
        dashboard = MonitoringDashboard()
        
        # Test stats update
        dashboard.update_stats(total_requests=10, successful_requests=9, failed_requests=1)
        assert dashboard.stats['total_requests'] == 10
        assert dashboard.stats['successful_requests'] == 9
        assert dashboard.stats['failed_requests'] == 1
        logger.info("✓ Stats update works")
        
        # Test request recording
        dashboard.record_request(True)
        dashboard.record_request(False)
        assert dashboard.stats['total_requests'] == 12
        assert dashboard.stats['successful_requests'] == 10
        assert dashboard.stats['failed_requests'] == 2
        logger.info("✓ Request recording works")
        
        # Test feature recording
        feature_data = {'category': '剧情扩展', 'game_id': 1000001}
        dashboard.record_feature_added(feature_data)
        assert dashboard.stats['features_added'] == 1
        assert '剧情扩展' in dashboard.feature_breakdown
        logger.info("✓ Feature recording works")
        
        # Test report generation
        report = dashboard.generate_report()
        assert 'overall_stats' in report
        assert 'feature_breakdown' in report
        logger.info("✓ Report generation works")
        
        logger.info("✓ Monitoring dashboard working correctly")
        return True
    except Exception as e:
        logger.error(f"✗ Dashboard test failed: {e}")
        return False


def test_complete_workflow():
    """Test complete workflow simulation"""
    try:
        from feature_generator import FeatureGenerator
        from monitor_dashboard import MonitoringDashboard
        
        logger.info("Testing complete workflow...")
        
        # Initialize
        generator = FeatureGenerator()
        dashboard = MonitoringDashboard()
        
        # Simulate adding features
        game_ids = [1000001, 1000002, 1000003, 1000004, 1000005]
        features = generator.generate_batch(game_ids, batch_size=5)
        
        for feature in features:
            dashboard.record_feature_added(feature)
            dashboard.record_game_scanned(feature['game_id'])
        
        # Update stats
        dashboard.update_stats(
            total_requests=len(features),
            successful_requests=len(features),
            failed_requests=0,
            features_added=len(features),
            games_scanned=len(set(f['game_id'] for f in features))
        )
        
        # Generate report
        report = dashboard.generate_report()
        
        # Verify report
        assert report['overall_stats']['total_requests'] == 5
        assert report['overall_stats']['features_added'] == 5
        assert len(report['feature_breakdown']) > 0
        
        logger.info(f"✓ Complete workflow test passed")
        logger.info(f"  Generated {len(features)} features")
        logger.info(f"  Features by category: {report['feature_breakdown']}")
        
        return True
    except Exception as e:
        logger.error(f"✗ Complete workflow test failed: {e}")
        import traceback
        traceback.print_exc()
        return False


def main():
    """Run all tests"""
    print("\n" + "="*60)
    print("66RPG SYSTEM TEST SUITE")
    print("="*60 + "\n")
    
    tests = [
        ("Import Tests", test_imports),
        ("Configuration Test", test_config),
        ("Feature Generator Test", test_feature_generator),
        ("Rate Limiter Test", test_rate_limiter),
        ("Dashboard Test", test_dashboard),
        ("Complete Workflow Test", test_complete_workflow),
    ]
    
    results = {}
    
    for test_name, test_func in tests:
        print(f"\nRunning {test_name}...")
        try:
            results[test_name] = test_func()
        except Exception as e:
            logger.error(f"Test {test_name} crashed: {e}")
            import traceback
            traceback.print_exc()
            results[test_name] = False
    
    # Summary
    print("\n" + "="*60)
    print("TEST SUMMARY")
    print("="*60)
    
    passed = sum(1 for result in results.values() if result)
    total = len(results)
    
    for test_name, result in results.items():
        status = "[PASS]" if result else "[FAIL]"
        print(f"{status}: {test_name}")
    
    print(f"\nTotal: {passed}/{total} tests passed")
    print("="*60 + "\n")
    
    if passed == total:
        print("All tests passed! System is ready to use.")
        print("\nTo run the system:")
        print("  python 66rpg_analyzer.py")
        return 0
    else:
        print("Some tests failed. Please check the errors above.")
        return 1


if __name__ == "__main__":
    sys.exit(main())
