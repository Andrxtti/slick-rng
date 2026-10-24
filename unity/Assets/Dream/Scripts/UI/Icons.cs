// Line icons for the tab bar, the creator and the closet, drawn as vectors so they stay sharp at any size.
// Shapes are written as SVG path data (M L H V C Q Z, absolute or relative) in a 24 x 24 box, plus circles
// ("c:cx,cy,r") and rounded rectangles ("r:x,y,w,h,radius"). Prefix "f:" to fill instead of stroke.
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public static class VPath
    {
        // Traces SVG path data into the painter. `map` turns path units into element pixels.
        public static void Trace(Painter2D p, string d, Func<Vector2, Vector2> map)
        {
            var t = Tokens(d); int i = 0; char cmd = 'M';
            Vector2 cur = Vector2.zero, start = Vector2.zero;
            float N() { if (i < t.Count && t[i] is float f) { i++; return f; } return 0; }
            while (i < t.Count)
            {
                if (t[i] is char c)
                {
                    cmd = c; i++;
                    if (char.ToUpperInvariant(c) == 'Z') { p.ClosePath(); cur = start; }
                    continue;
                }
                // a number: run the current command again (SVG's implicit repeat)
                bool rel = char.IsLower(cmd); Vector2 off = rel ? cur : Vector2.zero;
                switch (char.ToUpperInvariant(cmd))
                {
                    case 'M': cur = off + new Vector2(N(), N()); start = cur; p.MoveTo(map(cur)); cmd = rel ? 'l' : 'L'; break;
                    case 'L': cur = off + new Vector2(N(), N()); p.LineTo(map(cur)); break;
                    case 'H': cur = new Vector2((rel ? cur.x : 0) + N(), cur.y); p.LineTo(map(cur)); break;
                    case 'V': cur = new Vector2(cur.x, (rel ? cur.y : 0) + N()); p.LineTo(map(cur)); break;
                    case 'C':
                        {
                            var a = off + new Vector2(N(), N()); var b = off + new Vector2(N(), N()); var e = off + new Vector2(N(), N());
                            p.BezierCurveTo(map(a), map(b), map(e)); cur = e; break;
                        }
                    case 'Q':
                        {
                            var a = off + new Vector2(N(), N()); var e = off + new Vector2(N(), N());
                            p.QuadraticCurveTo(map(a), map(e)); cur = e; break;
                        }
                    default: i++; break;
                }
            }
        }

        static readonly Dictionary<string, List<object>> cache = new Dictionary<string, List<object>>();
        static List<object> Tokens(string d)
        {
            if (cache.TryGetValue(d, out var l)) return l;
            l = new List<object>(); int i = 0;
            while (i < d.Length)
            {
                char c = d[i];
                if (char.IsLetter(c)) { l.Add(c); i++; continue; }
                if (c == '-' || c == '.' || char.IsDigit(c))
                {
                    int j = i + 1; bool dot = c == '.';
                    while (j < d.Length && (char.IsDigit(d[j]) || (d[j] == '.' && !dot)))
                    { if (d[j] == '.') dot = true; j++; }
                    if (float.TryParse(d.Substring(i, j - i), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) l.Add(v);
                    i = j; continue;
                }
                i++;
            }
            cache[d] = l; return l;
        }

        // Draws a list of shapes (see the header) with the given colours. Strokes scale with the box.
        public static void Draw(Painter2D p, IList<string> shapes, Rect box, float units, Color ink, float stroke = 1.8f, Color? cut = null)
        {
            float k = Mathf.Min(box.width, box.height) / units;
            var o = new Vector2(box.x + (box.width - units * k) / 2, box.y + (box.height - units * k) / 2);
            Vector2 M(Vector2 v) => o + v * k;
            p.lineWidth = stroke * k; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round;
            foreach (var raw in shapes)
            {
                string s = raw; bool fill = false, knock = false;
                if (s.StartsWith("f:")) { fill = true; s = s.Substring(2); }
                else if (s.StartsWith("k:")) { knock = true; s = s.Substring(2); } // fill with the cut-out colour, then stroke
                p.BeginPath();
                if (s.StartsWith("c:") || s.StartsWith("r:"))
                {
                    var n = Nums(s.Substring(2));
                    if (s[0] == 'c' && n.Length >= 3) p.Arc(M(new Vector2(n[0], n[1])), n[2] * k, Angle.Degrees(0), Angle.Degrees(360));
                    else if (n.Length >= 5) RoundRect(p, M(new Vector2(n[0], n[1])), n[2] * k, n[3] * k, n[4] * k);
                }
                else Trace(p, s, M);
                if (fill) { p.fillColor = ink; p.Fill(); }
                else
                {
                    if (knock && cut.HasValue) { p.fillColor = cut.Value; p.Fill(); }
                    p.strokeColor = ink; p.Stroke();
                }
            }
        }
        static float[] Nums(string s)
        {
            var parts = s.Split(','); var r = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++) float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out r[i]);
            return r;
        }
        public static void RoundRect(Painter2D p, Vector2 o, float w, float h, float r)
        {
            r = Mathf.Min(r, w / 2, h / 2);
            p.MoveTo(o + new Vector2(r, 0)); p.LineTo(o + new Vector2(w - r, 0)); p.QuadraticCurveTo(o + new Vector2(w, 0), o + new Vector2(w, r));
            p.LineTo(o + new Vector2(w, h - r)); p.QuadraticCurveTo(o + new Vector2(w, h), o + new Vector2(w - r, h));
            p.LineTo(o + new Vector2(r, h)); p.QuadraticCurveTo(o + new Vector2(0, h), o + new Vector2(0, h - r));
            p.LineTo(o + new Vector2(0, r)); p.QuadraticCurveTo(o, o + new Vector2(r, 0)); p.ClosePath();
        }
    }

    public class Icon : VisualElement
    {
        public static readonly Dictionary<string, string[]> Set = new Dictionary<string, string[]>
        {
            { "play", new[] { "r:5,3,10,14,2", "k:M11 7H17Q19 7 19 9V19Q19 21 17 21H11Q9 21 9 19V9Q9 7 11 7Z" } },
            { "closet", new[] { "M10 6.2C10 5 10.9 4 12 4C13.1 4 14 4.9 14 6C14 7.2 12 7.6 12 9.5V11", "M12 11L3.5 17.5C2.7 18.1 3.1 19 4 19H20C20.9 19 21.3 18.1 20.5 17.5Z" } },
            { "career", new[] { "M8 4H16V9C16 11.2 14.2 13 12 13C9.8 13 8 11.2 8 9Z", "M8 6H5C5 8 6.5 10 8 10M16 6H19C19 8 17.5 10 16 10", "M12 13V17M8.5 20H15.5M10 17H14" } },
            { "online", new[] { "M12 3a9 9 0 1 0 0 18a9 9 0 1 0 0-18Z", "M3 12H21", "M12 3Q7 8 7 12Q7 16 12 21Q17 16 17 12Q17 8 12 3Z" } },
            { "story", new[] { "M4 5.5Q4 4 5.5 4H11V20H5.5Q4 20 4 18.5Z", "M20 5.5Q20 4 18.5 4H13V20H18.5Q20 20 20 18.5Z" } },
            { "dice", new[] { "r:4,4,16,16,4", "f:c:9,9,1.3", "f:c:15,15,1.3", "f:c:15,9,1.3", "f:c:9,15,1.3" } },
            { "pencil", new[] { "M4 20H8L19 9L15 5L4 16Z", "M13.5 6.5L17.5 10.5" } },
            { "rotate", new[] { "M3 12C3 7 7 3 12 3C14.6 3 17 4.1 18.6 5.9L21 8", "M21 3V8H16" } },
            { "lock", new[] { "r:5,11,14,9,2", "M8 11V8C8 5.8 9.8 4 12 4C14.2 4 16 5.8 16 8V11" } },
            { "check", new[] { "M5 12L10 17L19 7" } },
            { "plus", new[] { "M12 6V18M6 12H18" } },
            { "close", new[] { "M6 6L18 18M18 6L6 18" } },
            { "bookmark", new[] { "M7 4H17V20L12 16.5L7 20Z" } },
            { "chev-l", new[] { "M15 6L9 12L15 18" } },
            { "chev-r", new[] { "M9 6L15 12L9 18" } },
            { "tag", new[] { "M3 12V4H11L20.5 13.5L13.5 20.5Z", "f:c:7.5,7.5,1.4" } },
            { "info", new[] { "c:12,12,9", "M12 11V16", "f:c:12,8,1.1" } },
            { "skin", new[] { "c:12,12,8", "f:M12 4C16.4 4 20 7.6 20 12C20 16.4 16.4 20 12 20Z" } },
            { "face", new[] { "M6 9C6 5.7 8.7 3 12 3C15.3 3 18 5.7 18 9V12C18 15.9 15.3 19 12 19C8.7 19 6 15.9 6 12Z", "M10 15.5Q12 16.5 14 15.5" } },
            { "hair", new[] { "M5 14C4 6 9 3 13 3C17 3 20 6 19 11C18 9 16 8 14 8C11 8 8 10 5 14Z", "M7 13V19M17 11V17" } },
            { "eye", new[] { "M2 12C2 12 6 6 12 6C18 6 22 12 22 12C22 12 18 18 12 18C6 18 2 12 2 12Z", "c:12,12,3" } },
            { "beard", new[] { "M5 8V12C5 17 8 21 12 21C16 21 19 17 19 12V8", "M9 15Q12 13 15 15" } },
            { "sculpt", new[] { "M12 3V7M12 17V21M3 12H7M17 12H21", "M6.5 6.5L8.5 8.5M15.5 15.5L17.5 17.5M6.5 17.5L8.5 15.5M15.5 8.5L17.5 6.5" } },
            { "tops", new[] { "M8 4L4 7L6 11L8 10V20H16V10L18 11L20 7L16 4Q14 6 12 6Q10 6 8 4Z" } },
            { "jacket", new[] { "M8 4L3 8V20H7V12M16 4L21 8V20H17V12M8 4L12 10L16 4M12 10V20" } },
            { "formal", new[] { "M10 3H14L13 6H11Z", "M11 6L9 16L12 20L15 16L13 6" } },
            { "head", new[] { "M4 15C4 10.6 7.6 7 12 7C16.4 7 20 10.6 20 15Z", "M20 15H22" } },
            { "eyes", new[] { "c:7,14,4", "c:17,14,4", "M11 14Q12 13 13 14" } },
            { "chain", new[] { "M5 5Q12 19 19 5", "c:12,15,2" } },
            { "ear", new[] { "M12 4V8", "c:12,13,5" } },
            { "ring", new[] { "c:12,14,6", "M9.5 6L12 3.5L14.5 6L12 8.5Z" } },
            { "wrist", new[] { "c:12,12,7", "f:c:12,5,1.6", "f:c:19,12,1.6", "f:c:12,19,1.6", "f:c:5,12,1.6", "f:c:17,7,1.6", "f:c:17,17,1.6", "f:c:7,17,1.6", "f:c:7,7,1.6" } },
            { "watch", new[] { "c:12,12,5", "M9 7.5L9.5 3H14.5L15 7.5M9 16.5L9.5 21H14.5L15 16.5M12 10V12L13.5 13" } },
            { "patch", new[] { "M12 3L19 6V11C19 15.5 16 19 12 21C8 19 5 15.5 5 11V6Z" } },
            { "sleeve", new[] { "c:12,12,8", "M12 7.5L13.3 10.6L16.5 10.9L14.1 13L14.8 16.2L12 14.5L9.2 16.2L9.9 13L7.5 10.9L10.7 10.6Z" } },
            { "home", new[] { "M4 11L12 4L20 11M6 9.5V20H18V9.5M10 20V14H14V20" } },
            { "bottom", new[] { "M7 3H17L18 21H13.5L12 11L10.5 21H6Z", "M7 7H17" } },
            { "shoes", new[] { "M3 17V13L8 12L12 8L14 10L20 13Q22 14 21 17Z", "M3 17H21", "M12 8L10 10M14 10L12 12" } },
            { "pet", new[] { "f:c:6,10,2", "f:c:10,6.5,2", "f:c:14,6.5,2", "f:c:18,10,2", "f:M12 11.5C9 11.5 6.5 15 6.5 17C6.5 19 8.5 19.5 12 18.6C15.5 19.5 17.5 19 17.5 17C17.5 15 15 11.5 12 11.5Z" } },
            { "ride", new[] { "M3 16V12.5L5.5 8H15L19 12L21 12.8V16Z", "c:7.5,16.5,2", "c:16.5,16.5,2" } },
            { "shirts", new[] { "M8 4L4 7L6 11L8 10V20H16V10L18 11L20 7L16 4L12 8Z", "M12 8V20" } },
            { "street", new[] { "M8 5L3 9V20H7V12M16 5L21 9V20H17V12", "M8 5Q12 2 16 5V20H8Z", "M10 14H14" } },
        };

        readonly string[] shapes; Color ink; Color? cut; float stroke;
        public Icon(string name, float size, Color ink, float stroke = 1.8f, Color? cut = null)
        {
            Set.TryGetValue(name ?? "", out shapes);
            this.ink = ink; this.cut = cut ?? C.Ink; this.stroke = stroke;
            style.width = size; style.height = size; style.flexShrink = 0;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += mgc => { if (shapes != null) VPath.Draw(mgc.painter2D, shapes, contentRect, 24, this.ink, this.stroke, this.cut); };
        }
        public Icon Tint(Color c) { ink = c; MarkDirtyRepaint(); return this; }
        public Icon Cut(Color c) { cut = c; MarkDirtyRepaint(); return this; }
    }
}
