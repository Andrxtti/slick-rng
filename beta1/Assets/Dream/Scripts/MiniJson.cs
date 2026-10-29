using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Dream
{
    // A small JSON reader: objects -> Dictionary<string, object>, arrays -> List<object>, numbers -> double.
    public static class MiniJson
    {
        public static object Parse(string s) { int i = 0; return Value(s, ref i); }

        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object Value(string s, ref int i)
        {
            Ws(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                var d = new Dictionary<string, object>(); i++;
                while (true)
                {
                    Ws(s, ref i); if (s[i] == '}') { i++; return d; }
                    var k = Str(s, ref i); Ws(s, ref i); i++;   // ':'
                    d[k] = Value(s, ref i); Ws(s, ref i);
                    if (s[i] == ',') i++;
                }
            }
            if (c == '[')
            {
                var l = new List<object>(); i++;
                while (true)
                {
                    Ws(s, ref i); if (s[i] == ']') { i++; return l; }
                    l.Add(Value(s, ref i)); Ws(s, ref i);
                    if (s[i] == ',') i++;
                }
            }
            if (c == '"') return Str(s, ref i);
            if (s.Substring(i).StartsWith("true")) { i += 4; return true; }
            if (s.Substring(i).StartsWith("false")) { i += 5; return false; }
            if (s.Substring(i).StartsWith("null")) { i += 4; return null; }
            int st = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(st, i - st), CultureInfo.InvariantCulture);
        }

        static string Str(string s, ref int i)
        {
            var b = new StringBuilder(); i++;
            while (s[i] != '"')
            {
                if (s[i] == '\\')
                {
                    i++;
                    switch (s[i]) { case 'n': b.Append('\n'); break; case 't': b.Append('\t'); break; case 'u': b.Append((char)int.Parse(s.Substring(i + 1, 4), NumberStyles.HexNumber)); i += 4; break; default: b.Append(s[i]); break; }
                }
                else b.Append(s[i]);
                i++;
            }
            i++; return b.ToString();
        }
    }
}
