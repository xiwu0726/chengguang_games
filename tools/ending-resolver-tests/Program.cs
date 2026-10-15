// 《时光之痕》EndingResolver 独立测试工程（位于 Assets 之外，Unity 不会编译本目录）。
// 运行：dotnet run --project tools/ending-resolver-tests
// 同时写出 csharp-results.json，供 node cross-check.js 与 player-template.html 的
// JS 实现（whenSatisfied / resolveEnding / ruleTitleFor / topAffection）逐用例比对。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Shiguang.Story.Tests
{
    internal static class Program
    {
        private static int passed;
        private static readonly List<string> Failures = new List<string>();

        /// <summary>共享用例的 C# 侧结果（name → JSON 片段），交给 cross-check.js 比对。</summary>
        private static readonly Dictionary<string, string> CrossCheckEntries = new Dictionary<string, string>();

        private static int Main()
        {
            string projectDir = FindProjectDirectory();
            string repoRoot = Directory.GetParent(projectDir).Parent.FullName;
            string realTable = File.ReadAllText(
                Path.Combine(repoRoot, "Assets", "Resources", "StoryConfig", "endings.json"), Encoding.UTF8);
            Json casesDoc = JsonParser.Parse(
                File.ReadAllText(Path.Combine(projectDir, "cases.json"), Encoding.UTF8));

            Console.WriteLine("== 共享用例（cases.json，与 JS 交叉验证同一批） ==");
            RunSharedCases(casesDoc, realTable);
            Console.WriteLine();
            Console.WriteLine("== C# 侧行为测试 ==");
            RunResolverBehaviourTests(realTable);

            File.WriteAllText(Path.Combine(projectDir, "csharp-results.json"),
                BuildResultsJson(), new UTF8Encoding(false));

            return Report();
        }

        // ---------- 共享用例 ----------

        private static void RunSharedCases(Json casesDoc, string realTable)
        {
            string[] defaultCast = ReadStringArray(casesDoc.Get("castOrder"));
            foreach (Json c in casesDoc.Get("cases").Items)
            {
                Json nameNode = c.Get("name");
                if (nameNode == null || nameNode.Kind != JsonKind.String)
                {
                    throw new Exception("cases.json 中存在缺少 name 的用例。");
                }
                string name = nameNode.Text;
                Test(name, () => RunOneCase(c, realTable, defaultCast));
            }
        }

        private static void RunOneCase(Json c, string realTable, string[] defaultCast)
        {
            string tableText = OptionalString(c.Get("tableText"));
            Json expectError = c.Get("expectError");
            if (expectError != null)
            {
                AssertLoadRejects(tableText, OptionalString(expectError.Get("contains")));
                return;
            }

            EndingSnapshot state = BuildSnapshot(c, defaultCast);
            EndingResolution pick = EndingResolver.Resolve(tableText ?? realTable, state);
            RecordResult(OptionalString(c.Get("name")), pick);

            if (c.Get("expectNull") != null)
            {
                Assert(pick == null, "期望无命中（null），实际 " + Describe(pick));
                return;
            }
            Json expect = c.Get("expect");
            Assert(expect != null, "用例缺少 expect / expectNull / expectError。");
            Assert(pick != null, "期望命中 " + expect.Get("scene").Text + "，实际无命中。");
            Assert(pick.Scene == expect.Get("scene").Text,
                "scene 不符：期望 " + expect.Get("scene").Text + "，实际 " + pick.Scene);
            Assert(pick.RuleIndex == (int)expect.Get("index").Number,
                "index 不符：期望 " + (int)expect.Get("index").Number + "，实际 " + pick.RuleIndex);
            Json title = expect.Get("title");
            if (title != null)
            {
                Assert(pick.Title == title.Text, "title 不符：期望 " + title.Text + "，实际 " + pick.Title);
            }
        }

        private static void AssertLoadRejects(string tableText, string contains)
        {
            EndingTableException error = ExpectThrows<EndingTableException>(
                () => EndingResolver.ParseTable(tableText));
            if (!string.IsNullOrEmpty(contains))
            {
                Assert(error.Message.IndexOf(contains, StringComparison.Ordinal) >= 0,
                    "错误信息应包含 \"" + contains + "\"，实际：" + error.Message);
            }
        }

        private static EndingSnapshot BuildSnapshot(Json c, string[] defaultCast)
        {
            Json st = c.Get("state");
            string[] flags = st == null ? new string[0] : ReadStringArray(st.Get("flags"));
            int insomnia = st == null ? 0 : (int)NumberOr(st.Get("insomnia"), 0);
            int shards = st == null ? 0 : (int)NumberOr(st.Get("shards"), 0);
            List<KeyValuePair<string, int>> affection = ReadAffection(st == null ? null : st.Get("affection"));
            Json castNode = c.Get("castOrder");
            string[] cast = castNode != null ? ReadStringArray(castNode) : defaultCast;
            return new EndingSnapshot(flags, insomnia, shards, affection, cast);
        }

        // ---------- C# 侧行为测试 ----------

        private static void RunResolverBehaviourTests(string realTable)
        {
            Test("real_table_exposes_20_rules_and_3_e20_fallback", () =>
            {
                EndingTable table = EndingResolver.ParseTable(realTable); // Arrange
                Assert(table.RuleCount == 20, "规则数应为 20，实际 " + table.RuleCount); // Act / Assert
                Assert(table.FallbackScene == "3-e20", "fallback 应为 3-e20，实际 " + table.FallbackScene);
            });

            Test("top_affection_tie_prefers_earlier_cast_member", () =>
            {
                EndingSnapshot snap = new EndingSnapshot(new string[0], 0, 0,
                    new Dictionary<string, int> { { "艾莉丝", 60 }, { "雷恩", 60 } },
                    new[] { "林晓", "艾莉丝", "雷恩" });
                Assert(snap.TopAffectionName == "艾莉丝", "并列时应取 cast 顺序靠前者，实际 " + snap.TopAffectionName);
                Assert(snap.TopAffectionValue == 60, "好感值应为 60，实际 " + snap.TopAffectionValue);
            });

            Test("top_affection_with_empty_cast_order_yields_empty_name_zero_value", () =>
            {
                EndingSnapshot snap = new EndingSnapshot(new[] { "a" }, 0, 0,
                    new Dictionary<string, int> { { "艾莉丝", 99 } }, new string[0]);
                Assert(snap.TopAffectionName == "" && snap.TopAffectionValue == 0,
                    "空 castOrder 应产出 (\"\", 0)，实际 (" + snap.TopAffectionName + ", " + snap.TopAffectionValue + ")");
            });

            Test("missing_affection_entries_count_as_zero_and_first_cast_member_wins", () =>
            {
                EndingSnapshot snap = new EndingSnapshot(null, 0, 0, null, new[] { "艾莉丝", "雷恩" });
                Assert(snap.AffectionOf("艾莉丝") == 0, "缺失好感应记 0");
                Assert(snap.TopAffectionName == "艾莉丝" && snap.TopAffectionValue == 0,
                    "全 0 好感时首位 cast 成员当选（JS -Infinity 起点语义）");
            });

            Test("flag_membership_is_exact_and_count_includes_duplicates", () =>
            {
                EndingSnapshot snap = new EndingSnapshot(new[] { "a", "a", "b" }, 0, 0, null, null);
                Assert(snap.HasFlag("a") && !snap.HasFlag("c"), "HasFlag 应为精确匹配");
                Assert(snap.FlagCount == 3, "FlagCount 应按原始列表长度计（JS state.flags.length），实际 " + snap.FlagCount);
            });

            Test("rule_title_for_returns_first_non_empty_title_of_scene", () =>
            {
                EndingTable table = EndingResolver.ParseTable(
                    "{\"rules\":[{\"scene\":\"3-e99\",\"title\":\"\"},{\"scene\":\"3-e99\",\"title\":\"T2\"},{\"scene\":\"3-e98\",\"title\":\"T3\"}]}");
                Assert(table.RuleTitleFor("3-e99") == "T2", "应跳过空标题取第一条非空，实际 " + table.RuleTitleFor("3-e99"));
                Assert(table.RuleTitleFor("3-e00") == "", "未知场景应返回空串");
                Assert(table.RuleTitleFor("") == "", "空场景 id 应返回空串");
            });

            Test("resolve_rejects_null_state", () =>
            {
                EndingTable table = EndingResolver.ParseTable("{\"rules\":[]}");
                ExpectThrows<ArgumentNullException>(() => table.Resolve(null));
            });

            Test("convenience_resolve_parses_and_resolves_in_one_call", () =>
            {
                EndingResolution pick = EndingResolver.Resolve(
                    "{\"rules\":[{\"scene\":\"3-e99\"}]}", new EndingSnapshot(null, 0, 0, null, null));
                Assert(pick != null && pick.Scene == "3-e99" && pick.RuleIndex == 0, "便捷入口应命中第一条无约束规则");
            });

            Test("json_string_escapes_round_trip", () =>
            {
                EndingTable table = EndingResolver.ParseTable(
                    "{\"rules\":[{\"scene\":\"3-e99\",\"title\":\"a\\\"b\\\\c\\nd\\u0041\"}]}");
                EndingResolution pick = table.Resolve(new EndingSnapshot(null, 0, 0, null, null));
                Assert(pick.Title == "a\"b\\c\ndA", "转义还原不符，实际：" + pick.Title);
            });

            Test("json_surrogate_pair_decodes_to_astral_char", () =>
            {
                EndingTable table = EndingResolver.ParseTable(
                    "{\"rules\":[{\"scene\":\"3-e99\",\"title\":\"\\ud83d\\ude00\"}]}");
                EndingResolution pick = table.Resolve(new EndingSnapshot(null, 0, 0, null, null));
                Assert(pick.Title == "\U0001F600", "代理对应还原为 😀");
            });

            Test("json_rejects_lone_high_surrogate", () =>
            {
                ExpectThrows<EndingTableException>(
                    () => EndingResolver.ParseTable("{\"rules\":[{\"scene\":\"\\ud83d\"}]}"));
            });

            Test("json_rejects_bad_hex_escape", () =>
            {
                ExpectThrows<EndingTableException>(
                    () => EndingResolver.ParseTable("{\"rules\":[{\"scene\":\"\\uZZZZ\"}]}"));
            });

            Test("json_accepts_whitespace_and_negative_zero_means_no_constraint", () =>
            {
                // JS：-0 !== 0 为 false（=== 视 -0 等于 0）→ 无约束；C# 用 == 0.0 判断，行为一致。
                EndingTable table = EndingResolver.ParseTable(
                    " \t\r\n{\"rules\" : [ { \"scene\" : \"3-e99\", \"when\" : { \"insomniaMin\" : -0 } } ] } \n");
                EndingResolution pick = table.Resolve(new EndingSnapshot(null, 0, 0, null, null));
                Assert(pick != null && pick.Scene == "3-e99", "-0 应视为无约束");
            });

            Test("json_rejects_trailing_comma_in_array", () =>
            {
                ExpectThrows<EndingTableException>(
                    () => EndingResolver.ParseTable("{\"rules\":[],}"));
            });
        }

        // ---------- 微型测试框架与工具 ----------

        private static void Test(string name, Action body)
        {
            try
            {
                body();
                passed++;
                Console.WriteLine("  PASS  " + name);
            }
            catch (Exception e)
            {
                Failures.Add(name + " :: " + e.Message);
                Console.WriteLine("  FAIL  " + name + " :: " + e.Message);
            }
        }

        private static int Report()
        {
            Console.WriteLine();
            Console.WriteLine("测试汇总：通过 " + passed + "，失败 " + Failures.Count);
            foreach (string failure in Failures)
            {
                Console.WriteLine("  FAILED  " + failure);
            }
            return Failures.Count == 0 ? 0 : 1;
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) { throw new Exception(message); }
        }

        private static T ExpectThrows<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T expected)
            {
                return expected;
            }
            catch (Exception other)
            {
                throw new Exception("期望 " + typeof(T).Name + "，实际抛出 " + other.GetType().Name + "：" + other.Message);
            }
            throw new Exception("期望 " + typeof(T).Name + "，但未抛出任何异常");
        }

        private static string FindProjectDirectory()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "EndingResolverTests.csproj")))
            {
                dir = dir.Parent;
            }
            if (dir == null)
            {
                throw new InvalidOperationException("无法定位测试工程目录（EndingResolverTests.csproj）。");
            }
            return dir.FullName;
        }

        private static void RecordResult(string name, EndingResolution pick)
        {
            if (name == null) { return; }
            CrossCheckEntries[name] = pick == null
                ? "null"
                : "{\"scene\": " + JsonEscape(pick.Scene)
                  + ", \"title\": " + JsonEscape(pick.Title)
                  + ", \"index\": " + pick.RuleIndex.ToString(CultureInfo.InvariantCulture) + "}";
        }

        private static string BuildResultsJson()
        {
            var sb = new StringBuilder();
            sb.Append("{\n");
            bool first = true;
            foreach (KeyValuePair<string, string> entry in CrossCheckEntries)
            {
                if (!first) { sb.Append(",\n"); }
                first = false;
                sb.Append("  ").Append(JsonEscape(entry.Key)).Append(": ").Append(entry.Value);
            }
            sb.Append("\n}\n");
            return sb.ToString();
        }

        private static string JsonEscape(string value)
        {
            var sb = new StringBuilder("\"");
            foreach (char ch in value)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < ' ') { sb.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture)); }
                        else { sb.Append(ch); }
                        break;
                }
            }
            return sb.Append('"').ToString();
        }

        private static string Describe(EndingResolution pick)
        {
            return pick == null ? "null" : pick.Scene + " #" + pick.RuleIndex;
        }

        private static string OptionalString(Json node)
        {
            return node != null && node.Kind == JsonKind.String ? node.Text : null;
        }

        private static double NumberOr(Json node, double fallback)
        {
            return node != null && node.Kind == JsonKind.Number ? node.Number : fallback;
        }

        private static string[] ReadStringArray(Json node)
        {
            if (node == null || node.Kind != JsonKind.Array) { return new string[0]; }
            List<Json> items = node.Items;
            var array = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                array[i] = items[i].Kind == JsonKind.String ? items[i].Text : null;
            }
            return array;
        }

        private static List<KeyValuePair<string, int>> ReadAffection(Json node)
        {
            var list = new List<KeyValuePair<string, int>>();
            if (node == null || node.Kind != JsonKind.Object) { return list; }
            foreach (KeyValuePair<string, Json> member in node.Members)
            {
                if (member.Value.Kind == JsonKind.Number)
                {
                    list.Add(new KeyValuePair<string, int>(member.Key, (int)member.Value.Number));
                }
            }
            return list;
        }
    }
}
