using System.Globalization;
using VCWpfControls.Grid.Data;
using VCWpfControls.Localization;

namespace VCWpfControls.Conditions;

/// <summary>Thrown when a condition expression cannot be parsed.</summary>
public sealed class ConditionParseException : Exception
{
    /// <summary>Creates the exception.</summary>
    public ConditionParseException(string message, int position)
        : base($"{message} (position {position})")
    {
        Position = position;
    }

    /// <summary>The zero-based position in the input text.</summary>
    public int Position { get; }
}

/// <summary>
/// The text form of a condition tree (DevExpress-style):
/// <c>[Salary] &gt; 1000 And [Name] StartsWith 'A' Or ([Dept] In ('IT', 'HR'))</c>.
/// Fields are bracketed; strings are single-quoted with '' escaping; numbers, true/false,
/// null and dates (<c>#2020-01-31#</c>) are literals; Between takes a comma pair
/// (<c>Between 1, 10</c>); In takes a comma list. <c>And</c> binds tighter than <c>Or</c>;
/// parentheses make groups. <see cref="Parse"/> and <see cref="Format"/> round-trip.
/// </summary>
public static class ConditionParser
{
    /// <summary>Parses the text into a condition tree (an And group when mixed).</summary>
    public static ConditionNode Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ConditionParseException("The expression is empty.", 0);
        }

        var tokens = new Tokenizer(text);
        var node = ParseOr(tokens);
        if (tokens.Peek() is not null)
        {
            throw new ConditionParseException($"Unexpected '{tokens.Peek()}'.", tokens.Position);
        }

        return node;
    }

    /// <summary>Formats the tree back into the text form (canonical, round-trips).</summary>
    public static string Format(ConditionNode? node)
        => node switch
        {
            null => string.Empty,
            ConditionExpression expression => FormatExpression(expression),
            ConditionGroup group => FormatGroup(group, nested: false),
            _ => string.Empty,
        };

    private static ConditionGroup Wrap(ConditionGroupOperator operatorKind, List<ConditionNode> children)
    {
        var group = new ConditionGroup(operatorKind);
        group.Children.AddRange(children);
        return group;
    }

    private static ConditionNode ParseOr(Tokenizer tokens)
    {
        var children = new List<ConditionNode> { ParseAnd(tokens) };
        while (tokens.MatchKeyword("or"))
        {
            children.Add(ParseAnd(tokens));
        }

        return children.Count == 1 ? children[0] : Wrap(ConditionGroupOperator.Or, children);
    }

    private static ConditionNode ParseAnd(Tokenizer tokens)
    {
        var children = new List<ConditionNode> { ParsePrimary(tokens) };
        while (tokens.MatchKeyword("and"))
        {
            children.Add(ParsePrimary(tokens));
        }

        return children.Count == 1 ? children[0] : Wrap(ConditionGroupOperator.And, children);
    }

    private static ConditionNode ParsePrimary(Tokenizer tokens)
    {
        if (tokens.MatchPunctuation("("))
        {
            var inner = ParseOr(tokens);
            if (!tokens.MatchPunctuation(")"))
            {
                throw new ConditionParseException("Expected ')'.", tokens.Position);
            }

            // A parenthesized node stays a group so Format can round-trip the shape;
            // a bare single expression in parens becomes a one-child And group.
            return inner is ConditionGroup group
                ? group
                : Wrap(ConditionGroupOperator.And, new List<ConditionNode> { inner });
        }

        var field = tokens.ReadField();
        var operatorKind = tokens.ReadOperator();
        object? value;
        object? value2 = null;
        if (operatorKind is GridFilterOperator.Between or GridFilterOperator.NotBetween)
        {
            value = tokens.ReadValue();
            tokens.ExpectPunctuation(",");
            value2 = tokens.ReadValue();
        }
        else if (operatorKind is GridFilterOperator.In or GridFilterOperator.NotIn)
        {
            tokens.ExpectPunctuation("(");
            var list = new List<object?>();
            list.Add(tokens.ReadValue());
            while (tokens.MatchPunctuation(","))
            {
                list.Add(tokens.ReadValue());
            }

            tokens.ExpectPunctuation(")");
            value = list;
        }
        else if (operatorKind is GridFilterOperator.IsNull or GridFilterOperator.IsNotNull)
        {
            value = null;
        }
        else
        {
            value = tokens.ReadValue();
        }

        return new ConditionExpression(field, operatorKind, value, value2);
    }

    private static string FormatExpression(ConditionExpression expression)
    {
        var builder = new System.Text.StringBuilder();
        builder.Append('[').Append(expression.FieldName).Append("] ").Append(OperatorToken(expression.OperatorKind));
        switch (expression.OperatorKind)
        {
            case GridFilterOperator.Between:
            case GridFilterOperator.NotBetween:
                builder.Append(' ').Append(FormatValue(expression.Value)).Append(", ").Append(FormatValue(expression.Value2));
                break;
            case GridFilterOperator.In:
            case GridFilterOperator.NotIn:
                var list = (System.Collections.IEnumerable?)expression.Value ?? Array.Empty<object>();
                builder.Append(" (").Append(string.Join(", ", list.Cast<object?>().Select(FormatValue))).Append(')');
                break;
            case GridFilterOperator.IsNull:
            case GridFilterOperator.IsNotNull:
                break;
            default:
                builder.Append(' ').Append(FormatValue(expression.Value));
                break;
        }

        return builder.ToString();
    }

    private static string FormatGroup(ConditionGroup group, bool nested)
    {
        var joiner = group.OperatorKind == ConditionGroupOperator.And ? " And " : " Or ";
        var body = string.Join(
            joiner,
            group.Children.Select(child => child is ConditionGroup inner ? FormatGroup(inner, nested: true) : FormatExpression((ConditionExpression)child)));
        return nested ? "(" + body + ")" : body;
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "null",
            bool flag => flag ? "true" : "false",
            DateTime date => "#" + date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + "#",
            string text => "'" + text.Replace("'", "''") + "'",
            double or float or decimal => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? "null",
        };
    }

    private static string OperatorToken(GridFilterOperator operatorKind)
        => operatorKind switch
        {
            GridFilterOperator.Equals => "=",
            GridFilterOperator.NotEquals => "!=",
            GridFilterOperator.Contains => "Contains",
            GridFilterOperator.NotContains => "NotContains",
            GridFilterOperator.StartsWith => "StartsWith",
            GridFilterOperator.EndsWith => "EndsWith",
            GridFilterOperator.GreaterThan => ">",
            GridFilterOperator.GreaterThanOrEqual => ">=",
            GridFilterOperator.LessThan => "<",
            GridFilterOperator.LessThanOrEqual => "<=",
            GridFilterOperator.Between => "Between",
            GridFilterOperator.NotBetween => "NotBetween",
            GridFilterOperator.In => "In",
            GridFilterOperator.NotIn => "NotIn",
            GridFilterOperator.IsNull => "IsNull",
            GridFilterOperator.IsNotNull => "IsNotNull",
            _ => throw new ArgumentException($"Operator {operatorKind} is not representable in text.", nameof(operatorKind)),
        };

    private sealed class Tokenizer
    {
        private readonly string _text;
        private int _index;

        public Tokenizer(string text)
        {
            _text = text;
        }

        public int Position => _index;

        public string? Peek()
        {
            SkipWhitespace();
            return _index < _text.Length ? _text[_index].ToString() : null;
        }

        public bool MatchPunctuation(string punctuation)
        {
            SkipWhitespace();
            if (_index < _text.Length && _text[_index].ToString() == punctuation)
            {
                _index++;
                return true;
            }

            return false;
        }

        public void ExpectPunctuation(string punctuation)
        {
            if (!MatchPunctuation(punctuation))
            {
                throw new ConditionParseException($"Expected '{punctuation}'.", _index);
            }
        }

        public bool MatchKeyword(string keyword)
        {
            SkipWhitespace();
            var start = _index;
            if (_index + keyword.Length > _text.Length)
            {
                return false;
            }

            if (!string.Equals(_text.Substring(_index, keyword.Length), keyword, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var end = _index + keyword.Length;
            if (end < _text.Length && (char.IsLetterOrDigit(_text[end]) || _text[end] == '_' || _text[end] == '['))
            {
                return false;
            }

            _index = end;
            return true;
        }

        public string ReadField()
        {
            SkipWhitespace();
            if (_index >= _text.Length || _text[_index] != '[')
            {
                throw new ConditionParseException("Expected a bracketed field like [Name].", _index);
            }

            var close = _text.IndexOf(']', _index + 1);
            if (close < 0)
            {
                throw new ConditionParseException("Field is missing ']'.", _index);
            }

            var field = _text.Substring(_index + 1, close - _index - 1).Trim();
            if (field.Length == 0)
            {
                throw new ConditionParseException("Field name is empty.", _index);
            }

            _index = close + 1;
            return field;
        }

        public GridFilterOperator ReadOperator()
        {
            SkipWhitespace();
            string token;
            if (_index < _text.Length && "<>!=".Contains(_text[_index]))
            {
                var two = _index + 1 < _text.Length ? _text.Substring(_index, 2) : _text.Substring(_index, 1);
                token = two is ">=" or "<=" or "!=" or "<>" ? two : _text[_index].ToString();
                _index += token.Length;
            }
            else
            {
                var start = _index;
                while (_index < _text.Length && (char.IsLetter(_text[_index]) || _text[_index] == '_'))
                {
                    _index++;
                }

                token = _text.Substring(start, _index - start);
            }

            return token.ToLowerInvariant() switch
            {
                "=" => GridFilterOperator.Equals,
                "==" => GridFilterOperator.Equals,
                "!=" => GridFilterOperator.NotEquals,
                "<>" => GridFilterOperator.NotEquals,
                ">" => GridFilterOperator.GreaterThan,
                ">=" => GridFilterOperator.GreaterThanOrEqual,
                "<" => GridFilterOperator.LessThan,
                "<=" => GridFilterOperator.LessThanOrEqual,
                "contains" => GridFilterOperator.Contains,
                "notcontains" => GridFilterOperator.NotContains,
                "startswith" => GridFilterOperator.StartsWith,
                "endswith" => GridFilterOperator.EndsWith,
                "between" => GridFilterOperator.Between,
                "notbetween" => GridFilterOperator.NotBetween,
                "in" => GridFilterOperator.In,
                "notin" => GridFilterOperator.NotIn,
                "isnull" => GridFilterOperator.IsNull,
                "isnotnull" => GridFilterOperator.IsNotNull,
                "" => throw new ConditionParseException("Expected an operator.", _index),
                _ => throw new ConditionParseException($"Unknown operator '{token}'.", _index),
            };
        }

        public object? ReadValue()
        {
            SkipWhitespace();
            if (_index >= _text.Length)
            {
                throw new ConditionParseException("Expected a value.", _index);
            }

            var c = _text[_index];
            if (c == '\'')
            {
                _index++;
                var builder = new System.Text.StringBuilder();
                while (true)
                {
                    if (_index >= _text.Length)
                    {
                        throw new ConditionParseException("String is missing the closing quote.", _index);
                    }

                    if (_text[_index] == '\'')
                    {
                        if (_index + 1 < _text.Length && _text[_index + 1] == '\'')
                        {
                            builder.Append('\'');
                            _index += 2;
                            continue;
                        }

                        _index++;
                        return builder.ToString();
                    }

                    builder.Append(_text[_index]);
                    _index++;
                }
            }

            if (c == '#')
            {
                var close = _text.IndexOf('#', _index + 1);
                if (close < 0)
                {
                    throw new ConditionParseException("Date is missing the closing '#'.", _index);
                }

                var dateText = _text.Substring(_index + 1, close - _index - 1);
                if (!DateTime.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    throw new ConditionParseException($"'{dateText}' is not a date (expected #yyyy-MM-dd#).", _index);
                }

                _index = close + 1;
                return date;
            }

            var start = _index;
            while (_index < _text.Length && (_text[_index] != ',' && _text[_index] != ')' && !char.IsWhiteSpace(_text[_index])))
            {
                _index++;
            }

            var literal = _text.Substring(start, _index - start);
            return literal.ToLowerInvariant() switch
            {
                "null" => null,
                "true" => true,
                "false" => false,
                _ when int.TryParse(literal, NumberStyles.Integer, CultureInfo.InvariantCulture, out var small) => small,
                _ when long.TryParse(literal, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer) => integer,
                _ when decimal.TryParse(literal, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) => number,
                _ => throw new ConditionParseException($"'{literal}' is not a value (quote strings, use #yyyy-MM-dd# for dates).", start),
            };
        }

        private void SkipWhitespace()
        {
            while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
            {
                _index++;
            }
        }
    }
}
