// The story: scenes are lists of beats (who talks, what they say, choices). Most of the text comes from the web
// build's data; this file holds the beats that depend on your career, the choice effects, and the scenes the
// calendar era rewrote (the job choice, going broke, the boss texting you while you still work for him).
using System;
using System.Collections.Generic;
using System.Linq;

namespace CFDream
{
    public class ScenePlay { public string id; public int i; public List<Beat> extra = new List<Beat>(); public bool replay; public string lastSp, lastOther; }

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
                after.beats.Insert(after.beats.IndexOf(decide), B("n", "Nobody says you have to decide forever. Keep the paycheck and play around it, or walk now and bet on yourself. You can always quit later."));
            }
            Put("p_quit", "office", null, null, "c1",
                B("you", "Lou? Redo it yourself."),
                B("n", "The elevator ride down is the best thirty-four floors of your life.\nNo paycheck. No plan B."),
                B("slick", $"Heard you walked out. I put ${Fmt.Money(Calendar.GiftQuit)} in your account. Not a loan. Don't make me regret it.", text: true),
                B("slick", "Rent is due on the first. Play three games a day, max. Never play above your roll.", text: true),
                B("mom", "BRO. YOU QUIT YOUR JOB?? CALL ME", text: true));
            Put("p_keep", "office", null, null, "c1",
                B("n", "You keep the lanyard.\nBut tonight you open a new spreadsheet and name it BANKROLL."),
                B("n", $"${Fmt.Money(Calendar.Salary)} lands every Friday. Days for Natso SweetLou. Three sessions a week for the dream."),
                B("slick", "Heard about Saturday. Smart. A paycheck buys you time to learn. Never play above your roll. Text me when you win something.", text: true));
            // going broke: no automatic bailout. Slick lays out the options.
            Put("broke", "diner", "Rock bottom", null, "hub",
                B("n", "Lucky Seven Diner, Calle Ocho. 2:13 AM. Same stool. Same pie. Empty wallet."),
                B("slick", "Busted?"),
                B("you", "Every last dollar."),
                B("slick", "Good. Now you're a poker player. Everybody goes broke once. The good ones learn why."),
                new Beat { sp = "slick", t = "Here's how you climb out. Freerolls on CF Tilt every day. A real job while you rebuild. Or my money, and half your profits until it's back.",
                    choice = new List<Choice>
                    {
                        C("Take Slick's stake", null, B("slick", "Half your profits come back to me until it's paid. Respect the roll this time.")),
                        C("Grind the freerolls", null, B("slick", "Free money is the best money. Show me you can build it back from zero.")),
                        C("Ask Lou for the job back", null, B("n", "Natso SweetLou smirks for a full ten seconds. Then he slides a badge across the desk.")),
                    } });
            Put("rehire", "office", "Back on Floor 34", null, "hub",
                B("derek", "Look who came crawling back. Same desk. Same Henderson deck. Smaller title."),
                B("n", $"${Fmt.Money(Calendar.Salary)} every Friday again. Three sessions a week for the dream."));
            Put("quit2", "office", "Two weeks' notice", null, "hub",
                B("you", "Lou? I'm done. Full-time."),
                B("derek", "You'll be back."),
                B("n", "No paycheck now. Three games a day, and rent is still due on the first."));
            // David, your best friend, keeps pushing you toward the dream the longer you keep the badge
            Put("david1", "home", "A text", null, "hub",
                B("mom", "TEN THOUSAND?? FROM POKER?? AND YOU STILL WENT TO WORK TODAY???", text: true),
                B("mom", "im just saying. lou doesnt deserve you. the felt does", text: true));
            Put("david2", "home", "A text", null, "hub",
                B("mom", "25K. bro. thats a YEAR of lou's bonuses in like two months", text: true),
                B("mom", "when do we call it. seriously. when", text: true));
            Put("david3", "home", "A text", null, "hub",
                B("mom", "50 THOUSAND. I TOLD EVERYONE AT THE BAR. EVERYONE", text: true),
                B("mom", "you know what slick would say. you know what I say. the dream doesnt wait", text: true));
            Put("david4", "home", "A text", null, "hub",
                B("mom", "100K and youre still doing the henderson deck???? I cant", text: true),
                B("mom", "quit. move. go full time. ill drive you to the cardroom myself", text: true));
            // a quarter of a million: the badge comes off, whether you were ready or not
            Put("quit250", "office", "Decision made", "Floor 34", "hub",
                B("mom", "250K. a QUARTER MILLION. if you walk into that office tomorrow I will personally come get you", text: true),
                B("derek", "The partners saw the poker site. A quarter million, they said. So which is it: the deck, or the cards?"),
                B("you", "The cards, Lou. It was always the cards."),
                B("n", "You leave the badge on the desk. No paycheck. Every day is yours now: three games, or a live one and the rest online."),
                B("slick", "About time. Rent is still due on the first. Never play above your roll.", text: true));
            // after every event: the walk away from the table, from a cooler's silence to a title's roar (text picks a variant)
            Put("after_win", "cardroom", "After", null, "hub", B("n", "", mood: "win"), B("you", "", mood: "win"));
            Put("after_cash", "cardroom", "After", null, "hub", B("n", "", mood: "cash"), B("you", "", mood: "cash"));
            Put("after_bust", "cardroom", "After", null, "hub", B("n", "", mood: "bust"), B("you", "", mood: "bust"));
            Put("after_cooler", "cardroom", "After", null, "hub", B("n", "", mood: "cooler"), B("you", "", mood: "cooler"));
            foreach (var id in new[] { "after_win", "after_cash", "after_bust", "after_cooler" }) foreach (var b in DB.Scenes[id].beats) b.fn = true;
            // the calendar decides when things happen, so chapter openers stop promising a month
            Replace("c2", 0, "slick", "You've outgrown Biscayne. The Circuit stops at Silver Strike every couple of months. Every event gives a ring. Check the calendar.");
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
            if (sceneId.StartsWith("after_"))
            {
                var r = Game.Last; int v = (int)Math.Abs(s.FlagN("events")) % 3; string ev = r?.ev?.name ?? "the game", cash = r != null ? Fmt.Money(r.prize) : "0";
                switch (sceneId + ":" + index)
                {
                    case "after_win:0": return new[] { $"The last card falls and the room comes apart.\n{ev}. Yours.", $"They push the whole thing across the felt. Every chip.\n{ev}, and nobody is sitting down.", $"Phones up. Dealer standing. Floor staff grinning.\n{ev} belongs to you." }[v];
                    case "after_win:1": return new[] { "YES. YES. YES.", "I never doubted it. Not once. (Twice.)", "Text Slick. Text David. Text everyone." }[v];
                    case "after_cash:0": return new[] { $"Not the trophy, but a walk to the cage.\n${cash} from {ev}.", $"The cashier counts it twice. ${cash}.\nYou count it four times on the way home.", $"Out before the end, but with money.\n${cash}, and a plan for the next one." }[v];
                    case "after_cash:1": return new[] { "Good. Next time, further.", "Bankroll first. Glory later.", "That's rent. That's the roll. That's enough." }[v];
                    case "after_bust:0": return new[] { $"Chair pushed back. Chips gone. The lights feel brighter.\n{ev} goes on without you.", $"The long walk past tables that are still playing.\nYou replay it anyway: {ev}, hand by hand.", $"No cash. No excuses. The felt doesn't care.\n{ev}: busted." }[v];
                    case "after_bust:1": return new[] { "Review the hands. Every one.", "Variance. Or me. Tomorrow I'll know which.", "Sleep. Study. Back tomorrow." }[v];
                    case "after_cooler:0": return new[] { "Set over set. Kings into aces. Whatever you call it, it hurt.\nThe table goes quiet the way it only does for a bad beat.", "All in good. Called by worse. The river did the rest.\nSomeone mutters \"brutal\" and means it.", "You did everything right and the deck said no.\nThe dealer won't look at you." }[v];
                    case "after_cooler:1": return new[] { "...Brutal.", "Right play. Wrong deck.", "Don't tilt. Don't tilt. Don't tilt." }[v];
                }
            }
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
            if (id == "quit250") { s.job = "quit"; Career.Moment(s, "🚪", "A quarter million. Quit the day job for good."); }
        }
        // the boss scene depends on whether you still work for him
        public static string Resolve(string id, State s) => id == "derek" && s.job == "nights" ? "derek_job" : id;
    }
}
