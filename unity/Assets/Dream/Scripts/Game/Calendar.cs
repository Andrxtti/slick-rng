// The career runs on a calendar, Sunday to Saturday. Every day has its games: the card room every night,
// Sundays online, and the majors (Circuit stops, the WCS Summer Series, the Royal Tour, the GLO Cup, the
// Titan Series) on their dates. Quit your job and you can play 3 games a day; keep it and you get a
// paycheck every Friday but only 3 games a week. Rent is due on the 1st. Go to sleep to start a new day.
// Live events follow the real-world calendar (holidays, specials) and give gear you can't get anywhere else.
using System;
using System.Collections.Generic;
using System.Linq;

namespace CFDream
{
    public struct GameDate { public int y, Y, m, dd, dow, last; }

    public class SeriesDef
    {
        public string id, n, venue, icon; public int m, from, days; public bool wcs;
        public (string when, string ev)[] ev = new (string, string)[0];
    }
    public class SeriesOn { public SeriesDef sr; public int o, s0; }
    public class DayEvent { public EventDef ev; public string why; }
    public class LiveDef { public string id, from, to, icon, n; public ItemDef item; public EventDef ev; }

    public static class Calendar
    {
        static readonly DateTime Day0 = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc); // Monday, Jan 5 of Year 1: the prologue
        public static readonly string[] Dow = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        public static readonly string[] DowL = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        public static readonly string[] Mon = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        public static readonly string[] MonL = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        public static GameDate Date(int d)
        {
            var t = Day0.AddDays(d);
            return new GameDate { Y = t.Year, y = t.Year - 2025, m = t.Month, dd = t.Day, dow = (int)t.DayOfWeek, last = DateTime.DaysInMonth(t.Year, t.Month) };
        }
        public static int DayOf(int Y, int m, int dd) => (int)Math.Round((new DateTime(Y, m, dd, 0, 0, 0, DateTimeKind.Utc) - Day0).TotalDays);
        public static int WeekOf(int d) => (int)Math.Floor((d + 1) / 7.0); // weeks run Sunday to Saturday (day 0 is a Monday)
        public static string Str(int d, bool longForm = false) { var D = Date(d); return longForm ? $"{DowL[D.dow]}, {MonL[D.m - 1]} {D.dd}" : $"{Dow[D.dow]} {Mon[D.m - 1]} {D.dd}"; }

        // ------------------------------------------------------------ money: paychecks, rent, travel
        public const double Salary = 2000;     // every Friday while you keep the day job
        public const double GiftQuit = 5000;   // Slick backs you when you quit
        public const int GamesCap = 3;         // online games: per day full-time, per week with the day job
        public const int LiveCap = 1;          // live games: one a day full-time, one a week (weekend only) with the day job
        public static readonly Dictionary<string, double> HomeCost = new Dictionary<string, double> { { "studio", 650 }, { "apt", 1600 }, { "condo", 4200 }, { "penthouse", 15000 }, { "mansion", 42000 }, { "island", 120000 } };
        public static readonly Dictionary<string, double> RideCost = new Dictionary<string, double> { { "bus", 90 }, { "hatch", 320 }, { "coupe", 750 }, { "sedan", 1600 }, { "super", 6500 }, { "jet", 45000 } };
        public const double Living = 700;
        public static double Bills(State s) => (HomeCost.TryGetValue(s.Eq("home"), out var h) ? h : 0) + (RideCost.TryGetValue(s.Eq("ride"), out var r) ? r : 0) + Living;
        public static readonly Dictionary<string, double> Travel = new Dictionary<string, double> { { "vegas", 250 }, { "main", 250 }, { "tv", 350 }, { "titan", 1000 } };
        static readonly double[] LowMark = { 0, 400, 2000, 8000, 30000, 100000, 100000, 100000 };
        public static bool Low(State s) => s.br < LowMark[Math.Min(7, s.ch)];
        public static bool Broke(State s) => s.br < 60;

        // ------------------------------------------------------------ the schedule
        static readonly string[] EveryDay = { "nightly", "glob55", "tilt11" };
        static readonly Dictionary<int, string[]> Weekday = new Dictionary<int, string[]> { { 0, new[] { "deep", "tilt55", "tilt215" } }, { 2, new[] { "tilt530" } }, { 4, new[] { "tilt2100" } } };
        static SeriesDef S(string id, string n, string venue, string icon, int m, int from, int days, params (string, string)[] ev) => new SeriesDef { id = id, n = n, venue = venue, icon = icon, m = m, from = from, days = days, ev = ev };
        static readonly (string, string)[] CircuitEv = { ("0-6", "sat1700"), ("1", "ring400"), ("3", "ring600"), ("5", "ring400"), ("7", "ring1700") };
        public static readonly List<SeriesDef> Series = new List<SeriesDef>
        {
            S("rtpi", "Royal Tour Paradise Islands", "tv", "👑", 1, 10, 7, ("1", "rpt3500")),
            S("glow", "GLO Cup Winter", "tv", "🥇", 1, 21, 6, ("1", "glo10k"), ("4", "glo10k")),
            S("cfeb", "Circuit: Silver Strike", "casino", "💍", 2, 10, 10, CircuitEv),
            S("rtla", "Royal Tour LA Classic", "tv", "👑", 3, 14, 7, ("2", "rpt3500")),
            S("capr", "Circuit: Silver Strike", "casino", "💍", 4, 2, 10, CircuitEv),
            S("tspr", "Titan Series Spring", "titan", "🔱", 4, 18, 10, ("0-4", "sat25k"), ("2", "t25k"), ("5", "t25k"), ("7", "t100k"), ("all", "private")),
            new SeriesDef { id = "wcs", n = "WCS Summer Series", venue = "vegas", icon = "📿", m = 5, from = 30, days = 48, wcs = true },
            S("caug", "Circuit: Silver Strike", "casino", "💍", 8, 4, 10, CircuitEv),
            S("rtfl", "Royal Tour Florida", "tv", "👑", 8, 22, 7, ("2", "rpt3500")),
            S("tfal", "Titan Series Fall", "titan", "🔱", 9, 12, 10, ("0-4", "sat25k"), ("2", "t25k"), ("5", "t25k"), ("7", "t100k"), ("8", "t250k"), ("all", "private")),
            S("glof", "GLO Cup Fall", "tv", "🥇", 10, 20, 6, ("1", "glo10k"), ("4", "glo10k")),
            S("cnov", "Circuit: Silver Strike", "casino", "💍", 11, 3, 10, CircuitEv),
            S("rt5d", "Royal Tour Five Diamond", "tv", "👑", 12, 5, 7, ("1", "rpt3500"), ("4", "rpt10k")),
            S("hall", "Hall of Legends Invitational", "hall", "🏛️", 12, 18, 1, ("0", "legends")),
        };
        // the next start of a series from today: "February 10", or "now" when one is running
        static string NextOn(State s, Func<SeriesDef, bool> pick)
        {
            int best = int.MaxValue; string nm = null;
            foreach (var sr in Series)
                foreach (var Y in new[] { Date(s.cal.d).Y, Date(s.cal.d).Y + 1 })
                {
                    if (!pick(sr)) continue;
                    int s0 = DayOf(Y, sr.m, sr.from), end = s0 + sr.days - 1; if (end < s.cal.d) continue;
                    int at = Math.Max(s0, s.cal.d); if (at < best) { best = at; nm = s0 <= s.cal.d ? "now" : MonL[sr.m - 1] + " " + sr.from; }
                }
            return nm ?? "soon";
        }
        public static string NextCircuit(State s) => NextOn(s, sr => !sr.wcs && sr.venue == "casino");
        public static string NextWcs(State s) => NextOn(s, sr => sr.wcs);
        public static string NextTitan(State s) => NextOn(s, sr => sr.venue == "titan");
        // the Main Event is one day, July 5: "July 5", or "today"
        public static string NextMain(State s)
        {
            foreach (var Y in new[] { Date(s.cal.d).Y, Date(s.cal.d).Y + 1 }) { int d = DayOf(Y, 7, 5); if (d == s.cal.d) return "today"; if (d > s.cal.d) return "July 5"; }
            return "July 5";
        }
        public static List<SeriesOn> SeriesOnDay(int d)
        {
            var D = Date(d); var out_ = new List<SeriesOn>();
            foreach (var sr in Series)
                foreach (var Y in new[] { D.Y - 1, D.Y })
                {
                    int s0 = DayOf(Y, sr.m, sr.from), o = d - s0;
                    if (o >= 0 && o < sr.days) out_.Add(new SeriesOn { sr = sr, o = o, s0 = s0 });
                }
            return out_;
        }
        // the WCS Summer Series: bracelets twice a week each, the $10K Championship, the Main in July
        static List<(string id, string name)> WcsDay(int d, int o, int s0)
        {
            var D = Date(d); var l = new List<(string, string)> { };
            if (o >= 2 && o <= 19) l.Add(("sat10k", null));
            int n = 0; for (int k = 0; k <= o; k++) { int w = Date(s0 + k).dow; if (w == 1 || w == 4 || w == 3 || w == 6) n++; }
            if (D.dow == 1 || D.dow == 4) l.Add(("b1500", $"WCS Event #{n}: $1,500 No-Limit"));
            if (D.dow == 3 || D.dow == 6) l.Add(("b3000", $"WCS Event #{n}: $3,000 Six-Max"));
            if (D.m == 6 && D.dd == 18) l.Add(("b10k", null));
            if ((D.m == 6 && D.dd >= 20) || (D.m == 7 && D.dd <= 4)) l.Add(("satmain", null));
            if (D.m == 7 && D.dd == 5) l.Add(("main", null));
            return l;
        }

        // ------------------------------------------------------------ live events: the real-world calendar
        public static readonly List<LiveDef> Live = new List<LiveDef>
        {
            new LiveDef { id = "haunt", from = "10-20", to = "11-02", icon = "🎃", n = "Haunted Biscayne",
                item = new ItemDef { slot = "patch", id = "haunt", name = "“Haunted Biscayne” patch", live = "haunt", req = "Haunted Biscayne live event (10/20 to 11/02)" },
                ev = new EventDef { id = "live_haunt", ch = 1, kind = "mtt", name = "Haunted Biscayne Costume Freeroll", venue = "cardroom", buyin = 0, free = true, field = 160, prizeBase = 40, live = "haunt",
                    blurb = "Costumes required, buy-in waived. Play it once and the Haunted Biscayne patch is yours. Real-world event: October 20 to November 2." } },
        };
        public static IEnumerable<ItemDef> LiveItems => Live.Select(x => x.item);
        public static string RealToday() { var n = DateTime.Now; return $"{n.Year}-{n.Month:00}-{n.Day:00}"; }
        public static List<LiveDef> LiveNow()
        {
            var n = DateTime.Now; string k = $"{n.Month:00}-{n.Day:00}";
            return Live.Where(x => string.CompareOrdinal(x.from, x.to) <= 0 ? string.CompareOrdinal(k, x.from) >= 0 && string.CompareOrdinal(k, x.to) <= 0 : string.CompareOrdinal(k, x.from) >= 0 || string.CompareOrdinal(k, x.to) <= 0).ToList();
        }
        public static bool LivePlayed(State s, string id) => s.live.TryGetValue(id, out var l) && l.Contains(RealToday());
        public static ItemDef LiveReward(State s, EventDef ev)
        {
            var L = Live.FirstOrDefault(x => x.id == ev.live); if (L == null) return null;
            if (!s.live.ContainsKey(L.id)) s.live[L.id] = new List<string>();
            s.live[L.id].Add(RealToday());
            if (s.Owns(L.item.slot, L.item.id)) return null;
            s.owned.Add(L.item.Key); Fashion.GotIt(s, L.item.Key, L.n); s.newItems.Add(L.item.Key);
            Career.Moment(s, L.icon, $"Live event exclusive: {L.item.name}");
            return L.item;
        }

        // ------------------------------------------------------------ what runs on a day
        public static List<DayEvent> DayEvents(State s, int d)
        {
            var list = new List<DayEvent>();
            void Push(string id, string name, string why)
            {
                var t = DB.Event(id); if (t == null || list.Any(x => x.ev.id == id)) return;
                if (t.req > 0 && s.ch < t.req) return;
                if (t.ch > Math.Max(1, s.ch)) return;   // events unlock by chapter
                if (t.kind == "cash") return;   // no cash games in the story: tournaments only
                list.Add(new DayEvent { ev = name != null ? t.Copy(name) : t, why = why });
            }
            if (s.ch == 0) { if (d == 5 || d == s.cal.d) Push("local", null, "Your free seat"); return list; } // the prologue jumps to Saturday when you sit down
            var c = s.cal; var D = Date(d);
            if (c.inv != null && c.inv.d == d) list.Add(new DayEvent { ev = c.inv.ev, why = "invite" });
            if (d == c.d && s.br < 400) Push("freeroll", null, "freeroll");   // the comeback freeroll: only when the roll is under $400
            if (s.Ticket("classic2") > 0 && D.dow == 0) Push("classic2", null, "Day 2 · your chips are bagged");
            foreach (var x in SeriesOnDay(d))
            {
                if (x.sr.wcs) { foreach (var (id, name) in WcsDay(d, x.o, x.s0)) Push(id, name, x.sr.n); continue; }
                foreach (var (when, id) in x.sr.ev)
                {
                    bool ok;
                    if (when == "all") ok = true;
                    else if (when.Contains("-")) { var p = when.Split('-'); ok = x.o >= int.Parse(p[0]) && x.o <= int.Parse(p[1]); }
                    else ok = int.Parse(when) == x.o;
                    if (ok) Push(id, null, x.sr.n);
                }
            }
            if (D.dow == 6 && D.dd + 7 > D.last) Push("classic", null, "Last Saturday of the month");
            if (Weekday.TryGetValue(D.dow, out var wk)) foreach (var id in wk) Push(id, null, DowL[D.dow]);
            foreach (var id in EveryDay) Push(id, null, "Every day");
            return list;
        }
        public static List<DayEvent> Today(State s) => DayEvents(s, s.cal.d);
        public static DayEvent OnToday(State s, string id) => Today(s).FirstOrDefault(x => x.ev.id == id);

        // ------------------------------------------------------------ games left
        public struct Slots { public int left, cap, used; public bool week; }
        public static Slots SlotsLeft(State s)
        {
            var c = s.cal;
            if (s.ch == 0) return new Slots { left = 1, cap = 1 };
            if (s.job == "nights") { c.wk.TryGetValue(WeekOf(c.d), out int u); return new Slots { left = Math.Max(0, GamesCap - u), cap = GamesCap, used = u, week = true }; }
            // full-time: 3 online a day, but a day with a live game is 1 live + 1 online (the Spacepad comes with you)
            c.used.TryGetValue(c.d, out int ud); c.liveD.TryGetValue(c.d, out int lt); int cap = lt > 0 ? 1 : GamesCap;
            return new Slots { left = Math.Max(0, cap - ud), cap = cap, used = Math.Min(ud, cap) };
        }
        public static void UseSlot(State s, bool live = false)
        {
            var c = s.cal; int w = WeekOf(c.d);
            if (live) { c.liveD[c.d] = (c.liveD.TryGetValue(c.d, out var l) ? l : 0) + 1; c.liveW[w] = (c.liveW.TryGetValue(w, out var y) ? y : 0) + 1; }
            else { c.used[c.d] = (c.used.TryGetValue(c.d, out var u) ? u : 0) + 1; c.wk[w] = (c.wk.TryGetValue(w, out var x) ? x : 0) + 1; }
            Prune(c);
        }
        // live games: with the day job, one on the weekend; full-time, one a day (on top of the online games)
        public static int LiveLeft(State s)
        {
            var c = s.cal; if (s.ch == 0) return 1;
            if (s.job == "nights") { c.liveW.TryGetValue(WeekOf(c.d), out int u); return Math.Max(0, LiveCap - u); }
            c.liveD.TryGetValue(c.d, out int ld); c.used.TryGetValue(c.d, out int od);
            return od >= 2 ? 0 : Math.Max(0, LiveCap - ld);   // two online games already today: no room for a live one
        }
        // ------------------------------------------------------------ the week on the Spacepad (day job)
        public static int WeekStart(int d) => d - Date(d).dow;   // the Sunday
        public static string PlanFor(State s, int d) => s.cal.plan.TryGetValue(d, out var v) ? v : null;
        public static bool PlanDue(State s) => s.ch >= 1 && s.cal.planned < WeekOf(s.cal.d) && Date(s.cal.d).dow == 0;   // every Sunday, job or not
        public static void SetPlan(State s, int d, string what)
        {
            if (string.IsNullOrEmpty(what)) s.cal.plan.Remove(d); else s.cal.plan[d] = what;
            foreach (var k in s.cal.plan.Keys.Where(k => k < WeekStart(s.cal.d) - 7).ToList()) s.cal.plan.Remove(k);
        }
        public static void PlanDone(State s) { s.cal.planned = WeekOf(s.cal.d); s.Save(); }
        // today is a day you planned to play (or you have no plan at all)
        public static bool Today(State s, out string what) { what = PlanFor(s, s.cal.d); return what != null; }
        // with the day job: how many nights to skip to the next night you can play (0 = tonight)
        public static int SkipTo(State s)
        {
            int dow = Date(s.cal.d).dow;
            if (s.Ticket("classic2") > 0 && dow == 6) return 1;   // Day 2 of the Classic is tomorrow
            // a planned week (job or not): sleeping goes straight to the next night you picked; nights you didn't pick
            // are never lived through one at a time
            var week = s.cal.plan.Keys.Where(k => k >= WeekStart(s.cal.d) && k <= WeekStart(s.cal.d) + 6).OrderBy(k => k).ToList();
            if (week.Count > 0)
            {
                var ahead = week.Where(k => k > s.cal.d).ToList();
                foreach (var k in ahead) { var w = s.cal.plan[k]; if (s.job != "nights" || (w == "live" ? LiveLeft(s) > 0 : SlotsLeft(s).left > 0)) return k - s.cal.d; }
                return 7 - dow;   // planned nights all behind you: next Sunday, a new plan
            }
            if (s.job != "nights") return 0;
            if (AllPlayed(s)) return 7 - dow;                                  // next Sunday: a new week
            if (SlotsLeft(s).left == 0 && LiveLeft(s) > 0 && dow >= 1 && dow <= 5) return 6 - dow;   // Saturday's live game
            return 0;
        }
        public static bool Weekend(State s) { int dow = Date(s.cal.d).dow; return dow == 0 || dow == 6; }
        // why you can't play this one today (null when you can)
        public static string CantPlay(State s, EventDef ev)
        {
            if (ev.daily || s.ch == 0 || ev.id == "classic2") return null;
            if (ev.online) { var sl = SlotsLeft(s); return sl.left > 0 ? null : s.job == "nights" ? "No online games left this week" : "No online games left today"; }
            if (s.job == "nights" && !Weekend(s)) return "Live games are weekends only while you have the day job";
            if (LiveLeft(s) > 0) return null;
            if (s.job == "nights") return "Your live game this weekend is played";
            s.cal.liveD.TryGetValue(s.cal.d, out int lp); return lp > 0 ? "One live game a day" : "Two online games played: a live day is 1 live + 1 online";
        }
        // nothing left to do today (or this week, with the job): sleep is the only move
        public static bool AllPlayed(State s)
        {
            return SlotsLeft(s).left == 0 && LiveLeft(s) == 0;
        }
        static void Prune(CalState c)
        {
            foreach (var k in c.used.Keys.Where(k => k < c.d - 14).ToList()) c.used.Remove(k);
            foreach (var k in c.wk.Keys.Where(k => k < WeekOf(c.d) - 2).ToList()) c.wk.Remove(k);
            foreach (var k in c.liveD.Keys.Where(k => k < c.d - 14).ToList()) c.liveD.Remove(k);
            foreach (var k in c.liveW.Keys.Where(k => k < WeekOf(c.d) - 2).ToList()) c.liveW.Remove(k);
            foreach (var k in c.trav.Keys.Where(k => k < c.d - 3).ToList()) c.trav.Remove(k);
            foreach (var k in c.re.Keys.Where(k => int.Parse(k.Split('|')[0]) < c.d - 3).ToList()) c.re.Remove(k);
        }
        public static double TravelFor(State s, EventDef ev)
        {
            if (ev.online || !Travel.TryGetValue(ev.venue ?? "", out var cost)) return 0;
            return s.cal.trav.TryGetValue(s.cal.d, out var l) && l.Contains(ev.venue) ? 0 : cost;
        }

        // ------------------------------------------------------------ a new day
        static Rng RngFor(State s, int d, string salt) => new Rng($"{s.cal.seed}|{d}|{salt}");
        public static List<(string icon, string text)> Sleep(State s, int days = 1)
        {
            var news = new List<(string, string)>();
            for (int i = 0; i < days; i++) { s.cal.d++; NewDay(s, news, i == days - 1); }
            if (news.Count == 0) news.Add(("☀️", "A quiet night. Nothing new this morning."));
            Prune(s.cal); Career.Progress(s); s.Save();
            return news;
        }
        static void NewDay(State s, List<(string, string)> news, bool wake = true)
        {
            var c = s.cal; var D = Date(c.d);
            if (c.inv != null && c.inv.d < c.d) { if (wake || c.inv.seen) news.Add(("📭", $"The {c.inv.ev.name} invite expired.")); c.inv = null; }
            if (c.meet != null && c.meet.d < c.d) c.meet = null;
            if (s.Ticket("classic2") > 0 && D.dow == 1) { s.tickets["classic2"] = 0; news.Add(("🎟", "You missed Day 2 of the Biscayne Classic. Your chips were blinded out.")); }
            if (s.Ticket("classic2") > 0 && D.dow == 0) news.Add(("🏆", "Day 2 of the Biscayne Classic. Noon. Your chips are bagged."));
            if (s.job == "nights" && D.dow == 5) { Career.Add(s, Salary, "Paycheck"); news.Add(("💵", $"Payday. +${Fmt.Money(Salary)} from the day job.")); }
            string mk = $"{D.Y}-{D.m}";
            if (D.dd == 1 && c.paid != mk) { c.paid = mk; PayBills(s, news); }
            var start = SeriesOnDay(c.d).FirstOrDefault(x => x.o == 0); if (start != null) news.Add((start.sr.icon, $"{start.sr.n} starts today."));
            if (s.ch >= 1 && wake) { MaybeInvite(s, news); MaybeMeet(s, news); }
        }
        static void PayBills(State s, List<(string, string)> news)
        {
            double due = Bills(s);
            if (s.br >= due) { Career.Add(s, -due, "Rent and bills"); news.Add(("🏠", $"Rent and bills paid: −${Fmt.Money(due)}.")); return; }
            Career.Add(s, -s.br, "Rent and bills (short)");
            bool moved = s.Eq("home") != "studio" || s.Eq("ride") != "bus"; s.eq["home"] = "studio"; s.eq["ride"] = "bus";
            news.Add(("📦", $"Couldn't cover ${Fmt.Money(due)} in bills." + (moved ? " You moved back to the studio and sold the car." : "")));
            Career.Moment(s, "📦", moved ? "Short on rent. Moved back to the studio over the laundromat." : "Short on rent this month.");
        }

        // ------------------------------------------------------------ private game invites: rare, and a real decision
        static readonly int[][] Stakes = { new[] { 5, 10 }, new[] { 10, 25 }, new[] { 25, 50 }, new[] { 50, 100 }, new[] { 100, 200 }, new[] { 200, 400 }, new[] { 500, 1000 }, new[] { 1000, 2000 } };
        class Host { public string n, at, line; public double soft; public int lo, hi; }
        static readonly Host[] Hosts =
        {
            new Host { n = "Tommy \"The Shark\" Maio", at = "the back room at Biscayne", line = "Friends only, cash only, no questions. Half the table has been drinking since noon.", soft = 0.18, lo = 0, hi = 2 },
            new Host { n = "a retired NBA star", at = "his place in the hills", line = "He plays poker the way he drove the lane: straight at you, every time.", soft = 0.3, lo = 1, hi = 4 },
            new Host { n = "a tech founder", at = "his rooftop", line = "Just sold his company. Plays every hand and calls it \"learning\".", soft = 0.35, lo = 1, hi = 5 },
            new Host { n = "a movie producer", at = "a celebrity home game", line = "Two actors, one rapper, one pro. Guess which seat is yours.", soft = 0.28, lo = 2, hi = 6 },
            new Host { n = "a hedge fund manager", at = "a private club", line = "Thinks poker is \"just math.\" About to find out what that means.", soft = 0.22, lo = 3, hi = 7 },
            new Host { n = "Mustich's dad", at = "the yacht club", line = "Old money, older ego, and he wants to see what beat his son.", soft = 0.15, lo = 3, hi = 7 },
            new Host { n = "three Titan regulars", at = "a penthouse suite", line = "\"Pros only. Bring your A-game.\" No fish, just pride and a lot of money.", soft = -0.15, lo = 4, hi = 7 },
            new Host { n = "a visiting billionaire", at = "his yacht", line = "Flew in for one night. Loses millions for fun and remembers who was kind about it.", soft = 0.42, lo = 5, hi = 7 },
        };
        static void MaybeInvite(State s, List<(string, string)> news)
        {
            var c = s.cal; if (c.inv != null) return;
            var r = RngFor(s, c.d, "inv"); if (r.Next() > 0.045) return; // about once a month
            // the stakes are sized to your roll, and sometimes they are too big
            double roll = Math.Max(500, s.br), u = r.Next(), ratio = u < 0.45 ? 0.05 + r.Next() * 0.15 : u < 0.8 ? 0.2 + r.Next() * 0.4 : 0.6 + r.Next() * 0.9;
            int k = 0; for (int i = 0; i < Stakes.Length; i++) if (Stakes[i][1] * 100 <= roll * ratio) k = i;
            var st = Stakes[k]; var pool = Hosts.Where(h => k >= h.lo && k <= h.hi).ToList(); var host = pool.Count > 0 ? pool[r.Int(pool.Count)] : Hosts[0];
            int when = c.d + (r.Next() < 0.55 ? 1 : 2); string name = $"${Fmt.Money(st[1] * 100)} Private Game";   // never the same night it lands
            var ev = new EventDef { id = "inv", ch = Math.Min(7, Math.Max(1, k)), kind = "mtt", field = 18, name = name, venue = k >= 4 ? "titan" : "cardroom", buyin = st[1] * 100, invite = true, soft = host.soft, host = host.n,
                blurb = $"Hosted by {host.n} at {host.at}. Eighteen players, three paid, one night. {host.line}" };
            c.inv = new Invite { ev = ev, d = when, seen = true };
            news.Add(("📨", $"A private game invite: {name}, {(when == c.d + 1 ? "tomorrow night" : DowL[Date(when).dow] + " night")}."));
        }

        // ------------------------------------------------------------ Slick shows up when you're in trouble
        static void MaybeMeet(State s, List<(string, string)> news)
        {
            var c = s.cal; if (c.meet != null || !Low(s) || s.FlagN("slick_off") > 0) return;   // Slick walked away: no diner meetups
            var r = RngFor(s, c.d, "meet"); if (r.Next() > (Broke(s) ? 0.35 : 0.12)) return;
            double k = r.Next(); string seat = s.ch >= 3 ? "tilt215" : "deep";
            c.meet = k < 0.5 ? new Meetup { kind = "stake", amt = Career.StakeSize(s), d = c.d } : new Meetup { kind = "seat", id = seat, d = c.d };
            news.Add(("🕶️", "Slick texted: \"Diner. 2 AM. Bring an appetite.\""));
        }
        public static void MeetTake(State s)
        {
            var m = s.cal.meet; if (m == null) return;
            if (m.kind == "stake") { Career.Add(s, m.amt, "Slick's stake"); s.debt += m.amt; s.SetFlag("stakes", s.FlagN("stakes") + 1); Career.Moment(s, "🥧", $"Slick staked you ${Fmt.Money(m.amt)}"); }
            else if (m.kind == "seat") { s.tickets[m.id] = s.Ticket(m.id) + 1; Career.Moment(s, "🎟", $"Slick paid your seat into the {DB.Event(m.id)?.name}"); }
            else Career.Add(s, m.amt, "From Slick, for groceries");
            s.cal.meet = null;
        }

        // ------------------------------------------------------------ re-entry: big events only, while registration is open
        public static bool CanReenter(State s, Result r)
        {
            var ev = r.ev; if (!ev.reentry || !r.bust || r.ticketUsed) return false;
            if (r.stageI > 1) return false; // late registration closes after the middle levels
            s.cal.re.TryGetValue($"{s.cal.d}|{ev.id}", out int n);
            return n < 2 && s.br >= ev.buyin;
        }

        public static List<SeriesOn> Ahead(State s, int n = 5)
        {
            var seen = new HashSet<string>(); var l = new List<SeriesOn>();
            for (int d = s.cal.d; d < s.cal.d + 400 && l.Count < n; d++)
                foreach (var x in SeriesOnDay(d)) { var key = x.sr.id + x.s0; if (seen.Add(key)) l.Add(x); }
            return l;
        }
        public static string DayIcons(State s, int d)
        {
            var icons = new List<string>(); foreach (var x in SeriesOnDay(d)) if (!icons.Contains(x.sr.icon)) icons.Add(x.sr.icon);
            var D = Date(d); if (D.dow == 6 && D.dd + 7 > D.last) icons.Add("🏆");
            if (s.cal.inv != null && s.cal.inv.d == d) icons.Add("📨");
            return string.Join("", icons);
        }

        // older careers start at a point in the year that fits their chapter
        public static void Migrate(State s)
        {
            if (s.cal.d == 0 && s.made && s.ch >= 1) s.cal.d = new[] { 0, 7, 49, 140, 210, 250, 531, 330 }[Math.Min(7, s.ch)];
            if (s.made && s.ch >= 1 && string.IsNullOrEmpty(s.job)) s.job = "quit";
        }
    }
}
