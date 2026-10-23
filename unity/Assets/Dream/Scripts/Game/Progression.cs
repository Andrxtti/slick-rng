// One career level across everything (XP comes from decisions, not money), a level road of table cosmetics,
// daily quests, tiered achievements and skill mastery.
using System;
using System.Collections.Generic;
using System.Linq;

namespace CFDream
{
    public class Prog
    {
        public double xp0, xp; public List<int> levels = new List<int>();
        public List<(string kind, string id, string why)> cos = new List<(string, string, string)>();
        public List<(string t, int xp)> quests = new List<(string, int)>();
        public List<(string id, int tier)> ach = new List<(string, int)>();
        public List<(string slot, string id, int lv)> wear = new List<(string, string, int)>();
        public bool bonus;
    }

    public static class Progression
    {
        public static int XpNeed(int lvl) => 120 + 30 * (lvl - 1);
        public static (int lvl, double into, int need) LevelOf(double xp)
        {
            int l = 1; double left = xp;
            while (left >= XpNeed(l) && l < 99) { left -= XpNeed(l); l++; }
            return (l, left, XpNeed(l));
        }
        public static readonly string[] Skills = { "first", "raise", "threebet", "jam", "icm", "short" };
        public static readonly Dictionary<string, (string n, string d)> SkillNames = new Dictionary<string, (string, string)>
        {
            { "first", ("First in", "Opening, or folding, when nobody has raised") }, { "raise", ("Facing a raise", "Calling, 3-betting or folding to an open") },
            { "threebet", ("Facing a 3-bet+", "When the pot has been re-raised") }, { "jam", ("Facing a jam", "Call off or let it go") },
            { "icm", ("Bubble and final table", "Decisions where ICM bends the math") }, { "short", ("Short stack", "15 big blinds or fewer") },
        };
        static bool Icm(string stage) => stage == "bubble" || stage == "satbubble" || stage == "deep" || stage == "final";
        public static List<string> SkillKeys(HandInfo h, string stage)
        {
            var k = new List<string> { h.cat == "Facing a jam" ? "jam" : h.cat == "Facing a raise" ? "raise" : h.cat == "Facing a 3-bet or more" ? "threebet" : "first" };
            if (Icm(stage)) k.Add("icm");
            if (h.bb <= 15) k.Add("short");
            return k;
        }
        public static int? SkillAcc(Progress p, string k) => p.sk.TryGetValue(k, out var a) && a.Count > 0 ? (int?)Math.Round(a.Average()) : null;
        public static int SkillStars(Progress p, string k)
        {
            if (!p.sk.TryGetValue(k, out var a) || a.Count < 8) return 0; var acc = SkillAcc(p, k) ?? 0;
            return acc >= 90 ? 3 : acc >= 78 ? 2 : acc >= 62 ? 1 : 0;
        }

        // ------------------------------------------------------------ quests: three a day, the same for everyone that day
        public static Progress Pg(State s)
        {
            var p = s.pg; string today = Calendar.RealToday();
            if (p.qDay != today)
            {
                uint h = 0; foreach (char ch in today) h = unchecked(h * 31u + ch);
                var pool = DB.List("DR_QUESTS").Where(q => J.I(q, "ch") <= s.ch).Select(q => J.S(q, "id")).ToList(); var pick = new List<string>();
                while (pick.Count < 3 && pool.Count > 0) { h = unchecked(h * 1103515245u + 12345u); int i = (int)(h % (uint)pool.Count); pick.Add(pool[i]); pool.RemoveAt(i); }
                p.qDay = today; p.qBonus = false; p.q = pick.Select(id => new Quest { id = id }).ToList();
            }
            return p;
        }
        public static Dictionary<string, object> QuestDef(string id) => DB.List("DR_QUESTS").FirstOrDefault(q => J.S(q, "id") == id) as Dictionary<string, object>;
        public static void QuestBump(State s, string id, int by, Prog o)
        {
            var p = Pg(s); var q = p.q.FirstOrDefault(x => x.id == id); if (q == null || q.done) return;
            var def = QuestDef(id); int n = J.I(def, "n", 1);
            q.v = id == "combo" ? Math.Max(q.v, by) : q.v + by;
            if (q.v >= n) { q.done = true; q.v = n; p.Bump("quests"); int xp = J.I(def, "xp", 50); o.quests.Add((J.S(def, "t"), xp)); GainXp(s, xp, o); }
            if (!p.qBonus && p.q.All(x => x.done)) { p.qBonus = true; o.bonus = true; GainXp(s, 100, o); }
        }

        public static void GainXp(State s, double amt, Prog o)
        {
            var p = s.pg; int before = LevelOf(p.xp).lvl; p.xp += Math.Max(0, Math.Round(amt)); int after = LevelOf(p.xp).lvl;
            for (int l = before + 1; l <= after; l++)
            {
                o.levels.Add(l);
                var road = DB.Obj("DR_ROAD"); var step = J.L(road, l.ToString());
                if (step != null) { var g = Grant(s, step[0] as string, step[1] as string, $"Career level {l}"); if (g.HasValue) o.cos.Add(g.Value); }
                Career.Moment(s, "⬆️", $"Reached career level {l}");
            }
        }
        public static (string, string, string)? Grant(State s, string kind, string id, string why)
        {
            var p = s.pg; string key = kind + ":" + id; if (p.cos.Contains(key)) return null;
            p.cos.Add(key);
            var starter = new Dictionary<string, string> { { "back", "classic" }, { "felt", "green" }, { "frame", "none" }, { "title", "none" } };
            if (p.eq.TryGetValue(kind, out var cur) && starter.TryGetValue(kind, out var st) && cur == st) p.eq[kind] = id;
            return (kind, id, why);
        }

        // ------------------------------------------------------------ achievements
        static int AchValue(State s, string id)
        {
            var p = s.pg;
            switch (id)
            {
                case "textbook": return p.St("textbook");
                case "brilliant": return p.St("finds");
                case "jamcall": return p.St("jamCall");
                case "jamfold": return p.St("jamFold");
                case "bubble": return p.St("bubble");
                case "wins": return p.St("wins");
                case "streak": return p.St("bestCombo");
                case "days": return p.days.Count;
                case "legends": return s.legends.Count;
                case "flawless": return p.St("flawless");
                case "comeback": return p.St("comeback");
                case "coolers": return p.St("cleanBeats");
                case "bounce": return p.St("bounce");
                case "seats": return p.St("seats");
                case "skills": return Skills.Count(k => k != "short" && SkillStars(p, k) >= 2);
                case "quests": return p.St("quests");
                case "bracelets": return s.Title("bracelet");
                case "collector": return p.cos.Count + s.owned.Count;
                case "signature": return s.wear.Keys.Count(k => Fashion.WearLv(s, k) >= 2);
                case "drip": return Math.Max(p.St("bestFit"), Fashion.FitScore(s).score);
                default: return 0;
            }
        }
        static void CheckAch(State s, Prog o)
        {
            var p = s.pg;
            foreach (var a in DB.List("DR_ACH"))
            {
                string id = J.S(a, "id"); var t = J.Nums(J.L(a, "t")); if (t.Count < 3) continue;
                int v = AchValue(s, id), have = p.ach.TryGetValue(id, out var h) ? h : 0, tier = have;
                while (tier < 3 && v >= t[tier]) tier++;
                for (int k = have; k < tier; k++)
                {
                    o.ach.Add((id, k)); GainXp(s, new[] { 50, 150, 400 }[k], o);
                    var gold = J.Strs(J.L(a, "gold")); if (k == 2 && gold.Count == 2) { var g = Grant(s, gold[0], gold[1], $"{J.S(a, "n")} (Gold)"); if (g.HasValue) o.cos.Add(g.Value); }
                }
                p.ach[id] = tier;
            }
        }

        // ------------------------------------------------------------ hooks from the game
        public static void Track(Decision r, Run run)
        {
            var s = Game.State; var p = Pg(s); var h = r.hand; bool good = r.tier == "perfect" || r.tier == "good";
            if (run.prog == null) run.prog = new Prog { xp0 = p.xp };
            var o = run.prog; int sc = DB.Labels.TryGetValue(r.lab, out var L) ? L.score : good ? 85 : 20;
            foreach (var k in SkillKeys(h, r.stage)) { if (!p.sk.TryGetValue(k, out var a)) p.sk[k] = a = new List<int>(); a.Add(sc); if (a.Count > 30) a.RemoveAt(0); }
            if (r.tier == "bad" && run.spot != null)
            {
                p.mis.Add(new MisSpot { g = run.spot.gid, t = run.spot.tableIndex, n = run.spot.nodeIndex, c = run.spot.cls, p = string.Join(",", run.spot.path), lab = r.lab, d = Career.Now() });
                if (p.mis.Count > 60) p.mis.RemoveAt(0);
            }
            if (r.tier == "perfect") { p.Bump("textbook"); QuestBump(s, "textbook", 1, o); }
            if (r.lab == "brilliant" || r.lab == "great") { p.Bump("finds"); QuestBump(s, "find", 1, o); }
            if (h.cat == "Facing a jam" && good) { p.Bump(h.mineK == "fold" ? "jamFold" : "jamCall"); QuestBump(s, "jam", 1, o); }
            if (good && Icm(r.stage)) QuestBump(s, "icm", 1, o);
            p.st["bestCombo"] = Math.Max(p.St("bestCombo"), run.combo); QuestBump(s, "combo", run.combo, o);
            if (run.stack < 40) run.fumes = true;
            Fashion.WearDecision(s, good, o, run);
            GainXp(s, Math.Round(sc / 10.0), o);
        }

        public static void EventDone(Result res, Run run)
        {
            var s = Game.State; var p = Pg(s); var ev = res.ev; string today = Calendar.RealToday();
            if (run.prog == null) run.prog = new Prog { xp0 = p.xp };
            var o = run.prog;
            if (!p.days.Contains(today)) { p.days.Add(today); if (p.days.Count > 400) p.days.RemoveAt(0); }
            string kind = ev.kind; int paid = kind == "sat" ? ev.seats : kind == "cash" ? 0 : Play.Paid(ev);
            bool cashed = kind == "cash" ? res.net > 0 : res.place > 0 && res.place <= paid, won = res.place == 1 && kind != "cash";
            double xp = 20 + (cashed ? 25 : 0) + (won ? 60 : 0) + (res.acc >= 85 ? 20 : 0);
            GainXp(s, xp, o);
            if (won) { p.Bump("wins"); if (run.fumes) p.Bump("comeback"); }
            if (res.cooler != null) { p.Bump("coolers"); if (res.acc >= 85) p.Bump("cleanBeats"); p.st["lastCool"] = 1; }
            else { if (won && p.St("lastCool") == 1) p.Bump("bounce"); p.st["lastCool"] = 0; }
            if (kind == "sat" && res.ticket != null) p.Bump("seats");
            if (cashed && run.stages.Contains("bubble") && run.si >= run.stages.IndexOf("bubble")) p.Bump("bubble");
            if (res.acc == 100 && res.n >= 6) p.Bump("flawless");
            p.st["bestFit"] = Math.Max(p.St("bestFit"), Fashion.FitScore(s).score);
            QuestBump(s, "story", 1, o);
            if (cashed && kind != "cash") QuestBump(s, "cash", 1, o);
            if (ev.daily) QuestBump(s, "grind", 1, o);
            if (res.acc >= 85 && res.n >= 5) QuestBump(s, "acc", 1, o);
            Fashion.WearEvent(s, res, run, cashed, won, kind != "cash" && run.stages.Contains("final") && run.si >= run.stages.Count - 1 && (!run.bust || res.place <= 9));
            CheckAch(s, o);
            o.xp = p.xp - o.xp0; res.prog = o;
        }
    }
}
