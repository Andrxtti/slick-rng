// Money, chapters, unlocks and reputation: the career around the hands.
using System;
using System.Collections.Generic;
using System.Linq;

namespace CFDream
{
    public static class Career
    {
        public static State S => Game.State;

        public static void Add(State s, double amt, string why)
        {
            s.br = Math.Max(0, Math.Round(s.br + amt)); s.peak = Math.Max(s.peak, s.br);
        }
        // profit pays back Slick's stake first (half of it until the debt is gone)
        public static double Repay(State s, double profit)
        {
            if (s.debt <= 0 || profit <= 0) return 0;
            double pay = Math.Min(s.debt, Math.Round(profit / 2)); s.debt -= pay; Add(s, -pay, "Paid Slick back"); return pay;
        }
        public static double StakeSize(State s) => new double[] { 300, 300, 1500, 6000, 25000, 120000, 60000, 150000 }[Math.Min(7, s.ch)];
        public static void Moment(State s, string icon, string text)
        {
            s.timeline.Add(new Moment { d = Now(), icon = icon, text = text });
            if (s.timeline.Count > 150) s.timeline.RemoveRange(0, s.timeline.Count - 150);
        }
        public static double Now() => (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;

        // ------------------------------------------------------------ what unlocks each item (never bought)
        static readonly Dictionary<string, string> Rule = new Dictionary<string, string>
        {
            { "outfit:office", "" }, { "outfit:tee", "" }, { "outfit:cardigan", "played:3" }, { "outfit:jersey", "dec:100" }, { "outfit:denim", "flag:cashWin" }, { "outfit:track", "streak:3" },
            { "outfit:vest", "peak:10000" }, { "outfit:varsity", "flag:circuitCash" }, { "outfit:leather", "peak:50000" }, { "outfit:fleece", "ch:3" }, { "outfit:hoodie", "played:1" },
            { "outfit:polo", "ch:1" }, { "outfit:bomber", "titles:1" }, { "outfit:blazer", "ch:2" }, { "outfit:suit", "title:bracelet" }, { "outfit:designer", "peak:1000000" }, { "outfit:tux", "flag:meft" },
            { "eyes:none", "" }, { "eyes:readers", "" }, { "eyes:round", "dec:50" }, { "eyes:thick", "played:2" }, { "eyes:sport", "peak:25000" }, { "eyes:tinted", "flag:circuitCash" },
            { "eyes:aviator", "title:ring" }, { "eyes:shades", "titles:1" },
            { "head:none", "" }, { "head:cap", "played:5" }, { "head:snap", "ch:1" }, { "head:bucket", "played:10" }, { "head:cowboy", "title:ring" }, { "head:fedora", "peak:500000" },
            { "head:phones", "dec:100" }, { "head:beanie", "ch:3" }, { "head:visor", "streak:7" },
            { "ear:none", "" }, { "ear:stud", "peak:2500" }, { "ear:hoop", "peak:15000" }, { "ear:diamond", "peak:75000" },
            { "ring:none", "" }, { "ring:signet", "peak:20000" }, { "ring:gold", "title:bracelet" }, { "ring:pinky", "peak:500000" }, { "ring:champ", "title:me" },
            { "wrist:none", "" }, { "wrist:beads", "played:4" }, { "wrist:gold", "peak:40000" }, { "wrist:tennis", "peak:400000" },
            { "sleeve:none", "" }, { "sleeve:grind", "streak:5" }, { "sleeve:site", "flag:circuitCash" }, { "sleeve:energy", "title:bracelet" }, { "sleeve:global", "title:me" },
            { "watch:digital", "" }, { "watch:steel", "peak:25000" }, { "watch:gold", "peak:250000" }, { "watch:iced", "peak:2000000" }, { "watch:hyper", "title:me" },
            { "chain:none", "" }, { "chain:silver", "peak:10000" }, { "chain:cuban", "peak:100000" }, { "chain:rope", "peak:250000" }, { "chain:layered", "title:bracelet" }, { "chain:iced", "peak:1000000" },
            { "home:studio", "" }, { "home:apt", "peak:25000" }, { "home:condo", "peak:150000" }, { "home:penthouse", "peak:1000000" }, { "home:mansion", "peak:5000000" }, { "home:island", "title:me" },
            { "ride:bus", "" }, { "ride:hatch", "peak:5000" }, { "ride:coupe", "peak:50000" }, { "ride:sedan", "peak:250000" }, { "ride:super", "peak:2000000" }, { "ride:jet", "title:titan" },
            { "bottom:khakis", "" }, { "bottom:jeans", "" }, { "bottom:blackjeans", "played:3" }, { "bottom:sweats", "played:1" }, { "bottom:joggers", "streak:3" }, { "bottom:trackpants", "peak:10000" },
            { "bottom:shorts", "flag:cashWin" }, { "bottom:cargo", "played:5" }, { "bottom:slacks", "ch:2" }, { "bottom:plaidpants", "title:ring" }, { "bottom:tuxpants", "flag:meft" },
            { "shoes:sneakers", "" }, { "shoes:runners", "played:3" }, { "shoes:highs", "streak:3" }, { "shoes:slides", "flag:cashWin" }, { "shoes:loafers", "ch:2" }, { "shoes:dress", "peak:50000" }, { "shoes:boots", "title:ring" }, { "shoes:icy", "peak:1000000" },
            { "pet:cat", "played:5" }, { "pet:pup", "titles:1" }, { "pet:frenchie", "title:ring" }, { "pet:doberman", "peak:1000000" },
            { "patch:none", "" }, { "patch:grind", "streak:3" }, { "patch:tavern", "flag:cashWin" }, { "patch:site", "title:ring" }, { "patch:energy", "title:bracelet" }, { "patch:global", "title:me" },
        };
        // brand packs add their own items with the same rule codes (plus "redeem": only a code unlocks it)
        public static void AddRule(string key, string rule) { Rule[key] = rule ?? "redeem"; }

        public static bool Test(State s, ItemDef it)
        {
            if (!Rule.TryGetValue(it.Key, out var r)) return false;
            if (r == "") return true;
            var p = r.Split(':'); string k = p[0], v = p.Length > 1 ? p[1] : "";
            switch (k)
            {
                case "played": return s.played >= int.Parse(v);
                case "dec": return s.dec >= int.Parse(v);
                case "peak": return s.peak >= double.Parse(v);
                case "flag": return s.Flag(v);
                case "streak": return s.bestStreak >= int.Parse(v);
                case "ch": return s.ch >= int.Parse(v);
                case "titles": return s.TitleCount >= int.Parse(v);
                case "title": return s.Title(v) >= 1;
                case "level": return Progression.LevelOf(s.pg.xp).lvl >= int.Parse(v);
                case "event": return s.events.TryGetValue(v, out var er) && er.played > 0;
                case "won": return s.events.TryGetValue(v, out var ew) && ew.wins > 0;
                case "redeem": return false;
            }
            return false;
        }

        static readonly HashSet<string> NoAutoWear = new HashSet<string> { "head", "sleeve", "home", "ride" };
        public static List<ItemDef> Unlocks(State s, string at = null)
        {
            var fresh = new List<ItemDef>();
            foreach (var it in DB.Items)
            {
                if (s.owned.Contains(it.Key) || !Test(s, it)) continue;
                s.owned.Add(it.Key);
                Fashion.GotIt(s, it.Key, it.req == "Where it all started" ? "Day one" : at ?? "Storyline");
                if (!string.IsNullOrEmpty(it.req) && it.req != "Where it all started" && it.id != "none") fresh.Add(it);
                // wear the newest thing you earn (later in the list is the upgrade); homes and rides cost rent, so those are your call
                int cur = DB.Items.FindIndex(x => x.slot == it.slot && x.id == s.Eq(it.slot)), mine = DB.Items.IndexOf(it);
                bool wearingLeague = DB.LeagueItems.Any(x => x.slot == it.slot && x.id == s.Eq(it.slot));
                if (it.id != "none" && mine > cur && !NoAutoWear.Contains(it.slot) && !wearingLeague) s.eq[it.slot] = it.id;
            }
            foreach (var f in fresh) s.newItems.Add(f.Key);
            return fresh;
        }

        // ------------------------------------------------------------ chapters
        public static bool ChapterDone(State s, int ch)
        {
            switch (ch)
            {
                case 0: return s.story.Contains("p_after");
                case 1: return s.Title("room") > 0 || s.br >= 10000;
                case 2: return s.Title("ring") >= 2 && s.br >= 40000;
                case 3: return s.Title("bracelet") > 0;
                case 4: return s.Title("tour") + s.Title("glo") >= 2 && s.br >= 750000;
                case 5: return s.Title("titan") >= 2 && s.br >= 5e6;
                case 6: return s.Title("me") > 0;
                default: return false;
            }
        }
        public static (double frac, string text) ChapterProg(State s)
        {
            int legends = s.legends.Count, total = DB.List("DR_LEGENDS").Count;
            switch (s.ch)
            {
                case 0: return (s.story.Contains("p_after") ? 1 : 0, "");
                case 1: return (Math.Min(1, s.br / 10000), $"${Fmt.Money(s.br)} of $10,000");
                case 2: return ((Math.Min(2, s.Title("ring")) / 2.0 + Math.Min(1, s.br / 40000)) / 2, $"💍 {Math.Min(2, s.Title("ring"))}/2 rings · ${Fmt.Money(s.br)} of $40,000");
                case 3: return (Math.Min(1, s.Title("bracelet")), s.Title("bracelet") > 0 ? "bracelet won" : "no bracelet yet");
                case 4: { int t = Math.Min(2, s.Title("tour") + s.Title("glo")); return ((t / 2.0 + Math.Min(1, s.br / 750000)) / 2, $"👑 {t}/2 titles · ${Fmt.Short(s.br)} of $750K"); }
                case 5: return ((Math.Min(2, s.Title("titan")) / 2.0 + Math.Min(1, s.br / 5e6)) / 2, $"🔱 {Math.Min(2, s.Title("titan"))}/2 Titans · ${Fmt.Short(s.br)} of $5M");
                case 6: return (s.Title("me") > 0 ? 1 : 0, s.Title("me") > 0 ? "World Champion" : "not yet");
                default: return (((double)legends / total + Math.Min(1, s.Title("bracelet") / 15.0)) / 2, $"{legends}/{total} legends · {s.Title("bracelet")}/15 bracelets");
            }
        }
        // ------------------------------------------------------------ online: the lobby (Casual, Ranked, League)
        public class OnlineGame { public string id, name, blurb, tier, note; public bool soon; public EventDef ev; }
        static double OnlineBuyin(State s, double baseBuyin) => Math.Round(baseBuyin * new[] { 1, 1, 3, 10, 25, 50, 100, 200 }[Math.Min(7, Math.Max(0, s.ch))]);
        static EventDef Quick(State s, string id, string name, string blurb, int field, int paid, List<string> stages, List<int> perS, double buyin, bool turbo = false)
            => new EventDef { id = "ol_" + id, kind = "sng", name = name, blurb = blurb, venue = "cftilt", online = true, field = field, seats = paid, buyin = OnlineBuyin(s, buyin), ch = Math.Max(1, s.ch), turbo = turbo, stages = stages, perS = perS };
        public static List<OnlineGame> Online(State s)
        {
            var L = new List<OnlineGame>();
            L.Add(new OnlineGame { id = "sng", tier = "Casual", name = "Quick SnG", blurb = "A nine-handed single table in ten hands: early, bubble, final.", ev = Quick(s, "sng", "Quick SnG", "Nine players, three paid. Ten hands.", 9, 3, new List<string> { "early", "bubble", "final" }, new List<int> { 3, 3, 4 }, 11) });
            L.Add(new OnlineGame { id = "hyper6", tier = "Casual", name = "6-Max Hyper", blurb = "Six players, two paid. Blinds fly.", ev = Quick(s, "hyper6", "6-Max Hyper", "Six players, two paid. About three minutes.", 6, 2, new List<string> { "early", "final" }, new List<int> { 3, 4 }, 7, true) });
            L.Add(new OnlineGame { id = "trivia", tier = "Casual", name = "Poker Trivia", blurb = "Family-feud style. Three rounds: 5, 5 and the big one.", soon = true, note = "Coming" });
            L.Add(new OnlineGame { id = "icm", tier = "Casual", name = "ICMirighhht?", blurb = "Spot the ICM blunder before the clock does.", soon = true, note = "Coming" });
            L.Add(new OnlineGame { id = "mosh", tier = "Ranked", name = "Mosh Pit", blurb = "An 18-player double table that is all bubble and final table. Shove-fest.", ev = Quick(s, "mosh", "Mosh Pit", "Eighteen runners, four paid, and it starts on the bubble.", 18, 4, new List<string> { "bubble", "final" }, new List<int> { 6, 6 }, 33, true) });
            L.Add(new OnlineGame { id = "dbl18", tier = "Ranked", name = "Double Table", blurb = "Two tables down to one. The long one.", ev = Quick(s, "dbl18", "Double Table", "Eighteen players, four paid. Two tables down to one.", 18, 4, new List<string> { "early", "middle", "bubble", "final" }, new List<int> { 3, 3, 3, 3 }, 22) });
            L.Add(new OnlineGame { id = "hu", tier = "Ranked", name = "Heads-Up Turbo", blurb = "One opponent, turbo blinds, first to the felt.", soon = true, note = "Coming" });
            L.Add(new OnlineGame { id = "3max", tier = "Ranked", name = "Heads-Up 3-Max", blurb = "Three players, winner takes it.", soon = true, note = "Coming" });
            L.Add(new OnlineGame { id = "league", tier = "League", name = "Season League", blurb = "Live-service calendar, divisions, promotion and relegation, full-league SnGs that launch when 90% ready up.", soon = true, note = "Coming" });
            return L;
        }
        public static EventDef OnlineEvent(State s, string id) => Online(s).FirstOrDefault(g => g.ev != null && g.ev.id == id)?.ev;

        // after anything happens: next chapter, rock bottom
        public static void Progress(State s)
        {
            if (s.ch >= 1 && s.ch < 7 && ChapterDone(s, s.ch) && !s.story.Contains("c" + (s.ch + 1)) && !s.queue.Contains("c" + (s.ch + 1))) s.queue.Add("c" + (s.ch + 1));
            // going broke: Slick lays out the options, unless you took three shots above your roll first
            if (s.ch >= 1 && Calendar.Broke(s) && s.FlagN("brokeNow") <= 0 && !s.queue.Contains("broke") && !s.queue.Contains("broke_told"))
            { Moment(s, "🥧", "Went broke"); s.SetFlag("brokeNow"); s.queue.Add(s.FlagN("shots") >= 3 || s.FlagN("slick_off") > 0 ? "broke_told" : "broke"); }
            if (s.FlagN("brokeNow") > 0 && s.br >= 400) s.SetFlag("brokeNow", 0);
            if (s.ch == 2)
            {
                if (s.Title("ring") >= 2 && s.br < 40000 && !s.story.Contains("c2_roll") && !s.queue.Contains("c2_roll")) s.queue.Add("c2_roll");
                if (s.br >= 40000 && s.Title("ring") < 2 && !s.story.Contains("c2_rings") && !s.queue.Contains("c2_rings")) s.queue.Add("c2_rings");
            }
            if (s.FlagN("slick_off") > 0 && s.br >= 10000 && !s.queue.Contains("slick_back")) s.queue.Add("slick_back");
            // keeping the day job: David leans on you at every milestone, and a quarter million settles it
            if (s.ch >= 1 && s.job == "nights")
            {
                void Once(string id, bool when) { if (when && !s.story.Contains(id) && !s.queue.Contains(id)) s.queue.Insert(0, id); }   // David first: his text comes before the next chapter opens
                Once("david1", s.peak >= 10000); Once("david2", s.peak >= 25000); Once("david3", s.peak >= 50000); Once("david4", s.peak >= 100000);
                Once("quit250", s.peak >= 250000);
            }
        }

        // the line under your name, earned as you climb
        public static string Rep(State s)
        {
            if (s.Title("me") > 0 && s.Title("bracelet") >= 15) return "The New GOAT";
            if (s.Title("me") > 0) return "World Champion";
            if (s.Title("titan") > 0) return "Titan";
            if (s.Title("tour") + s.Title("glo") > 0) return "Tour Champion";
            if (s.Title("bracelet") > 0) return "Bracelet Winner";
            if (s.Title("ring") > 0) return "Circuit Ring Winner";
            if (s.ch >= 2) return "Circuit Grinder";
            if (s.Title("room") > 0) return "Biscayne Champion";
            if (s.ch >= 1) return s.job == "quit" ? "Full-time Dreamer" : "Moonlighter";
            return "Cubicle Dreamer";
        }

        // ------------------------------------------------------------ what it costs to sit down
        public static double CostToday(State s, EventDef ev) => (ev.free || s.Ticket(ev.id) > 0 ? 0 : ev.buyin) + Calendar.TravelFor(s, ev);
        public static bool Afford(State s, EventDef ev) => s.br >= CostToday(s, ev);
        public static (string k, string label) Risk(State s, EventDef ev)
        {
            if (ev.free) return ("free", "Free seat");
            if (s.Ticket(ev.id) > 0) return ("free", "🎟 You have a seat");
            double cost = CostToday(s, ev);
            if (s.br < cost) return ("no", $"Need ${Fmt.Money(cost)}");
            double r = cost / Math.Max(1, s.br);
            return r <= 0.05 ? ("safe", "Safe for your roll") : r <= 0.2 ? ("shot", "Taking a shot") : ("yolo", "Betting the dream");
        }
        // the daily freeroll pays like a small tournament at your level, funded by the sponsor
        public static EventDef DailyEvent(State s) { var e = DB.Daily.Copy(); e.prizeBase = new double[] { 15, 15, 60, 250, 1000, 4000, 2000, 5000 }[Math.Min(7, s.ch)]; return e; }
        public static bool DailyDone(State s) => s.daily.ContainsKey(Calendar.RealToday());

        // Slick's pick: the biggest tournament tonight your roll can take without betting the dream
        public static string Pick(State s)
        {
            var list = Calendar.Today(s).Select(x => x.ev).ToList();
            var ok = list.Where(e => { var k = Risk(s, e).k; return k == "safe" || k == "free" || k == "shot"; }).ToList();
            var mtt = ok.Where(e => e.kind == "mtt").OrderByDescending(e => e.buyin).ToList();
            var safe = mtt.Where(e => Risk(s, e).k != "shot").ToList();
            var pick = safe.FirstOrDefault() ?? mtt.LastOrDefault() ?? ok.FirstOrDefault();
            return pick?.id;
        }
    }
}
