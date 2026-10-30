// The month, many times over (task 5): Chapter 1's calendar with the real engine for every event, a player who picks
// the solver's best play with probability q, and the game's schedule rules (Flow.CantPlay): with the day job 3 online
// games a week and one live game a week on the weekend, $1,250 every Friday; full-time one live and one online a day.
// $600 rent every Friday on both. Broke (< $55): Slick's $1,000 stake, half of every profit back until repaid.
//   remote: simcal <keep|quit> <q> <runs>   -> Logs/simcal.txt
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dream.Game;
using UnityEditor;
using UnityEngine;

namespace Dream.EditorTools
{
    [InitializeOnLoad]
    public static class SimCal
    {
        static SimCal()
        {
            Remote.Extra["simcal2"] = a => { var p = a.Split(' '); Run2(p[0], double.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture), int.Parse(p[2]), double.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture)); };
            Remote.Extra["simcal"] = a => { var p = a.Split(' '); Run(p[0], double.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture), int.Parse(p[2])); }; }

        static System.Random rng;
        static double q;

        // one event: buy-in already paid; returns the prize and the accuracy
        static (double prize, int acc, int n, int place) Play(EventDef ev, string seed)
        {
            var run = Hands.Start(ev, seed);
            while (true)
            {
                var sp = Hands.Deal(run); int pick = Hands.Grade(sp, 0).bestIdx;
                if (rng.NextDouble() > q && sp.acts.Count > 1) { var o = Enumerable.Range(0, sp.acts.Count).Where(j => j != pick).ToList(); pick = o[rng.Next(o.Count)]; }
                Hands.Answer(run, pick);
                var w = Hands.Next(run); if (w == "finish" || w == "cooler") break;
            }
            int place = Hands.Place(run), paid = Hands.Paid(ev);
            return (place <= paid ? Hands.PrizeFor(ev, place) : 0, Hands.Accuracy(run.results), run.results.Count, place);
        }

        // Chapter 2 (Milestone B): from the first Circuit stop with a bankroll, the Circuit calendar (Flow.StopDay) and the
        // home grind between stops, until a ring or $15,000 after the first night at the Strike (Steven 12:09)
        static readonly string[] Plan = { "sat1700", "ring400", "ring400", "ring600", "sat1700", "sat1700", "sat1700", "ring600", "sat1700", "ring1700", "" };
        static void Run2(string job, double qq, int runs, double start)
        {
            Lib.Reset(); Lib.Load(); Hands.Chapter = 2; q = qq; bool keep = job == "keep";
            var days = new List<int>(); int viaRing = 0, never = 0, loansRuns = 0;
            for (int r = 0; r < runs; r++)
            {
                rng = new System.Random(r * 104729 + (keep ? 3 : 4) + (int)(q * 1000));
                double br = start, debt = 0; int loans = 0, seq = 0, rings = 0, week = -1, online = 0; bool strike = false, done = false;
                for (int d = Flow.Ch2Start; d < Flow.Ch2Start + 365 && !done; d++)
                {
                    int dow = (d % 7 + 1) % 7, wk = (d + 1) / 7; if (wk != week) { week = wk; online = 0; }
                    if (dow == 5) { if (keep) br += 1250; br -= 600; if (br < 55) { br += 1000; debt += 1000; loans++; } }
                    var today = new List<EventDef>(); int o = Flow.StopDay(d);
                    bool weekend = dow == 0 || dow == 6;
                    if (o >= 0) { if (Plan[o] != "" && (!keep || weekend)) today.Add(Lib.Event(Plan[o])); }
                    else if (!keep || weekend) today.Add(Lib.Event("nightly"));
                    if (dow == 0) today.Add(Lib.Event("tilt55")); else if (!keep || online < 3) today.Add(Lib.Event("glob55"));
                    foreach (var ev in today)
                    {
                        if (br < ev.buyin) continue;
                        if (ev.online) online++;
                        br -= ev.buyin; var res = Play(ev, $"c2|{job}|{q}|{r}|{seq++}");
                        double prize = res.prize, profit = prize - ev.buyin;
                        if (debt > 0 && profit > 0) { double back = System.Math.Min(debt, System.Math.Round(profit / 2)); debt -= back; prize -= back; }
                        br += prize; if (br < 55) { br += 1000; debt += 1000; loans++; }
                        if (ev.venue == "casino") strike = true;
                        if (ev.title == "ring" && res.prize > 0 && res.place == 1) rings++;
                        if (strike && (rings >= 1 || br >= 15000)) { done = true; days.Add(d - Flow.Ch2Start); if (rings >= 1) viaRing++; break; }
                    }
                }
                if (!done) never++; if (loans > 0) loansRuns++;
            }
            days.Sort();
            string M(int k) => days.Count > 0 ? days[System.Math.Min(days.Count - 1, days.Count * k / 4)].ToString() : "-";
            var line = $"ch2 {job} q {q:0.00} start ${start:0} runs {runs}: done within a year {100 * (runs - never) / runs}%, days to the goal median {M(2)} (25%: {M(1)}, 75%: {M(3)}), by a ring {100 * viaRing / System.Math.Max(1, runs - never)}%, Slick's loan {100 * loansRuns / runs}%";
            File.AppendAllLines(Path.Combine(Application.dataPath, "..", "Logs", "simcal.txt"), new[] { line });
            Remote.Log("simcal2: " + line);
        }

        static void Run(string job, double qq, int runs)
        {
            Lib.Load(); Hands.Chapter = 1; q = qq; bool keep = job == "keep";
            var local = Lib.Event("local"); var nightly = Lib.Event("nightly"); var glob = Lib.Event("glob55"); var day1 = Lib.Event("classic"); var day2 = Lib.Event("classic2");
            var stakeDays = new List<int>(); int loansRuns = 0, reached = 0; long accSum = 0, accN = 0;
            var weekly = new Dictionary<int, List<double>>(); int[] marks = { 13, 20, 27, 34, 39 };
            foreach (var m in marks) weekly[m] = new List<double>();
            var finals = new List<double>();
            for (int r = 0; r < runs; r++)
            {
                rng = new System.Random(r * 7919 + (keep ? 1 : 2) + (int)(q * 1000));
                double br = 0, debt = 0; int loans = 0, stake = -1, seq = 0;
                void Take(double prize, double buyin)
                {
                    double profit = prize - buyin;
                    if (debt > 0 && profit > 0) { double back = System.Math.Min(debt, System.Math.Round(profit / 2)); debt -= back; prize -= back; }
                    br += prize;
                }
                bool Enter(EventDef ev)
                {
                    if (!ev.free && br < ev.buyin) return false;
                    if (!ev.free) br -= ev.buyin;
                    var res = Play(ev, $"cal|{job}|{q}|{r}|{seq++}"); accSum += res.acc * res.n; accN += res.n;
                    Take(res.prize, ev.free ? 0 : ev.buyin);
                    if (br < 55) { br += 1000; debt += 1000; loans++; }   // rock bottom: Slick's stake
                    return true;
                }
                Take(Play(local, $"cal|{job}|{r}|local").prize, 0);
                br += keep ? 2000 : 2000;   // keep: $2,000 savings; quit: Slick backs you with $2,000 (Test Beta 1.0, Steven 11:27)
                int onlineWeek = -1, onlineUsed = 0, liveWeek = -1;
                for (int d = 7; d <= 40; d++)
                {
                    int dow = (d % 7 + 1) % 7;   // day 0 is a Monday: Sunday = 0 ... Saturday = 6
                    int week = (d + 1) / 7;
                    if (dow == 5) { if (keep) br += 1250; br -= 600; if (br < 55) { br += 1000; debt += 1000; loans++; } }   // Friday: pay and rent
                    if (d == 40) { if (Enter(day1) && rng.NextDouble() < 0.19) Enter(day2); break; }   // the Classic (Day 1 bags about 40 of 212)
                    if (week != onlineWeek) { onlineWeek = week; onlineUsed = 0; }
                    if (keep)
                    {
                        if ((dow == 0 || dow == 6) && liveWeek != week && Enter(nightly)) liveWeek = week;
                        while (onlineUsed < 3 && Enter(glob)) onlineUsed++;
                    }
                    else { Enter(nightly); Enter(glob); }
                    if (stake < 0 && br >= 5000) stake = d;
                    if (weekly.ContainsKey(d)) weekly[d].Add(br);
                }
                if (stake >= 0) { reached++; stakeDays.Add(stake); }
                if (loans > 0) loansRuns++;
                finals.Add(br);
            }
            double Med(List<double> l) { if (l.Count == 0) return double.NaN; var s = l.OrderBy(x => x).ToList(); return s[s.Count / 2]; }
            stakeDays.Sort();
            var line = $"{job} q {q:0.00} acc {accSum / System.Math.Max(1, accN)}% runs {runs}: $5,000 reached in {100 * reached / runs}% of months, median day {(stakeDays.Count > 0 ? stakeDays[stakeDays.Count / 2].ToString() : "-")} (25%: {(stakeDays.Count > 0 ? stakeDays[stakeDays.Count / 4].ToString() : "-")}, 75%: {(stakeDays.Count > 0 ? stakeDays[stakeDays.Count * 3 / 4].ToString() : "-")}); Slick's loan in {100 * loansRuns / runs}%; median bankroll "
                + string.Join(", ", marks.Select(m => $"day {m} ${Med(weekly[m]):0}")) + $", after the Classic ${Med(finals):0}";
            File.AppendAllLines(Path.Combine(Application.dataPath, "..", "Logs", "simcal.txt"), new[] { line });
            Remote.Log("simcal: " + line);
        }
    }
}
