// Small building blocks for the screens: the palette, fonts, text, buttons that work inside scroll views,
// bars, chips and cards. Every screen is plain UI Toolkit built from code, styled by Resources/Dream/dream.uss.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public static class C
    {
        public static readonly Color Ink = H("#0b0a08"), Ink2 = H("#13110d"), Card = H("#1a1712"), Card2 = H("#221e17"), Line = H("#3a3226"), Line2 = H("#4d4232"),
            Gold = H("#d9b36c"), Gold2 = H("#f4dfa6"), Gold3 = H("#8f6f33"), Cream = H("#f3ecdc"), Mute = H("#a99d86"), Faint = H("#6f6555"),
            Felt = H("#0e4d39"), Red = H("#e8705a"), Green = H("#5cbf64"), Blue = H("#5aa9de"), Violet = H("#c084fc"), Clear = new Color(0, 0, 0, 0);
        public static Color H(string hex, Color? fallback = null)
        {
            if (!string.IsNullOrEmpty(hex))
            {
                var h = hex.Trim(); if (!h.StartsWith("#")) h = "#" + h;
                if (ColorUtility.TryParseHtmlString(h, out var c)) return c;
            }
            return fallback ?? new Color(0.5f, 0.5f, 0.5f);
        }
        public static Color A(Color c, float a) { c.a = a; return c; }
        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
        public static Color Mix(Color a, Color b, float t) => Color.Lerp(a, b, t);
    }

    // ------------------------------------------------------------ fonts
    // Fonts ship in Resources/Dream/Fonts. When this Unity version can build TextCore font assets at runtime, each
    // font gets DejaVu (card suits, arrows) and Noto Color Emoji as fallbacks; otherwise it uses the plain font.
    public static class Fonts
    {
        public static Font Body, Medium, Semi, Bold, Disp, Mono, MonoB, Sym, Emoji;
        public static bool EmojiOk; // false where colour emoji can't be drawn (the web build): text drops them instead of showing boxes
        static readonly Dictionary<Font, StyleFontDefinition> defs = new Dictionary<Font, StyleFontDefinition>();
        static Type faType;
        public static string Report = "";

        static Font L(string n) { var f = Resources.Load<Font>("Dream/Fonts/" + n); if (f == null) Report += $" missing {n};"; return f; }
        // the optional types we look up by name, with the module that ships them
        static readonly Dictionary<string, string[]> Homes = new Dictionary<string, string[]>
        {
            { "UnityEngine.TextCore.Text.FontAsset", new[] { "UnityEngine.TextCoreTextEngineModule", "UnityEngine.TextCoreModule" } },
            { "UnityEngine.TextCore.LowLevel.GlyphRenderMode", new[] { "UnityEngine.TextCoreFontEngineModule", "UnityEngine.TextCoreModule" } },
            { "UnityEngine.EventSystems.EventSystem", new[] { "UnityEngine.UI" } },
            { "UnityEngine.InputSystem.UI.InputSystemUIInputModule", new[] { "Unity.InputSystem" } },
        };
        public static Type FindType(string full)
        {
            if (full == "UnityEngine.TextCore.Text.FontAsset") { var p = typeof(FontDefinition).GetProperty("fontAsset"); if (p != null) return p.PropertyType; }
            if (Homes.TryGetValue(full, out var mods))
                foreach (var m in mods) { try { var t = Type.GetType(full + ", " + m, false); if (t != null) return t; } catch { } }
            return Type.GetType(full, false);
        }

        // Runs on every Play: Unity 6 can enter Play mode without reloading scripts, and the font assets built
        // last time were destroyed when Play stopped, so they are rebuilt here each time.
        public static void Load()
        {
            defs.Clear(); Report = "";
            Body = L("Inter-Regular"); Medium = L("Inter-Medium"); Semi = L("Inter-SemiBold"); Bold = L("Inter-Bold");
            Disp = L("Gloock-Regular"); Mono = L("IBMPlexMono-Regular"); MonoB = L("IBMPlexMono-Bold"); Sym = L("DejaVuSans-Bold"); Emoji = L("NotoColorEmoji");
            faType = FindType("UnityEngine.TextCore.Text.FontAsset");
            object sym = Make(Sym, false), emo = Make(Emoji, true);
            foreach (var f in new[] { Body, Medium, Semi, Bold, Disp, Mono, MonoB, Sym })
            {
                if (f == null || defs.ContainsKey(f)) continue;
                object fa = f == Sym ? sym : Make(f, false);
                if (fa != null && f != Sym) AddFallback(fa, sym);
                if (fa != null) AddFallback(fa, emo);
                defs[f] = fa != null && TryDef(fa, out var d) ? new StyleFontDefinition(d) : new StyleFontDefinition(FontDefinition.FromFont(f));
            }
            Report += sym != null ? " suits ok;" : " suits via plain font;";
            EmojiOk = emo != null;
            Report += emo != null ? " emoji ok;" : " emoji unavailable;";
            Debug.Log("[Dream] Fonts:" + Report);
        }
        static object Make(Font f, bool color)
        {
            if (f == null || faType == null) return null;
            try
            {
                if (!color)
                {
                    var m = faType.GetMethod("CreateFontAsset", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Font) }, null);
                    return m?.Invoke(null, new object[] { f });
                }
                var grm = FindType("UnityEngine.TextCore.LowLevel.GlyphRenderMode");
                if (grm == null || !Enum.GetNames(grm).Contains("COLOR")) return null;
                foreach (var m in faType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (m.Name != "CreateFontAsset") continue;
                    var ps = m.GetParameters();
                    if (ps.Length < 6 || ps[0].ParameterType != typeof(Font) || ps[1].ParameterType != typeof(int) || ps[2].ParameterType != typeof(int)
                        || ps[3].ParameterType != grm || ps[4].ParameterType != typeof(int) || ps[5].ParameterType != typeof(int)) continue;
                    var args = new object[ps.Length];
                    args[0] = f; args[1] = 109; args[2] = 2; args[3] = Enum.Parse(grm, "COLOR"); args[4] = 1024; args[5] = 1024;
                    for (int i = 6; i < ps.Length; i++)
                        args[i] = ps[i].HasDefaultValue ? ps[i].DefaultValue : ps[i].ParameterType.IsValueType ? Activator.CreateInstance(ps[i].ParameterType) : null;
                    return m.Invoke(null, args);
                }
            }
            catch (Exception e) { Report += $" {f.name}: {e.GetBaseException().Message};"; }
            return null;
        }
        static void AddFallback(object fa, object fb)
        {
            if (fa == null || fb == null) return;
            try
            {
                var p = faType.GetProperty("fallbackFontAssetTable", BindingFlags.Public | BindingFlags.Instance); if (p == null) return;
                var list = p.GetValue(fa) as IList;
                if (list == null) { list = (IList)Activator.CreateInstance(p.PropertyType); p.SetValue(fa, list); }
                if (!list.Contains(fb)) list.Add(fb);
            }
            catch (Exception e) { Report += " fallback: " + e.GetBaseException().Message + ";"; }
        }
        static bool TryDef(object fa, out FontDefinition d)
        {
            d = default;
            try
            {
                var m = typeof(FontDefinition).GetMethod("FromSDFFont", BindingFlags.Public | BindingFlags.Static); if (m == null) return false;
                d = (FontDefinition)m.Invoke(null, new[] { fa }); return true;
            }
            catch { return false; }
        }
        public static void Use(VisualElement e, Font f)
        {
            if (f == null) return;
            if (!defs.TryGetValue(f, out var d)) defs[f] = d = new StyleFontDefinition(FontDefinition.FromFont(f));
            e.style.unityFontDefinition = d;
        }
    }

    // ------------------------------------------------------------ elements
    public static class U
    {
        public static T Cls<T>(this T e, params string[] cls) where T : VisualElement
        {
            foreach (var c in cls) if (!string.IsNullOrEmpty(c)) foreach (var p in c.Split(' ')) if (p.Length > 0) e.AddToClassList(p);
            return e;
        }
        public static T Kids<T>(this T p, params VisualElement[] kids) where T : VisualElement { foreach (var k in kids) if (k != null) p.Add(k); return p; }
        public static T Show<T>(this T e, bool on) where T : VisualElement { e.style.display = on ? DisplayStyle.Flex : DisplayStyle.None; return e; }
        public static T Bg<T>(this T e, Color c) where T : VisualElement { e.style.backgroundColor = c; return e; }
        public static T Fg<T>(this T e, Color c) where T : VisualElement { e.style.color = c; return e; }
        public static T Border<T>(this T e, Color c, float w = 1) where T : VisualElement
        {
            e.style.borderTopColor = c; e.style.borderBottomColor = c; e.style.borderLeftColor = c; e.style.borderRightColor = c;
            e.style.borderTopWidth = w; e.style.borderBottomWidth = w; e.style.borderLeftWidth = w; e.style.borderRightWidth = w; return e;
        }
        public static T Radius<T>(this T e, float r) where T : VisualElement
        {
            e.style.borderTopLeftRadius = r; e.style.borderTopRightRadius = r; e.style.borderBottomLeftRadius = r; e.style.borderBottomRightRadius = r; return e;
        }
        public static T Pad<T>(this T e, float v, float h = -1) where T : VisualElement
        {
            if (h < 0) h = v; e.style.paddingTop = v; e.style.paddingBottom = v; e.style.paddingLeft = h; e.style.paddingRight = h; return e;
        }
        public static T Mb<T>(this T e, float v) where T : VisualElement { e.style.marginBottom = v; return e; }
        public static T Mt<T>(this T e, float v) where T : VisualElement { e.style.marginTop = v; return e; }
        public static T Ml<T>(this T e, float v) where T : VisualElement { e.style.marginLeft = v; return e; }
        public static T Size<T>(this T e, float w, float h) where T : VisualElement { if (w >= 0) e.style.width = w; if (h >= 0) e.style.height = h; return e; }
        public static T Font<T>(this T e, Font f) where T : VisualElement { Fonts.Use(e, f); return e; }
        public static T Fs<T>(this T e, float px) where T : VisualElement { e.style.fontSize = px; return e; }
        public static T Grow<T>(this T e, float g = 1) where T : VisualElement { e.style.flexGrow = g; e.style.flexShrink = 1; return e; }
        public static T Op<T>(this T e, float o) where T : VisualElement { e.style.opacity = o; return e; }
        public static T NoPick<T>(this T e) where T : VisualElement { e.pickingMode = PickingMode.Ignore; return e; }

        public static VisualElement Div(params string[] cls) => new VisualElement().Cls(cls);
        public static VisualElement Row(params VisualElement[] kids) => Div("row").Kids(kids);
        public static VisualElement Col(params VisualElement[] kids) => Div("col").Kids(kids);
        public static VisualElement Space(float h) { var e = new VisualElement(); e.style.height = h; e.style.flexShrink = 0; return e; }
        public static VisualElement Spring() => Div().Grow();

        public static Label T(string text, string cls = null, Font f = null)
        {
            var l = new Label(Fonts.EmojiOk ? text ?? "" : NoEmoji(text)) { enableRichText = true };
            l.Cls("t", cls); Fonts.Use(l, f ?? Fonts.Body); return l;
        }
        public static Label H1(string t) => T(t, "h1", Fonts.Disp);
        public static Label H2(string t) => T(t, "h2", Fonts.Disp);
        public static Label H3(string t) => T(t, "h3", Fonts.Semi);
        public static Label Kicker(string t) => T((t ?? "").ToUpperInvariant(), "kicker", Fonts.Bold);
        public static Label P(string t) => T(t, "p");
        public static Label Hint(string t) => T(t, "hint");
        public static Label Num(string t, string cls = null) => T(t, "num " + (cls ?? ""), Fonts.MonoB);
        public static Label Sym(string t, string cls = null) => T(t, cls, Fonts.Sym);

        public static VisualElement Card(params string[] cls) => Div("card").Cls(cls);

        public static T Named<T>(this T e, string n) where T : VisualElement { e.name = n; return e; }

        // A tap that survives scroll views: it fires only if the pointer barely moved between down and up.
        public static T Tap<T>(this T e, Action a) where T : VisualElement
        {
            Vector2 start = default; bool down = false; int pid = -1;
            e.RegisterCallback<PointerDownEvent>(ev => { if (ev.button > 0) return; down = true; pid = ev.pointerId; start = ev.position; e.AddToClassList("down"); });
            e.RegisterCallback<PointerUpEvent>(ev =>
            {
                if (!down || ev.pointerId != pid) return; down = false; e.RemoveFromClassList("down");
                if (((Vector2)ev.position - start).sqrMagnitude > 196 || e.ClassListContains("off")) return;
                ev.StopPropagation();
                try { a?.Invoke(); } catch (Exception x) { Debug.LogException(x); }
            });
            e.RegisterCallback<PointerLeaveEvent>(ev => e.RemoveFromClassList("down"));
            e.RegisterCallback<PointerCancelEvent>(ev => { down = false; e.RemoveFromClassList("down"); });
            return e;
        }

        // kinds: "gold" (main action), "ghost" (outline), "" (dark), plus "small", "wide"
        public static VisualElement Btn(string text, Action a, string kind = "", bool enabled = true, string sub = null)
        {
            var b = Div("btn").Cls(kind);
            b.Add(T(text, "btn-t", Fonts.Semi).NoPick());
            if (!string.IsNullOrEmpty(sub)) b.Add(T(sub, "btn-sub").NoPick());
            if (!enabled) b.AddToClassList("off");
            return b.Tap(a);
        }

        public static VisualElement Bar(double frac, Color fill, float h = 6, Color? back = null)
        {
            var bar = Div("bar"); bar.style.height = h; bar.Radius(h / 2); if (back.HasValue) bar.style.backgroundColor = back.Value;
            var f = Div("bar-fill").Bg(fill).Radius(h / 2); f.style.width = Length.Percent((float)(Math.Max(0, Math.Min(1, frac)) * 100)); bar.Add(f);
            return bar;
        }
        public static VisualElement Chip(string text, Color c, bool solid = false)
        {
            var ch = Div("chip").Bg(solid ? c : C.A(c, 0.14f));
            ch.Add(T(text, "chip-t", Fonts.Semi).Fg(solid ? C.Ink : c).NoPick());
            return ch;
        }
        public static VisualElement Stat(string value, string label, Color? c = null)
        {
            var s = Col().Cls("stat");
            s.Add(Num(value, "stat-v").Fg(c ?? C.Cream)); s.Add(T(label, "stat-l"));
            return s;
        }
        public static VisualElement Divider() => Div("divider");

        // emoji live outside the basic plane (surrogate pairs) plus a few joiners; card suits and ★ ✓ are kept
        public static string NoEmoji(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? "";
            var sb = new System.Text.StringBuilder(s.Length); bool cut = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsHighSurrogate(c) && i + 1 < s.Length && char.IsLowSurrogate(s[i + 1])) { i++; cut = true; continue; }
                if (c == '\uFE0F' || c == '\u200D' || c == '\u20E3') { cut = true; continue; }
                sb.Append(c);
            }
            if (!cut) return s;
            var r = sb.ToString().Trim();
            while (r.Contains("  ")) r = r.Replace("  ", " ");
            return r;
        }

        public static string Money(double n) => (n < 0 ? "−$" : "$") + Fmt.Money(Math.Abs(n));
        public static string Signed(double n) => (n < 0 ? "−$" : "+$") + Fmt.Money(Math.Abs(n));
        public static string Plural(int n, string one, string many = null) => $"{n} {(n == 1 ? one : many ?? one + "s")}";
        public static string Esc(string s) => (s ?? "").Replace("<", "‹").Replace(">", "›");
    }
}
