using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AgentPack.VisualDirection
{
    /// <summary>
    /// Parser JSON nho cho Editor tool: object -> Dictionary&lt;string, object&gt;, array -> List&lt;object&gt;,
    /// number -> double, string, bool, null. Dung vi JsonUtility khong doc duoc dictionary tuy y
    /// (profileMap, values) va Template khong phu thuoc Newtonsoft.
    /// </summary>
    public static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var p = new Parser(json);
            object value = p.ParseValue();
            p.SkipWhitespace();
            if (!p.AtEnd) throw p.Error("ky tu thua sau JSON");
            return value;
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;

            public Parser(string s)
            {
                _s = s;
                _i = 0;
                if (_s.Length > 0 && _s[0] == '\uFEFF') _i = 1; // BOM
            }

            public bool AtEnd => _i >= _s.Length;

            public FormatException Error(string message) => new FormatException($"MiniJson: {message} (vi tri {_i})");

            public void SkipWhitespace()
            {
                while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (AtEnd) throw Error("het du lieu");
                char c = _s[_i];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber();
                        throw Error($"ky tu khong hop le '{c}'");
                }
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0) throw Error($"mong doi '{word}'");
                _i += word.Length;
            }

            private Dictionary<string, object> ParseObject()
            {
                var result = new Dictionary<string, object>(StringComparer.Ordinal);
                _i++; // {
                SkipWhitespace();
                if (!AtEnd && _s[_i] == '}') { _i++; return result; }
                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || _s[_i] != '"') throw Error("mong doi ten khoa");
                    string key = ParseString();
                    SkipWhitespace();
                    if (AtEnd || _s[_i] != ':') throw Error("mong doi ':'");
                    _i++;
                    result[key] = ParseValue();
                    SkipWhitespace();
                    if (AtEnd) throw Error("object chua dong");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == '}') { _i++; return result; }
                    throw Error("mong doi ',' hoac '}'");
                }
            }

            private List<object> ParseArray()
            {
                var result = new List<object>();
                _i++; // [
                SkipWhitespace();
                if (!AtEnd && _s[_i] == ']') { _i++; return result; }
                while (true)
                {
                    result.Add(ParseValue());
                    SkipWhitespace();
                    if (AtEnd) throw Error("array chua dong");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == ']') { _i++; return result; }
                    throw Error("mong doi ',' hoac ']'");
                }
            }

            private string ParseString()
            {
                var sb = new StringBuilder();
                _i++; // "
                while (true)
                {
                    if (AtEnd) throw Error("string chua dong");
                    char c = _s[_i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) throw Error("escape chua dong");
                    char e = _s[_i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_i + 4 > _s.Length) throw Error("\\u thieu ky tu");
                            sb.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            _i += 4;
                            break;
                        default: throw Error($"escape khong hop le '\\{e}'");
                    }
                }
            }

            private double ParseNumber()
            {
                int start = _i;
                if (_s[_i] == '-') _i++;
                while (_i < _s.Length && "0123456789.eE+-".IndexOf(_s[_i]) >= 0) _i++;
                string token = _s.Substring(start, _i - start);
                if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    throw Error($"so khong hop le '{token}'");
                return d;
            }
        }
    }
}
