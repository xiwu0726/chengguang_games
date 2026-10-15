// 《时光之痕》结局规则解析器。
// 刻意不依赖 UnityEngine：本文件同时被 Unity 工程（Assets 内）和独立测试工程
// tools/ending-resolver-tests 引用编译，因此自带一个最小 JSON 读取器，
// 不使用 UnityEngine.JsonUtility，也不引外部 JSON 库。
// 语义唯一标准：story/tools/player-template.html 中的 whenSatisfied / resolveEnding /
// ruleTitleFor / topAffection（逐条对齐，见各方法注释）。
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Shiguang.Story
{
    /// <summary>结局表缺失、JSON 非法或规则校验失败时抛出（边界处显式报错，不静默吞掉）。</summary>
    public sealed class EndingTableException : Exception
    {
        public EndingTableException(string message) : base(message) { }
    }

    /// <summary>一次结局解析的不可变结果。</summary>
    public sealed class EndingResolution
    {
        public string Scene { get; private set; }
        public string Title { get; private set; }

        /// <summary>命中规则在 rules 数组中的下标；走 fallback 时为 EndingResolver.FallbackRuleIndex。</summary>
        public int RuleIndex { get; private set; }

        internal EndingResolution(string scene, string title, int ruleIndex)
        {
            Scene = scene;
            Title = title;
            RuleIndex = ruleIndex;
        }
    }

    /// <summary>
    /// 结局判定所需的玩家状态快照，构造后不可变。
    /// 对应 JS 侧的 state.flags / state.insomnia / state.memoryShards / state.affection，
    /// 以及 JS 全局 CAST_ORDER（topAffection 并列时的优先顺序）。
    /// </summary>
    public sealed class EndingSnapshot
    {
        private readonly string[] flags;
        private readonly string[] castOrder;
        private readonly Dictionary<string, int> affection;

        public int Insomnia { get; private set; }
        public int ShardCount { get; private set; }

        /// <summary>对应 JS 的 state.flags.length（按原始列表长度计，含重复项）。</summary>
        public int FlagCount { get; private set; }

        /// <summary>好感度最高者；并列时取 castOrder 中靠前者（与 JS topAffection 一致）。</summary>
        public string TopAffectionName { get; private set; }
        public int TopAffectionValue { get; private set; }

        public EndingSnapshot(
            IEnumerable<string> flags,
            int insomnia,
            int shardCount,
            IEnumerable<KeyValuePair<string, int>> affection,
            IEnumerable<string> castOrder)
        {
            this.flags = ToArray(flags);
            this.castOrder = ToArray(castOrder);
            FlagCount = this.flags.Length;
            Insomnia = insomnia;
            ShardCount = shardCount;

            this.affection = new Dictionary<string, int>(StringComparer.Ordinal);
            if (affection != null)
            {
                foreach (KeyValuePair<string, int> entry in affection)
                {
                    if (entry.Key == null) { continue; }
                    this.affection[entry.Key] = entry.Value; // 重复键后者覆盖，与 JS 对象赋值一致
                }
            }

            ComputeTopAffection();
        }

        /// <summary>镜像 JS 的 state.flags.indexOf(f) >= 0。</summary>
        public bool HasFlag(string flag)
        {
            return Array.IndexOf(flags, flag) >= 0;
        }

        /// <summary>镜像 JS 的 Number(state.affection[name] || 0)：缺失记 0。</summary>
        public int AffectionOf(string who)
        {
            int value;
            return who != null && affection.TryGetValue(who, out value) ? value : 0;
        }

        private void ComputeTopAffection()
        {
            // JS：bestV 初始 -Infinity，严格 > 才更新 → 首个 cast 成员必当选，并列保先。
            string best = castOrder.Length > 0 ? castOrder[0] : "";
            double bestValue = double.NegativeInfinity;
            for (int i = 0; i < castOrder.Length; i++)
            {
                double v = AffectionOf(castOrder[i]);
                if (v > bestValue)
                {
                    bestValue = v;
                    best = castOrder[i];
                }
            }
            if (double.IsNegativeInfinity(bestValue)) { bestValue = 0; } // JS 同款归零
            TopAffectionName = best;
            TopAffectionValue = (int)bestValue;
        }

        private static string[] ToArray(IEnumerable<string> source)
        {
            if (source == null) { return new string[0]; }
            var list = source as IList<string> ?? new List<string>(source);
            var array = new string[list.Count];
            list.CopyTo(array, 0);
            return array;
        }
    }

    /// <summary>
    /// 单条规则的 when 条件。JS 语义：每个运算符仅在「类型正确且非零值」时生效，
    /// 否则视为不存在（跳过）；全部生效项按 AND 组合。
    /// </summary>
    internal sealed class EndingCondition
    {
        internal static readonly EndingCondition AlwaysTrue =
            new EndingCondition(null, null, null, null, null, null, null, null);

        private readonly string[] flags;       // null = 约束不存在
        private readonly string[] notFlags;
        private readonly double? insomniaMin;
        private readonly double? insomniaMax;
        private readonly double? shardsMin;
        private readonly double? flagCountMin;
        private readonly string topAffection;  // null = 约束不存在
        private readonly double? affectionMin;

        private EndingCondition(
            string[] flags, string[] notFlags,
            double? insomniaMin, double? insomniaMax,
            double? shardsMin, double? flagCountMin,
            string topAffection, double? affectionMin)
        {
            this.flags = flags;
            this.notFlags = notFlags;
            this.insomniaMin = insomniaMin;
            this.insomniaMax = insomniaMax;
            this.shardsMin = shardsMin;
            this.flagCountMin = flagCountMin;
            this.topAffection = topAffection;
            this.affectionMin = affectionMin;
        }

        internal bool Matches(EndingSnapshot state)
        {
            if (flags != null)
            {
                foreach (string flag in flags)
                {
                    if (!state.HasFlag(flag)) { return false; }
                }
            }
            if (notFlags != null)
            {
                foreach (string flag in notFlags)
                {
                    if (state.HasFlag(flag)) { return false; }
                }
            }
            if (insomniaMin.HasValue && state.Insomnia < insomniaMin.Value) { return false; }
            if (insomniaMax.HasValue && state.Insomnia > insomniaMax.Value) { return false; }
            if (shardsMin.HasValue && state.ShardCount < shardsMin.Value) { return false; }
            if (flagCountMin.HasValue && state.FlagCount < flagCountMin.Value) { return false; }
            if (topAffection != null && !string.Equals(state.TopAffectionName, topAffection, StringComparison.Ordinal)) { return false; }
            if (affectionMin.HasValue && state.TopAffectionValue < affectionMin.Value) { return false; }
            return true;
        }

        /// <summary>
        /// 镜像 JS whenSatisfied 的入口判断：when 缺失、为 null、或不是 object（含数组）时恒真。
        /// 数组在 JS 里 typeof 也是 object，但没有具名键，效果等同空约束，这里同样产出无约束条件。
        /// </summary>
        internal static EndingCondition FromJson(Json when)
        {
            if (when == null || (when.Kind != JsonKind.Object && when.Kind != JsonKind.Array))
            {
                return AlwaysTrue;
            }
            return new EndingCondition(
                ReadFlagList(when.Get("flags")),
                ReadFlagList(when.Get("notFlags")),
                ReadNumericConstraint(when.Get("insomniaMin")),
                ReadNumericConstraint(when.Get("insomniaMax")),
                ReadNumericConstraint(when.Get("shardsMin")),
                ReadNumericConstraint(when.Get("flagCountMin")),
                ReadStringConstraint(when.Get("topAffection")),
                ReadNumericConstraint(when.Get("affectionMin")));
        }

        /// <summary>JS：Array.isArray(w.flags) 才生效；非字符串元素永远无法命中（indexOf 严格相等）。</summary>
        private static string[] ReadFlagList(Json node)
        {
            if (node == null || node.Kind != JsonKind.Array) { return null; }
            var items = node.Items;
            var names = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                names[i] = items[i].Kind == JsonKind.String ? items[i].Text : null;
            }
            return names;
        }

        /// <summary>JS：typeof value === 'number' && value !== 0 才生效，0 表示无约束。</summary>
        private static double? ReadNumericConstraint(Json node)
        {
            if (node == null || node.Kind != JsonKind.Number) { return null; }
            if (node.Number == EndingResolver.NoNumericConstraint) { return null; }
            return node.Number;
        }

        /// <summary>JS：typeof value === 'string' && value !== '' 才生效。</summary>
        private static string ReadStringConstraint(Json node)
        {
            if (node == null || node.Kind != JsonKind.String) { return null; }
            return node.Text.Length == 0 ? null : node.Text;
        }
    }

    /// <summary>单条结局规则（加载期校验：必须是对象且 scene 为非空字符串）。</summary>
    internal sealed class EndingRule
    {
        internal string Scene { get; private set; }
        internal string Title { get; private set; }
        internal EndingCondition Condition { get; private set; }

        private EndingRule(string scene, string title, EndingCondition condition)
        {
            Scene = scene;
            Title = title;
            Condition = condition;
        }

        internal static EndingRule FromJson(Json node, int index)
        {
            if (node == null || node.Kind != JsonKind.Object)
            {
                throw new EndingTableException("rules[" + index + "] 不是 JSON 对象。");
            }
            Json sceneNode = node.Get("scene");
            if (sceneNode == null || sceneNode.Kind != JsonKind.String || sceneNode.Text.Length == 0)
            {
                throw new EndingTableException("rules[" + index + "] 缺少非空字符串字段 \"scene\"。");
            }
            Json titleNode = node.Get("title");
            // JS：r.title || '' —— 非字符串/缺失都按空标题处理
            string title = titleNode != null && titleNode.Kind == JsonKind.String ? titleNode.Text : "";
            return new EndingRule(sceneNode.Text, title, EndingCondition.FromJson(node.Get("when")));
        }
    }

    /// <summary>解析后的结局规则表，不可变。</summary>
    public sealed class EndingTable
    {
        private readonly EndingRule[] rules;

        /// <summary>兜底场景 id；缺失、非字符串或空串时为 null（JS 同款：此时 resolve 返回 null）。</summary>
        public string FallbackScene { get; private set; }

        public int RuleCount { get { return rules.Length; } }

        internal EndingTable(EndingRule[] rules, string fallbackScene)
        {
            this.rules = rules;
            FallbackScene = fallbackScene;
        }

        /// <summary>
        /// 自上而下第一条 when 满足的规则胜出；全部不中走 FallbackScene（RuleIndex = -1）；
        /// 无 fallback 时返回 null（对应 JS resolveEnding 返回 null → 走旧五结局）。
        /// </summary>
        public EndingResolution Resolve(EndingSnapshot state)
        {
            if (state == null) { throw new ArgumentNullException("state"); }
            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i].Condition.Matches(state))
                {
                    return new EndingResolution(rules[i].Scene, rules[i].Title, i);
                }
            }
            if (string.IsNullOrEmpty(FallbackScene)) { return null; }
            return new EndingResolution(FallbackScene, RuleTitleFor(FallbackScene), EndingResolver.FallbackRuleIndex);
        }

        /// <summary>JS ruleTitleFor：第一条 scene 相等且 title 为非空字符串的规则标题，否则空串。</summary>
        public string RuleTitleFor(string sceneId)
        {
            if (string.IsNullOrEmpty(sceneId)) { return ""; }
            for (int i = 0; i < rules.Length; i++)
            {
                if (string.Equals(rules[i].Scene, sceneId, StringComparison.Ordinal) && rules[i].Title.Length > 0)
                {
                    return rules[i].Title;
                }
            }
            return "";
        }
    }

    /// <summary>结局解析入口：解析结局表 JSON 文本，并对状态快照求命中规则。</summary>
    public static class EndingResolver
    {
        /// <summary>JS：数值约束里 0 表示「无约束」，不是阈值。</summary>
        internal const double NoNumericConstraint = 0.0;

        /// <summary>fallback 结果的 RuleIndex 标记，与 JS resolveEnding 的 index:-1 一致。</summary>
        public const int FallbackRuleIndex = -1;

        /// <summary>解析并校验结局表。表为空、JSON 非法、rules 非数组、规则缺 scene 均抛 EndingTableException。</summary>
        public static EndingTable ParseTable(string endingsJson)
        {
            if (string.IsNullOrEmpty(endingsJson))
            {
                throw new EndingTableException("结局表 JSON 文本为 null 或空。");
            }
            Json root = JsonParser.Parse(endingsJson);
            if (root.Kind != JsonKind.Object)
            {
                throw new EndingTableException("结局表根节点必须是 JSON 对象，实际为 " + root.Kind + "。");
            }
            Json rulesNode = root.Get("rules");
            if (rulesNode == null || rulesNode.Kind != JsonKind.Array)
            {
                throw new EndingTableException("结局表缺少 \"rules\" 数组。");
            }
            var items = rulesNode.Items;
            var rules = new EndingRule[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                rules[i] = EndingRule.FromJson(items[i], i);
            }
            Json fallbackNode = root.Get("fallback");
            string fallback = fallbackNode != null && fallbackNode.Kind == JsonKind.String && fallbackNode.Text.Length > 0
                ? fallbackNode.Text : null;
            return new EndingTable(rules, fallback);
        }

        /// <summary>便捷入口：一次完成解析 + 判定。</summary>
        public static EndingResolution Resolve(string endingsJson, EndingSnapshot state)
        {
            return ParseTable(endingsJson).Resolve(state);
        }
    }

    // ---------- 最小 JSON 读取器（RFC 8259 子集：object/array/string/number/true/false/null） ----------
    // 理由：Unity 2021.3 的 .NET Standard 2.1 档位下 System.Text.Json 的可用性无法在本机
    // （无 Unity Editor）验证，自带解析器零依赖、可被测试工程完整覆盖。

    internal enum JsonKind { Object, Array, String, Number, Bool, Null }

    internal sealed class Json
    {
        internal static readonly Json NullValue = new Json(JsonKind.Null);

        internal readonly JsonKind Kind;
        internal string Text;                                  // String
        internal double Number;                                // Number
        internal bool Bool;                                    // Bool
        internal List<KeyValuePair<string, Json>> Members;     // Object（重复键后者覆盖，与 JSON.parse 一致）
        internal List<Json> Items;                             // Array

        private Json(JsonKind kind) { Kind = kind; }

        internal static Json NewString(string text)
        {
            var node = new Json(JsonKind.String);
            node.Text = text;
            return node;
        }

        internal static Json NewNumber(double value)
        {
            var node = new Json(JsonKind.Number);
            node.Number = value;
            return node;
        }

        internal static Json NewBool(bool value)
        {
            var node = new Json(JsonKind.Bool);
            node.Bool = value;
            return node;
        }

        internal static Json NewObject()
        {
            var node = new Json(JsonKind.Object);
            node.Members = new List<KeyValuePair<string, Json>>();
            return node;
        }

        internal static Json NewArray()
        {
            var node = new Json(JsonKind.Array);
            node.Items = new List<Json>();
            return node;
        }

        internal void SetMember(string key, Json value)
        {
            for (int i = 0; i < Members.Count; i++)
            {
                if (Members[i].Key == key)
                {
                    Members[i] = new KeyValuePair<string, Json>(key, value);
                    return;
                }
            }
            Members.Add(new KeyValuePair<string, Json>(key, value));
        }

        /// <summary>取对象成员；非对象或键不存在返回 null。</summary>
        internal Json Get(string key)
        {
            if (Members == null) { return null; }
            for (int i = Members.Count - 1; i >= 0; i--)
            {
                if (Members[i].Key == key) { return Members[i].Value; }
            }
            return null;
        }
    }

    internal sealed class JsonParser
    {
        private readonly string source;
        private int pos;

        private JsonParser(string text) { source = text; }

        internal static Json Parse(string text)
        {
            if (text.Length > 0 && text[0] == '\uFEFF') { text = text.Substring(1); } // 容忍 UTF-8 BOM
            var parser = new JsonParser(text);
            Json value = parser.ParseValue();
            parser.SkipWhitespace();
            if (!parser.AtEnd) { parser.Error("JSON 值之后存在多余内容"); }
            return value;
        }

        private bool AtEnd { get { return pos >= source.Length; } }
        private char Peek { get { return source[pos]; } }

        private Json ParseValue()
        {
            SkipWhitespace();
            if (AtEnd) { Error("JSON 输入意外结束"); }
            char c = Peek;
            switch (c)
            {
                case '{': return ParseObject();
                case '[': return ParseArray();
                case '"': return Json.NewString(ParseString());
                case 't': ExpectLiteral("true"); return Json.NewBool(true);
                case 'f': ExpectLiteral("false"); return Json.NewBool(false);
                case 'n': ExpectLiteral("null"); return Json.NullValue;
                default:
                    if (c == '-' || (c >= '0' && c <= '9')) { return Json.NewNumber(ParseNumber()); }
                    Error("意外的字符 '" + c + "'");
                    return Json.NullValue; // 不可达，仅为编译器满足
            }
        }

        private Json ParseObject()
        {
            pos++; // '{'
            Json obj = Json.NewObject();
            SkipWhitespace();
            if (!AtEnd && Peek == '}') { pos++; return obj; }
            while (true)
            {
                SkipWhitespace();
                if (AtEnd || Peek != '"') { Error("对象的键必须是字符串"); }
                string key = ParseString();
                SkipWhitespace();
                if (AtEnd || Peek != ':') { Error("键之后期望 ':'"); }
                pos++;
                obj.SetMember(key, ParseValue());
                SkipWhitespace();
                if (!AtEnd && Peek == ',') { pos++; continue; }
                if (!AtEnd && Peek == '}') { pos++; return obj; }
                Error("对象中期望 ',' 或 '}'");
            }
        }

        private Json ParseArray()
        {
            pos++; // '['
            Json array = Json.NewArray();
            SkipWhitespace();
            if (!AtEnd && Peek == ']') { pos++; return array; }
            while (true)
            {
                array.Items.Add(ParseValue());
                SkipWhitespace();
                if (!AtEnd && Peek == ',') { pos++; continue; }
                if (!AtEnd && Peek == ']') { pos++; return array; }
                Error("数组中期望 ',' 或 ']'");
            }
        }

        private string ParseString()
        {
            pos++; // 开引号
            var builder = new System.Text.StringBuilder();
            while (true)
            {
                if (AtEnd) { Error("字符串未闭合"); }
                char c = Peek;
                if (c == '"') { pos++; return builder.ToString(); }
                if (c == '\\') { pos++; ParseEscape(builder); continue; }
                if (c < ' ') { Error("字符串中含未转义控制字符"); }
                builder.Append(c);
                pos++;
            }
        }

        private void ParseEscape(System.Text.StringBuilder builder)
        {
            if (AtEnd) { Error("转义序列未结束"); }
            char c = Peek;
            pos++;
            switch (c)
            {
                case '"': builder.Append('"'); return;
                case '\\': builder.Append('\\'); return;
                case '/': builder.Append('/'); return;
                case 'b': builder.Append('\b'); return;
                case 'f': builder.Append('\f'); return;
                case 'n': builder.Append('\n'); return;
                case 'r': builder.Append('\r'); return;
                case 't': builder.Append('\t'); return;
                case 'u': AppendUnicodeEscape(builder); return;
                default: Error("非法转义字符 '\\" + c + "'"); return;
            }
        }

        private void AppendUnicodeEscape(System.Text.StringBuilder builder)
        {
            int code = ReadHex4();
            if (code >= 0xD800 && code <= 0xDBFF)
            {
                // 高位代理：必须紧跟 \uXXXX 低位代理，两个码元一起写入
                if (pos + 1 < source.Length && source[pos] == '\\' && source[pos + 1] == 'u')
                {
                    pos += 2;
                    int low = ReadHex4();
                    if (low >= 0xDC00 && low <= 0xDFFF)
                    {
                        builder.Append((char)code);
                        builder.Append((char)low);
                        return;
                    }
                }
                Error("高位代理后缺少低位代理");
            }
            builder.Append((char)code);
        }

        private int ReadHex4()
        {
            if (pos + 4 > source.Length) { Error("\\u 转义不足 4 位十六进制"); }
            int value = 0;
            for (int i = 0; i < 4; i++, pos++)
            {
                int digit = HexDigit(source[pos]);
                if (digit < 0) { Error("非法十六进制数字 '" + source[pos] + "'"); }
                value = value * 16 + digit;
            }
            return value;
        }

        private static int HexDigit(char c)
        {
            if (c >= '0' && c <= '9') { return c - '0'; }
            if (c >= 'a' && c <= 'f') { return c - 'a' + 10; }
            if (c >= 'A' && c <= 'F') { return c - 'A' + 10; }
            return -1;
        }

        private double ParseNumber()
        {
            int start = pos;
            if (!AtEnd && Peek == '-') { pos++; }
            while (!AtEnd && IsDigit(Peek)) { pos++; }
            if (!AtEnd && Peek == '.')
            {
                pos++;
                while (!AtEnd && IsDigit(Peek)) { pos++; }
            }
            if (!AtEnd && (Peek == 'e' || Peek == 'E'))
            {
                pos++;
                if (!AtEnd && (Peek == '+' || Peek == '-')) { pos++; }
                while (!AtEnd && IsDigit(Peek)) { pos++; }
            }
            string slice = source.Substring(start, pos - start);
            double value;
            if (!double.TryParse(slice, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                Error("非法数字 '" + slice + "'");
            }
            return value;
        }

        private static bool IsDigit(char c) { return c >= '0' && c <= '9'; }

        private void ExpectLiteral(string word)
        {
            if (pos + word.Length > source.Length || source.IndexOf(word, pos, word.Length, StringComparison.Ordinal) != pos)
            {
                Error("期望字面量 " + word);
            }
            pos += word.Length;
        }

        private void SkipWhitespace()
        {
            while (!AtEnd && (Peek == ' ' || Peek == '\t' || Peek == '\n' || Peek == '\r')) { pos++; }
        }

        private void Error(string message)
        {
            throw new EndingTableException("结局表 JSON 解析失败：位置 " + pos + "，" + message + "。");
        }
    }
}
