// Cards and the table: hero cards with four-colour suits, a felt oval drawn with Painter2D, and the seats around
// it (position, stack, the action they took, chips in front), turned so you always sit at the bottom.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public static class CardView
    {
        static readonly Color[] SuitCol = { C.H("#1f8a42"), C.H("#1f6fd1"), C.H("#d0243a"), C.H("#17171a") }; // c d h s

        public static VisualElement Face(int card, float w = 64)
        {
            float h = w * 1.4f;
            var c = U.Div("pcard").Size(w, h).Radius(w * 0.12f);
            var col = SuitCol[card & 3];
            var rank = U.T(Rank(card), "pcard-r", Fonts.Bold).Fg(col).Fs(w * 0.46f).NoPick();
            var suit = U.Sym(global::CFDream.Cards.SuitSym(card), "pcard-s").Fg(col).Fs(w * 0.42f).NoPick();
            c.Add(rank); c.Add(suit);
            return c;
        }
        public static string Rank(int card) { var r = global::CFDream.Cards.Rank(card); return r == "T" ? "10" : r; }

        public static VisualElement Back(float w, string back = null)
        {
            var b = U.Div("pcard-back").Size(w, w * 1.4f).Radius(w * 0.14f);
            var cfg = J.O(DB.Obj("DR_BACKS"), back ?? Game.State?.pg?.eq?.GetValueOr("back", "classic") ?? "classic");
            var bd = C.H(J.S(cfg, "bd", "#f3ecdc"), C.Cream); var bg = BackColor(cfg);
            b.Bg(bg).Border(bd, Mathf.Max(1, w * 0.06f));
            return b;
        }
        static Color BackColor(Dictionary<string, object> cfg)
        {
            var css = J.S(cfg, "bg", "#b3262c");
            int i = css.IndexOf('#'); if (i >= 0 && i + 7 <= css.Length) return C.H(css.Substring(i, 7));
            return C.H("#b3262c");
        }
        public static string Short(int[] hand) => hand == null || hand.Length < 2 ? "" : Rank(hand[0]) + global::CFDream.Cards.SuitSym(hand[0]) + Rank(hand[1]) + global::CFDream.Cards.SuitSym(hand[1]);
    }

    static class DictExt
    {
        public static string GetValueOr(this Dictionary<string, string> d, string k, string def) => d != null && d.TryGetValue(k, out var v) ? v : def;
    }

    public class TableView : VisualElement
    {
        readonly Spot sp; readonly Color felt, rim;
        readonly string vs;   // someone from the story at this table: their seat is marked
        public TableView(Spot spot, bool showHeroCards = true, int answered = -1, string vs = null)
        {
            this.vs = vs;
            sp = spot;
            var fcfg = J.O(DB.Obj("DR_FELTS"), Game.State?.pg?.eq?.GetValueOr("felt", "green") ?? "green");
            felt = C.H(J.S(fcfg, "c", "#0e4d39"), C.Felt);
            rim = C.H("#2a1d12");
            AddToClassList("table");
            generateVisualContent += DrawFelt;
            Build(answered);
        }

        void DrawFelt(MeshGenerationContext mgc)
        {
            var r = contentRect; if (r.width < 10) return;
            var p = mgc.painter2D;
            float inset = 14, w = r.width - inset * 2, h = r.height - inset * 2, rad = Mathf.Min(w, h) / 2;
            Pill(p, r.x + inset - 7, r.y + inset - 7, w + 14, h + 14, rad + 7); p.fillColor = rim; p.Fill();
            Pill(p, r.x + inset - 2, r.y + inset - 2, w + 4, h + 4, rad + 2); p.fillColor = C.H("#3b2a19"); p.Fill();
            Pill(p, r.x + inset, r.y + inset, w, h, rad); p.fillColor = felt; p.Fill();
            Pill(p, r.x + inset + 16, r.y + inset + 16, w - 32, h - 32, Mathf.Max(4, rad - 16)); p.strokeColor = C.A(Color.white, 0.07f); p.lineWidth = 1.5f; p.Stroke();
        }
        static void Pill(Painter2D p, float x, float y, float w, float h, float r)
        {
            r = Mathf.Min(r, Mathf.Min(w, h) / 2);
            p.BeginPath();
            p.MoveTo(new Vector2(x + r, y)); p.LineTo(new Vector2(x + w - r, y));
            p.Arc(new Vector2(x + w - r, y + r), r, Angle.Degrees(-90), Angle.Degrees(0));
            p.LineTo(new Vector2(x + w, y + h - r));
            p.Arc(new Vector2(x + w - r, y + h - r), r, Angle.Degrees(0), Angle.Degrees(90));
            p.LineTo(new Vector2(x + r, y + h));
            p.Arc(new Vector2(x + r, y + h - r), r, Angle.Degrees(90), Angle.Degrees(180));
            p.LineTo(new Vector2(x, y + r));
            p.Arc(new Vector2(x + r, y + r), r, Angle.Degrees(180), Angle.Degrees(270));
            p.ClosePath();
        }

        void Build(int answered)
        {
            int n = sp.seats.Count; if (n == 0) return;
            // pot in the middle
            var pot = U.Col().Cls("pot");
            pot.Add(U.T("POT", "pot-k", Fonts.Bold)); pot.Add(U.Num(Fmt.Bb(sp.pot) + " bb", "pot-v"));
            Add(pot);
            // the rival sits in the seat you're up against: the biggest bet still in the hand
            int rival = -1;
            if (vs != null)
                for (int i = 0; i < n; i++) { var x = sp.seats[i]; if (i == sp.hero || x.folded) continue; if (rival < 0 || x.put > sp.seats[rival].put) rival = i; }
            for (int i = 0; i < n; i++)
            {
                var s = sp.seats[i]; int k = (i - sp.hero + n) % n;
                double a = Math.PI / 2 + k * 2 * Math.PI / n;
                float x = 50 + 44 * (float)Math.Cos(a), y = 50 + 41 * (float)Math.Sin(a);
                bool hero = i == sp.hero;
                var seat = U.Col().Cls("seat", hero ? "hero" : "", s.folded ? "folded" : "");
                seat.style.left = Length.Percent(x); seat.style.top = Length.Percent(y);
                seat.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
                var top = U.Row(); top.style.justifyContent = Justify.Center;
                top.Add(U.T(s.pos, "seat-pos", Fonts.Bold));
                if (s.pos == "BTN") top.Add(U.T("D", "dealer", Fonts.Bold));
                if (i == rival)
                {
                    seat.AddToClassList("rival");
                    var fig = Fig.For(vs); if (fig != null) { var pt = new Portrait(fig, 30, false); pt.AddToClassList("seat-face"); seat.Add(pt); }
                    seat.Add(U.T(Story.NameOf(vs).Split(' ')[0], "seat-rival", Fonts.Semi));
                }
                seat.Add(top);
                seat.Add(U.Num(Fmt.Bb(Math.Max(0, s.stack - s.put)), "seat-stack"));
                if (!string.IsNullOrEmpty(s.act) && !hero) seat.Add(U.T(s.act, "seat-act " + ActCls(s.act), Fonts.Semi));
                if (!hero && !s.folded)
                {
                    var backs = U.Row().Cls("seat-cards"); backs.Add(CardView.Back(11)); backs.Add(CardView.Back(11)); seat.Add(backs);
                }
                Add(seat);
                if (s.put > 0)
                {
                    float bx = 50 + 25 * (float)Math.Cos(a), by = 50 + 23 * (float)Math.Sin(a);
                    var chip = U.Row().Cls("bet");
                    chip.style.left = Length.Percent(bx); chip.style.top = Length.Percent(by);
                    chip.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
                    var dot = U.Div("chip-dot"); chip.Add(dot); chip.Add(U.Num(Fmt.Bb(s.put), "bet-v"));
                    Add(chip);
                }
            }
        }
        static string ActCls(string act)
        {
            var a = (act ?? "").ToLowerInvariant();
            return a.StartsWith("fold") ? "a-fold" : a.StartsWith("call") || a.StartsWith("limp") || a.StartsWith("check") ? "a-call" : a.StartsWith("jam") || a.StartsWith("all") ? "a-jam" : "a-raise";
        }
    }
}
