// Playing an event: real decisions from the pre-solved library, graded like a chess engine grades moves.
// Good decisions grow your tournament life, mistakes cost a lot, and a cooler can still hit between levels.
using System;
using System.Collections.Generic;
using System.Linq;

namespace CFDream
{
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

    public class Odds { public double call, pot; public int need; }
    public class HandInfo
    {
        public int[] cards; public string pos, cat, mine, best, mineK, bestK, lab, unit; public double bb; public double? loss; public bool limp;
        public int bestF, myF, heroCls, facing; public Odds odds; public List<string> labels; public List<double> strat;
    }
    public class Decision { public int j, pts; public bool ok; public string tier, lab, stage; public int before, after; public HandInfo hand; }

    public class Run
    {
        public EventDef ev; public List<string> stages; public List<int> perS;
        public int si, h, stack = 100, total, combo, bestCombo, tip, travel;
        public bool bust, reentry, usedTicket, fumes, turbo; public string state = "ready", boss, cooler; public Showdown show;
        public List<Decision> results = new List<Decision>();
        public Rng rng; public Spot spot;
        public string libGid; public int libIndex = -1; public HashSet<int> seen = new HashSet<int>(); public List<string> usedTables = new List<string>();
        public List<(string slot, string id)> worn;
        public Prog prog;
        public List<MisSpot> drill;   // a study session: the spots you misplayed, dealt again in order
        public string Stage => stages[Math.Min(si, stages.Count - 1)];
        public bool ftIntro;   // the final-table scene has played
    }

    public class Result
    {
        public EventDef ev; public int n, place, paid, stageI, bestCombo, acc, streak; public double pct, before, after, prize, net, repaid, travel;
        public bool bust, quit, ticketUsed, reentry; public string boss, beat, met, ticket, cooler, stage;
        public List<string> titles = new List<string>(); public List<ItemDef> unlocked = new List<ItemDef>();
        public List<Decision> all = new List<Decision>(), keys = new List<Decision>(); public Prog prog;
        public int fixedN, drillN;   // a study session: spots put right, spots dealt
    }

    public static class Play
    {
        public const int Hands = 3, CashHands = 5;
        static State S => Game.State;

        public static bool LiveEv(EventDef ev) => !(ev.online || ev.daily || ev.kind == "sng");
        public static (List<string> stages, List<int> perS) PlanFor(EventDef ev, int ch)
        {
            if (ev.stages != null && ev.stages.Count > 0) return (ev.stages, ev.perS);
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
        public static int Per(Run run) => run.perS != null ? run.perS[Math.Min(run.si, run.perS.Count - 1)] : run.ev.kind == "cash" ? CashHands : Hands;

        // ------------------------------------------------------------ starting an event
        // reentry: firing another bullet the same day (no new game slot, no second hotel night)
        public static Run Start(string id, bool reentry = false)
        {
            var s = S; EventDef ev; LiveDef live = null;
            if (id == "daily") ev = Career.DailyEvent(s);
            else if (id.StartsWith("ol_")) { ev = Career.OnlineEvent(s, id); if (ev == null) return null; }
            else
            {
                live = Calendar.Live.FirstOrDefault(l => l.ev.id == id);
                var today = Calendar.OnToday(s, id);
                bool prologue = id == "local" && s.ch == 0 && !s.story.Contains("p_after");   // the first tournament always opens: it is the story, not the calendar
                ev = today != null ? today.ev : live != null && Calendar.LiveNow().Contains(live) ? live.ev : Game.AnyDay || prologue ? DB.Event(id) : null;
                if (ev == null || (live != null && Calendar.LivePlayed(s, live.id))) return null;
            }
            if (ev.id == "freeroll") { ev = ev.Copy(); ev.prizeBase = new[] { 0, 1, 1.5, 4, 8, 16, 16, 16 }[Math.Min(7, s.ch)]; }
            bool story = !ev.daily;
            if (story && !reentry && !Game.AnyDay && ev.id != "local" && ev.id != "classic2" && (ev.online ? Calendar.SlotsLeft(s).left <= 0 : Calendar.LiveLeft(s) <= 0)) return null;
            if (!Career.Afford(s, ev)) return null;
            if (ev.daily && Career.DailyDone(s)) return null;
            double travel = story ? Calendar.TravelFor(s, ev) : 0;
            bool usedTicket = false;
            if (s.Ticket(ev.id) > 0 && !reentry) { s.tickets[ev.id]--; usedTicket = true; }
            else if (!ev.free) Career.Add(s, -ev.buyin, (reentry ? "Re-entry: " : "Buy-in: ") + ev.name);
            if (story)
            {
                var c = s.cal;
                if (travel > 0) { Career.Add(s, -travel, "Hotel and travel"); if (!c.trav.ContainsKey(c.d)) c.trav[c.d] = new List<string>(); c.trav[c.d].Add(ev.venue); }
                if (reentry) { var k = $"{c.d}|{ev.id}"; c.re[k] = (c.re.TryGetValue(k, out var n) ? n : 0) + 1; }
                else if (!Game.AnyDay && ev.id != "classic2") Calendar.UseSlot(s, !ev.online);
                if (ev.invite) { c.inv = null; s.SetFlag("invites", s.FlagN("invites") + 1); Career.Moment(s, "📨", $"Sat in a private game: {ev.name}, hosted by {ev.host}"); }
                if (ev.id == "local" && s.ch == 0) c.d = 5;
            }
            s.lastEvent = id; s.Save();
            var seed = ev.daily ? $"daily|{Calendar.RealToday()}" : $"{id}|{DateTime.UtcNow.Ticks}|{UnityEngine.Random.value}";
            var plan = PlanFor(ev, s.ch);
            var run = new Run { ev = ev, stages = plan.stages, perS = plan.perS, rng = new Rng(seed), reentry = reentry, travel = (int)travel, usedTicket = usedTicket, turbo = ev.turbo };
            run.boss = PickBoss(s, ev, run.rng);
            return run;
        }
        // ------------------------------------------------------------ study: your misplayed spots, again
        // the same seat, the same stacks, the same action in front of you; a spot leaves the list once you get it right
        public static List<MisSpot> Misses(State s)
        {
            var seen = new HashSet<string>(); var outp = new List<MisSpot>();
            for (int i = s.pg.mis.Count - 1; i >= 0; i--) { var m = s.pg.mis[i]; if (Valid(m) && seen.Add(m.g + "|" + m.t + "|" + m.n + "|" + m.c)) outp.Add(m); }
            return outp;
        }
        static bool Valid(MisSpot m)
        {
            var pool = DB.Tables(m.g); if (pool == null || m.t < 0 || m.t >= pool.Count) return false;
            var nodes = J.L((Dictionary<string, object>)pool[m.t], "n"); if (nodes == null || m.n < 0 || m.n >= nodes.Count) return false;
            return string.Join(",", J.Strs(J.L(nodes[m.n], "p"))) == (m.p ?? "");   // the library was re-solved since: the spot moved
        }
        public static Run StartDrill(State s, int max = 8)
        {
            var mis = Misses(s).Take(max).ToList(); if (mis.Count == 0) return null;
            var ev = new EventDef { id = "drill", kind = "drill", name = "Study session", venue = "home", online = true, free = true, buyin = 0, blurb = "The spots you misplayed, dealt again." };
            var run = new Run { ev = ev, stages = new List<string> { "drill" }, perS = new List<int> { mis.Count }, rng = new Rng($"drill|{DateTime.UtcNow.Ticks}"), drill = mis };
            run.libGid = mis[0].g; run.libIndex = mis[0].t;
            return run;
        }
        static Spot DealMiss(Run run, MisSpot m)
        {
            var pool = DB.Tables(m.g); var table = (Dictionary<string, object>)pool[m.t]; var nodes = J.L(table, "n"); var node = (Dictionary<string, object>)nodes[m.n];
            var rows = J.L(node, "r"); var row = rows.FirstOrDefault(r => (int)J.Num(((List<object>)r)[0]) == m.c) ?? rows[0];
            run.libGid = m.g; run.libIndex = m.t; run.seen.Add(m.n);
            return BuildSpot(run, table, node, m.n, (List<object>)row, Difficulty(run));
        }
        static string PickBoss(State s, EventDef ev, Rng rng)
        {
            if (!string.IsNullOrEmpty(ev.boss)) return ev.boss;
            if (ev.legends != null && ev.legends.Count > 0)
            {
                var fresh = ev.legends.Where(id => !s.legends.Contains(id)).ToList(); var lp = fresh.Count > 0 ? fresh : ev.legends;
                return lp[rng.Int(lp.Count)];
            }
            // anywhere else, more often than not a familiar face makes the final table: the card room regulars, Slick, a legend
            if (ev.kind != "mtt" || ev.daily || ev.id == "local" || rng.Next() > 0.6) return null;
            var pool = new List<string> { "slick" };
            if (!ev.online && ev.venue == "cardroom") { pool.Add("trent"); pool.Add("trent"); pool.Add("lou"); pool.Add("lou"); }
            if (!ev.online && ev.venue == "casino") pool.Add("trent");
            foreach (var o in DB.List("DR_LEGENDS") ?? new List<object>()) { string id = J.S(o, "id"); if (!string.IsNullOrEmpty(id) && J.I(o, "ch") <= Math.Max(1, s.ch) + 1) pool.Add(id); }
            return pool[rng.Int(pool.Count)];
        }

        // ------------------------------------------------------------ a table for this stage
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
            if (run.drill != null) return;   // a study session deals its own spots
            string stage = run.Stage, gid = DB.StageGid(stage); var pool = DB.Tables(gid);
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
            var ev = run.ev; string stage = run.Stage; double d;
            if (ev.daily || ev.kind == "drill") d = 0.15 + 0.11 * Math.Min(7, S.ch);
            else d = new[] { 0.12, 0.22, 0.36, 0.5, 0.64, 0.78, 0.9, 1 }[Math.Min(7, ev.id == "main" ? 6 : ev.ch)];
            if (stage == "final" || stage == "deep") d += 0.08;
            if (run.boss != null) d += 0.05;
            return Math.Max(0, Math.Min(1, d));
        }

        // a hand from a pre-solved table: a real situation (weighted by how often it happens), then a hand that
        // makes it a decision. Each situation's hands are stored hardest-first; difficulty picks the window.
        public static Spot Deal(Run run)
        {
            if (run.drill != null) return DealMiss(run, run.drill[Math.Min(run.h, run.drill.Count - 1)]);
            var pool = DB.Tables(run.libGid); var table = (Dictionary<string, object>)pool[run.libIndex];
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
            return BuildSpot(run, table, node, pick, row, d);
        }
        static Spot BuildSpot(Run run, Dictionary<string, object> table, Dictionary<string, object> node, int ni, List<object> row, double d)
        {
            int cls = (int)J.Num(row[0]); var combos = DB.ClassCombos[cls]; var hand = combos[run.rng.Int(combos.Count)];
            var rows = J.L(node, "r"); int idx = rows.IndexOf(row);
            var v = J.O(node, "v"); string u = J.S(table, "u", "bb"); double k = J.N(table, "k", 1);
            var sp = new Spot
            {
                table = table, node = node, gid = run.libGid, tableIndex = run.libIndex, nodeIndex = ni, cls = cls, hero = J.I(node, "h"), hand = new[] { hand[0], hand[1] },
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

        // ------------------------------------------------------------ grading, Chess.com style
        static readonly Dictionary<string, double[]> LossSteps = new Dictionary<string, double[]> { { "bb", new[] { 0.05, 0.2, 0.6, 1.5 } }, { "icm", new[] { 0.015, 0.06, 0.18, 0.5 } } };
        static string hero0(Spot sp) => sp.seats != null && sp.hero >= 0 && sp.hero < sp.seats.Count ? sp.seats[sp.hero].pos : "";
        public static (string lab, double? loss, int bestIdx) LabelFor(List<double> strat, List<double> ev, int j, string unit, double hard, string kind)
        {
            double fmax = strat.Max(); int sb = strat.IndexOf(fmax); double mine = strat[j];
            double? loss = null; int best = sb;
            if (ev != null && ev.Count == strat.Count && ev.All(x => !double.IsNaN(x) && !double.IsInfinity(x))) { best = ev.IndexOf(ev.Max()); loss = Math.Max(0, ev[best] - ev[j]); }
            var T = LossSteps.TryGetValue(unit, out var t) ? t : LossSteps["bb"];
            string lab;
            if (loss == null) { double rel = fmax > 0 ? mine / fmax : 0; lab = mine < 0.05 ? (rel < 0.02 ? "blunder" : "mistake") : rel >= 0.9 ? "best" : rel >= 0.5 ? "good" : "inaccuracy"; }
            else { double l = loss.Value; lab = l <= 1e-9 ? "best" : l <= T[0] ? "excellent" : l <= T[1] ? "good" : l <= T[2] ? "inaccuracy" : l <= T[3] ? "mistake" : "blunder"; }
            // the solver's mix is the truth: a play it makes often is never worse than Good
            if (mine >= 0.6 || j == sb) lab = (lab == "best" || lab == "excellent") ? lab : "best";
            else if (mine >= 0.25 && (lab == "inaccuracy" || lab == "mistake" || lab == "blunder")) lab = "good";
            else if (mine < 0.05 && (lab == "best" || lab == "excellent" || lab == "good")) lab = "inaccuracy";
            // a clear answer and you picked something it almost never does: a real error, not a slip
            if (mine <= 0.12 && fmax >= 0.7) lab = fmax >= 0.88 && mine <= 0.04 && (loss == null || loss > T[0]) ? (lab == "blunder" || loss == null || loss > T[1] ? "blunder" : "mistake") : lab == "blunder" ? "blunder" : "mistake";
            // rare on purpose: only the very closest spots, and Brilliant only for a brave play
            if (lab == "best" && hard >= 0.95) lab = (kind == "allin" || kind == "raise" || kind == "raiseB" || kind == "call") && mine <= 0.8 ? "brilliant" : "great";
            return (lab, loss, best);
        }

        public static Decision Answer(Run run, int j)
        {
            var sp = run.spot; var strat = sp.strat;
            var ev = sp.ev != null && sp.evScale != 1 ? sp.ev.Select(x => x * sp.evScale).ToList() : sp.ev;
            var g = LabelFor(strat, ev, j, sp.unit == "bbicm" ? "bb" : sp.unit, sp.hard, sp.acts[j]);
            // the limp angle: the solver completes from the small blind and you raised — the conventional play, not a
            // leak: partial credit (never worse than an inaccuracy) and the limp gets explained
            bool limpSpot = sp.acts[g.bestIdx] == "call" && sp.path.Count(k => k != "fold") == 0 && (sp.acts[j] == "raise" || sp.acts[j] == "raiseB") && hero0(sp) == "SB";
            if (limpSpot && (g.lab == "mistake" || g.lab == "blunder")) g.lab = "inaccuracy";
            var L = DB.Labels[g.lab];
            string tier = L.tier; bool ok = tier != "bad"; int pts = L.score - (tier == "bad" ? 60 : 0);
            int before = run.stack; if (run.drill == null) run.stack = Math.Max(0, Math.Min(200, run.stack + L.life));
            run.combo = ok && tier != "close" ? run.combo + 1 : 0; run.bestCombo = Math.Max(run.bestCombo, run.combo);
            var hero = sp.seats[sp.hero]; int best = g.bestIdx;
            Odds odds = null; int ci = sp.acts.IndexOf("call");
            if (ci >= 0)
            {
                var m = System.Text.RegularExpressions.Regex.Match(sp.Label(ci), @"([\d.]+)");
                if (m.Success && double.TryParse(m.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var amt0))
                {
                    double amt = amt0 - hero.put;
                    if (amt > 0 && sp.pot > 0) odds = new Odds { call = Math.Round(amt * 10) / 10, pot = Math.Round(sp.pot * 10) / 10, need = (int)Math.Round(amt / (sp.pot + amt) * 100) };
                }
            }
            var hand = new HandInfo
            {
                cards = sp.hand, pos = hero.pos, bb = Math.Round((hero.stack - hero.put) * 10) / 10, cat = sp.cat, mine = sp.Label(j), best = sp.Label(best), mineK = sp.acts[j], bestK = sp.acts[best], limp = limpSpot,
                bestF = (int)Math.Round(strat[best] * 100), myF = (int)Math.Round(strat[j] * 100), odds = odds, labels = Enumerable.Range(0, sp.acts.Count).Select(sp.Label).ToList(),
                heroCls = sp.cls, lab = g.lab, loss = g.loss, unit = sp.unit, strat = strat.ToList(), facing = sp.path.Count(k => k != "fold"),
            };
            var r = new Decision { j = j, pts = pts, ok = ok, tier = tier, lab = g.lab, stage = run.Stage, before = before, after = run.stack, hand = hand };
            run.total += pts; run.results.Add(r);
            Progression.Track(r, run);
            if (run.stack <= 0) run.bust = true;
            run.state = "answered";
            return r;
        }
        public static int Accuracy(List<Decision> rs) => rs.Count == 0 ? 0 : (int)Math.Round(rs.Average(r => (double)(DB.Labels.TryGetValue(r.lab, out var L) ? L.score : r.ok ? 80 : 10)));

        // ------------------------------------------------------------ between hands
        static readonly List<string> Coolers = new List<string>();
        public static string CoolerLine(Run run) { var l = DB.Strings("DR_COOLERS"); return l[run.rng.Int(l.Count)]; }
        public static bool CoolerRoll(Run run)
        {
            var ev = run.ev; double p = Math.Max(0.02, Math.Min(0.22, 0.05 + 0.025 * Math.Log10(Math.Max(2, ev.field)) - (run.stack - 100) / 700.0));
            return run.rng.Next() < p;
        }
        // returns what happens next: "hand", "level", "cooler" or "finish"
        public static string Next(Run run)
        {
            if (run.bust) return "finish";
            int per = Per(run);
            if (++run.h < per) return "hand";
            run.h = 0;
            if (run.si + 1 < run.stages.Count)
            {
                if (run.ev.kind != "cash" && run.drill == null && CoolerRoll(run)) { run.bust = true; run.cooler = CoolerLine(run); run.state = "cooler"; return "cooler"; }
                run.si++; run.tip++; return "level";
            }
            run.si++; return "finish";
        }
        public static double CashNet(Run run, double luck) => Math.Round(run.ev.buyin * Math.Max(-1, Math.Min(2.5, (run.stack - 100) / 40.0 + luck)));

        // ------------------------------------------------------------ where you finish
        public static int Paid(EventDef ev)
        {
            if (ev.kind == "sat") return ev.seats;
            var p = DB.Payouts(ev.id == "classic2" ? 212 : ev.field); return p != null ? J.I(p, "paid") : Math.Max(1, ev.field / 7);   // Day 2 pays the whole Classic's prize pool
        }
        public static double PrizeFor(EventDef ev, int place)
        {
            var p = DB.Payouts(ev.id == "classic2" ? 212 : ev.field); if (p == null) return 0;
            var am = J.Nums(J.L(p, "amounts")); if (place < 1 || place > am.Count) return 0;
            return Math.Round(am[place - 1] * (ev.prizeBase > 0 ? ev.prizeBase : ev.buyin));
        }
        public static int Place(Run run, EventDef ev)
        {
            var rng = run.rng; int N = Math.Max(1, ev.field);
            int Between(int a, int b) => Math.Max(1, Math.Min(N, a + rng.Int(Math.Max(1, b - a + 1))));
            if (ev.kind == "sat")
            {
                int Ps = ev.seats;
                if (!run.bust) return Between(1, Ps);
                return run.Stage == "early" ? Between(Ps + 6, N) : run.h == 0 ? Ps + 1 : Between(Ps + 1, Ps + 5);
            }
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

        // ------------------------------------------------------------ the results
        public static Result Finish(Run run, bool quit)
        {
            var s = S; var ev = run.ev;
            if (run.drill != null) return FinishDrill(run, s);
            if (quit && ev.kind != "cash") run.bust = true;
            int n = run.results.Count; double pct = n > 0 ? run.total / (100.0 * n) : 0;
            int perfect = run.results.Count(r => r.tier == "perfect"), good = run.results.Count(r => r.tier == "good"), close = run.results.Count(r => r.tier == "close"), bad = run.results.Count(r => r.tier == "bad");
            s.dec += n; s.perfect += perfect; s.good += good; s.close += close; s.mistakes += bad; s.played++;
            if (!s.events.TryGetValue(ev.id, out var rec)) s.events[ev.id] = rec = new EventRec();
            rec.played++;
            var keyHands = run.results.OrderBy(r => r.pts).ToList();
            var res = new Result
            {
                ev = ev, n = n, pct = pct, all = run.results.ToList(), acc = Accuracy(run.results), bestCombo = run.bestCombo, quit = quit, boss = run.boss,
                before = s.br + (ev.free || run.usedTicket ? 0 : ev.buyin) + run.travel, stageI = run.si, bust = run.bust, ticketUsed = run.usedTicket, reentry = run.reentry, travel = run.travel,
            };
            res.keys = keyHands.Where(r => r.tier == "bad").Take(2).Concat(keyHands.Where(r => r.tier == "perfect" && r.hand.bestF < 80).Reverse().Take(1)).Take(3).ToList();
            if (ev.kind == "sat")
            {
                int place = Place(run, ev); res.place = place; res.paid = ev.seats;
                rec.best = rec.best > 0 ? Math.Min(rec.best, place) : place;
                if (place <= ev.seats) { var tgt = DB.Event(ev.target); Career.Moment(s, "🎟", $"Won a seat into the {tgt?.name}"); s.tickets[ev.target] = s.Ticket(ev.target) + 1; res.ticket = ev.target; rec.wins++; s.SetFlag("satWin"); }
                res.net = -(run.usedTicket ? 0 : ev.buyin);
            }
            else if (ev.kind == "cash")
            {
                double net = run.bust ? -ev.buyin : CashNet(run, quit ? 0 : (run.rng.Next() - 0.5) * 0.3 + ev.soft * 0.5);
                Career.Add(s, ev.buyin + net, "Cashed out: " + ev.name);
                res.net = net; res.prize = ev.buyin + net; rec.net += net;
                if (net > 0) { s.SetFlag("cashWin"); res.repaid = Career.Repay(s, net); }
            }
            else
            {
                int P = Paid(ev), place = Place(run, ev); double prize = place <= P ? PrizeFor(ev, place) : 0;
                res.place = place; res.paid = P; res.prize = prize; res.net = prize - (ev.free ? 0 : ev.buyin);
                rec.best = rec.best > 0 ? Math.Min(rec.best, place) : place;
                if (prize > 0 && s.cashes == 0) Career.Moment(s, "💵", $"First cash: {Fmt.Ord(place)} in the {ev.name}");
                if (prize > 0) { Career.Add(s, prize, $"{Fmt.Ord(place)} in {ev.name}"); s.cashes++; rec.cashes++; rec.cash = Math.Max(rec.cash, prize); if (ev.ch == 2) s.SetFlag("circuitCash"); res.repaid = Career.Repay(s, res.net); }
                rec.net += res.net;
                bool reachedFinal = !run.bust || run.Stage == "final";
                res.cooler = run.cooler; res.stage = run.Stage;
                if (reachedFinal && run.si >= run.stages.Count - 1) { s.ft++; if (ev.id == "main") s.SetFlag("meft"); if (run.boss != null) { s.met.Add(run.boss); res.met = run.boss; } }
                if (place == 1)
                {
                    rec.wins++;
                    if (!string.IsNullOrEmpty(ev.title))
                    {
                        s.titles[ev.title] = s.Title(ev.title) + 1; res.titles.Add(ev.title);
                        s.trophies.Insert(0, new Trophy { t = ev.title, ev = ev.name, prize = prize, d = Career.Now() });
                        Career.Moment(s, DB.TitleIcon(ev.title), $"Won the {ev.name} for ${Fmt.Money(prize)}");
                    }
                    else if (!ev.daily) Career.Moment(s, "🏅", $"Won the {ev.name}");
                    if (run.boss != null && DB.Legend(run.boss) != null) { if (!s.legends.Contains(run.boss)) Career.Moment(s, "🂡", $"Beat {J.S(DB.Legend(run.boss), "name")} heads-up"); s.legends.Add(run.boss); res.beat = run.boss; }
                    if (run.boss == "ghost") Career.Moment(s, "🕶️", "Beat Bio The Ghost for the world title");
                    if (run.boss == "trent") { s.SetFlag("trentW", s.FlagN("trentW") + 1); Career.Moment(s, "😏", $"Beat Mustich heads-up ({s.FlagN("trentW")} and counting)"); }
                    void Once(string id) { if (!s.story.Contains(id) && !s.queue.Contains(id)) s.queue.Add(id); }
                    if (ev.title == "room") Once("trentbeat");
                    if (s.TitleCount == 1) Once("firstwin");
                    if (ev.title == "ring" && s.Title("ring") == 1) Once("ring1");
                    if (ev.title == "bracelet" && s.Title("bracelet") == 1) Once("bracelet1");
                    if ((ev.title == "tour" || ev.title == "glo") && s.Title("tour") + s.Title("glo") == 1) Once("tour1");
                    if (ev.title == "titan" && s.Title("titan") == 1) Once("titan1");
                    if (ev.id == "main") Once("champ");
                }
            }
            if (ev.daily)
            {
                string today = Calendar.RealToday(), yday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                s.streak = s.daily.ContainsKey(yday) ? s.streak + 1 : 1; s.bestStreak = Math.Max(s.bestStreak, s.streak);
                s.daily[today] = new List<double> { res.place, res.prize }; res.streak = s.streak;
                foreach (var k in s.daily.Keys.OrderBy(k => k).Take(Math.Max(0, s.daily.Count - 120)).ToList()) s.daily.Remove(k);
            }
            // the aftermath, as a scene in the venue (the first tournament too: it's where the story turns)
            // the Classic films its own: bagging chips after Day 1, and the winner's moment after Day 2
            if (ev.id == "classic" && res.ticket != null) s.queue.Add("classic_day1");
            else if (ev.id == "classic2" && res.place != 1) s.queue.Add(Story.ClassicLoss(s, res.met));
            else if (ev.id == "classic2") { }
            else if (Story.FtEnd(res) is string ft) s.queue.Insert(0, ft);   // the final hand against whoever was waiting at the final table
            else if (!ev.daily && (s.ch >= 1 || ev.id == "local"))
            {
                string after = run.cooler != null ? "after_cooler" : res.place == 1 && ev.kind != "cash" ? "after_win" : res.prize > 0 || (ev.kind == "cash" && res.net > 0) ? "after_cash" : "after_bust";
                if (DB.Scenes.TryGetValue(after, out var sc)) { sc.place = string.IsNullOrEmpty(ev.venue) || ev.online ? "home" : ev.venue; Story.Aftermath(s, res, run, sc, after); s.queue.Add(after); }
                s.SetFlag("events", s.FlagN("events") + 1);
            }
            if (ev.id == "local" && !s.story.Contains("p_after") && !s.queue.Contains("p_after")) s.queue.Add("p_after");
            if (run.cooler != null && s.ch >= 1 && !s.story.Contains("badbeat") && !s.queue.Contains("badbeat")) s.queue.Add("badbeat");
            if (run.cooler == null && !ev.daily && res.prize <= 0 && s.ch >= 1 && s.ch <= 5) { string les = "lesson" + s.ch; if (DB.Scenes.ContainsKey(les) && !s.story.Contains(les) && !s.queue.Contains(les)) s.queue.Add(les); }
            if (s.peak >= 100000 && !s.story.Contains("derek") && !s.queue.Contains("derek")) s.queue.Add("derek");
            res.after = s.br;
            res.unlocked = Career.Unlocks(s, ev.name);
            if (!string.IsNullOrEmpty(ev.live)) { var it = Calendar.LiveReward(s, ev); if (it != null) res.unlocked.Add(it); }
            Career.Progress(s);
            Progression.EventDone(res, run);
            s.Save();
            return res;
        }

        // a study session ends: the spots you got right leave the list; no money moves, the calendar doesn't care
        static Result FinishDrill(Run run, State s)
        {
            int n = run.results.Count; var res = new Result { ev = run.ev, n = n, all = run.results.ToList(), acc = Accuracy(run.results), bestCombo = run.bestCombo, before = s.br, after = s.br, stage = "drill", drillN = run.drill.Count };
            for (int i = 0; i < run.results.Count && i < run.drill.Count; i++)
            {
                var r = run.results[i]; var m = run.drill[i];
                if (r.tier == "perfect" || r.tier == "good") { res.fixedN++; s.pg.mis.RemoveAll(x => x.g == m.g && x.t == m.t && x.n == m.n && x.c == m.c); }
            }
            res.keys = run.results.Where(r => r.tier == "bad").Take(3).ToList();
            s.dec += n; s.perfect += run.results.Count(r => r.tier == "perfect"); s.good += run.results.Count(r => r.tier == "good"); s.close += run.results.Count(r => r.tier == "close"); s.mistakes += run.results.Count(r => r.tier == "bad");
            if (run.prog != null) { Progression.GainXp(s, 10 * res.fixedN, run.prog); res.prog = run.prog; }
            s.SetFlag("drills", s.FlagN("drills") + 1); s.SetFlag("fixed", s.FlagN("fixed") + res.fixedN);
            s.Save();
            return res;
        }

        // Slick's one-line lesson after a mistake or a borderline play
        public static string Tip(Decision r)
        {
            var h = r.hand; if (h == null || r.tier == "perfect" || r.tier == "good") return "";
            bool icm = r.stage == "bubble" || r.stage == "satbubble" || r.stage == "deep" || r.stage == "final";
            bool loose = h.mineK != "fold" && h.bestK == "fold", tight = h.mineK == "fold" && h.bestK != "fold"; string c = h.cat;
            if (h.limp) return $"Raising is fine here. The solver completes from the small blind with this hand instead: at {h.bb} bb the limp keeps the pot small, lets you see a flop with your whole range and leaves a raise behind for when the big blind attacks. Limping is a small-blind-only play; from any other seat, open-raise.";
            if (!loose && !tight) return $"Right idea, wrong size. The solver prefers {h.best} here.";
            if (c == "Facing a jam") return loose ? "Calling a jam takes a much stronger hand than shoving one." + (icm ? " Near the money, stronger still: busting costs more than chips." : "") : "This hand is ahead of their shoving range often enough. Folding gives up too much." + (icm ? " Even with ICM, this one is a call." : "");
            if (c == "First in / vs limps") return loose ? $"Too loose from {h.pos}. With players still to act behind you, open fewer hands." : $"This is a profitable open from {h.pos}. Folding lets the blinds walk.";
            if (c == "Facing a raise") return loose ? "Facing a raise, play tighter than when you open. Their range is already strong." : $"Good enough to continue from {h.pos}. Folding here is too tight.";
            return loose ? "Against a 3-bet or more, only the top of your range keeps going." : "You're folding too much to aggression. This hand wants to continue.";
        }
    }
}
