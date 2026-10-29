// The solver-graded hands: ported from the old project's Scripts/Game/Play.cs and Scripts/Data/DB.cs (spots part) with
// the dealing, grading, chip swings, coolers, placement and payouts kept line for line. The library is
// Resources/Data/spots.json, a byte copy of the old one (tools/export_ch1.py); Chapter 1's events, labels and payouts
// are Resources/Data/ch1.json. Editor/GradeCheck.cs proves LabelFor grades exactly like the old code.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dream.Game
{
    // mulberry32, seeded from a string (old Core/Util.cs Rng, verbatim)
    public class Rng
    {
        uint a;
        public Rng(string seed) { a = Hash(seed) | 1u; }
        public static uint Hash(string s) { uint h = 2166136261; foreach (char c in s) { h ^= c; h *= 16777619; } return h; }
        public double Next()
        {
            unchecked
            {
                a += 0x6D2B79F5; uint t = a;
                t = (t ^ (t >> 15)) * (t | 1); t ^= t + (t ^ (t >> 7)) * (t | 61);
                return ((t ^ (t >> 14)) >> 0) / 4294967296.0;
            }
        }
        public int Int(int n) => n <= 0 ? 0 : (int)(Next() * n) % n;
    }

    // small readers for MiniJson's dictionaries and lists
    public static class J
    {
        public static object G(object o, string k) => o is Dictionary<string, object> d && d.TryGetValue(k, out var v) ? v : null;
        public static Dictionary<string, object> O(object o, string k) => G(o, k) as Dictionary<string, object>;
        public static List<object> L(object o, string k) => G(o, k) as List<object>;
        public static double Num(object v) => v is double d ? d : v is bool b ? (b ? 1 : 0) : v == null ? 0 : Convert.ToDouble(v);
        public static double N(object o, string k, double def = 0) { var v = G(o, k); return v == null ? def : Num(v); }
        public static int I(object o, string k, int def = 0) => (int)N(o, k, def);
        public static string S(object o, string k, string def = null) => G(o, k) as string ?? def;
        public static bool B(object o, string k) { var v = G(o, k); return v is bool b ? b : v != null && Num(v) != 0; }
        public static bool Has(object o, string k) => G(o, k) != null;
        public static List<double> Nums(object v) => (v as List<object>)?.Select(Num).ToList() ?? new List<double>();
        public static List<string> Strs(object v) => (v as List<object>)?.Select(x => x as string).ToList() ?? new List<string>();
    }

    public class LabelDef { public string id, n, icon, c, tier, line; public int score, life; }
    public class EventDef
    {
        public string id, kind, name, venue, title, boss, blurb; public int ch, buyin, field, seats; public bool free, turbo, reentry; public double soft, prizeBase;
        public static EventDef From(Dictionary<string, object> o) => new EventDef
        {
            id = J.S(o, "id"), kind = J.S(o, "kind"), name = J.S(o, "name"), venue = J.S(o, "venue"), title = J.S(o, "title"), boss = J.S(o, "boss"), blurb = J.S(o, "blurb", ""),
            ch = J.I(o, "ch"), buyin = J.I(o, "buyin"), field = J.I(o, "field"), seats = J.I(o, "seats"), free = J.B(o, "free"), turbo = J.B(o, "turbo"), reentry = J.B(o, "reentry"),
            soft = J.N(o, "soft"), prizeBase = J.N(o, "prizeBase"),
        };
    }

    public static class Lib
    {
        static Dictionary<string, object> spots, data;
        public static Dictionary<string, LabelDef> Labels = new Dictionary<string, LabelDef>();
        public static List<List<int[]>> ClassCombos = new List<List<int[]>>();
        public static List<string> ClassNames = new List<string>();
        public static List<EventDef> Events = new List<EventDef>();

        public static void Load()
        {
            if (data != null) return;
            data = MiniJson.Parse(Resources.Load<TextAsset>("Data/ch1").text) as Dictionary<string, object>;
            foreach (var kv in J.O(data, "labels"))
            {
                var o = kv.Value;
                Labels[kv.Key] = new LabelDef { id = kv.Key, n = J.S(o, "n"), icon = J.S(o, "icon"), c = J.S(o, "c"), tier = J.S(o, "tier"), line = J.S(o, "line"), score = J.I(o, "score"), life = J.I(o, "life") };
            }
            foreach (var o in J.L(data, "classes"))
            {
                ClassNames.Add(J.S(o, "name"));
                var combos = new List<int[]>();
                foreach (var c in J.L(o, "combos")) { var p = J.Nums(c); combos.Add(new[] { (int)p[0], (int)p[1] }); }
                ClassCombos.Add(combos);
            }
            foreach (var o in J.L(data, "events")) Events.Add(EventDef.From((Dictionary<string, object>)o));
        }
        public static Dictionary<string, object> Spots
        {
            get { if (spots == null) spots = MiniJson.Parse(Resources.Load<TextAsset>("Data/spots").text) as Dictionary<string, object>; return spots; }
        }
        public static List<object> Tables(string gid) => J.L(Spots, gid) ?? new List<object>();
        public static string StageGid(string stage) => J.S(J.O(J.O(data, "stage"), stage), "gid", "ring");
        public static Dictionary<string, object> Payouts(int field) => J.O(J.O(data, "payouts"), field.ToString());
        public static EventDef Event(string id) { Load(); return Events.FirstOrDefault(e => e.id == id); }
    }

    public class SeatView { public string pos, act; public double stack, put; public bool folded; }
    public class Spot
    {
        public Dictionary<string, object> table, node;
        public string gid; public int tableIndex, nodeIndex;
        public int cls, hero; public int[] hand;
        public List<string> acts, opts; public List<double> strat, ev;
        public List<SeatView> seats = new List<SeatView>();
        public double pot, hard, diff, evScale = 1; public string cat, unit = "bb";
        public List<string> path;
        public string Label(int i) => opts != null && i < opts.Count ? opts[i] : acts[i];
    }
    public class Decision { public int j, pts, bestIdx; public bool ok; public string tier, lab, stage, mine, best; public int before, after; public double? loss; public Spot spot; }

    public class Run
    {
        public EventDef ev; public List<string> stages; public List<int> perS;
        public int si, h, stack = 100, total, combo, bestCombo;
        public bool bust, turbo; public string cooler;
        public List<Decision> results = new List<Decision>();
        public Rng rng; public Spot spot;
        public string libGid; public int libIndex = -1; public HashSet<int> seen = new HashSet<int>(); public List<string> usedTables = new List<string>();
        public string Stage => stages[Math.Min(si, stages.Count - 1)];
    }

    public static class Hands
    {
        public const int PerStage = 3, CashHands = 5;
        public static int Chapter = 1;   // difficulty follows the chapter (old: Math.Max(S.ch, ev.ch))

        public static (List<string> stages, List<int> perS) PlanFor(EventDef ev, int ch)
        {
            if (ev.kind == "cash") return (new List<string> { "cash" }, null);
            if (ev.kind == "sat") return (new List<string> { "early", "satbubble" }, null);
            if (ev.turbo) return (new List<string> { "early", "middle", "bubble", "final" }, new List<int> { 1, 2, 2, 2 });
            int c = Math.Max(ch, ev.ch); bool big = ev.field >= 1500;
            (string, int)[] plan = c <= 1 ? new[] { ("early", 4), ("middle", 2), ("bubble", 1), ("final", 1) }
                : c == 2 ? new[] { ("early", 2), ("middle", 4), ("bubble", 1), ("final", 1) }
                : c == 3 ? new[] { ("early", 1), ("middle", 3), ("bubble", 2), ("final", 2) }
                : c == 4 ? (big ? new[] { ("early", 1), ("middle", 1), ("bubble", 3), ("deep", 2), ("final", 3) } : new[] { ("early", 1), ("middle", 1), ("bubble", 3), ("final", 3) })
                : new[] { ("early", 1), ("bubble", 3), ("deep", 2), ("final", 4) };
            return (plan.Select(p => p.Item1).ToList(), plan.Select(p => p.Item2).ToList());
        }
        public static int Per(Run run) => run.perS != null ? run.perS[Math.Min(run.si, run.perS.Count - 1)] : run.ev.kind == "cash" ? CashHands : PerStage;

        public static Run Start(EventDef ev, string seed)
        {
            Lib.Load();
            var plan = PlanFor(ev, Chapter);
            var run = new Run { ev = ev, stages = plan.stages, perS = plan.perS, rng = new Rng(seed), turbo = ev.turbo };
            PickTable(run);
            return run;
        }

        static bool LiveEv(EventDef ev) => true;   // Beta 1.0 plays only the live card room (big-blind ante tables)
        static bool Fit(string stage, Dictionary<string, object> t, bool turbo)
        {
            double avg = J.N(t, "avg", 40), left = J.N(t, "left", 99);
            if (turbo && stage == "early") stage = "middle";
            switch (stage)
            {
                case "early": return avg >= 32;
                case "middle": return avg < 32;
                case "deep": return left > 9;
                case "final": return left <= 9;
                default: return true;
            }
        }
        public static void PickTable(Run run)
        {
            string stage = run.Stage, gid = Lib.StageGid(stage); var pool = Lib.Tables(gid);
            bool bb = LiveEv(run.ev);
            var idx = Enumerable.Range(0, pool.Count).ToList();
            var anteOk = idx.Where(i => J.B(pool[i], "bbAnte") == bb || !J.Has(pool[i], "left")).ToList();
            var fit = anteOk.Where(i => Fit(stage, (Dictionary<string, object>)pool[i], run.turbo)).ToList();
            var ok = fit.Count > 0 ? fit : anteOk.Count > 0 ? anteOk : idx;
            var fresh = ok.Where(i => !run.usedTables.Contains(gid + i)).ToList(); var from = fresh.Count > 0 ? fresh : ok;
            int ti = from[run.rng.Int(from.Count)]; run.usedTables.Add(gid + ti);
            run.libGid = gid; run.libIndex = ti; run.seen.Clear();
        }

        public static double Difficulty(Run run)
        {
            var ev = run.ev; string stage = run.Stage;
            double d = new[] { 0.12, 0.22, 0.36, 0.5, 0.64, 0.78, 0.9, 1 }[Math.Min(7, ev.id == "main" ? 6 : ev.ch)];
            if (stage == "final" || stage == "deep") d += 0.08;
            if (run.ev.boss != null && run.ev.ch > 0) d += 0.05;
            return Math.Max(0, Math.Min(1, d));
        }

        // a hand from a pre-solved table: a real situation (weighted by how often it happens), then a hand that makes it a
        // decision. Each situation's hands are stored hardest-first; difficulty picks the window. (old Play.Deal, verbatim)
        public static Spot Deal(Run run)
        {
            var pool = Lib.Tables(run.libGid); var table = (Dictionary<string, object>)pool[run.libIndex];
            var nodes = J.L(table, "n"); var rng = run.rng; double d = Difficulty(run);
            var cand = Enumerable.Range(0, nodes.Count).Where(i => !run.seen.Contains(i)).ToList();
            if (cand.Count == 0) cand = Enumerable.Range(0, nodes.Count).ToList();
            double tot = cand.Sum(i => Math.Sqrt(J.N(nodes[i], "w", 1))), x = rng.Next() * tot; int pick = cand[0];
            foreach (var i in cand) { x -= Math.Sqrt(J.N(nodes[i], "w", 1)); if (x < 0) { pick = i; break; } }
            run.seen.Add(pick);
            var node = (Dictionary<string, object>)nodes[pick];
            var acts = J.Strs(J.L(node, "a")); int fi = acts.IndexOf("fold");
            var rows = J.L(node, "r");
            bool IsFold(object r) { var st = J.Nums(((List<object>)r)[1]); return st.IndexOf(st.Max()) == fi; }
            var folds = rows.Where(IsFold).ToList(); var plays = rows.Where(r => !IsFold(r)).ToList();
            var want = rng.Next() < 0.5 ? plays : folds; var use = want.Count >= 2 ? want : rows;
            int L = use.Count, start = (int)Math.Round((1 - d) * 0.55 * L), width = Math.Max(2, (int)Math.Round(0.45 * L));
            int a0 = Math.Max(0, Math.Min(start, L - 2)), a1 = Math.Min(L, a0 + width);
            var window = use.Skip(a0).Take(Math.Max(1, a1 - a0)).ToList();
            var row = (List<object>)window[rng.Int(window.Count)];
            return run.spot = BuildSpot(run.libGid, run.libIndex, table, node, pick, row, d, run.rng);
        }
        public static Spot BuildSpot(string gid, int ti, Dictionary<string, object> table, Dictionary<string, object> node, int ni, List<object> row, double d, Rng rng)
        {
            int cls = (int)J.Num(row[0]); var combos = Lib.ClassCombos[cls]; var hand = combos[rng.Int(combos.Count)];
            var rows = J.L(node, "r"); int idx = rows.IndexOf(row);
            var v = J.O(node, "v"); string u = J.S(table, "u", "bb"); double k = J.N(table, "k", 1);
            var sp = new Spot
            {
                table = table, node = node, gid = gid, tableIndex = ti, nodeIndex = ni, cls = cls, hero = J.I(node, "h"), hand = new[] { hand[0], hand[1] },
                acts = J.Strs(J.L(node, "a")), strat = J.Nums(row[1]), ev = row.Count > 2 ? J.Nums(row[2]) : null, opts = J.Strs(J.L(v, "opts")),
                pot = J.N(v, "pot"), cat = J.S(v, "cat", ""), diff = d, hard = rows.Count > 1 ? 1 - idx / (double)(rows.Count - 1) : 0.5,
                unit = u == "icm" && k != 1 ? "bbicm" : u, evScale = u == "icm" && k != 1 ? k : 1, path = J.Strs(J.L(node, "p")),
            };
            foreach (var s in J.L(v, "seats"))
            {
                var a = (List<object>)s;
                sp.seats.Add(new SeatView { pos = a[0] as string, stack = J.Num(a[1]), put = J.Num(a[2]), folded = J.Num(a[3]) != 0, act = a.Count > 4 ? a[4] as string : "" });
            }
            return sp;
        }

        // ------------------------------------------------------------ grading, Chess.com style (old Play.LabelFor, verbatim)
        static readonly Dictionary<string, double[]> LossSteps = new Dictionary<string, double[]> { { "bb", new[] { 0.05, 0.2, 0.6, 1.5 } }, { "icm", new[] { 0.015, 0.06, 0.18, 0.5 } } };
        public static (string lab, double? loss, int bestIdx) LabelFor(List<double> strat, List<double> ev, int j, string unit, double hard, string kind)
        {
            double fmax = strat.Max(); int sb = strat.IndexOf(fmax); double mine = strat[j];
            double? loss = null; int best = sb;
            if (ev != null && ev.Count == strat.Count && ev.All(x => !double.IsNaN(x) && !double.IsInfinity(x))) { best = ev.IndexOf(ev.Max()); loss = Math.Max(0, ev[best] - ev[j]); }
            var T = LossSteps.TryGetValue(unit, out var t) ? t : LossSteps["bb"];
            string lab;
            if (loss == null)
            {
                double rel = fmax > 0 ? mine / fmax : 0; lab = mine < 0.05 ? (rel < 0.02 ? "blunder" : "mistake") : rel >= 0.9 ? "best" : rel >= 0.5 ? "good" : "inaccuracy";
                if (mine >= 0.6 || j == sb) lab = "best"; else if (mine >= 0.25 && (lab == "inaccuracy" || lab == "mistake" || lab == "blunder")) lab = "good";
            }
            else
            {
                double l = loss.Value; lab = l <= 1e-9 ? "best" : l <= T[0] ? "excellent" : l <= T[1] ? "good" : l <= T[2] ? "inaccuracy" : l <= T[3] ? "mistake" : "blunder";
                if (j == best || (j == sb && l <= T[0])) lab = "best";
                else if (mine >= 0.25 && (lab == "inaccuracy" || lab == "mistake" || lab == "blunder")) lab = "good";
            }
            if (lab == "best" && hard >= 0.95) lab = (kind == "allin" || kind == "raise" || kind == "raiseB" || kind == "call") && mine <= 0.8 ? "brilliant" : "great";
            return (lab, loss, best);
        }

        // the grade of a spot and answer, exactly as the old Play.Answer computes it (ICM EVs scaled first)
        public static (string lab, double? loss, int bestIdx) Grade(Spot sp, int j)
        {
            var ev = sp.ev != null && sp.evScale != 1 ? sp.ev.Select(x => x * sp.evScale).ToList() : sp.ev;
            return LabelFor(sp.strat, ev, j, sp.unit == "bbicm" ? "bb" : sp.unit, sp.hard, sp.acts[j]);
        }

        public static Decision Answer(Run run, int j)
        {
            var sp = run.spot; var g = Grade(sp, j);
            var L = Lib.Labels[g.lab];
            string tier = L.tier; bool ok = tier != "bad"; int pts = L.score - (tier == "bad" ? 60 : 0);
            int before = run.stack; run.stack = Math.Max(0, Math.Min(200, run.stack + L.life));
            run.combo = ok && tier != "close" ? run.combo + 1 : 0; run.bestCombo = Math.Max(run.bestCombo, run.combo);
            var r = new Decision { j = j, pts = pts, ok = ok, tier = tier, lab = g.lab, stage = run.Stage, before = before, after = run.stack, loss = g.loss, spot = sp, mine = sp.Label(j), best = sp.Label(g.bestIdx), bestIdx = g.bestIdx };
            run.total += pts; run.results.Add(r);
            if (run.stack <= 0) run.bust = true;
            return r;
        }
        public static int Accuracy(List<Decision> rs) => rs.Count == 0 ? 0 : (int)Math.Round(rs.Average(r => (double)(Lib.Labels.TryGetValue(r.lab, out var L) ? L.score : r.ok ? 80 : 10)));

        // ------------------------------------------------------------ between hands
        public static bool CoolerRoll(Run run)
        {
            var ev = run.ev; double p = Math.Max(0.02, Math.Min(0.22, 0.05 + 0.025 * Math.Log10(Math.Max(2, ev.field)) - (run.stack - 100) / 700.0));
            return run.rng.Next() < p;
        }
        // what happens next: "hand", "level", "cooler" or "finish"
        public static string Next(Run run)
        {
            if (run.bust) return "finish";
            int per = Per(run);
            if (++run.h < per) return "hand";
            run.h = 0;
            if (run.si + 1 < run.stages.Count)
            {
                if (run.ev.kind != "cash" && CoolerRoll(run)) { run.bust = true; run.cooler = "cooler"; return "cooler"; }
                run.si++; PickTable(run); return "level";
            }
            run.si++; return "finish";
        }
        public static double CashNet(Run run, double luck) => Math.Round(run.ev.buyin * Math.Max(-1, Math.Min(2.5, (run.stack - 100) / 40.0 + luck)));

        // ------------------------------------------------------------ where you finish (old Play.Place, verbatim)
        public static int Paid(EventDef ev)
        {
            if (ev.kind == "sat") return ev.seats;
            var p = Lib.Payouts(ev.field); return p != null ? J.I(p, "paid") : Math.Max(1, ev.field / 7);
        }
        public static double PrizeFor(EventDef ev, int place)
        {
            var p = Lib.Payouts(ev.field); if (p == null) return 0;
            var am = J.Nums(J.L(p, "amounts")); if (place < 1 || place > am.Count) return 0;
            return Math.Round(am[place - 1] * (ev.prizeBase > 0 ? ev.prizeBase : ev.buyin));
        }
        public static int Place(Run run)
        {
            var ev = run.ev; var rng = run.rng; int N = Math.Max(1, ev.field);
            int Between(int a, int b) => Math.Max(1, Math.Min(N, a + rng.Int(Math.Max(1, b - a + 1))));
            int P = Paid(ev); string stage = run.Stage; int n = run.results.Count; double pct = run.total / (100.0 * Math.Max(1, n));
            if (run.bust)
            {
                if (stage == "early") return Between(Math.Max(P + 2, (int)Math.Round(N * 0.45)), N);
                if (stage == "middle") return Between(P + 2, Math.Max(P + 2, (int)Math.Round(N * 0.45)));
                if (stage == "bubble") return run.h == 0 && run.cooler == null ? P + 1 : Between(Math.Min(P, 10), P);
                if (stage == "deep") return Between(10, Math.Max(10, Math.Min(P, (int)Math.Round(P * 0.3))));
                var ranges = new[] { new[] { 7, 9 }, new[] { 4, 6 }, new[] { 2, 3 } }; var rg = ranges[Math.Min(2, run.h * 3 / Math.Max(1, Per(run)))];
                return Between(Math.Min(rg[0], N), Math.Min(rg[1], N));
            }
            double winT = 0.6 + 0.04 * Math.Log10(N) + 0.01 * Math.Min(6, ev.ch), luck = (rng.Next() - 0.5) * 0.12, score = pct + luck + (run.stack - 100) / 1000.0;
            if (score >= winT) return 1;
            if (score >= winT - 0.12) return 2;
            if (score >= winT - 0.22) return 3;
            if (score >= winT - 0.36) return Between(4, Math.Min(9, N));
            if (score >= winT - 0.55) return Between(Math.Min(10, P), Math.Max(10, P));
            return Between(P + 1, Math.Max(P + 2, (int)Math.Round(P * 1.3)));
        }

        // cards: index 0..51 -> "As", "Td"... (rank = i / 4 from 2 upward, suit = i % 4), as the old DB.ClassCombos store them
        public static string Card(int i) => "23456789TJQKA"[i >> 2].ToString() + "cdhs"[i & 3];
    }
}
