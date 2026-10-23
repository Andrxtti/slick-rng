// The story: scenes are lists of beats (who talks, what they say, choices). Most of the text comes from the web
// build's data; this file holds the beats that depend on your career, the choice effects, and the scenes the
// calendar era rewrote (the job choice, going broke, the boss texting you while you still work for him).
using System;
using System.Collections.Generic;
using System.Linq;

namespace CFDream
{
    public class ScenePlay { public string id; public int i; public List<Beat> extra = new List<Beat>(); public bool replay; public string lastSp; }

    public static class Story
    {
        static Beat B(string sp, string t, string who = null, bool text = false, bool goal = false, bool big = false, string mood = null) => new Beat { sp = sp, t = t, who = who, text = text, goal = goal, big = big, mood = mood };
        static Choice C(string t, string go = null, Beat line = null) => new Choice { t = t, go = go, line = line };
        static void Put(string id, string place, string title, string sub, string end, params Beat[] beats) => DB.Scenes[id] = new Scene { id = id, place = place, title = title, sub = sub, end = end, beats = beats.ToList() };

        public static void ApplyOverrides()
        {
            // the job choice: quit (Slick backs you, 3 games a day) or keep it (a paycheck every Friday, 3 games a week)
            var after = DB.Scenes["p_after"];
            var decide = after.beats.FirstOrDefault(b => b.choice != null);
            if (decide != null)
            {
                decide.choice = new List<Choice> { C("Quit. Today.", "p_quit"), C("Keep the job. Grind nights and weekends.", "p_keep") };
            }
            Put("p_quit", "office", null, null, "c1",
                B("you", "Lou? Redo it yourself."),
                B("n", "The elevator ride down is the best thirty-four floors of your life.\nNo paycheck. No plan B."),
                B("slick", $"Heard you walked out. I put ${Fmt.Money(Calendar.GiftQuit)} in your account. Not a loan. Don't make me regret it.", text: true),
                B("slick", "Rent is due on the first. Play three games a day, max. Never play above your roll.", text: true),
                B("mom", "BRO. YOU QUIT YOUR JOB?? CALL ME", text: true));
            Put("p_keep", "office", null, null, "c1",
                B("n", "You keep the lanyard.\nBut tonight you open a new spreadsheet and name it BANKROLL."),
                B("n", $"${Fmt.Money(Calendar.Salary)} lands every Friday. Days for Corporate Lou. Three sessions a week for the dream."),
                B("slick", "Heard about Saturday. Smart. A paycheck buys you time to learn. Never play above your roll. Text me when you win something.", text: true));
            // going broke: no automatic bailout. Slick lays out the options.
            Put("broke", "diner", "Rock bottom", null, "hub",
                B("n", "Lucky Seven Diner. 2:13 AM. Same stool. Same pie. Empty wallet."),
                B("slick", "Busted?"),
                B("you", "Every last dollar."),
                B("slick", "Good. Now you're a poker player. Everybody goes broke once. The good ones learn why."),
                new Beat { sp = "slick", t = "Here's how you climb out. Freerolls on CF Tilt every day. A real job while you rebuild. Or my money, and half your profits until it's back.",
                    choice = new List<Choice>
                    {
                        C("Take Slick's stake", null, B("slick", "Half your profits come back to me until it's paid. Respect the roll this time.")),
                        C("Grind the freerolls", null, B("slick", "Free money is the best money. Show me you can build it back from zero.")),
                        C("Ask Lou for the job back", null, B("n", "Corporate Lou smirks for a full ten seconds. Then he slides a badge across the desk.")),
                    } });
            Put("rehire", "office", "Back on Floor 34", null, "hub",
                B("derek", "Look who came crawling back. Same desk. Same Henderson deck. Smaller title."),
                B("n", $"${Fmt.Money(Calendar.Salary)} every Friday again. Three sessions a week for the dream."));
            Put("quit2", "office", "Two weeks' notice", null, "hub",
                B("you", "Lou? I'm done. Full-time."),
                B("derek", "You'll be back."),
                B("n", "No paycheck now. Three games a day, and rent is still due on the first."));
            // the calendar decides when things happen, so chapter openers stop promising a month
            Replace("c2", 0, "slick", "You've outgrown Riverside. The Circuit stops at Silver Strike every couple of months. Every event gives a ring. Check the calendar.");
            Replace("c3", 0, "n", "Every June, Las Vegas hosts the WCS Summer Series. 112 degrees outside, 68 inside, ten thousand people chasing the same gold. It's on your calendar now.");
            Replace("c6", 0, "n", "Every July: eight thousand players, ten thousand dollars each.\nOne of them ends up on the cover of every poker magazine in the world.");
            // the boss texts you while you still work for him
            Put("derek_job", "office", "Meanwhile", "Floor 34", "hub",
                new Beat { sp = "derek", t = "Saw your name on a poker site. Is THAT why the Henderson numbers were late?", text = true,
                    choice = new List<Choice>
                    {
                        C("Quit. Go full-time.", "quit2"),
                        C("\"They'll be on your desk Monday.\"", null, B("n", "You put the phone face down. The bankroll spreadsheet is still open in the other tab.")),
                    } });
        }
        static void Replace(string id, int i, string sp, string t) { if (DB.Scenes.TryGetValue(id, out var sc) && i < sc.beats.Count) { sc.beats[i].sp = sp; sc.beats[i].t = t; sc.beats[i].fn = false; } }

        // beats whose text depends on the career
        public static string Text(string sceneId, Beat b, int index, State s)
        {
            if (!b.fn) return b.t ?? "";
            if (sceneId == "p_after")
            {
                var local = DB.Event("local");
                return s.events.TryGetValue("local", out var r) && r.best > 0 && r.best <= Play.Paid(local)
                    ? $"You finished {Fmt.Ord(r.best)} on Saturday and took home ${Fmt.Money(r.cash)}.\nYou've replayed every hand in your head since."
                    : "You busted Saturday. And you have never felt more alive.\nYou've replayed every hand in your head since.";
            }
            return b.t ?? "";
        }

        // what a choice does to your career
        public static void Act(string sceneId, int k, State s)
        {
            switch (sceneId)
            {
                case "p_diner": s.SetFlag("honest", k == 0 ? 1 : 0); break;
                case "p_after":
                    s.cal.d = 7; // Monday morning
                    if (k == 0) { s.job = "quit"; Career.Add(s, Calendar.GiftQuit, "Slick's backing"); }
                    else { s.job = "nights"; Career.Add(s, 2000, "Savings, keeping the day job"); }
                    break;
                case "tour1": s.SetFlag(k == 0 ? "quote" : "humble"); break;
                case "broke":
                    if (k == 0) { double amt = Career.StakeSize(s); Career.Add(s, amt, "Slick's stake"); s.debt += amt; s.SetFlag("stakes", s.FlagN("stakes") + 1); Career.Moment(s, "🥧", $"Slick staked you ${Fmt.Money(amt)}"); }
                    if (k == 2 && s.job == "quit") { s.job = "nights"; Career.Moment(s, "🪪", "Went back to the day job"); }
                    break;
                case "derek_job": break;
            }
        }
        public static void OnEnd(string id, State s)
        {
            if (id.Length == 2 && id[0] == 'c' && char.IsDigit(id[1]))
            {
                int n = id[1] - '0';
                if (n > s.ch) Career.Moment(s, "📖", $"{J.S(DB.Chapter(n), "n")}: {J.S(DB.Chapter(n), "t")}");
                s.ch = Math.Max(s.ch, n);
            }
            if (id == "p_quit") Career.Moment(s, "🚪", "Quit the 9-to-5");
            if (id == "p_keep") Career.Moment(s, "🌙", "Kept the day job. Nights are for the dream.");
            if (id == "rehire") { s.job = "nights"; Career.Moment(s, "🪪", "Went back to the day job"); }
            if (id == "quit2") { s.job = "quit"; Career.Moment(s, "🚪", "Quit the day job to play full-time"); }
        }
        // the boss scene depends on whether you still work for him
        public static string Resolve(string id, State s) => id == "derek" && s.job == "nights" ? "derek_job" : id;
    }
}
