// The career tab: level road, today's quests, how you play (skills), achievements, trophies, the legends, and
// the table cosmetics you've earned (card backs, felts, frames, titles).
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public static class CareerView
    {
        public static VisualElement Build(State s)
        {
            var col = U.Col();
            col.Add(Level(s));
            col.Add(Quests(s));
            col.Add(Numbers(s));
            col.Add(Skills(s));
            col.Add(Trophies(s));
            col.Add(Achievements(s));
            col.Add(Legends(s));
            col.Add(TableStyle(s));
            return col;
        }

        static VisualElement Level(State s)
        {
            var c = U.Card(); var lv = Progression.LevelOf(s.pg.xp);
            var r = U.Row(); r.Add(U.Num(lv.lvl.ToString(), "big-num").Fg(C.Gold2));
            var l = U.Col().Grow(); l.style.marginLeft = 12; l.Add(U.Kicker("Career level")); l.Add(U.Bar(lv.into / Math.Max(1, lv.need), C.Gold, 6).Mt(4)); l.Add(U.Hint($"{Math.Round(lv.into)} / {lv.need} XP · XP comes from decisions, not money").Mt(2));
            r.Add(l); c.Add(r);
            var road = DB.Obj("DR_ROAD");
            if (road != null)
            {
                var next = road.Keys.Select(k => int.TryParse(k, out var n) ? n : 0).Where(n => n > lv.lvl).OrderBy(n => n).Take(3).ToList();
                if (next.Count > 0)
                {
                    var nr = U.Row().Cls("wrap").Mt(8);
                    foreach (var n in next) { var st = J.L(road, n.ToString()); nr.Add(U.Chip($"Lv {n}: {CosName(st[0] as string, st[1] as string)}", C.Mute)); }
                    c.Add(nr);
                }
            }
            return c;
        }
        public static string CosName(string kind, string id)
        {
            switch (kind)
            {
                case "title": return "“" + J.S(DB.Obj("DR_TITLES_COS"), id, id) + "” title";
                case "back": return J.S(J.O(DB.Obj("DR_BACKS"), id), "n", id) + " card back";
                case "felt": return J.S(J.O(DB.Obj("DR_FELTS"), id), "n", id) + " felt";
                case "frame": return J.S(J.O(DB.Obj("DR_FRAMES"), id), "n", id) + " frame";
                default: return id;
            }
        }

        static VisualElement Quests(State s)
        {
            var p = Progression.Pg(s); var c = U.Card();
            var hd = U.Row(); hd.Add(U.Kicker("Today's quests").Grow()); hd.Add(U.Hint(p.qBonus ? "Bonus earned" : "+100 XP for all three")); c.Add(hd);
            foreach (var q in p.q)
            {
                var def = Progression.QuestDef(q.id); int n = J.I(def, "n", 1);
                var r = U.Row().Cls("quest", q.done ? "done" : "");
                r.Add(U.T(q.done ? "✓" : "○", "quest-i", Fonts.Sym).Fg(q.done ? C.Green : C.Faint));
                var l = U.Col().Grow(); l.Add(U.P(J.S(def, "t", q.id))); if (!q.done) l.Add(U.Bar(q.v / (double)n, C.Blue, 4).Mt(3)); r.Add(l);
                r.Add(U.Num($"+{J.I(def, "xp", 50)}", "quest-xp").Fg(C.Gold));
                c.Add(r);
            }
            return c;
        }

        static VisualElement Numbers(State s)
        {
            var c = U.Card(); c.Add(U.Kicker("Career numbers"));
            int acc = s.dec > 0 ? (int)Math.Round(100.0 * (s.perfect + s.good) / s.dec) : 0;
            var r = U.Row().Cls("stats wrap").Mt(6);
            r.Add(U.Stat(s.played.ToString(), "Events")); r.Add(U.Stat(s.cashes.ToString(), "Cashes")); r.Add(U.Stat(s.ft.ToString(), "Final tables")); r.Add(U.Stat(s.TitleCount.ToString(), "Titles", C.Gold2));
            c.Add(r);
            var r2 = U.Row().Cls("stats wrap").Mt(6);
            r2.Add(U.Stat(s.dec.ToString("#,0"), "Decisions")); r2.Add(U.Stat(acc + "%", "Good or better")); r2.Add(U.Stat(U.Money(s.peak), "Peak roll")); r2.Add(U.Stat(s.bestStreak.ToString(), "Best streak"));
            c.Add(r2);
            return c;
        }

        static VisualElement Skills(State s)
        {
            var c = U.Card(); c.Add(U.Kicker("How you play"));
            foreach (var k in Progression.Skills)
            {
                var (n, d) = Progression.SkillNames[k]; var acc = Progression.SkillAcc(s.pg, k); int stars = Progression.SkillStars(s.pg, k);
                var r = U.Row().Cls("skill");
                var l = U.Col().Grow(); l.Add(U.T(n, "skill-n", Fonts.Semi)); l.Add(U.Hint(d)); r.Add(l);
                var rr = U.Col().Cls("skill-r");
                rr.Add(U.Sym(new string('★', stars) + new string('☆', 3 - stars), "stars").Fg(stars > 0 ? C.Gold : C.Faint));
                rr.Add(U.Hint(acc.HasValue ? acc + "%" : "—"));
                r.Add(rr); c.Add(r);
            }
            return c;
        }

        static VisualElement Trophies(State s)
        {
            var c = U.Card(); c.Add(U.Kicker("Trophy room"));
            var row = U.Row().Cls("wrap").Mt(4);
            foreach (var kv in DB.Obj("DR_TITLES") ?? new Dictionary<string, object>())
            {
                int n = s.Title(kv.Key); var t = U.Col().Cls("trophy", n > 0 ? "has" : "");
                t.Add(U.T(J.S(kv.Value, "icon", "🏆"), "trophy-i")); t.Add(U.Num(n.ToString(), "trophy-n")); t.Add(U.T(J.S(kv.Value, "plural", kv.Key), "trophy-l"));
                row.Add(t);
            }
            c.Add(row);
            foreach (var t in s.trophies.Take(12))
            {
                var r = U.Row().Cls("moment"); r.Add(U.T(DB.TitleIcon(t.t), "moment-i"));
                var l = U.Col().Grow(); l.Add(U.P(t.ev)); l.Add(U.Hint(DateTimeOffset.FromUnixTimeMilliseconds((long)t.d).LocalDateTime.ToString("MMM d, yyyy"))); r.Add(l);
                r.Add(U.Num(U.Money(t.prize), "").Fg(C.Gold2)); c.Add(r);
            }
            return c;
        }

        static VisualElement Achievements(State s)
        {
            var c = U.Card(); c.Add(U.Kicker("Achievements"));
            string[] tierN = { "Bronze", "Silver", "Gold" }; Color[] tierC = { C.H("#c58b56"), C.H("#cfd6de"), C.Gold };
            foreach (var a in DB.List("DR_ACH") ?? new List<object>())
            {
                string id = J.S(a, "id"); int tier = s.pg.ach.TryGetValue(id, out var t) ? t : 0; var th = J.Nums(J.L(a, "t"));
                var r = U.Row().Cls("ach", tier > 0 ? "has" : "");
                var l = U.Col().Grow(); l.Add(U.T(J.S(a, "n", id), "ach-n", Fonts.Semi)); l.Add(U.Hint(J.S(a, "d", "") + (th.Count >= 3 ? $" · {th[0]:0} / {th[1]:0} / {th[2]:0}" : ""))); r.Add(l);
                var pips = U.Row();
                for (int i = 0; i < 3; i++) pips.Add(U.Div("ach-pip").Bg(i < tier ? tierC[i] : C.Line));
                r.Add(pips);
                if (tier > 0) r.Add(U.T(tierN[tier - 1], "ach-tier", Fonts.Bold).Fg(tierC[tier - 1]));
                c.Add(r);
            }
            return c;
        }

        static VisualElement Legends(State s)
        {
            var c = U.Card(); var all = DB.List("DR_LEGENDS") ?? new List<object>();
            c.Add(U.Kicker($"Legends · beaten {s.legends.Count} of {all.Count}"));
            var grid = U.Row().Cls("wrap legends").Mt(6);
            foreach (var o in all)
            {
                string id = J.S(o, "id"); bool beat = s.legends.Contains(id), met = beat || s.met.Contains(id);
                var t = U.Col().Cls("legend", beat ? "beat" : met ? "met" : "");
                var f = Fig.For(id); if (f != null) t.Add(new Portrait(f, 54, false).Op(met ? 1 : 0.35f));
                t.Add(U.T(met ? J.S(o, "name", id) : "???", "legend-n", Fonts.Semi));
                t.Add(U.T(met ? J.S(o, "nick", "") : $"Chapter {J.I(o, "ch")}", "legend-k"));
                if (beat) t.Add(U.Chip("Beaten", C.Gold));
                grid.Add(t);
            }
            c.Add(grid);
            return c;
        }

        static VisualElement TableStyle(State s)
        {
            var c = U.Card(); c.Add(U.Kicker("Table style"));
            foreach (var (kind, label) in new[] { ("back", "Card back"), ("felt", "Felt"), ("frame", "Frame"), ("title", "Title") })
            {
                var owned = s.pg.cos.Where(k => k.StartsWith(kind + ":")).Select(k => k.Substring(kind.Length + 1)).ToList();
                c.Add(U.Hint(label).Mt(8));
                var row = U.Row().Cls("wrap");
                string cur = s.pg.eq.TryGetValue(kind, out var e) ? e : "none";
                foreach (var id in owned)
                {
                    string name = kind == "title" ? J.S(DB.Obj("DR_TITLES_COS"), id, id) : J.S(J.O(DB.Obj(kind == "back" ? "DR_BACKS" : kind == "felt" ? "DR_FELTS" : "DR_FRAMES"), id), "n", id);
                    var t = Screens.Toggle(name, cur == id, () => { s.pg.eq[kind] = id; s.Save(); Game.Render(); });
                    if (kind == "back") t.Insert(0, CardView.Back(12, id).NoPick());
                    if (kind == "felt") t.Insert(0, U.Div("fin-dot").Bg(C.H(J.S(J.O(DB.Obj("DR_FELTS"), id), "c", "#0e4d39"))).NoPick());
                    row.Add(t);
                }
                c.Add(row);
            }
            return c;
        }
    }
}
