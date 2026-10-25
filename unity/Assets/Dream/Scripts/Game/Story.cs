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
            // the prologue: the diner sends you to the card room, where Miles takes your envelope at the cage
            DB.Scenes["p_diner"].end = "p_seat";
            Put("p_seat", "cardroom", "Prologue", "Biscayne Card Room", "local",
                B("n", "Saturday. 11:40 AM. Biscayne Card Room, off the causeway.\nCarpet from 1994. Coffee from this morning, maybe."),
                B("rosa", "Help you?"),
                B("you", "Somebody bought me a seat. The Biscayne Championship."),
                B("rosa", "Free seat? Lucky you. Table nine, seat four. Cards in the air at noon."),
                B("n", "A hundred and eighteen players. Two hundred and fifty dollars a seat. You paid nothing, and your hands are shaking anyway."));
            // Chapter 2 opens on the real calendar and on whichever job you chose
            if (DB.Scenes.TryGetValue("c2", out var c2s) && c2s.beats.Count > 2) { c2s.beats[0].fn = true; c2s.beats[2].fn = true; }
            // the nightly schedule: The Winn Nightly at the card room, two CF Tilt events online
            var nt = DB.Event("nightly");
            if (nt != null) { nt.name = "The Winn Nightly $110"; nt.buyin = 110; nt.field = 60; nt.turbo = false; nt.blurb = "Biscayne's nightly. Sixty players, deep enough to actually play."; }
            if (DB.Event("glob55") == null && DB.Event("tilt11") != null)
            {
                var g = DB.Event("tilt11").Copy("CF $55 Global Rush"); g.id = "glob55"; g.buyin = 55; g.field = 1104; g.turbo = false;
                g.blurb = "CF Tilt's nightly flagship. Players from everywhere, a real prize pool."; DB.Events.Add(g);
            }
            var c1d = DB.Event("classic");
            if (c1d != null && DB.Event("classic2") == null)
            {
                var d2 = c1d.Copy("Biscayne Classic · Day 2"); d2.id = "classic2"; d2.field = 40; d2.reentry = false; d2.blurb = "Forty players come back Sunday. Thirty-two get paid. One gets the trophy.";
                d2.stages = new List<string> { "bubble", "deep", "final" }; d2.perS = new List<int> { 2, 2, 3 };
                DB.Events.Add(d2);
                c1d.name = "Biscayne Classic · Day 1"; c1d.kind = "sat"; c1d.seats = 40; c1d.target = "classic2"; c1d.title = null; c1d.boss = null;
                c1d.stages = new List<string> { "early", "middle" }; c1d.perS = new List<int> { 4, 3 };
                c1d.blurb = "The big one at Biscayne, over two days. Survive Saturday and bag your chips for Sunday. Mustich wants it too.";
            }
            Put("classic_day1", "cardroom", "The Biscayne Classic", "Day 1", "hub",
                B("n", "Bags and tags. Two hundred and twelve started this morning. Forty come back tomorrow, and you're one of them."),
                B("rosa", "Bag your chips, write your name on it. Day 2 is noon tomorrow. Don't be late."),
                B("trent", "Look who survived. Don't get used to it."),
                B("slick", "Day 2. Go to sleep. Don't celebrate yet.", text: true));
            Put("classic_mustich", "cardroom", "The Biscayne Classic", "Day 2", "hub",
                B("n", "", mood: "bust"),
                B("n", "Final hand. Mustich shoves with his dad watching from the rail. Called. An ace on the river."),
                B("n", "The room explodes. Mustich holds the trophy over his head like he bought it."),
                B("trent", "Write it down, Florida. Some people are born for this."),
                B("you", ""),
                B("lou", "Next month, kid. That trophy isn't going anywhere."));
            Put("classic_shark", "cardroom", "The Biscayne Classic", "Day 2", "hub",
                B("n", "", mood: "bust"),
                B("n", "Final hand. Tommy \"The Shark\" Maio turns over two kings and the whole rail stops breathing."),
                B("n", "The river bricks. Thirty years at this card room, and the old man finally has his trophy."),
                B("lou", "Thirty years! You hear me? Thirty years!"),
                B("you", ""),
                B("rosa", "His name goes on the board by the cage. Next month, it could be yours."));
            Put("classic_other", "cardroom", "The Biscayne Classic", "Day 2", "hub",
                B("n", "", mood: "bust"),
                B("n", "Final hand. All in, called, and the board runs clean."),
                B("n", "The room comes to its feet. The Biscayne Classic has a new name on the board."),
                B("slick", ClassicWin["slick"]),
                B("you", ""),
                B("rosa", "The name goes on the board by the cage. Next month, it could be yours."));
            foreach (var id in new[] { "classic_mustich", "classic_shark", "classic_other" }) { DB.Scenes[id].beats[0].fn = true; DB.Scenes[id].beats[4].fn = true; }
            // the final table: who's waiting for you there, then how it ends between the two of you
            Put("ft_intro", "cardroom", "Final table", null, "run", B("n", ""), B("trent", PreLine["trent"]), B("you", "Let's play."));
            Put("ft_won", "cardroom", "Final table", null, "hub", B("n", "", mood: "win"), B("trent", LoseLine["trent"]), B("you", "Good game."));
            Put("ft_lost", "cardroom", "Final table", null, "hub", B("n", "", mood: "bust"), B("trent", WinLine("trent")), B("you", "Good game. I'll see you again."));
            foreach (var id in new[] { "ft_intro", "ft_won", "ft_lost" }) DB.Scenes[id].beats[0].fn = true;
            Insert("trentbeat", 0, B("n", "Final hand. Heads-up for the Biscayne Classic, and the whole card room is standing on chairs."));
            // Monday at the office is only Monday now; the job decision happens at home, when Slick texts
            var after = DB.Scenes["p_after"];
            after.beats.RemoveAll(b => b.choice != null);
            after.beats.Add(B("n", "Seven more hours of spreadsheets. Your mind never leaves table nine."));
            Put("p_offer", "home", "Prologue", "Monday night", "hub",
                B("n", "Monday night. Your phone buzzes on the counter."),
                B("slick", "Saw your results from Saturday. Here's my offer.", text: true),
                B("slick", $"Quit Lou. I'll put ${Fmt.Money(Calendar.GiftQuit)} in your account tomorrow. Not a loan. Don't make me regret it.", text: true),
                B("slick", "Or keep the job and play on the side. Your call, kid. It's the biggest one you'll make.", text: true),
                new Beat { sp = "n", t = "Decision time.", choice = new List<Choice> { C("Quit. Text Lou tonight.", "p_quit"), C("Keep the job. Grind nights and weekends.", "p_keep") } });
            // the job choice: quit (Slick backs you, 3 games a day) or keep it (a paycheck every Friday, 3 games a week)
            Put("p_quit", "home", "Prologue", "Full-time", "c1",
                B("you", "Lou. Redo it yourself.", text: true),
                B("n", "Sent. No paycheck. No plan B.\nYou have never felt lighter."),
                B("slick", $"Done. ${Fmt.Money(Calendar.GiftQuit)} is in your account. Not a loan. Don't make me regret it.", text: true),
                B("slick", "Rent is due on the first. Never play above your roll.", text: true),
                B("mom", "BRO. YOU QUIT YOUR JOB?? CALL ME", text: true));
            Put("p_keep", "home", "Prologue", "Nights and weekends", "c1",
                B("n", "You keep the lanyard.\nBut tonight you open a new spreadsheet and name it BANKROLL."),
                B("n", $"${Fmt.Money(Calendar.Salary)} lands every Friday. Days for Natso SweetLou. Three sessions a week for the dream."),
                B("slick", "Smart. A paycheck buys you time to learn. Never play above your roll. Text me when you win something.", text: true));
            Put("c2_roll", "casino", "Chapter 2", "The roll", "hub", B("slick", "Two rings. Now the roll. Forty thousand, and not one dollar of it borrowed from tomorrow.", text: true));
            Put("c2_rings", "casino", "Chapter 2", "The hardware", "hub", B("slick", "Money's there. Now the hardware. Bankrolls don't get introduced at the rail. Rings do.", text: true));
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
                        C("Ask Lou for the job back", null, B("n", "Lou laughs for a full ten seconds. \"Your job? We gave it to the intern.\"\nThat night Slick sends $500: \"Make it last.\"")),
                    } });
            Put("broke_n", "diner", "Rock bottom", null, "hub",
                B("n", "Lucky Seven Diner, Calle Ocho. 2:13 AM. Same stool. Same pie. Empty wallet."),
                B("slick", "Busted?"),
                B("you", "Every last dollar."),
                B("slick", "Good. Now you're a poker player. Everybody goes broke once. The good ones learn why."),
                new Beat { sp = "slick", t = "You've still got Lou's paycheck. Freerolls on CF Tilt while it builds back. Or my money, and half your profits until it's back.",
                    choice = new List<Choice>
                    {
                        C("Take Slick's stake", null, B("slick", "Half your profits come back to me until it's paid. Respect the roll this time.")),
                        C("Grind the freerolls", null, B("slick", "Free money is the best money. Show me you can build it back from zero.")),
                    } });
            // three shots above your roll and broke anyway: Slick walks until you've built it back and said sorry
            Put("broke_told", "diner", "Rock bottom", "I told you so", "hub",
                B("n", "Lucky Seven Diner. 2:13 AM. Same stool. Same pie. Empty wallet. Again."),
                B("slick", "I told you. Three times I told you."),
                B("you", "I know."),
                B("slick", "You want to play above your roll, you do it without me. No stake. No seats. Nothing."),
                B("slick", "Freerolls on CF Tilt. Grind it back to ten grand. Then come find me."));
            Put("slick_back", "home", "Square", null, "hub",
                B("n", "Ten thousand dollars. Built back from the freerolls, one min-cash at a time."),
                B("you", "You were right. I didn't listen. I'm sorry.", text: true),
                B("slick", "Took you long enough. Welcome back, kid.", text: true),
                B("slick", "Now respect the roll.", text: true));
            // full-time and asking for the old desk back: Lou says no (and Slick, once a month, sends a little something)
            Put("lou_no", "office", "Floor 34", "Asking for the job back", "hub",
                B("derek", "Your job? We gave it to the intern. He's better at spreadsheets than you ever were."),
                B("n", "Thirty-four floors down. The elevator has never felt slower."),
                B("slick", "Heard Lou laughed you out. Here's $500. Make it last.", text: true));
            Put("lou_no2", "office", "Floor 34", "Asking for the job back", "hub",
                B("derek", "Your job? We gave it to the intern. He's better at spreadsheets than you ever were."),
                B("n", "Thirty-four floors down. The elevator has never felt slower."));
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
            Put("quit250", "home", "Decision made", "David was right", "hub",
                B("mom", "250K. a QUARTER MILLION. if you walk into that office tomorrow I will personally come get you", text: true),
                B("mom", "im not joking. this is the sign. quit.", text: true),
                B("you", "Yeah. You're right. I'm done."),
                B("n", "Monday morning you leave the badge on Lou's desk with a sticky note: Redo it yourself.\nNo paycheck. Every day is yours now."),
                B("slick", "About time. Rent is still due on the first. Never play above your roll.", text: true));
            // after every event: the walk away from the table, from a cooler's silence to a title's roar (text picks a variant)
            Put("after_win", "cardroom", "After", null, "hub", B("n", "", mood: "win"), B("you", "", mood: "win"));
            Put("after_cash", "cardroom", "After", null, "hub", B("n", "", mood: "cash"), B("you", "", mood: "cash"));
            Put("after_bust", "cardroom", "After", null, "hub", B("n", "", mood: "bust"), B("you", "", mood: "bust"));
            Put("after_cooler", "cardroom", "After", null, "hub", B("n", "", mood: "cooler"), B("you", "", mood: "cooler"));
            foreach (var id in new[] { "after_win", "after_cash", "after_bust", "after_cooler" }) { foreach (var b in DB.Scenes[id].beats) b.fn = true; DB.Scenes[id].beats.Add(B("lou", id == "after_win" ? "Kid. Don't let it go to your head. Let it go to your bankroll." : id == "after_cash" ? "A cash is a cash. The trophy's next." : id == "after_bust" ? "Happens to everybody. Back tomorrow." : "Brutal. Thirty years and that one still stings.")); }   // the third voice: a witness, chosen when it plays
            // the calendar decides when things happen, so chapter openers stop promising a month
            Replace("c1", 1, "rosa", "Back again? Sign the list. One-two has a seat in ten. Nightly turbo kicks off at seven.");
            Replace("c2", 0, "slick", "You've outgrown Biscayne. The Circuit stops at Silver Strike every couple of months. Every event gives a ring. Check the calendar.");
            Replace("c3", 0, "n", "Every June, Las Vegas hosts the WCS Summer Series. 112 degrees outside, 68 inside, ten thousand people chasing the same gold. It's on your calendar now.");
            Replace("c6", 0, "n", "Every July: eight thousand players, ten thousand dollars each.\nOne of them ends up on the cover of every poker magazine in the world.");
            // the later chapters open like the early ones: the place, the people, the lesson, somebody from home, the goal
            Insert("c4", 0, B("n", "A soundstage in Hollywood, Florida. Three cameras, a hole-card reader, and forty thousand people watching you blink."));
            Insert("c4", 4, B("n", "Under the lights everything shows. The rail can see your hole cards. The table can see your face."));
            Insert("c4", 5, B("slick", "Cameras change people. Don't let them change your ranges. Play the hand you'd play in an empty room."));
            Insert("c4", 7, B("mom", "youre on the STREAM. my mom is watching. MY MOM", text: true));
            Insert("c5", 2, B("you", "Welcome to the club."));
            Insert("c5", 5, B("mom", "a HOUSE. you bought in for a HOUSE. im not telling your mother", text: true));
            Insert("c6", 1, B("n", "Day one. A ballroom the size of an airport. Two hundred tables, and somewhere in the noise, a seat with your name on it."));
            Insert("c6", 3, B("ghost", "Still here, kid. So am I."));
            Insert("c7", 1, B("n", "Stu. Colt. Chance. The Ghost. And a kid from Floor 34 who got told to redo the Henderson deck."));
            Insert("c7", 3, B("slick", "There's nobody left to teach you. So teach somebody. I sat on that stool once too, remember."));
            Insert("c7", 4, B("you", "I remember."));
            // the openers name the next date, like Chapter 2 does
            foreach (var k in new[] { "c3", "c5", "c6" }) if (DB.Scenes.TryGetValue(k, out var oc) && oc.beats.Count > 0) oc.beats[0].fn = true;
            // the lessons: the first time you bust in a chapter, Slick finds you and names the leak (and the Spacepad drills it)
            Put("lesson1", "diner", "A lesson", "Fold the junk", "hub",
                B("n", "Lucky Seven. 1:40 AM. The pie is the same. You are a little poorer."),
                B("slick", "Tell me the hand that got you."),
                B("you", "King-jack, early position. I raised. Got three-bet. Called. Flopped a jack. Lost everything to kings."),
                B("slick", "Then the hand didn't get you. The first decision did. From early, king-jack is a fold. Everything after was a story you told yourself."),
                B("slick", "Fold the junk. That's not advice, it's arithmetic. Your misses are on the Spacepad. Drill them until the fold is boring."));
            Put("lesson2", "casino", "A lesson", "Pressure the scared ones", "hub",
                B("n", "The Circuit floor at midnight. Chips being racked all around you. None of them yours."),
                B("slick", "You check too much."),
                B("you", "I didn't have anything."),
                B("slick", "Neither did they. On a dry board the raiser bets small and often, and everybody without a pair goes away. You gave them a free card and a free look at you."),
                B("slick", "Pressure the scared ones. Small bets, lots of them. It's in your misses. Go run them."));
            Put("lesson3", "vegas", "A lesson", "What the money's worth", "hub",
                B("n", "Two hundred and eleven players left. Two hundred get paid. You were two hundred and twelve."),
                B("slick", "You called a shove with ace-ten on the bubble."),
                B("you", "I was ahead of his range."),
                B("slick", "Ahead of his range in chips. Behind in money. Near the bubble, the chips you lose are worth more than the chips you win. That is the whole of ICM."),
                B("slick", "Think about what the money's worth. Not the chips. The spot's on your Spacepad. Drill it till it stops hurting."));
            Put("lesson4", "tv", "A lesson", "People, not boards", "hub",
                B("n", "The stream is over. The chat has moved on. The hand has not."),
                B("slick", "You bluffed the one guy who never folds."),
                B("you", "The board was perfect for it."),
                B("slick", "Perfect for a bluff against a player who folds. He doesn't. A bluff is a bet against a person, not a board. Pick the person first."),
                B("slick", "Ranges, not hands. People, not boards. Drill the river spots. They're waiting for you."));
            Put("lesson5", "titan", "A lesson", "The spots nobody studies", "hub",
                B("n", "A room where everybody knows the math. You lost anyway."),
                B("slick", "They all have the same solver. So where's the edge?"),
                B("you", "There isn't one."),
                B("slick", "There's one in every spot they don't study. Odd stacks. Four-bet pots. Limped pots with antes. The book is thin there. Be the one who read it."),
                B("slick", "Your misses in the strange spots. That's the list now. Drill those."));
            // the lessons talk about the hand that actually cost you, when there is one (the written story is the fallback)
            for (int L = 1; L <= 4; L++) if (DB.Scenes.TryGetValue("lesson" + L, out var ls) && ls.beats.Count > 3) { ls.beats[1].fn = true; ls.beats[2].fn = true; ls.beats[3].fn = true; if (L == 3) ls.beats[0].fn = true; }
            // the boss texts you while you still work for him
            Put("derek_job", "office", "Meanwhile", "Floor 34", "hub",
                new Beat { sp = "derek", t = "Saw your name on a poker site. Is THAT why the Henderson numbers were late?", text = true,
                    choice = new List<Choice>
                    {
                        C("Quit. Go full-time.", "quit2"),
                        C("\"They'll be on your desk Monday.\"", null, B("n", "You put the phone face down. The bankroll spreadsheet is still open in the other tab.")),
                    } });
        }
        // ------------------------------------------------------------ the walk away from the table
        // the third beat of an aftermath: somebody at the venue says something (a text from home when you played online)
        static readonly Dictionary<string, string[]> Witness = new Dictionary<string, string[]>
        {
            { "win:lou", new[] { "Thirty years in this room. I've seen three people do that. You're the fourth.", "Kid. Don't let it go to your head. Let it go to your bankroll." } },
            { "win:trent", new[] { "Enjoy it. Variance is a loan.", "Congrats. I'd have won it with a better dealer." } },
            { "win:L", new[] { "Good game. I'll remember your name.", "Well played. Most people fold there." } },
            { "win:host", new[] { "WHAT a finish! Say something for the stream!", "The cubicle kid does it again! Chat, calm down!" } },
            { "win:ghost", new[] { "Good game, kid.", "You're learning. That's the dangerous part." } },
            { "win:mom", new[] { "I SAW IT. I SAW THE WHOLE THING. DINNERS ON YOU", "FIRST PLACE?? ok im telling everyone at the bar" } },
            { "cash:rosa", new[] { "Cage is that way. Don't spend it all on the drive home.", "Nice run. Sign up for tomorrow's on your way out." } },
            { "cash:L", new[] { "A cash is a cash. The trophy's next.", "You left early. Next time stay for the end." } },
            { "cash:host", new[] { "A min-cash, but a cash! The grind continues.", "In the money and out the door. See you next week!" } },
            { "cash:mom", new[] { "PAID. we eating tonight", "a cash is a cash bro. proud of you" } },
            { "bust:trent", new[] { "Out already? Dad's going to love this.", "Tough. Not really.", "Maybe stick to the spreadsheets." } },
            { "bust:L", new[] { "Happens to everybody. Even me. Once.", "You played fine. The cards didn't." } },
            { "bust:host", new[] { "And the cubicle kid is OUT. Tough break, chat.", "That's poker, folks. Back after the break." } },
            { "bust:ghost", new[] { "Sit down. Think about the last hand. Then forget it.", "Busted is information. Read it." } },
            { "bust:slick", new[] { "Review the hands. Then sleep.", "Busted is information. Use it." } },
            { "cooler:lou", new[] { "Brutal. Thirty years and that one still stings.", "That wasn't poker. That was weather." } },
            { "cooler:L", new[] { "That's the game. The good ones shrug.", "Right play. The deck owed you nothing." } },
            { "cooler:host", new[] { "OH NO. Chat, we don't say the word. You know the word.", "Sick. Absolutely sick. Give him a second, folks." } },
            { "cooler:ghost", new[] { "The deck doesn't know who you are. Good.", "You'll see that hand at 3 AM for a week. Then never again." } },
            { "cooler:slick", new[] { "Right play. Wrong deck. Same you tomorrow.", "I count the beats, remember. That one's on the list." } },
        };
        public static IEnumerable<(string sp, string t)> WitnessLines()
        {
            foreach (var kv in Witness) { string sp = kv.Key.Substring(kv.Key.IndexOf(':') + 1); if (sp == "L") continue; foreach (var t in kv.Value) yield return (sp, t); }
            foreach (var kv in Witness) if (kv.Key.EndsWith(":L")) foreach (var lg in new[] { "moneybaker", "hellbent", "loophole", "kfoxglove", "mizrockie" }) foreach (var t in kv.Value) yield return ("L:" + lg, t);
        }
        public static void Aftermath(State s, Result res, Run run, Scene sc, string after)
        {
            if (sc.beats.Count < 3) return;
            string outcome = after.Substring(6); var b = sc.beats[2]; bool online = string.IsNullOrEmpty(res.ev.venue) || res.ev.online;
            string venue = res.ev.venue ?? "cardroom", sp;
            if (online) sp = outcome == "win" || outcome == "cash" ? "mom" : "slick";
            else if (outcome == "win" && res.beat != null && DB.Legend(res.beat) != null) sp = "L:" + res.beat;
            else switch (venue)
            {
                case "cardroom": sp = outcome == "win" ? "lou" : outcome == "cash" ? "rosa" : outcome == "bust" ? "trent" : "lou"; break;
                case "casino": sp = outcome == "win" || outcome == "bust" ? "trent" : "L:moneybaker"; break;
                case "vegas": sp = outcome == "cooler" ? "ghost" : "L:hellbent"; break;
                case "tv": sp = "host"; break;
                case "titan": sp = outcome == "win" || outcome == "bust" ? "trent" : "L:loophole"; break;
                case "main": sp = outcome == "cash" ? "L:mizrockie" : "ghost"; break;
                default: sp = "lou"; break;
            }
            string key = outcome + ":" + (sp.StartsWith("L:") ? "L" : sp);
            if (!Witness.TryGetValue(key, out var pool)) { key = outcome + ":L"; pool = Witness[key]; if (!sp.StartsWith("L:")) sp = "L:moneybaker"; }
            int v = (int)Math.Abs(s.FlagN("events")) % pool.Length;
            b.sp = sp; b.t = pool[v]; b.fn = false; b.who = null; b.text = online; b.mood = null;
        }
        // ------------------------------------------------------------ who wins the Classic when it isn't you
        public static readonly Dictionary<string, string> ClassicWin = new Dictionary<string, string>
        {
            { "slick", "Somebody had to keep you humble, kid." },
            { "L:hungar", "Thirty years and the comeback still works." },
            { "L:brunswick", "Son, that's how you close a tournament." },
            { "L:chance", "Told you I take my time. Took the whole thing." },
            { "L:armadillo", "Now that's a story I'll be telling for years, partner." },
            { "L:moneybaker", "Spreadsheets to trophies. Never gets old." },
            { "L:hellbent", "Finally, a field that didn't get lucky against me." },
            { "L:negrono", "I put them all on ace-queen. I was close enough." },
            { "L:handsome", "Seven-four suited. Told you it was my favourite." },
            { "L:escandalo", "And for my final trick, the trophy disappears with me." },
            { "L:mouth", "You see that? You SEE that? Nobody was calling that!" },
            { "L:fargo", "Is a nice trophy. Is my trophy now." },
            { "L:sidle", "Good game." },
            { "L:swan", "I had it the whole time. Mostly." },
            { "L:antionius", "Your move. Next month." },
            { "L:holdz", "I studied this exact final table this morning." },
            { "L:bonanza", "Equilibrium says I win. So I win." },
            { "L:kenny", "Nice trophy. I will take it." },
            { "L:chillwick", "Good game, everyone. Genuinely." },
            { "L:kahoon", "Discipline. Every single hand." },
            { "L:loophole", "My frequencies were correct. As expected." },
            { "L:hackston", "A textbook finish. I would know, I wrote the textbook." },
            { "L:addemup", "Shipped." },
            { "L:foxglove", "Points are points. Trophies are better." },
            { "L:kfoxglove", "You were nervous the whole final table. I noticed." },
            { "L:martini", "Another one for the cabinet." },
            { "L:lonestar", "GORILLA. MODE. CHAMPION." },
            { "L:mocha", "Cold out there. Warm in here, with this." },
            { "L:daisies", "Went big. Went home with it." },
            { "L:bindr", "Everyone was a nobody once. Not today." },
            { "L:punchline", "This is my office. Thank you for visiting." },
            { "L:glazer", "Pick a game. I'll win that one too." },
            { "L:mizrockie", "Champ of everything. Get used to it." },
        };
        // ------------------------------------------------------------ the final table
        // what they say before it starts, when you beat them, and when they beat you
        public static readonly Dictionary<string, string> PreLine = new Dictionary<string, string>
        {
            { "trent", "Final table. Dad flew in for this. Don't embarrass yourself." },
            { "lou", "Thirty years, kid, and I still get butterflies at a final table." },
            { "slick", "Didn't think you'd make it this far. Show me what you learned." },
            { "ghost", "Kid. Look at your cards, not at me." },
        };
        public static readonly Dictionary<string, string> LoseLine = new Dictionary<string, string>
        {
            { "trent", "Enjoy it. Variance is a loan." }, { "lou", "Ha! The kid got me. Thirty years and the kid got me." },
            { "slick", "That's my student. Now go cash that check." }, { "ghost", "Good game, kid." },
            { "L:hungar", "You saw the hand this time. Good." }, { "L:brunswick", "Well, I'll be. Tip of the hat, son." },
            { "L:chance", "Took my time. You took the pot." }, { "L:armadillo", "Partner, that one's going in the stories too." },
            { "L:moneybaker", "Good. Play your game. That's all I ever said." }, { "L:hellbent", "Unbelievable. The luckiest call I have ever seen." },
            { "L:negrono", "I had you on ace-queen. I was not close." }, { "L:handsome", "Seven-four doesn't always win. Today it didn't." },
            { "L:escandalo", "Hm. You saw through the trick." }, { "L:mouth", "You CALLED that? Who calls that? You called that!" },
            { "L:fargo", "Is a nice hand. Is your hand. Congratulations." }, { "L:sidle", "Well played." },
            { "L:swan", "I had air. You found out." }, { "L:antionius", "Your move was correct." },
            { "L:holdz", "I did not study this spot. I will tonight." }, { "L:bonanza", "Equilibrium says fold. I didn't. My mistake." },
            { "L:kenny", "Nice stack. Keep it." }, { "L:chillwick", "Good game. Genuinely." },
            { "L:kahoon", "Discipline beats talent. Today you had both." }, { "L:loophole", "Your frequency was correct. Noted." },
            { "L:hackston", "Interesting sizing. Correct, this time." }, { "L:addemup", "Nice." },
            { "L:foxglove", "Points are points. You earned these." }, { "L:kfoxglove", "You weren't nervous at all. I was wrong." },
            { "L:martini", "One for your cabinet, then." }, { "L:lonestar", "Gorilla mode... off." },
            { "L:mocha", "Cold out there. Colder now." }, { "L:daisies", "Went big. Went home." },
            { "L:bindr", "Everyone's a nobody once. You're not anymore." }, { "L:punchline", "My office. Your trophy." },
            { "L:glazer", "Fine. Pick a different game next time." }, { "L:mizrockie", "Defending champ, defeated. Take it." },
        };
        public static string WinLine(string sp) => sp == "trent" ? "Write it down. Some people are born for this." : sp == "lou" ? "Not today, kid. Thirty years of practice." : sp == "ghost" ? "Busted is information. Read it." : ClassicWin.TryGetValue(sp, out var t) ? t : "Good game.";
        public static string SpOf(string boss) => boss == null ? null : DB.Legend(boss) != null ? "L:" + boss : boss;
        public static string NameOf(string boss)
        {
            if (boss == null) return "";
            var lg = DB.Legend(boss); if (lg != null) return J.S(lg, "name", boss);
            return J.S(DB.Person(boss), "name", boss);
        }
        static string Pre(string boss) { var sp = SpOf(boss); if (PreLine.TryGetValue(sp, out var t)) return t; var l = J.S(DB.Legend(boss), "line", ""); return string.IsNullOrEmpty(l) || l == "…" ? "Final table. Good luck." : l; }
        static void Cast(string scene, string boss, string line, string venue, bool online)
        {
            var sc = DB.Scenes[scene]; sc.place = online || string.IsNullOrEmpty(venue) ? "home" : venue;
            var b = sc.beats[1]; b.sp = SpOf(boss); b.t = line; b.who = null; b.fn = false; b.text = online;
        }
        // the final table is set: a short scene with whoever's waiting there, then back to the felt
        public static bool FtIntro(Run run)
        {
            if (run?.boss == null || run.ev.kind == "cash" || run.ev.kind == "sat" || run.drill != null) return false;
            Cast("ft_intro", run.boss, Pre(run.boss), run.ev.venue, run.ev.online); return true;
        }
        // after it: you beat them, or they got you (the Classic has its own scenes)
        public static string FtEnd(Result res)
        {
            if (res?.met == null || res.ev.id == "classic2") return null;
            bool won = res.place == 1;
            Cast(won ? "ft_won" : "ft_lost", res.met, won ? (LoseLine.TryGetValue(SpOf(res.met), out var l) ? l : "Good game.") : WinLine(SpOf(res.met)), res.ev.venue, res.ev.online);
            return won ? "ft_won" : "ft_lost";
        }
        // Mustich and Tommy each get their own scene; everyone else (Slick and the legends) shares one, with their line in it
        public static string ClassicLoss(State s, string boss = null)
        {
            int n = (int)s.FlagN("classics"); s.SetFlag("classics", n + 1);
            // you lost to someone at the final table: they're the one holding the trophy
            if (boss == "trent") return "classic_mustich";
            if (boss == "lou") return "classic_shark";
            if (boss != null && ClassicWin.ContainsKey(SpOf(boss))) { var bb = DB.Scenes["classic_other"].beats[3]; bb.sp = SpOf(boss); bb.t = ClassicWin[SpOf(boss)]; bb.who = null; bb.fn = false; return "classic_other"; }
            var rng = new System.Random(s.cal.seed.GetHashCode() ^ (n * 7919));
            double r = rng.NextDouble();
            if (r < 0.2) return "classic_mustich";
            if (r < 0.32) return "classic_shark";
            var pool = ClassicWin.Keys.ToList(); var who = pool[rng.Next(pool.Count)];
            var b = DB.Scenes["classic_other"].beats[3]; b.sp = who; b.t = ClassicWin[who]; b.who = null; b.fn = false;
            return "classic_other";
        }
        static void Insert(string id, int i, Beat b) { if (DB.Scenes.TryGetValue(id, out var sc)) sc.beats.Insert(Math.Min(i, sc.beats.Count), b); }
        static void Replace(string id, int i, string sp, string t) { if (DB.Scenes.TryGetValue(id, out var sc) && i < sc.beats.Count) { sc.beats[i].sp = sp; sc.beats[i].t = t; sc.beats[i].fn = false; } }

        // beats whose text depends on the career
        // the hand that cost you most in the event you just played (null when there isn't a clear one)
        static HandInfo LessonHand(Result r)
        {
            if (r == null) return null;
            var dec = r.keys.FirstOrDefault(x => x.tier == "bad") ?? r.all.OrderBy(x => x.pts).FirstOrDefault();
            var h = dec?.hand; if (h == null || h.cards == null || h.cards.Length < 2 || string.IsNullOrEmpty(h.best) || string.IsNullOrEmpty(h.mine) || h.mineK == h.bestK) return null;
            return h;
        }
        static string LessonRank(int c) { var r = Cards.Rank(c); return r == "T" ? "10" : r; }
        static string LessonHole(HandInfo h) => $"{LessonRank(h.cards[0])}{Cards.SuitSym(h.cards[0])} {LessonRank(h.cards[1])}{Cards.SuitSym(h.cards[1])}";
        static string LessonPos(string pos)
        {
            switch (pos) { case "BTN": return "the button"; case "SB": return "the small blind"; case "BB": return "the big blind"; case "CO": return "the cutoff"; case "HJ": return "the hijack"; case "UTG": case "UTG+1": case "MP": return "early position"; default: return string.IsNullOrEmpty(pos) ? "the table" : pos; }
        }
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
            if (sceneId == "c3" && index == 0) { string nx = Calendar.NextWcs(s); return "Every June, Las Vegas hosts the WCS Summer Series. 112 degrees outside, 68 inside, ten thousand people chasing the same gold. " + (nx == "now" ? "It's running right now." : $"It starts {nx}. It's on your calendar."); }
            if (sceneId == "c5" && index == 0) { string nx = Calendar.NextTitan(s); return "A private terminal. A plane with your name on the manifest. A buy-in that used to be a house. " + (nx == "now" ? "The Series is on right now." : $"The next Titan Series starts {nx}."); }
            if (sceneId == "c6" && index == 0) { string nx = Calendar.NextMain(s); return "Every July: eight thousand players, ten thousand dollars each.\nOne of them ends up on the cover of every poker magazine in the world.\n" + (nx == "today" ? "The Main Event starts today." : $"The next Main Event is {nx}."); }
            if (sceneId.StartsWith("lesson") && sceneId.Length == 7 && char.IsDigit(sceneId[6]))
            {
                var lh = LessonHand(Game.Last);
                if (lh == null) return b.t ?? "";
                int L = sceneId[6] - '0';
                if (L == 3 && index == 0) return "A Vegas hallway at 3 AM. Slot machines, and a little regret.";
                if (index == 1) return $"Tell me about {LessonHole(lh)} from {LessonPos(lh.pos)}.";
                if (index == 2) switch (lh.mineK) { case "fold": return "I let it go. It felt too dangerous."; case "call": return "I called. I thought I was ahead."; case "raise": case "raiseB": return "I raised. I thought I could take it."; case "allin": return "I jammed. I thought it was my spot."; default: return "I did what felt right."; }
                if (index == 3) return $"The solver wanted {lh.best}. You chose {lh.mine}. " + (L == 1 ? "Then the hand didn't get you. The first decision did. Everything after was a story you told yourself." : L == 2 ? "When nobody has anything, the one who bets small and often takes it. Every free card you give is a gift." : L == 3 ? "Near the bubble, the chips you lose are worth more than the chips you win. That is the whole of ICM." : "A bluff is a bet against a person, not a board. Pick the person first.");
            }
                        if (sceneId == "c2" && index == 0) { string nx = Calendar.NextCircuit(s); return "You've outgrown Biscayne. " + (nx == "now" ? "The Circuit is at Silver Strike right now." : $"The next Circuit stop at Silver Strike starts {nx}.") + " Every event gives a ring. Go get one."; }
            if (sceneId == "c2" && index == 2) return s.job == "nights" ? "You're the kid with the day job? I had one too. Spreadsheets and everything. Play your game and let them find out." : "You're the kid who quit the cubicle? I was an accountant. Spreadsheets and everything. Play your game and let them find out.";
            if (sceneId == "ft_intro")
            {
                var run = Game.Run; string nm = NameOf(run?.boss);
                return run == null ? "Final table." : $"Final table. {run.ev.name}.\nNine players left, and one of them is {nm}.";
            }
            if (sceneId == "ft_won" || sceneId == "ft_lost")
            {
                var r = Game.Last; string nm = NameOf(r?.met);
                return sceneId == "ft_won" ? $"Final hand. You and {nm}, everything in the middle.\nYour cards hold. {r?.ev?.name} is yours."
                    : $"Final hand. You and {nm}, everything in the middle.\nTheir cards hold. You're out in {Fmt.Ord(r?.place ?? 0)}" + ((r?.prize ?? 0) > 0 ? $", with ${Fmt.Money(r.prize)}." : ".");
            }
            if (sceneId == "classic_mustich" || sceneId == "classic_shark" || sceneId == "classic_other")
            {
                var r = Game.Last; int place = r?.place ?? 0; double prize = r?.prize ?? 0;
                if (index == 0) return place > 0 ? $"Sunday night. Day 2 of the Biscayne Classic.\nYou went out in {Fmt.Ord(place)}" + (prize > 0 ? $" for ${Fmt.Money(prize)}." : ", short of the money.") : "Sunday night. Day 2 of the Biscayne Classic.";
                return place > 0 && place <= 9 ? $"Final table. {Fmt.Ord(place)}. Close enough to touch it." : prize > 0 ? $"{Fmt.Ord(place)}. On the board, not the trophy. Next month." : "Made Day 2 and still went home with nothing. Next month.";
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
                case "p_offer":
                case "p_after":
                    s.cal.d = Math.Max(s.cal.d, 7); s.SetFlag("needjob", 0); // Monday
                    if (k == 0) { s.job = "quit"; Career.Add(s, Calendar.GiftQuit, "Slick's backing"); }
                    else { s.job = "nights"; Career.Add(s, 2000, "Savings, keeping the day job"); }
                    break;
                case "tour1": s.SetFlag(k == 0 ? "quote" : "humble"); break;
                case "broke":
                    if (k == 0) { double amt = Career.StakeSize(s); Career.Add(s, amt, "Slick's stake"); s.debt += amt; s.SetFlag("stakes", s.FlagN("stakes") + 1); Career.Moment(s, "🥧", $"Slick staked you ${Fmt.Money(amt)}"); }
                    if (k == 2) { Career.Add(s, 500, "From Slick, after Lou said no"); Career.Moment(s, "🚫", "Lou said no. Slick sent $500."); }
                    break;
                case "broke_n":
                    if (k == 0) { double amt = Career.StakeSize(s); Career.Add(s, amt, "Slick's stake"); s.debt += amt; s.SetFlag("stakes", s.FlagN("stakes") + 1); Career.Moment(s, "🥧", $"Slick staked you ${Fmt.Money(amt)}"); }
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
            if (id == "broke_told") { s.SetFlag("slick_off"); Career.Moment(s, "🕶️", "Slick walked away. Build it back to $10,000."); }
            if (id == "slick_back") { s.SetFlag("slick_off", 0); s.SetFlag("shots", 0); Career.Moment(s, "🤝", "Said sorry. Slick is back in your corner."); }
            if (id == "lou_no") { Career.Add(s, 500, "From Slick, after Lou said no"); s.SetFlag("lou500", s.cal.d + 1); }
            if (id == "quit250") { s.job = "quit"; Career.Moment(s, "🚪", "A quarter million. Quit the day job for good."); }
        }
        // the boss scene depends on whether you still work for him
        public static string Resolve(string id, State s) => id == "derek" && s.job == "nights" ? "derek_job" : id == "broke" && s.job == "nights" ? "broke_n" : id;
    }
}
