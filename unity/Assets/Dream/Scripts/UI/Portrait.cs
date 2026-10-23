// A 2D stand-in for the character until the 3D one lands: a vector bust drawn from the same look and gear the
// 3D model will use (skin, hair, facial hair, outfit colourway, eyewear, headwear, chain, earrings, patch).
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public class Fig
    {
        public Look look = new Look();
        public Dictionary<string, string> eq = new Dictionary<string, string>();
        public Dictionary<string, int> dye = new Dictionary<string, int>(), fin = new Dictionary<string, int>();
        public string E(string slot) => eq.TryGetValue(slot, out var v) && !string.IsNullOrEmpty(v) ? v : "none";

        public static Fig Of(State s) => new Fig { look = s.look, eq = s.eq, dye = s.dye, fin = s.fin };
        public static Fig Of(Look l, Dictionary<string, string> eq) => new Fig { look = l, eq = eq ?? new Dictionary<string, string>() };
        // people and legends from the story data
        public static Fig Person(Dictionary<string, object> p)
        {
            var f = new Fig();
            if (p == null) return f;
            if (J.Has(p, "look")) f.look = State.LookFrom(J.O(p, "look"));
            var eq = J.O(p, "eq"); if (eq != null) foreach (var kv in eq) if (kv.Value is string s) f.eq[kv.Key] = s;
            return f;
        }
        public static Fig Slick() => new Fig { look = new Look { skin = 3, hair = "slick", hairC = 0, facial = "stubble", eyeC = 1 }, eq = new Dictionary<string, string> { { "outfit", "leather" }, { "eyes", "shades" }, { "chain", "silver" } } };
        public static Fig For(string who)
        {
            if (who == "slick") return Slick();
            var p = DB.Person(who); if (p != null) return Person(p);
            var lg = DB.Legend(who);
            if (lg != null)
            {
                int i = DB.List("DR_LEGENDS").IndexOf(lg); var looks = DB.List("legendLooks");
                if (looks != null && i >= 0 && i < looks.Count) return Person(looks[i] as Dictionary<string, object>);
            }
            return null;
        }
    }

    public class Portrait : VisualElement
    {
        static readonly Dictionary<string, string> OutfitBase = new Dictionary<string, string> { { "office", "#cdd8e6" }, { "hoodie", "#5f636b" }, { "tee", "#1c1c20" }, { "varsity", "#1f3566" }, { "bomber", "#2a2d22" }, { "track", "#16161a" }, { "cardigan", "#6a2232" }, { "vest", "#c9b98a" }, { "denim", "#4a6fa0" }, { "leather", "#1a1716" }, { "fleece", "#7d8590" }, { "jersey", "#b3262c" }, { "polo", "#1f2a44" }, { "blazer", "#3b3f47" }, { "suit", "#1b2442" }, { "designer", "#4a1426" }, { "tux", "#121214" }, { "vampire", "#141216" }, { "flannel", "#a3262a" }, { "ugly", "#1d6b3a" } };
        static readonly Dictionary<string, string> HatBase = new Dictionary<string, string> { { "cap", "#17171a" }, { "snap", "#24427e" }, { "beanie", "#b3262c" }, { "bucket", "#d8cfb4" }, { "cowboy", "#8a5a32" }, { "fedora", "#2a2522" }, { "visor", "#1e8f4a" } };
        static readonly HashSet<string> Tailored = new HashSet<string> { "suit", "blazer", "tux", "designer", "vampire", "vest" };

        readonly Fig fig; readonly bool ring;
        public Portrait(Fig f, float size, bool ring = true)
        {
            fig = f ?? new Fig(); this.ring = ring;
            style.width = size; style.height = size * 1.1f; style.flexShrink = 0;
            generateVisualContent += Draw;
        }

        static Color Pal(string key, int i, string fb)
        {
            var l = DB.List(key); if (l == null || l.Count == 0) return C.H(fb);
            return C.H(l[Math.Max(0, Math.Min(l.Count - 1, i))] as string, C.H(fb));
        }
        Color Metal(string slot, string id)
        {
            string k = slot + ":" + id;
            if (fig.fin.TryGetValue(k, out var f) && f >= 0 && f < Fashion.Finishes.Length) return C.H(Fashion.Finishes[f].c);
            return id == "silver" || id == "iced" || id == "diamond" ? C.H("#dfe3ea") : C.H("#e3b94f");
        }
        double Sc(string k) => fig.look.sc.TryGetValue(k, out var v) ? v : 0;

        // ------------------------------------------------------------ drawing in a 100 x 110 box
        Vector2 o; float k;
        Vector2 P(float x, float y) => new Vector2(o.x + x * k, o.y + y * k);
        void Ell(Painter2D p, float cx, float cy, float rx, float ry)
        {
            const float m = 0.5523f;
            p.MoveTo(P(cx + rx, cy));
            p.BezierCurveTo(P(cx + rx, cy + ry * m), P(cx + rx * m, cy + ry), P(cx, cy + ry));
            p.BezierCurveTo(P(cx - rx * m, cy + ry), P(cx - rx, cy + ry * m), P(cx - rx, cy));
            p.BezierCurveTo(P(cx - rx, cy - ry * m), P(cx - rx * m, cy - ry), P(cx, cy - ry));
            p.BezierCurveTo(P(cx + rx * m, cy - ry), P(cx + rx, cy - ry * m), P(cx + rx, cy));
            p.ClosePath();
        }
        void FillEll(Painter2D p, Color c, float cx, float cy, float rx, float ry) { p.fillColor = c; p.BeginPath(); Ell(p, cx, cy, rx, ry); p.Fill(); }
        void Poly(Painter2D p, Color c, params float[] xy)
        {
            p.fillColor = c; p.BeginPath(); p.MoveTo(P(xy[0], xy[1]));
            for (int i = 2; i + 1 < xy.Length; i += 2) p.LineTo(P(xy[i], xy[i + 1]));
            p.ClosePath(); p.Fill();
        }
        void Line(Painter2D p, Color c, float w, params float[] xy)
        {
            p.strokeColor = c; p.lineWidth = w * k; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round; p.BeginPath(); p.MoveTo(P(xy[0], xy[1]));
            for (int i = 2; i + 1 < xy.Length; i += 2) p.LineTo(P(xy[i], xy[i + 1]));
            p.Stroke();
        }
        void Curve(Painter2D p, Color c, float w, float x0, float y0, float cx, float cy, float x1, float y1)
        {
            p.strokeColor = c; p.lineWidth = w * k; p.lineCap = LineCap.Round; p.BeginPath(); p.MoveTo(P(x0, y0)); p.QuadraticCurveTo(P(cx, cy), P(x1, y1)); p.Stroke();
        }

        void Draw(MeshGenerationContext mgc)
        {
            var r = contentRect; if (r.width < 4 || r.height < 4) return;
            k = Mathf.Min(r.width / 100f, r.height / 110f); o = new Vector2(r.x + (r.width - 100 * k) / 2, r.y + (r.height - 110 * k) / 2);
            var p = mgc.painter2D; var L = fig.look;
            Color skin = Pal("DR_SKIN", L.skin, "#e9b48a"), hairC = Pal("DR_HAIRC", L.hairC, "#3a2416"), eyeC = Pal("DR_EYEC", L.eyeC, "#4a2f1c");
            Color shade = Color.Lerp(skin, Color.black, 0.18f), line = Color.Lerp(skin, Color.black, 0.45f);
            string outfit = fig.E("outfit"); if (outfit == "none") outfit = "office";
            Color oc = fig.dye.TryGetValue(outfit, out var di) && di > 0 && di < Fashion.Dyes.Length && Fashion.Dyes[di] != null ? C.H(Fashion.Dyes[di]) : C.H(OutfitBase.TryGetValue(outfit, out var ob) ? ob : "#3b3f47");
            string hair = L.hair ?? "short", hat = fig.E("head");
            float fw = 17 + (float)Sc("fw") * 2 + (L.face == "round" ? 1.5f : L.face == "angular" ? -0.5f : 0), fh = 21, cx = 50, cy = 50;
            float jaw = L.face == "angular" ? 0.82f : L.face == "round" ? 1f : 0.9f;

            // backdrop
            if (ring)
            {
                FillEll(p, C.A(C.Gold, 0.08f), 50, 55, 49, 52);
                p.strokeColor = C.A(C.Gold, 0.35f); p.lineWidth = 1.2f * k; p.BeginPath(); Ell(p, 50, 55, 48.5f, 51.5f); p.Stroke();
            }
            // long hair falls behind the shoulders
            if (hair == "long" || hair == "locs" || hair == "twists" || hair == "mullet")
            {
                float dn = hair == "mullet" ? 74 : 88;
                Poly(p, Color.Lerp(hairC, Color.black, 0.15f), cx - fw - 3, 40, cx + fw + 3, 40, cx + fw + 5, dn, cx - fw - 5, dn);
            }
            if (hair == "afro") FillEll(p, hairC, cx, cy - 8, fw + 12, fh + 6);
            if (hair == "curly") FillEll(p, hairC, cx, cy - 12, fw + 5, 15);
            if (hair == "bun") FillEll(p, hairC, cx, cy - fh - 6, 6, 5);

            // torso
            p.fillColor = oc; p.BeginPath();
            p.MoveTo(P(4, 110)); p.BezierCurveTo(P(6, 96), P(16, 88), P(36, 84)); p.LineTo(P(64, 84)); p.BezierCurveTo(P(84, 88), P(94, 96), P(96, 110)); p.ClosePath(); p.Fill();
            Color oShade = Color.Lerp(oc, Color.black, 0.25f);
            if (Tailored.Contains(outfit))
            {
                Poly(p, outfit == "vest" ? C.H("#e9e6df") : C.H("#efece6"), 42, 84, 58, 84, 50, 104);
                if (outfit != "vest") { Poly(p, C.H(outfit == "tux" ? "#0a0a0a" : "#7a1f2b"), 48.5f, 86, 51.5f, 86, 52, 100, 50, 103, 48, 100); }
                Line(p, oShade, 1.6f, 42, 84, 50, 106); Line(p, oShade, 1.6f, 58, 84, 50, 106);
            }
            else if (outfit == "office" || outfit == "polo") { Poly(p, Color.Lerp(oc, Color.white, 0.25f), 41, 84, 50, 92, 59, 84, 56, 82, 50, 88, 44, 82); }
            else if (outfit == "hoodie" || outfit == "fleece") { Curve(p, oShade, 3, 36, 86, 50, 98, 64, 86); Line(p, C.A(Color.white, 0.7f), 0.8f, 46, 92, 45, 102); Line(p, C.A(Color.white, 0.7f), 0.8f, 54, 92, 55, 102); }
            else if (outfit == "jersey" || outfit == "varsity" || outfit == "track") { Curve(p, C.A(Color.white, 0.85f), 2, 40, 85, 50, 93, 60, 85); }
            else if (outfit == "flannel" || outfit == "ugly") { for (int i = 0; i < 5; i++) Line(p, C.A(Color.black, 0.18f), 1.2f, 14 + i * 18, 92, 14 + i * 18, 110); Line(p, C.A(Color.black, 0.18f), 1.2f, 6, 100, 94, 100); }
            else Curve(p, oShade, 2, 40, 85, 50, 91, 60, 85);

            // patch on the chest
            string patch = fig.E("patch");
            if (patch != "none")
            {
                var pd = J.O(DB.Obj("DR_PATCH"), patch); var pc = C.H(J.S(pd, "c", "#d9b36c"));
                p.fillColor = pc; p.BeginPath(); p.MoveTo(P(62, 95)); p.LineTo(P(74, 95)); p.LineTo(P(74, 102)); p.LineTo(P(62, 102)); p.ClosePath(); p.Fill();
            }

            // neck and head
            Poly(p, shade, cx - 6.5f, 66, cx + 6.5f, 66, cx + 7, 86, cx - 7, 86);
            float er = 3.2f + (float)Sc("er") * 0.8f;
            FillEll(p, shade, cx - fw + 0.5f, cy + 3, er, er + 1.5f); FillEll(p, shade, cx + fw - 0.5f, cy + 3, er, er + 1.5f);
            p.fillColor = skin; p.BeginPath();
            p.MoveTo(P(cx - fw, cy - 4));
            p.BezierCurveTo(P(cx - fw, cy - fh - 2), P(cx + fw, cy - fh - 2), P(cx + fw, cy - 4));
            p.BezierCurveTo(P(cx + fw, cy + 10), P(cx + fw * jaw, cy + fh - 2), P(cx, cy + fh + 1 + (float)Sc("ch") * 1.5f));
            p.BezierCurveTo(P(cx - fw * jaw, cy + fh - 2), P(cx - fw, cy + 10), P(cx - fw, cy - 4));
            p.ClosePath(); p.Fill();
            if (L.age == "old") { Line(p, C.A(line, 0.35f), 0.7f, cx - 8, cy - 12, cx - 2, cy - 12.5f); Line(p, C.A(line, 0.35f), 0.7f, cx + 2, cy - 12.5f, cx + 8, cy - 12); }

            // facial hair under the features
            string fac = L.facial ?? "none";
            if (fac == "stubble") { p.fillColor = C.A(hairC, 0.28f); p.BeginPath(); p.MoveTo(P(cx - fw + 1, cy + 4)); p.BezierCurveTo(P(cx - fw + 2, cy + 16), P(cx - 6, cy + fh + 1), P(cx, cy + fh + 1)); p.BezierCurveTo(P(cx + 6, cy + fh + 1), P(cx + fw - 2, cy + 16), P(cx + fw - 1, cy + 4)); p.LineTo(P(cx + 9, cy + 9)); p.LineTo(P(cx - 9, cy + 9)); p.ClosePath(); p.Fill(); }
            if (fac == "beard") { p.fillColor = Color.Lerp(hairC, skin, 0.08f); p.BeginPath(); p.MoveTo(P(cx - fw + 0.5f, cy + 2)); p.BezierCurveTo(P(cx - fw + 1, cy + 18), P(cx - 7, cy + fh + 4), P(cx, cy + fh + 4)); p.BezierCurveTo(P(cx + 7, cy + fh + 4), P(cx + fw - 1, cy + 18), P(cx + fw - 0.5f, cy + 2)); p.LineTo(P(cx + 8, cy + 10)); p.LineTo(P(cx - 8, cy + 10)); p.ClosePath(); p.Fill(); }
            if (fac == "goatee") FillEll(p, hairC, cx, cy + fh - 2, 4.5f, 4);
            if (fac == "chin") { p.strokeColor = hairC; p.lineWidth = 2.2f * k; p.BeginPath(); p.MoveTo(P(cx - fw + 0.5f, cy - 2)); p.BezierCurveTo(P(cx - fw + 1, cy + 14), P(cx - 7, cy + fh), P(cx, cy + fh)); p.BezierCurveTo(P(cx + 7, cy + fh), P(cx + fw - 1, cy + 14), P(cx + fw - 0.5f, cy - 2)); p.Stroke(); }
            if (fac == "mustache" || fac == "goatee" || fac == "beard") Curve(p, hairC, 1.8f, cx - 6, cy + 10.5f, cx, cy + 8.5f, cx + 6, cy + 10.5f);

            // eyes, brows, nose, mouth
            float es = 1 + (float)Sc("es") * 0.18f, sp = 7 + (float)Sc("ep") * 1.2f, ey = cy - 1;
            FillEll(p, Color.white, cx - sp, ey, 3.2f * es, 2.1f * es); FillEll(p, Color.white, cx + sp, ey, 3.2f * es, 2.1f * es);
            FillEll(p, eyeC, cx - sp, ey, 1.7f * es, 1.7f * es); FillEll(p, eyeC, cx + sp, ey, 1.7f * es, 1.7f * es);
            FillEll(p, Color.black, cx - sp, ey, 0.8f, 0.8f); FillEll(p, Color.black, cx + sp, ey, 0.8f, 0.8f);
            float bh = ey - 5.2f - (float)Sc("bh") * 1.2f, bw = L.brows == "thick" ? 2.2f : L.brows == "slit" ? 1.1f : 1.5f;
            if (L.brows == "arched") { Curve(p, hairC, bw, cx - sp - 4, bh + 1, cx - sp, bh - 2, cx - sp + 4, bh + 0.5f); Curve(p, hairC, bw, cx + sp - 4, bh + 0.5f, cx + sp, bh - 2, cx + sp + 4, bh + 1); }
            else { Line(p, hairC, bw, cx - sp - 4, bh + 0.8f, cx - sp + 4, bh); Line(p, hairC, bw, cx + sp - 4, bh, cx + sp + 4, bh + 0.8f); if (L.brows == "slit") Line(p, skin, 0.9f, cx + sp + 1, bh - 1, cx + sp + 2, bh + 1.5f); }
            Curve(p, line, 1.1f, cx - 0.5f, cy + 1, cx - 2.5f - (float)Sc("nw"), cy + 6 + (float)Sc("nl"), cx + 1.5f, cy + 6.5f + (float)Sc("nl"));
            float mw = 5 + (float)Sc("mw") * 1.2f, lp = 1.2f + (float)Sc("lp") * 0.5f;
            Curve(p, Color.Lerp(skin, C.H("#8a3b3b"), 0.55f), lp, cx - mw, cy + 13, cx, cy + 14.5f, cx + mw, cy + 13);
            if (L.freck) for (int i = 0; i < 6; i++) FillEll(p, C.A(line, 0.35f), cx + (i % 2 == 0 ? -1 : 1) * (7 + i), cy + 5 + (i % 3), 0.6f, 0.6f);

            // hair on top
            bool hatOn = hat != "none" && hat != "phones";
            if (hair != "bald" && !(hatOn && hair != "long" && hair != "afro"))
            {
                Color hc = hairC;
                switch (hair)
                {
                    case "buzz": HairCap(p, C.A(hc, 0.55f), cx, cy, fw, fh, 2, false); break;
                    case "mohawk": Poly(p, hc, cx - 3.5f, cy - fh - 7, cx + 3.5f, cy - fh - 7, cx + 3, cy - 6, cx - 3, cy - 6); HairCap(p, C.A(hc, 0.3f), cx, cy, fw, fh, 1, false); break;
                    case "cornrows": HairCap(p, hc, cx, cy, fw, fh, 2.5f, false); for (int i = -2; i <= 2; i++) Line(p, C.A(skin, 0.5f), 0.6f, cx + i * 4.5f, cy - fh, cx + i * 5.5f, cy - 6); break;
                    case "fade": case "waves": HairCap(p, hc, cx, cy, fw, fh, 3.5f, false); break;
                    case "quiff": HairCap(p, hc, cx, cy, fw, fh, 4, true); FillEll(p, hc, cx - 3, cy - fh - 2, 9, 5); break;
                    case "curtains": HairCap(p, hc, cx, cy, fw, fh, 5, true); Poly(p, hc, cx - 1, cy - fh + 2, cx - 12, cy - 6, cx - 15, cy - 10); Poly(p, hc, cx + 1, cy - fh + 2, cx + 12, cy - 6, cx + 15, cy - 10); break;
                    case "slick": HairCap(p, hc, cx, cy, fw, fh, 4.5f, false); for (int i = -1; i <= 1; i++) Line(p, C.A(Color.white, 0.12f), 0.6f, cx + i * 6, cy - fh + 1, cx + i * 7, cy - 10); break;
                    case "afro": case "curly": FillEll(p, hc, cx, cy - fh + 3, fw + 2, 9); break;
                    default: HairCap(p, hc, cx, cy, fw, fh, 4.5f, true); break;
                }
                if (hair == "ponytail" || hair == "bun") HairCap(p, hc, cx, cy, fw, fh, 4, false);
            }

            // ears: earrings
            string ear = fig.E("ear");
            if (ear != "none") { var mc = Metal("ear", ear); if (ear == "hoop") { p.strokeColor = mc; p.lineWidth = 1.1f * k; p.BeginPath(); Ell(p, cx - fw + 0.5f, cy + 8, 2.2f, 2.6f); p.Stroke(); } else FillEll(p, ear == "diamond" ? C.H("#eaf6ff") : mc, cx - fw + 0.5f, cy + 6.5f, 1.3f, 1.3f); }

            // eyewear
            string eyes = fig.E("eyes");
            if (eyes != "none")
            {
                bool dark = eyes == "shades" || eyes == "aviator" || eyes == "tinted" || eyes == "sport";
                Color fr = eyes == "aviator" ? C.H("#d8c27a") : eyes == "readers" ? C.H("#6b5a45") : C.H("#151515"), lens = eyes == "tinted" ? C.A(C.H("#b65a2a"), 0.55f) : C.A(C.H("#0d0d10"), dark ? 0.85f : 0.08f);
                float rw = eyes == "round" ? 4.2f : eyes == "sport" ? 5.4f : 4.8f, rh = eyes == "round" ? 4.2f : eyes == "aviator" ? 4.2f : 3.4f;
                foreach (var sx in new[] { -sp, sp })
                {
                    p.fillColor = lens; p.BeginPath(); Ell(p, cx + sx, ey + 0.5f, rw, rh); p.Fill();
                    p.strokeColor = fr; p.lineWidth = (eyes == "thick" ? 1.6f : 0.9f) * k; p.BeginPath(); Ell(p, cx + sx, ey + 0.5f, rw, rh); p.Stroke();
                }
                Line(p, fr, 0.9f, cx - sp + rw, ey, cx + sp - rw, ey);
                Line(p, fr, 0.8f, cx - sp - rw, ey, cx - fw, ey - 1); Line(p, fr, 0.8f, cx + sp + rw, ey, cx + fw, ey - 1);
            }

            // headwear
            if (hat != "none")
            {
                Color hc = fig.dye.TryGetValue(hat, out var hd) && hd > 0 && hd < Fashion.Dyes.Length && Fashion.Dyes[hd] != null ? C.H(Fashion.Dyes[hd]) : C.H(HatBase.TryGetValue(hat, out var hb) ? hb : "#222222");
                Color hs = Color.Lerp(hc, Color.black, 0.3f);
                switch (hat)
                {
                    case "cap": case "snap":
                        p.fillColor = hc; p.BeginPath(); p.MoveTo(P(cx - fw - 1.5f, cy - 7)); p.BezierCurveTo(P(cx - fw, cy - fh - 9), P(cx + fw, cy - fh - 9), P(cx + fw + 1.5f, cy - 7)); p.ClosePath(); p.Fill();
                        Poly(p, hs, cx - fw - 1, cy - 8, cx + fw + 9, cy - 8, cx + fw + 8, cy - 5, cx - fw - 1, cy - 5); break;
                    case "beanie":
                        p.fillColor = hc; p.BeginPath(); p.MoveTo(P(cx - fw - 1.5f, cy - 5)); p.BezierCurveTo(P(cx - fw - 1, cy - fh - 12), P(cx + fw + 1, cy - fh - 12), P(cx + fw + 1.5f, cy - 5)); p.ClosePath(); p.Fill();
                        Poly(p, hs, cx - fw - 2, cy - 10, cx + fw + 2, cy - 10, cx + fw + 2, cy - 4, cx - fw - 2, cy - 4); break;
                    case "bucket":
                        Poly(p, hc, cx - fw + 1, cy - 9, cx - fw + 4, cy - fh - 6, cx + fw - 4, cy - fh - 6, cx + fw - 1, cy - 9);
                        Poly(p, hs, cx - fw - 6, cy - 5, cx - fw + 1, cy - 10, cx + fw - 1, cy - 10, cx + fw + 6, cy - 5); break;
                    case "cowboy": case "fedora":
                        Poly(p, hc, cx - fw + 2, cy - 11, cx - fw + 4, cy - fh - 9, cx, cy - fh - 6, cx + fw - 4, cy - fh - 9, cx + fw - 2, cy - 11);
                        Poly(p, hs, cx - fw + 2, cy - 14, cx + fw - 2, cy - 14, cx + fw - 2, cy - 11, cx - fw + 2, cy - 11);
                        FillEll(p, hc, cx, cy - 10, fw + (hat == "cowboy" ? 13 : 8), 2.6f); break;
                    case "visor":
                        Poly(p, hc, cx - fw - 1, cy - 12, cx + fw + 1, cy - 12, cx + fw + 1, cy - 9, cx - fw - 1, cy - 9);
                        Poly(p, hs, cx - fw, cy - 10, cx + fw + 10, cy - 10, cx + fw + 8, cy - 7, cx - fw, cy - 7); break;
                    case "phones":
                        p.strokeColor = C.H("#202024"); p.lineWidth = 2.6f * k; p.BeginPath(); p.MoveTo(P(cx - fw - 1, cy)); p.BezierCurveTo(P(cx - fw - 2, cy - fh - 10), P(cx + fw + 2, cy - fh - 10), P(cx + fw + 1, cy)); p.Stroke();
                        FillEll(p, C.H("#202024"), cx - fw - 1, cy + 2, 3.5f, 5); FillEll(p, C.H("#202024"), cx + fw + 1, cy + 2, 3.5f, 5); break;
                }
            }

            // chain
            string chain = fig.E("chain");
            if (chain != "none")
            {
                var mc = Metal("chain", chain); float w = chain == "cuban" ? 2.4f : chain == "rope" ? 1.8f : chain == "iced" ? 2.2f : 1.2f;
                Curve(p, mc, w, cx - 9, 84, cx, 100, cx + 9, 84);
                if (chain == "layered") Curve(p, mc, 1f, cx - 11, 85, cx, 106, cx + 11, 85);
                if (chain == "iced") { FillEll(p, Color.white, cx, 92.5f, 1.4f, 1.4f); FillEll(p, C.H("#eaf6ff"), cx - 4, 91, 0.9f, 0.9f); FillEll(p, C.H("#eaf6ff"), cx + 4, 91, 0.9f, 0.9f); }
            }
        }

        void HairCap(Painter2D p, Color c, float cx, float cy, float fw, float fh, float thick, bool fringe)
        {
            p.fillColor = c; p.BeginPath();
            p.MoveTo(P(cx - fw - 0.8f, cy - 2));
            p.BezierCurveTo(P(cx - fw - 2, cy - fh - thick - 2), P(cx + fw + 2, cy - fh - thick - 2), P(cx + fw + 0.8f, cy - 2));
            p.LineTo(P(cx + fw - 1.5f, cy - 6));
            if (fringe) { p.BezierCurveTo(P(cx + fw - 6, cy - fh + 3), P(cx - 2, cy - fh + 6), P(cx - fw + 3, cy - 9)); }
            else p.BezierCurveTo(P(cx + fw - 4, cy - fh + 1), P(cx - fw + 4, cy - fh + 1), P(cx - fw + 1.5f, cy - 6));
            p.LineTo(P(cx - fw + 1.5f, cy - 6));
            p.ClosePath(); p.Fill();
        }
    }
}
