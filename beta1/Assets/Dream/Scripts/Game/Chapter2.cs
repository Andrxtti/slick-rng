using System;
using System.Collections.Generic;
using System.Linq;
using Dream.UI;

namespace Dream.Game
{
    // Chapter 2, the Circuit (Milestone B): the Flashy Lights Circuit stops at Silver Strike every eight weeks, eleven days a
    // stop, Thursday to the Sunday after. Each stop: the Mega Satellite into the Main (on the quiet days too), the $400
    // Opener, the $600 Double Stack, the $1,700 Main Event on the second Saturday, and the charity day last (Colt the Cowboy,
    // c2_cowboy). Tournaments only (Steven 10:40). Between stops you grind from home (the Winn Nightly, CF Tilt). CF Tilt's
    // Sunday Kickoff every Sunday. Goal (Steven 12:09): a Circuit ring or a $15,000 bankroll, then Chapter 3's opening.
    public partial class Flow
    {
        public const int Ch2Start = 66;      // Thursday 12 March 2026: the first stop, the morning Tommy drives you north
        public const int StopEvery = 56, StopLen = 11;
        public const double Ch2Goal = 15000; public const int Ch2Rings = 1;
        static readonly string[] StopPlan = { "sat1700", "ring400", "ring400", "ring600", "sat1700", "sat1700", "sat1700", "ring600", "sat1700", "ring1700", "" };   // "": the charity day

        public static int StopIndex(int d) => d < Ch2Start ? -1 : (d - Ch2Start) % StopEvery < StopLen ? (d - Ch2Start) / StopEvery : -1;
        public static int StopDay(int d) => StopIndex(d) < 0 ? -1 : (d - Ch2Start) % StopEvery;
        bool Ch2 => S != null && S.step == "ch2";

        IEnumerable<EventDef> TodayCh2()
        {
            int o = StopDay(S.day);
            if (o >= 0)
            {
                if (StopPlan[o] != "") yield return Lib.Event(StopPlan[o]);
            }
            else { yield return Lib.Event("nightly"); yield return Lib.Event("glob55"); }
            if (Dow(S.day) == 0) yield return Lib.Event("tilt55");
            if (S.br < Low) yield return Lib.Event("freeroll");
        }

        // after Chapter 1: the calendar goes to the first Circuit stop; every Friday on the way pays and charges as usual
        void BeginChapter2()
        {
            S.step = "ch2"; S.day2 = false;
            if (S.day < Ch2Start)
            {
                for (int d = S.day + 1; d < Ch2Start; d++)
                    if (Dow(d) == 5) { if (S.job == "nights") S.br += Salary; S.br = Math.Max(0, S.br - Bills); }
                S.day = Ch2Start;
            }
            S.peak = Math.Max(S.peak, S.br); S.Write();
        }

        // the Spacepad's line for the chapter's goal, and its progress (0..1)
        string GoalLine => Ch2 ? $"Win a Circuit ring or build {Ui.Money(Ch2Goal)}" : $"Stake {Ui.Money(Goal)} before the Classic";
        float GoalFill => Ch2 ? (float)Math.Max(Math.Min(1, S.br / Ch2Goal), Math.Min(1, S.rings / (double)Ch2Rings)) : (float)Math.Min(1, S.br / Goal);
        string Countdown()
        {
            if (Ch2)
            {
                int o = StopDay(S.day);
                if (o >= 0) return o == 9 ? "The Circuit Main is today" : $"Circuit stop · day {o + 1} of {StopLen}";
                int next = Ch2Start + ((S.day - Ch2Start) / StopEvery + 1) * StopEvery - S.day;
                return next == 1 ? "The Circuit is back tomorrow" : $"{next} days to the next Circuit stop";
            }
            int left = ClassicDay - S.day;
            return left > 1 ? $"{left} days to the Classic" : left == 1 ? "The Classic is tomorrow" : left == 0 ? "The Classic is today" : "The Classic is played";
        }
        string DayTag(int d)
        {
            if (Ch2 || d >= Ch2Start)
            {
                int o = StopDay(d);
                if (o >= 0) { string id = StopPlan[o]; return id == "ring1700" ? "MAIN" : id.StartsWith("ring") ? "RING" : id == "sat1700" ? "SAT" : "CHARITY"; }
                return Dow(d) == 5 ? "RENT" : Dow(d) == 0 ? "ONLINE" : S.job == "nights" ? (Weekend(d) ? "LIVE" : "ONLINE") : "LIVE";
            }
            return d == ClassicDay ? "CLASSIC" : Dow(d) == 5 ? "RENT" : S.job == "nights" ? (Weekend(d) ? "LIVE" : "ONLINE") : "LIVE";
        }
    }
}
