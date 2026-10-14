#!/usr/bin/env python3
"""
API 429错误自动绕过工具
支持自动重试、指数退避、请求限流等功能
"""

import time
import random
import json
import requests
from typing import Callable, Optional, Any
from functools import wraps

class RateLimiter:
    """请求限流器"""

    def __init__(self, max_requests: int = 100, period: int = 60):
        """
        初始化限流器

        Args:
            max_requests: 时间周期内最大请求数
            period: 时间周期（秒）
        """
        self.max_requests = max_requests
        self.period = period
        self.requests = []
        self.lock = __import__('threading').Lock()

    def wait_if_needed(self):
        """如果需要则等待"""
        with self.lock:
            now = time.time()
            # 移除过期的请求记录
            self.requests = [t for t in self.requests if now - t < self.period]

            if len(self.requests) >= self.max_requests:
                # 计算需要等待的时间
                oldest_request = self.requests[0]
                wait_time = self.period - (now - oldest_request) + 0.1
                time.sleep(wait_time)

            # 记录当前请求
            self.requests.append(now)

class API429Bypass:
    """API 429错误自动绕过处理器"""

    def __init__(self,
                 max_retries: int = 10,
                 initial_delay: float = 1.0,
                 max_delay: float = 60.0,
                 backoff_factor: float = 2.0,
                 rate_limiter: Optional[RateLimiter] = None):
        """
        初始化API绕过处理器

        Args:
            max_retries: 最大重试次数
            initial_delay: 初始延迟时间（秒）
            max_delay: 最大延迟时间（秒）
            backoff_factor: 退避因子
            rate_limiter: 请求限流器
        """
        self.max_retries = max_retries
        self.initial_delay = initial_delay
        self.max_delay = max_delay
        self.backoff_factor = backoff_factor
        self.rate_limiter = rate_limiter or RateLimiter()

    def exponential_backoff(self, attempt: int) -> float:
        """计算指数退避延迟"""
        delay = min(
            self.initial_delay * (self.backoff_factor ** attempt),
            self.max_delay
        )
        return delay

    def jitter(self, delay: float) -> float:
        """添加随机抖动，避免雷鸣群效应"""
        return delay * random.uniform(0.5, 1.5)

    def make_request(self,
                    method: str,
                    url: str,
                    headers: Optional[dict] = None,
                    params: Optional[dict] = None,
                    data: Optional[dict] = None,
                    json_data: Optional[dict] = None,
                    timeout: int = 30) -> requests.Response:
        """
        发送API请求，自动处理429错误

        Args:
            method: HTTP方法（GET, POST等）
            url: 请求URL
            headers: 请求头
            params: 查询参数
            data: 表单数据
            json_data: JSON数据
            timeout: 超时时间（秒）

        Returns:
            requests.Response对象

        Raises:
            Exception: 所有重试失败后抛出异常
        """
        attempt = 0
        last_exception = None

        while attempt <= self.max_retries:
            # 限流等待
            self.rate_limiter.wait_if_needed()

            try:
                response = requests.request(
                    method=method,
                    url=url,
                    headers=headers,
                    params=params,
                    data=data,
                    json=json_data,
                    timeout=timeout
                )

                # 如果请求成功，直接返回
                if response.status_code == 200:
                    return response

                # 处理429错误
                if response.status_code == 429:
                    attempt += 1
                    if attempt > self.max_retries:
                        raise Exception(f"达到最大重试次数 {self.max_retries}")

                    # 计算延迟时间
                    delay = self.exponential_backoff(attempt - 1)
                    jitter_delay = self.jitter(delay)

                    # 检查响应头中的Retry-After
                    retry_after = response.headers.get('Retry-After')
                    if retry_after:
                        try:
                            retry_after_seconds = int(retry_after)
                            jitter_delay = max(jitter_delay, retry_after_seconds)
                        except ValueError:
                            pass

                    print(f"[429] 请求过于频繁，等待 {jitter_delay:.2f} 秒后重试... (尝试 {attempt}/{self.max_retries})")
                    time.sleep(jitter_delay)
                    continue

                # 处理其他错误
                last_exception = Exception(f"HTTP {response.status_code}: {response.text}")
                raise last_exception

            except requests.exceptions.RequestException as e:
                attempt += 1
                last_exception = e

                if attempt > self.max_retries:
                    raise

                delay = self.exponential_backoff(attempt - 1)
                jitter_delay = self.jitter(delay)

                print(f"[错误] 请求失败: {str(e)}，等待 {jitter_delay:.2f} 秒后重试... (尝试 {attempt}/{self.max_retries})")
                time.sleep(jitter_delay)

        # 所有重试都失败
        raise last_exception

    def get(self,
            url: str,
            headers: Optional[dict] = None,
            params: Optional[dict] = None,
            timeout: int = 30) -> requests.Response:
        """发送GET请求"""
        return self.make_request('GET', url, headers=headers, params=params, timeout=timeout)

    def post(self,
             url: str,
             headers: Optional[dict] = None,
             params: Optional[dict] = None,
             data: Optional[dict] = None,
             json_data: Optional[dict] = None,
             timeout: int = 30) -> requests.Response:
        """发送POST请求"""
        return self.make_request('POST', url, headers=headers, params=params,
                               data=data, json_data=json_data, timeout=timeout)

    def put(self,
            url: str,
            headers: Optional[dict] = None,
            data: Optional[dict] = None,
            json_data: Optional[dict] = None,
            timeout: int = 30) -> requests.Response:
        """发送PUT请求"""
        return self.make_request('PUT', url, headers=headers, data=data, json_data=json_data, timeout=timeout)

    def delete(self,
               url: str,
               headers: Optional[dict] = None,
               timeout: int = 30) -> requests.Response:
        """发送DELETE请求"""
        return self.make_request('DELETE', url, headers=headers, timeout=timeout)


# 装饰器版本
def retry_on_429(max_retries: int = 10, initial_delay: float = 1.0, max_delay: float = 60.0):
    """
    装饰器：在429错误时自动重试

    Args:
        max_retries: 最大重试次数
        initial_delay: 初始延迟时间
        max_delay: 最大延迟时间
    """
    def decorator(func: Callable):
        @wraps(func)
        def wrapper(*args, **kwargs):
            bypass = API429Bypass(max_retries=max_retries,
                                 initial_delay=initial_delay,
                                 max_delay=max_delay)

            return bypass.make_request(
                method=func.__name__.upper(),
                url=args[0] if args else kwargs.get('url'),
                headers=kwargs.get('headers'),
                params=kwargs.get('params'),
                data=kwargs.get('data'),
                json_data=kwargs.get('json_data'),
                timeout=kwargs.get('timeout', 30)
            )
        return wrapper
    return decorator


# 示例用法
if __name__ == "__main__":
    print("=" * 60)
    print("API 429错误自动绕过工具")
    print("=" * 60)
    print()

    # 创建API绕过处理器
    bypass = API429Bypass(
        max_retries=10,
        initial_delay=1.0,
        max_delay=60.0,
        backoff_factor=2.0
    )

    # 创建限流器
    rate_limiter = RateLimiter(max_requests=100, period=60)

    # 示例：调用API
    try:
        print("示例1：调用API（会自动处理429错误）")
        print("-" * 40)

        response = bypass.get(
            url="https://api.example.com/data",
            headers={"Authorization": "Bearer your_token"},
            timeout=30
        )

        print(f"状态码: {response.status_code}")
        print(f"响应: {response.text[:200]}")

    except Exception as e:
        print(f"错误: {str(e)}")

    print()
    print("=" * 60)
    print("✓ API 429错误自动绕过工具示例完成")
    print("=" * 60)
