// Pieces the new screens share: the spotlight stage your character stands on (the 3D body when one is installed,
// the 2D bust until then), the level ring, round icon buttons, picture tiles and the fit chip.
using System;
using System.Collections.Generic;
using System.Linq;
using CFDream.Character;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public static class StageView
    {
        // A lit stage `height` tall. framing: "full", "bust" or "face". refresh redraws after a live change (sliders).
        public static VisualElement Make(Fig fig, Func<CharacterSpec> spec, float height, string framing, out Action refresh)
        {
            var st = U.Div("stage"); if (height > 0) st.style.height = height; else st.style.flexGrow = 1;
            var glow = new VisualElement().Cls("stage-glow").NoPick();
            glow.generateVisualContent += mgc =>
            {
                var r = glow.contentRect; var p = mgc.painter2D; if (r.width < 2) return;
                var c = new Vector2(r.center.x, r.height * 0.44f);
                for (int i = 0; i < 28; i++)
                {
                    float t = i / 27f; p.fillColor = C.A(C.Mix(C.Gold3, C.Card2, 0.35f), 0.05f * (1 - t) + 0.004f);
                    p.BeginPath(); Ellipse(p, c, r.width * (0.18f + 0.5f * t), r.height * (0.2f + 0.45f * t)); p.Fill();
                }
                // the floor the character stands on
                p.fillColor = C.A(Color.black, 0.35f); p.BeginPath(); Ellipse(p, new Vector2(r.center.x, r.height * 0.93f), r.width * 0.2f, r.height * 0.035f); p.Fill();
            };
            st.Add(glow);

            float h = height > 0 ? height : 320;
            float portraitSize = framing == "face" ? h * 0.86f : h * 0.74f;
            var portrait = new Portrait(fig, portraitSize, false);
            var holder = U.Div("stage-figure").NoPick();
            VisualElement view;
            if (Bodies.Available)
            {
                var cv = new CharacterView(spec(), portrait, h * 0.8f, h, framing);
                cv.pickingMode = PickingMode.Position;
                view = cv;
                holder.pickingMode = PickingMode.Ignore;
                st.Add(Hint("Drag to turn"));
            }
            else view = portrait;
            holder.Add(view);
            st.Add(holder);
            // a stage that fills its column: size the view to the stage once it's laid out (4:5, like the render)
            if (height <= 0 && view is CharacterView)
                st.RegisterCallback<GeometryChangedEvent>(e =>
                {
                    float hh = st.layout.height; if (hh < 40 || Mathf.Abs(view.resolvedStyle.height - hh) < 1) return;
                    view.style.height = hh; view.style.width = Mathf.Min(hh * 0.8f, st.layout.width);
                });
            refresh = () =>
            {
                portrait.MarkDirtyRepaint();
                if (Bodies.Available) CharacterStage.Instance?.Show(spec());
            };
            return st;
        }
        static VisualElement Hint(string text)
        {
            var r = U.Row().Cls("stage-hint").NoPick();
            r.Add(new Icon("rotate", 15, C.Mute)); r.Add(U.T(text, "stage-hint-t").NoPick());
            return r;
        }
        static void Ellipse(Painter2D p, Vector2 c, float rx, float ry)
        {
            const float m = 0.5523f;
            p.MoveTo(c + new Vector2(rx, 0));
            p.BezierCurveTo(c + new Vector2(rx, ry * m), c + new Vector2(rx * m, ry), c + new Vector2(0, ry));
            p.BezierCurveTo(c + new Vector2(-rx * m, ry), c + new Vector2(-rx, ry * m), c + new Vector2(-rx, 0));
            p.BezierCurveTo(c + new Vector2(-rx, -ry * m), c + new Vector2(-rx * m, -ry), c + new Vector2(0, -ry));
            p.BezierCurveTo(c + new Vector2(rx * m, -ry), c + new Vector2(rx, -ry * m), c + new Vector2(rx, 0));
            p.ClosePath();
        }
    }

    public static class Bits
    {
        // career level as a ring that fills towards the next level
        public static VisualElement LevelRing(int level, double frac, float size = 44)
        {
            var e = U.Div("lvl-ring"); e.style.width = size; e.style.height = size;
            e.generateVisualContent += mgc =>
            {
                var r = e.contentRect; var p = mgc.painter2D; var c = r.center; float rad = Mathf.Min(r.width, r.height) / 2 - 2.5f;
                p.lineWidth = 3; p.lineCap = LineCap.Round;
                p.strokeColor = C.Card2; p.BeginPath(); p.Arc(c, rad, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
                float f = Mathf.Clamp01((float)frac);
                if (f > 0.01f) { p.strokeColor = C.Gold; p.BeginPath(); p.Arc(c, rad, Angle.Degrees(-90), Angle.Degrees(-90 + 360 * f)); p.Stroke(); }
            };
            var n = U.Num(level.ToString(), "lvl-n").NoPick(); e.Add(n);
            return e;
        }

        public static VisualElement IconBtn(string icon, Action a, string label = null, string cls = null)
        {
            var b = U.Div("icon-btn").Cls(cls);
            b.Add(new Icon(icon, 20, C.Gold));
            if (!string.IsNullOrEmpty(label)) b.tooltip = label;
            return b.Tap(a);
        }

        public static VisualElement FitChip(State s, Action tap = null)
        {
            var fs = Fashion.FitScore(s);
            var c = U.Row().Cls("fit-chip");
            c.Add(new Icon("closet", 16, C.Gold));
            c.Add(U.Num(fs.score.ToString(), "fit-chip-n").NoPick());
            c.Add(U.T(fs.name, "fit-chip-t", Fonts.Medium).NoPick());
            if (tap != null) c.Tap(tap);
            return c;
        }

        // A picture tile for the creator and the closet. `art` is the picture; `sub` an optional second line.
        public static VisualElement Tile(VisualElement art, string label, bool on, Action tap, string sub = null, Color? subC = null, string cls = null)
        {
            var t = U.Div("tile").Cls(on ? "on" : "", cls);
            var pic = U.Div("tile-pic").NoPick(); pic.Add(art); t.Add(pic);
            t.Add(U.T(label, "tile-t", Fonts.Semi).NoPick());
            if (!string.IsNullOrEmpty(sub)) { var sl = U.T(sub, "tile-sub", Fonts.Medium).NoPick(); if (subC.HasValue) sl.style.color = subC.Value; t.Add(sl); }
            if (on) { var ck = U.Div("tile-check").NoPick(); ck.Add(new Icon("check", 11, C.Ink, 3)); t.Add(ck); }
            if (tap != null) t.Tap(tap);
            return t;
        }
        // tiles sit three to a row; pad the last row so they keep their width
        public static VisualElement Tiles(IEnumerable<VisualElement> tiles, int perRow = 3)
        {
            var g = U.Div("tiles"); int n = 0;
            foreach (var t in tiles) { g.Add(t); n++; }
            while (n % perRow != 0) { g.Add(U.Div("tile", "ghost-tile").NoPick()); n++; }
            return g;
        }

        public static VisualElement Segmented(IList<(string id, string label)> opts, string cur, Action<string> pick)
        {
            var r = U.Row().Cls("seg");
            foreach (var (id, label) in opts)
            {
                var b = U.Div("seg-b").Cls(id == cur ? "on" : "");
                b.Add(U.T(label, "seg-t", Fonts.Semi).NoPick());
                string k = id; b.Tap(() => pick(k));
                r.Add(b);
            }
            return r;
        }

        public static VisualElement PageHead(string title, State s, params VisualElement[] right)
        {
            // landscape panes are narrow: title on its own line, the controls under it
            if (DreamApp.Landscape)
            {
                var col = U.Col().Cls("page-head", "stacked");
                col.Add(U.T(title, "page-title", Fonts.Disp));
                var tools = U.Row().Cls("page-tools");
                foreach (var x in right) if (x != null) tools.Add(x);
                col.Add(tools);
                return col;
            }
            var r = U.Row().Cls("page-head");
            r.Add(U.T(title, "page-title", Fonts.Disp).Grow());
            foreach (var x in right) if (x != null) r.Add(x);
            return r;
        }
    }
}
