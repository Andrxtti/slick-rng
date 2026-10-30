using System;
using System.Collections.Generic;
using Dream.UI;

namespace Dream.Game
{
    // Chapter 3, the Summer (Test Beta 1.1.5): the WCoC Summer Series in Las Vegas, six weeks from Monday 1 June, every
    // year. A week of the series: the $10K Championship Satellite (Monday, Thursday), the $1,500 Millionaire Factory
    // (Tuesday, Friday, Sunday), the $3,000 Six-Max (Wednesday), the $10,000 Heads-Up Championship (Saturday); CF Tilt's
    // Sunday Seven-Figure online. Between series you grind from home. Tournaments only. Goal (STORY.md): a gold bracelet,
    // then bracelet1, c3_hello, and Chapter 4's opening.
    public partial class Flow
    {
        public const int Ch3Start = 147;   // Monday 1 June 2026
        public const int SeriesLen = 42, SeriesEvery = 364;
        static readonly string[] SeriesWeek = { "sat10k", "b1500", "b3000", "sat10k", "b1500", "b10k", "b1500" };   // Monday .. Sunday

        public static int SeriesDay(int d) => d < Ch3Start || (d - Ch3Start) % SeriesEvery >= SeriesLen ? -1 : (d - Ch3Start) % SeriesEvery;
        bool Ch3 => S != null && S.step == "ch3";

        IEnumerable<EventDef> TodayCh3()
        {
            bool away = S.day < S.awayUntil;   // flown home (the engagement party): nothing live until you're back
            int o = SeriesDay(S.day);
            if (!away)
            {
                if (o >= 0) yield return Lib.Event(SeriesWeek[o % 7]);
                else { yield return Lib.Event("nightly"); yield return Lib.Event("glob55"); }
            }
            if (Dow(S.day) == 0) yield return Lib.Event("tilt215");
            if (S.br < Low) yield return Lib.Event("freeroll");
        }

        // after Chapter 2: the calendar goes to the first day of the Series; Fridays on the way pay and charge as usual
        void BeginChapter3()
        {
            S.step = "ch3";
            if (S.day < Ch3Start)
            {
                for (int d = S.day + 1; d < Ch3Start; d++)
                    if (Dow(d) == 5) { if (S.job == "nights") S.br += Salary; S.br = Math.Max(0, S.br - Bills); }
                S.day = Ch3Start;
            }
            S.peak = Math.Max(S.peak, S.br); S.Write();
        }

        string Ch3Countdown()
        {
            int o = SeriesDay(S.day);
            if (o >= 0) return $"Summer Series · day {o + 1} of {SeriesLen}";
            int next = Ch3Start + ((S.day - Ch3Start) / SeriesEvery + 1) * SeriesEvery - S.day;
            return $"{next} days to the next Summer Series";
        }
        string Ch3Tag(int d)
        {
            int o = SeriesDay(d);
            if (o >= 0) { string id = SeriesWeek[o % 7]; return id == "b10k" ? "10K HU" : id.StartsWith("b") ? "BRACELET" : "SAT"; }
            return Dow(d) == 5 ? "RENT" : Dow(d) == 0 ? "ONLINE" : S.job == "nights" ? (Weekend(d) ? "LIVE" : "ONLINE") : "LIVE";
        }
    }
}
