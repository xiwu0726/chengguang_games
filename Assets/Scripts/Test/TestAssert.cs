#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System;
using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 测试用例用的极简断言。
    ///
    /// 这些测试脚本原先在各自的文件里写了一堆 `private void Assert.AreEqual(...)`
    /// ——C# 里方法名不能带点，所以整个 Test 目录其实编译不过。这里统一成一个真
    /// 静态类，调用点 `Assert.IsTrue(...)` 原样可用（命名空间内的 Assert 优先于
    /// UnityEngine.Assertions，不会被后者抢走）。
    /// </summary>
    public static class Assert
    {
        public static void IsTrue(bool condition, string message = "")
        {
            if (!condition)
            {
                Fail(message, "期望条件为 true，实际为 false");
            }
        }

        public static void IsNotNull(object obj, string message = "")
        {
            if (obj == null)
            {
                Fail(message, "期望非 null，实际为 null");
            }
        }

        public static void AreEqual(object expected, object actual, string message = "")
        {
            if (!Equals(expected, actual))
            {
                Fail(message, $"期望 {expected}，实际 {actual}");
            }
        }

        public static void AreEqual(int expected, int actual, string message = "")
        {
            if (expected != actual)
            {
                Fail(message, $"期望 {expected}，实际 {actual}");
            }
        }

        public static void AreEqual(float expected, float actual, string message = "")
        {
            if (Mathf.Abs(expected - actual) > 0.01f)
            {
                Fail(message, $"期望 {expected}，实际 {actual}");
            }
        }

        static void Fail(string message, string detail)
        {
            throw new Exception(string.IsNullOrEmpty(message) ? detail : message + "（" + detail + "）");
        }
    }
}
#endif
