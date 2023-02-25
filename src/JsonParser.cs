using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SlugBase
{
    internal static class JsonParser
    {
        public static object Parse(string text)
        {
            var ts = GetTokens(text).GetEnumerator();
            ts.MoveNext();

            var res = Parse(text, ts);
            AssertType(text, ts.Current, TokenType.EndOfFile);

            return res;
        }

        static object Parse(string text, IEnumerator<Token> ts)
        {
            return ts.Current.Type switch
            {
                TokenType.BeginArray => ParseArray(text, ts),
                TokenType.BeginObject => ParseObject(text, ts),
                TokenType.Literal => ParseLiteral(text, ts),
                TokenType.Number => ParseNumber(text, ts),
                TokenType.String => ParseString(text, ts),
                _ => throw new JsonParseException(text, ts.Current.Start, $"Unexpected token: {GetTokenName(ts.Current.Type)}"),
            };
        }

        static List<object> ParseArray(string text, IEnumerator<Token> ts)
        {
            var array = new List<object>();
            do
            {
                ts.MoveNext();
                if (ts.Current.Type != TokenType.EndArray)
                    array.Add(Parse(text, ts));
            }
            while (ts.Current.Type == TokenType.ValueSeparator);
            AssertType(text, ts.Current, TokenType.EndArray);

            ts.MoveNext();
            return array;
        }

        static Dictionary<string, object> ParseObject(string text, IEnumerator<Token> ts)
        {
            var dict = new Dictionary<string, object>();
            do
            {
                ts.MoveNext();
                if (ts.Current.Type != TokenType.EndObject)
                {
                    AssertType(text, ts.Current, TokenType.String);
                    var key = ParseString(text, ts);

                    AssertType(text, ts.Current, TokenType.NameSeparator);

                    ts.MoveNext();
                    var value = Parse(text, ts);

                    dict.Add(key, value);
                }
            }
            while (ts.Current.Type == TokenType.ValueSeparator);
            AssertType(text, ts.Current, TokenType.EndObject);

            ts.MoveNext();
            return dict;
        }

        static object ParseLiteral(string text, IEnumerator<Token> ts)
        {
            var literal = text.Substring(ts.Current.Start, ts.Current.Length);
            object result = literal switch
            {
                "null" => null,
                "true" => true,
                "false" => false,
                _ => throw new JsonParseException(text, ts.Current.Start, $"Unknown literal: {literal}"),
            };

            ts.MoveNext();
            return result;
        }

        static double ParseNumber(string text, IEnumerator<Token> ts)
        {
            var result = double.Parse(text.Substring(ts.Current.Start, ts.Current.Length));
            ts.MoveNext();
            return result;
        }

        static string ParseString(string text, IEnumerator<Token> ts)
        {
            var sb = new StringBuilder();

            int start = ts.Current.Start + 1;
            int current = start;
            int end = ts.Current.Start + ts.Current.Length - 1;

            while (current < end)
            {
                // Evaluate escape sequences
                if (text[current] == '\\')
                {
                    sb.Append(text, start, current - start);
                    current++;
                    if (current >= end) throw new JsonParseException(text, current, "Unexpected end of string escape sequence!");

                    switch (text[current])
                    {
                        case '\"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':

                            if (current + 4 >= end) throw new JsonParseException(text, end, "Unexpected end of string in unicode escape sequence!");

                            for (int i = 0; i < 4; i++)
                            {
                                var c = text[current + 1 + i];
                                if (!(c >= '0' && c <= '9')
                                    && !(c >= 'a' && c <= 'f')
                                    && !(c >= 'A' && c <= 'F'))
                                {
                                    throw new JsonParseException(text, current + 1 + i, $"Unexpected character in unicode escape sequence: {c}");
                                }
                            }
                            int codePoint = int.Parse(text.Substring(current + 1, 4), System.Globalization.NumberStyles.HexNumber);
                            sb.Append((char)codePoint);
                            current += 4;

                            break;
                    }
                    start = current + 1;
                }
                else if (text[current] <= 0x001F)
                {
                    bool lineBreak = text[current] == '\r' || text[current] == '\n';
                    throw new JsonParseException(text, current, $"Unexpected {(lineBreak ? "line break" : "control character")} in string!");
                }

                current++;
            }

            sb.Append(text, start, end - start);

            ts.MoveNext();
            return sb.ToString();
        }

        static void AssertType(string text, Token token, TokenType expectedType)
        {
            if (token.Type != expectedType)
                throw new JsonParseException(text, token.Start, $"Expected {GetTokenName(expectedType)}, found {GetTokenName(token.Type)}!");
        }

        static string GetTokenName(TokenType token)
        {
            return token switch
            {
                TokenType.BeginArray => "'['",
                TokenType.EndArray => "']'",
                TokenType.BeginObject => "'{'",
                TokenType.EndObject => "'}'",
                TokenType.Literal => "literal",
                TokenType.NameSeparator => "':'",
                TokenType.Number => "number",
                TokenType.Space => "whitespace",
                TokenType.String => "string",
                TokenType.ValueSeparator => "','",
                TokenType.EndOfFile => "end of file",
                _ => "unknown",
            };
        }

        static IEnumerable<Token> GetTokens(string text)
        {
            int i = 0;
            while (i < text.Length)
            {
                var token = NextToken(text, i);
                i += token.Length;

                if (token.Type != TokenType.Space)
                    yield return token;
            }

            yield return new Token()
            {
                Start = text.Length,
                Length = 0,
                Type = TokenType.EndOfFile
            };
        }

        static bool IsSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '\r' || c == '\n';
        }

        static readonly Regex _numberFormat = new(@"-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?");
        static readonly string[] _literals = { "true", "false", "null" };
        static Token NextToken(string text, int i)
        {
            var t = new Token();
            t.Start = i;
            t.Length = 1;
            char c = text[i];

            switch (c)
            {
                case '[': t.Type = TokenType.BeginArray; break;
                case ']': t.Type = TokenType.EndArray; break;
                case '{': t.Type = TokenType.BeginObject; break;
                case '}': t.Type = TokenType.EndObject; break;
                case ':': t.Type = TokenType.NameSeparator; break;
                case ',': t.Type = TokenType.ValueSeparator; break;
                default:

                    if (IsSpace(c))
                    {
                        t.Type = TokenType.Space;
                        while (i < text.Length && IsSpace(text[i]))
                            i++;
                        t.Length = i - t.Start;
                    }
                    else if (char.IsDigit(c) || c == '-')
                    {
                        t.Type = TokenType.Number;
                        t.Length = _numberFormat.Match(text, i).Length;
                    }
                    else if (c == '"')
                    {
                        t.Type = TokenType.String;
                        i++;
                        while (i < text.Length && text[i] != '"')
                        {
                            if (text[i] == '\\') i += 2;
                            else i += 1;
                        }
                        i++;
                        if (i > text.Length)
                            throw new JsonParseException(text, text.Length, "Unterminated string!");
                        t.Length = i - t.Start;
                    }
                    else
                    {
                        bool isLiteral = false;
                        foreach (var literal in _literals)
                        {
                            if (text.Length - i >= literal.Length
                                && text.Substring(i, literal.Length).Equals(literal, StringComparison.Ordinal))
                            {
                                t.Type = TokenType.Literal;
                                t.Length = literal.Length;
                                isLiteral = true;
                                break;
                            }
                        }

                        if (!isLiteral)
                            throw new JsonParseException(text, i, $"Unexpected character: {c}");
                    }

                    break;
            }

            return t;
        }

        struct Token
        {
            public int Start;
            public int Length;
            public TokenType Type;
        }

        enum TokenType
        {
            Space,
            BeginArray,
            EndArray,
            BeginObject,
            EndObject,
            NameSeparator,
            ValueSeparator,
            String,
            Number,
            Literal,
            EndOfFile
        }
    }

    /// <summary>
    /// Represents errors that occur when parsing JSON data.
    /// </summary>
    public class JsonParseException : Exception
    {
        /// <summary>
        /// The offset in the input string that the error occurred at.
        /// </summary>
        public int CharIndex { get; private set; }

        /// <summary>
        /// The line in the input string that the error occurred at.
        /// </summary>
        public int Line { get; private set; }

        internal JsonParseException(string text, int position) => FindLine(text, position);
        internal JsonParseException(string text, int position, string message) : base(message) => FindLine(text, position);
        internal JsonParseException(string text, int position, string message, Exception inner) : base(message, inner) => FindLine(text, position);

        void FindLine(string text, int position)
        {
            int line = 0;
            for (int i = 0; i < text.Length && i < position; i++)
            {
                if (text[i] == '\r' || text[i] == '\n')
                {
                    line++;
                    if (text[i] == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                        i++;
                }
            }

            CharIndex = position;
            Line = line;
        }
    }
}
