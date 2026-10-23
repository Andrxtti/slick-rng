// Minimal JSON for The Dream: parses into Dictionary<string, object> / List<object> / double / string / bool / null,
// and writes the same shapes back out. Small, allocation-light, no packages needed.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CFDream
{
    public static class Json
    {
        public static object Parse(string text)
        {
            if (text == null) return null;
            int i = 0;
            var v = ReadValue(text, ref i);
            return v;
        }

        static void Skip(string s, ref int i)
        {
            while (i < s.Length)
            {
                char c = s[i];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r') i++;
                else break;
            }
        }

        static object ReadValue(string s, ref int i)
        {
            Skip(s, ref i);
            if (i >= s.Length) return null;
            char c = s[i];
            if (c == '{') return ReadObject(s, ref i);
            if (c == '[') return ReadArray(s, ref i);
            if (c == '"') return ReadString(s, ref i);
            if (c == 't' && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (c == 'f' && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (c == 'n' && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            return ReadNumber(s, ref i);
        }

        static Dictionary<string, object> ReadObject(string s, ref int i)
        {
            var d = new Dictionary<string, object>();
            i++; // {
            Skip(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return d; }
            while (i < s.Length)
            {
                Skip(s, ref i);
                string key = ReadString(s, ref i);
                Skip(s, ref i);
                i++; // :
                d[key] = ReadValue(s, ref i);
                Skip(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; break; }
            }
            return d;
        }

        static List<object> ReadArray(string s, ref int i)
        {
            var l = new List<object>();
            i++; // [
            Skip(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return l; }
            while (i < s.Length)
            {
                l.Add(ReadValue(s, ref i));
                Skip(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; break; }
            }
            return l;
        }

        static string ReadString(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++; // opening quote
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') break;
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
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
                        if (i + 4 <= s.Length)
                        {
                            sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                            i += 4;
                        }
                        break;
                    default: sb.Append(e); break;
                }
            }
            return sb.ToString();
        }

        static object ReadNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length)
            {
                char c = s[i];
                if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E') i++;
                else break;
            }
            double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double v);
            return v;
        }

        // ------------------------------------------------------------ writing
        public static string Write(object v)
        {
            var sb = new StringBuilder();
            WriteValue(sb, v);
            return sb.ToString();
        }

        static void WriteValue(StringBuilder sb, object v)
        {
            switch (v)
            {
                case null: sb.Append("null"); break;
                case string str: WriteString(sb, str); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case double d: sb.Append(Num(d)); break;
                case float f: sb.Append(Num(f)); break;
                case int n: sb.Append(n.ToString(CultureInfo.InvariantCulture)); break;
                case long n: sb.Append(n.ToString(CultureInfo.InvariantCulture)); break;
                case IDictionary<string, object> d:
                    {
                        sb.Append('{'); bool first = true;
                        foreach (var kv in d) { if (!first) sb.Append(','); first = false; WriteString(sb, kv.Key); sb.Append(':'); WriteValue(sb, kv.Value); }
                        sb.Append('}'); break;
                    }
                case System.Collections.IEnumerable list:
                    {
                        sb.Append('['); bool first = true;
                        foreach (var x in list) { if (!first) sb.Append(','); first = false; WriteValue(sb, x); }
                        sb.Append(']'); break;
                    }
                default: WriteString(sb, v.ToString()); break;
            }
        }

        static string Num(double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) return "0";
            if (Math.Abs(d - Math.Round(d)) < 1e-12 && Math.Abs(d) < 1e15) return ((long)Math.Round(d)).ToString(CultureInfo.InvariantCulture);
            return d.ToString("R", CultureInfo.InvariantCulture);
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }

    // Typed access to parsed JSON without casts everywhere.
    public static class J
    {
        public static Dictionary<string, object> O(object o, string k) => o is Dictionary<string, object> d && d.TryGetValue(k, out var v) ? v as Dictionary<string, object> : null;
        public static List<object> L(object o, string k) => o is Dictionary<string, object> d && d.TryGetValue(k, out var v) ? v as List<object> : null;
        public static string S(object o, string k, string def = null) => o is Dictionary<string, object> d && d.TryGetValue(k, out var v) && v is string s ? s : def;
        public static double N(object o, string k, double def = 0) => o is Dictionary<string, object> d && d.TryGetValue(k, out var v) && v is double n ? n : def;
        public static int I(object o, string k, int def = 0) => (int)Math.Round(N(o, k, def));
        public static bool B(object o, string k) => o is Dictionary<string, object> d && d.TryGetValue(k, out var v) && v is bool b && b;
        public static bool Has(object o, string k) => o is Dictionary<string, object> d && d.ContainsKey(k) && d[k] != null;
        public static double Num(object v, double def = 0) => v is double n ? n : def;
        public static List<double> Nums(object v)
        {
            var l = new List<double>();
            if (v is List<object> a) foreach (var x in a) l.Add(x is double n ? n : 0);
            return l;
        }
        public static List<string> Strs(object v)
        {
            var l = new List<string>();
            if (v is List<object> a) foreach (var x in a) l.Add(x as string ?? "");
            return l;
        }
    }
}
