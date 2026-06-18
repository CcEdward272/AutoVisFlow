using System.Globalization;
using System.Text.RegularExpressions;

namespace Cc.IDE.Runtime;

/// <summary>
/// 运行时表达式求值器。对条件节点和连线条件中的布尔表达式进行求值，
/// 支持比较运算符、逻辑运算符、变量引用和字面值。
/// </summary>
/// <remarks>
/// <para>支持的表达式语法：</para>
/// <list type="bullet">
///   <item><b>变量引用</b>：<c>$varName</c> — 从 RuntimeContext.Variables 取值</item>
///   <item><b>输入引用</b>：<c>$input.varName</c> — 从 RuntimeContext.Inputs 取值</item>
///   <item><b>字面值</b>：数字（<c>5</c>、<c>3.14</c>）、字符串（<c>'hello'</c>、<c>"world"</c>）、布尔（<c>true</c>、<c>false</c>）、<c>null</c></item>
///   <item><b>比较运算符</b>：<c>==</c>、<c>!=</c>、<c>&gt;</c>、<c>&lt;</c>、<c>&gt;=</c>、<c>&lt;=</c></item>
///   <item><b>逻辑运算符</b>：<c>&amp;&amp;</c>（与）、<c>||</c>（或）、<c>!</c>（非）</item>
///   <item><b>分组</b>：<c>( ... )</c></item>
/// </list>
/// <para>示例：</para>
/// <list type="bullet">
///   <item><c>$voltage &gt; 3.0</c></item>
///   <item><c>$status == 'OK'</c></item>
///   <item><c>$initOK &amp;&amp; $warmupDone</c></item>
///   <item><c>!$error || $forceContinue</c></item>
///   <item><c>($a &gt; 5) &amp;&amp; ($b != 'skip')</c></item>
/// </list>
/// </remarks>
public sealed class ExpressionEvaluator
{
    private readonly RuntimeContext _context;

    /// <summary>
    /// 初始化表达式求值器的新实例，绑定到指定的运行时上下文。
    /// </summary>
    /// <param name="context">用于解析变量和输入引用的运行时上下文。</param>
    public ExpressionEvaluator(RuntimeContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// 对布尔表达式字符串进行求值。
    /// </summary>
    /// <param name="expression">要评估的表达式字符串。为 <c>null</c> 或空白时视为 <c>true</c>。</param>
    /// <returns>表达式的求值结果。空表达式默认为 <c>true</c>。</returns>
    /// <exception cref="ExpressionEvalException">表达式语法错误或求值失败时抛出。</exception>
    public bool Evaluate(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return true;

        var tokens = Tokenize(expression);
        if (tokens.Count == 0)
            return true;

        var (result, pos) = ParseOrExpr(tokens, 0);
        if (pos < tokens.Count)
            throw new ExpressionEvalException(
                $"表达式在位置 {tokens[pos].Position} 处有意外的标记 '{tokens[pos].Text}'。");

        if (result is bool b)
            return b;

        // 非布尔结果：truthy 检查
        return IsTruthy(result);
    }

    #region 词法分析

    private static readonly Regex TokenPattern = new(
        @"\s*(?<tok>" +
        @"==|!=|>=|<=|>|<|&&|\|\||[!()]" +          // 运算符和标点
        @"|(?<![$])(?:" +                             // 字面值（不以 $ 开头）
            @"(?<str>'(?:[^'\\]|\\.)*')" +            // 单引号字符串
            @"|(?<str2>""(?:[^""\\]|\\.)*"")" +       // 双引号字符串
            @"|(?<num>\d+\.?\d*)" +                    // 数字
            @"|(?<word>true|false|null|[a-zA-Z_]\w*)" +// 关键字或标识符
        @")" +
        @"|(?<var>\$[a-zA-Z_]\w*(?:\.[a-zA-Z_]\w*)?)" + // 变量引用 $var 或 $input.var
        @")",
        RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.IgnorePatternWhitespace);

    private static List<Token> Tokenize(string expression)
    {
        var tokens = new List<Token>();
        var matches = TokenPattern.Matches(expression);
        var pos = 0;

        foreach (Match m in matches)
        {
            // 跳过匹配之间的空白
            var skipped = expression[pos..m.Index];
            if (skipped.Any(c => !char.IsWhiteSpace(c)))
                throw new ExpressionEvalException($"位置 {pos} 附近存在无法识别的字符: '{skipped.Trim()}'。");

            pos = m.Index + m.Length;

            if (m.Groups["var"].Success)
                tokens.Add(new Token(TokenType.Variable, m.Groups["var"].Value, m.Index));
            else if (m.Groups["str"].Success)
                tokens.Add(new Token(TokenType.String, m.Groups["str"].Value, m.Index));
            else if (m.Groups["str2"].Success)
                tokens.Add(new Token(TokenType.String, m.Groups["str2"].Value, m.Index));
            else if (m.Groups["num"].Success)
                tokens.Add(new Token(TokenType.Number, m.Groups["num"].Value, m.Index));
            else if (m.Groups["word"].Success)
            {
                var word = m.Groups["word"].Value.ToLowerInvariant();
                tokens.Add(word switch
                {
                    "true" => new Token(TokenType.Boolean, true, m.Index),
                    "false" => new Token(TokenType.Boolean, false, m.Index),
                    "null" => new Token(TokenType.Null, null!, m.Index),
                    _ => throw new ExpressionEvalException(
                        $"位置 {m.Index} 处存在未定义的标识符 '{m.Groups["word"].Value}'。" +
                        " 变量引用请使用 $ 前缀（如 $myVar），字符串字面值请加引号。")
                });
            }
            else
            {
                var op = m.Groups["tok"].Value;
                tokens.Add(new Token(TokenType.Operator, op, m.Index));
            }
        }

        // 检查尾部是否有无法识别的字符
        if (pos < expression.Length)
        {
            var tail = expression[pos..].Trim();
            if (tail.Length > 0)
                throw new ExpressionEvalException($"位置 {pos} 附近存在无法识别的字符: '{tail}'。");
        }

        return tokens;
    }

    #endregion

    #region 递归下降解析器

    private (object? value, int pos) ParseOrExpr(List<Token> tokens, int pos)
    {
        var (left, nextPos) = ParseAndExpr(tokens, pos);
        while (nextPos < tokens.Count && IsOp(tokens[nextPos], "||"))
        {
            var (right, afterRight) = ParseAndExpr(tokens, nextPos + 1);
            left = ToBool(left) || ToBool(right);
            nextPos = afterRight;
        }
        return (left, nextPos);
    }

    private (object? value, int pos) ParseAndExpr(List<Token> tokens, int pos)
    {
        var (left, nextPos) = ParseNotExpr(tokens, pos);
        while (nextPos < tokens.Count && IsOp(tokens[nextPos], "&&"))
        {
            var (right, afterRight) = ParseNotExpr(tokens, nextPos + 1);
            left = ToBool(left) && ToBool(right);
            nextPos = afterRight;
        }
        return (left, nextPos);
    }

    private (object? value, int pos) ParseNotExpr(List<Token> tokens, int pos)
    {
        if (pos < tokens.Count && IsOp(tokens[pos], "!"))
        {
            var (value, nextPos) = ParseNotExpr(tokens, pos + 1);
            return (!ToBool(value), nextPos);
        }
        return ParsePrimary(tokens, pos);
    }

    private (object? value, int pos) ParsePrimary(List<Token> tokens, int pos)
    {
        if (pos >= tokens.Count)
            throw new ExpressionEvalException("表达式不完整：缺少值或操作数。");

        // 括号分组
        if (IsOp(tokens[pos], "("))
        {
            var (value, afterExpr) = ParseOrExpr(tokens, pos + 1);
            if (afterExpr >= tokens.Count || !IsOp(tokens[afterExpr], ")"))
                throw new ExpressionEvalException("表达式不完整：缺少右括号 ')'。");
            // 分组内若有比较，返回比较结果；否则返回原值
            return (value, afterExpr + 1);
        }

        return ParseComparison(tokens, pos);
    }

    private (object? value, int pos) ParseComparison(List<Token> tokens, int pos)
    {
        var (left, nextPos) = ParseValue(tokens, pos);

        // 可选的比较运算符
        if (nextPos < tokens.Count && IsComparisonOp(tokens[nextPos]))
        {
            var op = tokens[nextPos].Text;
            var (right, afterRight) = ParseValue(tokens, nextPos + 1);
            return (Compare(left, op, right, tokens[nextPos].Position), afterRight);
        }

        return (left, nextPos);
    }

    private (object? value, int pos) ParseValue(List<Token> tokens, int pos)
    {
        if (pos >= tokens.Count)
            throw new ExpressionEvalException("表达式不完整：缺少值。");

        var token = tokens[pos];

        return token.Type switch
        {
            TokenType.Number => (ParseNumber(token.Text), pos + 1),
            TokenType.String => (ParseString(token.Text), pos + 1),
            TokenType.Boolean => (token.Value, pos + 1),
            TokenType.Null => ((object?)null, pos + 1),
            TokenType.Variable => (ResolveVariable(token.Text, token.Position), pos + 1),
            _ => throw new ExpressionEvalException(
                $"位置 {token.Position} 处有意外的标记 '{token.Text}'，期望值、变量或括号。")
        };
    }

    #endregion

    #region 值解析与运算

    private static double ParseNumber(string text)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            return result;
        throw new ExpressionEvalException($"无效的数字: '{text}'。");
    }

    private static string ParseString(string text)
    {
        // 去除首尾引号并反转义
        var inner = text[1..^1];
        return Regex.Unescape(inner);
    }

    private object? ResolveVariable(string varRef, int position)
    {
        // 格式: $varName 或 $input.varName
        var parts = varRef.TrimStart('$').Split('.');
        if (parts.Length == 1)
        {
            // $varName → 查找 Variables，然后 Inputs，然后 Outputs
            var name = parts[0];
            if (_context.Variables.TryGetValue(name, out var v))
                return v;
            if (_context.Inputs.TryGetValue(name, out var i))
                return i;
            if (_context.Outputs.TryGetValue(name, out var o))
                return o;
            throw new ExpressionEvalException(
                $"位置 {position} 处: 未找到变量 '${{{name}}}'。" +
                " 请确认变量已在 Task/Project/Solution 中定义。");
        }
        else if (parts.Length == 2)
        {
            // $prefix.name
            var prefix = parts[0].ToLowerInvariant();
            var name = parts[1];
            var dict = prefix switch
            {
                "input" => _context.Inputs,
                "output" => _context.Outputs,
                "var" => _context.Variables,
                _ => null
            };
            if (dict == null)
                throw new ExpressionEvalException(
                    $"位置 {position} 处: 未知的引用前缀 '${{{prefix}}}'。支持的前缀: input, output, var。");

            if (dict.TryGetValue(name, out var value))
                return value;
            throw new ExpressionEvalException(
                $"位置 {position} 处: 未找到 '${{{prefix}.{name}}}'。");
        }
        else
        {
            throw new ExpressionEvalException(
                $"位置 {position} 处: 无效的变量引用 '{varRef}'。格式应为 $varName 或 $input.varName。");
        }
    }

    private static bool IsOp(Token token, string op) =>
        token.Type == TokenType.Operator && token.Text == op;

    private static bool IsComparisonOp(Token token) =>
        token.Type == TokenType.Operator && token.Text is "==" or "!=" or ">=" or "<=" or ">" or "<";

    private static bool ToBool(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            string s => s.Length > 0,
            double d => d != 0.0,
            int i => i != 0,
            _ => true
        };
    }

    private static bool IsTruthy(object? value) => ToBool(value);

    private static bool Compare(object? left, string op, object? right, int position)
    {
        try
        {
            // 尝试数字比较
            if (TryToDouble(left, out var ld) && TryToDouble(right, out var rd))
            {
                return op switch
                {
                    "==" => Math.Abs(ld - rd) < 0.000001,
                    "!=" => Math.Abs(ld - rd) >= 0.000001,
                    ">" => ld > rd,
                    "<" => ld < rd,
                    ">=" => ld >= rd,
                    "<=" => ld <= rd,
                    _ => throw new ExpressionEvalException($"未知的比较运算符 '{op}'。")
                };
            }

            // 尝试字符串比较
            // null 处理
            if (left is null && right is null)
                return op is "==" or ">=" or "<=";
            if (left is null || right is null)
                return op is "!=";

            // 优先使用类型精确比较
            if (left is string ls && right is string rs)
            {
                return op switch
                {
                    "==" => ls == rs,
                    "!=" => ls != rs,
                    ">" => string.Compare(ls, rs, StringComparison.Ordinal) > 0,
                    "<" => string.Compare(ls, rs, StringComparison.Ordinal) < 0,
                    ">=" => string.Compare(ls, rs, StringComparison.Ordinal) >= 0,
                    "<=" => string.Compare(ls, rs, StringComparison.Ordinal) <= 0,
                    _ => throw new ExpressionEvalException($"未知的比较运算符 '{op}'。")
                };
            }

            // 回退到通用比较
            if (left is IComparable cl && right is IComparable cr &&
                left.GetType() == right.GetType())
            {
                var cmp = cl.CompareTo(cr);
                return op switch
                {
                    "==" => cmp == 0,
                    "!=" => cmp != 0,
                    ">" => cmp > 0,
                    "<" => cmp < 0,
                    ">=" => cmp >= 0,
                    "<=" => cmp <= 0,
                    _ => throw new ExpressionEvalException($"未知的比较运算符 '{op}'。")
                };
            }

            // 最终回退：字符串化比较
            var lstr = left?.ToString() ?? "";
            var rstr = right?.ToString() ?? "";

            return op switch
            {
                "==" => string.Equals(lstr, rstr, StringComparison.Ordinal),
                "!=" => !string.Equals(lstr, rstr, StringComparison.Ordinal),
                ">" => string.Compare(lstr, rstr, StringComparison.Ordinal) > 0,
                "<" => string.Compare(lstr, rstr, StringComparison.Ordinal) < 0,
                ">=" => string.Compare(lstr, rstr, StringComparison.Ordinal) >= 0,
                "<=" => string.Compare(lstr, rstr, StringComparison.Ordinal) <= 0,
                _ => throw new ExpressionEvalException($"未知的比较运算符 '{op}'。")
            };
        }
        catch (ExpressionEvalException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ExpressionEvalException(
                $"位置 {position} 处比较操作失败: {ex.Message}", ex);
        }
    }

    private static bool TryToDouble(object? value, out double result)
    {
        result = 0;
        if (value is null) return false;
        if (value is double d) { result = d; return true; }
        if (value is int i) { result = i; return true; }
        if (value is float f) { result = f; return true; }
        if (value is long l) { result = l; return true; }
        if (value is short s) { result = s; return true; }
        if (value is bool) return false; // 布尔不做数字比较
        // 尝试字符串转数字
        var str = value.ToString();
        return double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    #endregion

    #region 内部类型

    private enum TokenType { Number, String, Boolean, Null, Variable, Operator }

    private readonly struct Token
    {
        public TokenType Type { get; }
        public object? Value { get; }
        public string Text { get; }
        public int Position { get; }

        public Token(TokenType type, object? value, int position)
        {
            Type = type;
            Value = value;
            Text = value?.ToString() ?? "";
            Position = position;
        }

        public Token(TokenType type, string text, int position)
        {
            Type = type;
            Value = null;
            Text = text;
            Position = position;
        }

        public override string ToString() => $"Token({Type}, '{Text}', @{Position})";
    }

    #endregion
}

/// <summary>
/// 表达式求值过程中发生的错误。
/// </summary>
public sealed class ExpressionEvalException : Exception
{
    /// <summary>
    /// 创建表达式求值异常。
    /// </summary>
    /// <param name="message">错误描述。</param>
    public ExpressionEvalException(string message) : base(message) { }

    /// <summary>
    /// 创建带内部异常的表达式求值异常。
    /// </summary>
    /// <param name="message">错误描述。</param>
    /// <param name="innerException">内部异常。</param>
    public ExpressionEvalException(string message, Exception innerException)
        : base(message, innerException) { }
}
