// Beta 1.0's whole game: title -> reel (first launch) -> prologue films -> the Biscayne Local Championship -> Chapter 1
// hub and events -> the table -> results -> Chapter 1's story films -> the goal -> the end card. Saves in PlayerPrefs.
using System;
using System.Collections.Generic;
using System.Linq;
using Dream.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Dream.Game
{
    [Serializable]
    public class Save
    {
        public string step = "new";           // new, prologue, choice, ch1, done
        public double br, peak, debt;         // bankroll, best bankroll, what's owed to Slick
        public string job = "";               // "quit" (full-time) or "nights" (kept the day job), as in the old game
        public int day = -1, liveDay = -99, liveWeek = -99;   // calendar day (0 = Mon 5 Jan 2026); last live game day and week
        public bool reelSeen, roomTitle;
        public List<string> story = new List<string>();   // Yarn's story variables ($job, seen flags), saved as name=value
        public int events, cashes, decisions, perfect;
        public List<string> log = new List<string>();

        const string Key = "dream_beta1_save";
        public static Save Load() { try { var j = PlayerPrefs.GetString(Key, ""); return string.IsNullOrEmpty(j) ? null : JsonUtility.FromJson<Save>(j); } catch { return null; } }
        public void Write() { PlayerPrefs.SetString(Key, JsonUtility.ToJson(this)); PlayerPrefs.Save(); }
        public static void Erase() { PlayerPrefs.DeleteKey(Key); }
    }

    public class Flow : MonoBehaviour
    {
        public static Flow I;
        public Save S;
        Canvas canvas; RectTransform root;
        public const double Goal = 10000;                                         // the Chapter 1 goal (also in Chapter1.yarn)
        public const double Salary = 2000;                                        // old Calendar.Salary, every Friday with the day job
        Story story;
        static readonly DateTime Day0 = new DateTime(2026, 1, 5);                 // old Calendar.Day0: a Monday
        static int Dow(int d) => (int)Day0.AddDays(d).DayOfWeek;
        static int WeekOf(int d) => (int)Math.Floor((d + 1) / 7.0);               // weeks run Sunday to Saturday
        static bool Weekend(int d) { int w = Dow(d); return w == 0 || w == 6; }
        static string DateStr(int d) => Day0.AddDays(d).ToString("dddd, MMM d", System.Globalization.CultureInfo.InvariantCulture);
        public string Rep => S.roomTitle ? "Biscayne Champion" : S.job == "quit" ? "Full-time Dreamer" : "Moonlighter";   // old Career.Rep, Chapter 1
        // old Calendar.CantPlay for live card-room games: null when you can play today
        string CantPlay()
        {
            if (S.job == "nights" && !Weekend(S.day)) return "Live games are weekends only while you have the day job";
            if (S.job == "nights" && S.liveWeek == WeekOf(S.day)) return "Your live game this weekend is played";
            if (S.job != "nights" && S.liveDay == S.day) return "One live game a day";
            return null;
        }
        public const string FeedbackUrl = "https://github.com/Andrxtti/slick-rng/issues/new?title=The%20Dream%20Beta%201.0%20feedback";

        void Start()
        {
            I = this; Application.targetFrameRate = 60;
            var cam = new GameObject("UiCamera").AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Ui.Night;
            canvas = Ui.Root("Dream"); root = (RectTransform)canvas.transform;
            Lib.Load();
            story = Story.Make(transform);
            Title();
        }

        RectTransform Screen(Color bg) { Ui.Clear(root); return Ui.Panel(root, "Screen", bg); }

        // ------------------------------------------------------------ title
        public void Title()
        {
            S = Save.Load();
            var s = Screen(Ui.Night);
            Ui.At(Ui.Label(s, "THE DREAM", 120, Ui.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 0.62f, 1, 0.78f);
            Ui.At(Ui.Label(s, "Beta 1.0  ·  Chapter 1: The Grind", 38, Ui.Gold), 0, 0.56f, 1, 0.62f);
            Ui.At(Ui.Label(s, "Every decision is a real solver spot.\nCoaching by Confirmed Fire.", 32, Ui.Dim), 0.1f, 0.46f, 0.9f, 0.54f);
            bool can = S != null && S.step != "new";
            Ui.At(Ui.Button(s, can ? "Continue" : "Begin", () => { if (can) Resume(); else NewCareer(); }, Ui.Gold, Ui.Night, 54), 0.15f, 0.3f, 0.85f, 0.37f);
            if (can) Ui.At(Ui.Button(s, "New career", () => { Save.Erase(); NewCareer(); }, Ui.Card, Ui.Ink, 40), 0.15f, 0.21f, 0.85f, 0.27f);
            Ui.At(Ui.Button(s, "Replay the reel", () => Films.Play(new[] { "reel" }, Title), Ui.Card, Ui.Dim, 36), 0.25f, 0.12f, 0.75f, 0.17f);
        }

        void NewCareer()
        {
            bool seen = S != null && S.reelSeen;
            S = new Save { step = "prologue", br = 0, day = 5, reelSeen = true }; S.Write();   // Saturday: the Biscayne Championship (free); the bankroll starts at zero, as in the old game
            var films = new List<string>(); if (!seen) films.Add("reel");
            films.AddRange(new[] { "p_office", "p_diner", "p_seat" });
            Films.Play(films, () => Play("local"));
        }

        void Resume()
        {
            if (S.step == "prologue") { Films.Play(new[] { "p_seat" }, () => Play("local")); return; }
            if (S.step == "choice") { story.Run("Decision", AfterStory); return; }
            if (S.step == "done") { EndCard(); return; }
            Hub();
        }

        // ------------------------------------------------------------ the hub
        public void Hub(string note = null)
        {
            var s = Screen(Ui.Night);
            Ui.At(Ui.Label(s, "BISCAYNE CARD ROOM", 40, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold), 0.06f, 0.92f, 0.94f, 0.96f);
            Ui.At(Ui.Label(s, "Chapter 1 · The Grind", 30, Ui.Dim, TextAnchor.MiddleLeft), 0.06f, 0.885f, 0.94f, 0.92f);
            Ui.At(Ui.Label(s, Ui.Money(S.br), 96, Ui.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), 0.06f, 0.79f, 0.94f, 0.87f);
            Ui.At(Ui.Label(s, "bankroll  ·  " + Rep, 28, Ui.Dim, TextAnchor.MiddleLeft), 0.06f, 0.765f, 0.94f, 0.79f);
            Ui.At(Ui.Label(s, DateStr(S.day), 28, Ui.Ink, TextAnchor.MiddleRight), 0.5f, 0.885f, 0.94f, 0.92f);
            // goal
            var bar = Ui.At(Ui.Panel(s, "GoalBar", Ui.Card), 0.06f, 0.715f, 0.94f, 0.735f);
            var fill = Ui.Panel(bar, "Fill", Ui.Gold); fill.anchorMax = new Vector2(Mathf.Clamp01((float)(S.br / Goal)), 1);
            Ui.At(Ui.Label(s, "Goal: win the Biscayne Classic, or build a $10,000 bankroll", 28, Ui.Dim, TextAnchor.MiddleLeft), 0.06f, 0.68f, 0.94f, 0.71f);
            if (note != null) Ui.At(Ui.Label(s, note, 32, Ui.Aqua, TextAnchor.MiddleLeft), 0.06f, 0.63f, 0.94f, 0.675f);
            float y = 0.6f; string why = CantPlay();
            foreach (var ev in Lib.Events.Where(e => e.ch == 1))
            {
                bool afford = S.br >= ev.buyin && why == null;
                var row = Ui.At(Ui.Panel(s, ev.id, Ui.Card), 0.06f, y - 0.1f, 0.94f, y);
                Ui.At(Ui.Label(row, ev.name, 40, afford ? Ui.Ink : Ui.Dim, TextAnchor.UpperLeft, FontStyle.Bold), 0.05f, 0.45f, 0.7f, 0.9f);
                Ui.At(Ui.Label(row, why ?? (S.br < ev.buyin ? "Not in your roll" : ev.blurb), 26, why != null || S.br < ev.buyin ? Ui.Neon : Ui.Dim, TextAnchor.UpperLeft), 0.05f, 0.08f, 0.7f, 0.45f);
                string price = (ev.kind == "cash" ? "Sit with " : "Buy-in ") + Ui.Money(ev.buyin) + (ev.field > 0 ? $"\n{ev.field} players" : "\n100 bb deep");
                Ui.At(Ui.Label(row, price, 26, Ui.Gold, TextAnchor.MiddleRight), 0.62f, 0.1f, 0.95f, 0.9f);
                var b = row.gameObject.AddComponent<Button>(); var id = ev.id;
                b.interactable = afford; b.onClick.AddListener(() => Enter(id));
                y -= 0.115f;
            }
            if (why != null) Ui.At(Ui.Button(s, S.job == "nights" ? "Sleep until the weekend" : "Sleep", Sleep, Ui.Aqua, Ui.Night, 42), 0.15f, 0.09f, 0.85f, 0.15f);
            Ui.At(Ui.Button(s, "Replay the reel", () => Films.Play(new[] { "reel" }, () => Hub()), Ui.Card, Ui.Dim, 30), 0.06f, 0.025f, 0.46f, 0.07f);
            Ui.At(Ui.Button(s, "Title", Title, Ui.Card, Ui.Dim, 30), 0.54f, 0.025f, 0.94f, 0.07f);
        }

        // a Yarn choice in the game's style: the card lines on top (first small, second big, the rest small), one button
        // per option with its hint under it
        public void ChoiceCard(List<string> cardLines, List<string> options, List<string> hints, Action<int> pick)
        {
            var s = Screen(Color.black);
            float y = 0.78f;
            for (int i = 0; i < cardLines.Count; i++)
            {
                bool big = i == 1 || cardLines.Count == 1; float h = big ? 0.1f : 0.04f;
                Ui.At(Ui.Label(s, cardLines[i], big ? 96 : 30, big ? Ui.Ink : Ui.Dim, TextAnchor.MiddleCenter, big ? FontStyle.Bold : FontStyle.Normal), 0.06f, y - h, 0.94f, y);
                y -= h + 0.005f;
            }
            y -= 0.05f;
            for (int i = 0; i < options.Count; i++)
            {
                int k = i; bool first = i == 0;
                Ui.At(Ui.Button(s, options[i], () => pick(k), first ? Ui.Gold : Ui.Card, first ? Ui.Night : Ui.Ink, 54), 0.12f, y - 0.08f, 0.88f, y);
                if (i < hints.Count) Ui.At(Ui.Label(s, hints[i], 28, first ? Ui.Gold : Ui.Dim), 0.08f, y - 0.12f, 0.92f, y - 0.085f);
                y -= 0.16f;
            }
        }

        // a plain Yarn line: a card on black, tap to go on
        public void Line(string text, Action next)
        {
            var s = Screen(Color.black);
            Ui.At(Ui.Label(s, text, 54, Ui.Ink), 0.08f, 0.4f, 0.92f, 0.6f);
            Ui.At(Ui.Button(s, "Continue", next, Ui.Gold, Ui.Night, 48), 0.2f, 0.08f, 0.8f, 0.14f);
        }

        // run a story node with a given job and bankroll (the test command "story <node> <job> <bankroll>")
        public void TestStory(string node, string job, double br)
        {
            S ??= new Save(); S.step = "ch1"; S.job = job; S.br = br; S.day = 13;
            S.story.RemoveAll(v => v.StartsWith("$job=")); S.story.Add("$job=" + job);
            story.Run(node, AfterStory, "nightly", false, true, false);
        }

        // after a story run: the prologue's choice moves the calendar to Monday night; the end of the chapter is the end card
        void AfterStory()
        {
            if (S.step == "ch1" && S.day < 7) { S.day = 7; S.Write(); }   // Monday night: Chapter 1 starts
            if (S.step == "done") EndCard(); else Hub();
        }

        // sleep to the next day you can play (old Calendar.SkipTo); the day job pays every Friday on the way
        void Sleep()
        {
            var news = new List<string>();
            do
            {
                S.day++;
                if (S.job == "nights" && Dow(S.day) == 5) { S.br += Salary; news.Add($"Payday. +{Ui.Money(Salary)} from the day job."); }
            } while (CantPlay() != null);
            S.peak = Math.Max(S.peak, S.br); S.Write();
            story.Run("AfterSleep", () => { if (S.step == "done") EndCard(); else Hub(news.Count > 0 ? string.Join("  ", news) : null); });
        }

        // texts on the phone: sender, the messages, tap to go on
        public void Texts(string from, IList<string> lines, Action next)
        {
            var s = Screen(Ui.Night);
            Ui.At(Ui.Label(s, from, 44, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 0.8f, 1, 0.86f);
            float y = 0.74f;
            foreach (var l in lines)
            {
                var bubble = Ui.At(Ui.Panel(s, "Text", Ui.Card), 0.08f, y - 0.1f, 0.92f, y);
                Ui.At(Ui.Label(bubble, l, 36, Ui.Ink, TextAnchor.MiddleLeft), 0.05f, 0.05f, 0.95f, 0.95f);
                y -= 0.12f;
            }
            Ui.At(Ui.Button(s, "Continue", next, Ui.Gold, Ui.Night, 48), 0.2f, 0.08f, 0.8f, 0.14f);
        }

        void Enter(string id)
        {
            var ev = Lib.Event(id);
            story.Run("BeforeEvent", () => Play(id), id);   // Yarn plays the Classic's opener the first time
        }

        // ------------------------------------------------------------ an event
        void Play(string id)
        {
            var ev = Lib.Event(id);
            if (!ev.free) S.br -= ev.buyin;
            S.Write();
            Hands.Chapter = S.step == "prologue" ? 0 : 1;
            var run = Hands.Start(ev, $"{id}|{DateTime.UtcNow.Ticks}");
            Ui.Clear(root);
            Table.Show(root, run, (bad) => Finish(run, bad));
        }

        void Finish(Run run, bool hadMistake)
        {
            var ev = run.ev; int n = run.results.Count; double prize = 0; int place = 0, paid = 0;
            if (ev.kind == "cash") { double net = run.bust ? -ev.buyin : Hands.CashNet(run, (run.rng.Next() - 0.5) * 0.3 + ev.soft * 0.5); prize = ev.buyin + net; }
            else { paid = Hands.Paid(ev); place = Hands.Place(run); prize = place <= paid ? Hands.PrizeFor(ev, place) : 0; }
            if (S.debt > 0 && prize - (ev.free ? 0 : ev.buyin) > 0)   // half your profits go back to Slick until the stake is repaid
            {
                double back = Math.Min(S.debt, Math.Round((prize - ev.buyin) / 2)); S.debt -= back; prize -= back;
                S.log.Insert(0, $"Repaid Slick {Ui.Money(back)}" + (S.debt <= 0 ? " (all of it)" : ""));
            }
            S.br += prize; S.peak = Math.Max(S.peak, S.br); S.events++; S.liveDay = S.day; S.liveWeek = WeekOf(S.day); S.decisions += n; S.perfect += run.results.Count(r => r.tier == "perfect");
            if (prize > 0) S.cashes++;
            bool won = ev.kind != "cash" && place == 1;
            if (won && ev.title == "room") S.roomTitle = true;
            S.log.Insert(0, ev.kind == "cash" ? $"{ev.name}: {(prize - ev.buyin >= 0 ? "+" : "")}{Ui.Money(prize - ev.buyin)}" : $"{ev.name}: {Ord(place)} of {ev.field}" + (prize > 0 ? $", {Ui.Money(prize)}" : ""));
            bool bust = ev.kind == "cash" ? prize < ev.buyin * 0.5 : prize <= 0;
            if (ev.id == "local") S.step = "choice";
            S.Write();
            Results(run, place, paid, prize, () => story.Run("AfterEvent", AfterStory, ev.id, won, bust, hadMistake));
        }

        static string Ord(int n) => n + (n % 100 is 11 or 12 or 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" });

        void Results(Run run, int place, int paid, double prize, Action next)
        {
            var ev = run.ev; var s = Screen(Ui.Night);
            Ui.At(Ui.Label(s, ev.name.ToUpperInvariant(), 36, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 0.05f, 0.88f, 0.95f, 0.93f);
            string head = ev.kind == "cash" ? (prize >= ev.buyin ? "Up " + Ui.Money(prize - ev.buyin) : "Down " + Ui.Money(ev.buyin - prize))
                : place == 1 ? "You won it." : prize > 0 ? $"{Ord(place)} place" : run.cooler != null ? "Coolered." : $"Out in {Ord(place)}";
            Ui.At(Ui.Label(s, head, 84, Ui.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), 0.05f, 0.77f, 0.95f, 0.87f);
            Ui.At(Ui.Label(s, prize > 0 ? "Paid " + Ui.Money(prize) : ev.kind == "cash" ? "" : $"{paid} paid of {ev.field}", 40, prize > 0 ? Ui.Gold : Ui.Dim), 0.05f, 0.72f, 0.95f, 0.77f);
            if (run.cooler != null) Ui.At(Ui.Label(s, "You did nothing wrong. Aces ran into kings between levels.", 30, Ui.Dim), 0.08f, 0.66f, 0.92f, 0.71f);
            Ui.At(Ui.Label(s, $"Accuracy {Hands.Accuracy(run.results)}%  ·  {run.results.Count} decisions", 36, Ui.Ink), 0.05f, 0.6f, 0.95f, 0.65f);
            float y = 0.56f;
            foreach (var r in run.results.Take(10))
            {
                var L = Lib.Labels[r.lab];
                var row = Ui.At(Ui.Box(s, "R"), 0.08f, y - 0.035f, 0.92f, y);
                string cards = Hands.Card(r.spot.hand[0]) + " " + Hands.Card(r.spot.hand[1]);
                Ui.At(Ui.Label(row, $"{cards}   {r.spot.seats[r.spot.hero].pos}   {r.mine}", 28, Ui.Ink, TextAnchor.MiddleLeft), 0, 0, 0.65f, 1);
                Ui.At(Ui.Label(row, L.n, 28, Table.Hex(L.c), TextAnchor.MiddleRight, FontStyle.Bold), 0.6f, 0, 1, 1);
                y -= 0.04f;
            }
            Ui.At(Ui.Label(s, "Bankroll " + Ui.Money(S.br), 40, Ui.Gold), 0.05f, 0.12f, 0.95f, 0.16f);
            Ui.At(Ui.Button(s, "Continue", next, Ui.Gold, Ui.Night, 48), 0.2f, 0.04f, 0.8f, 0.1f);
        }

        // ------------------------------------------------------------ the end of the beta
        public void EndCard()
        {
            var s = Screen(Color.black);
            Ui.At(Ui.Label(s, "Beta 1.0", 44, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 0.66f, 1, 0.71f);
            Ui.At(Ui.Label(s, "Chapter 2: The Circuit\ncoming soon", 72, Ui.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), 0.05f, 0.52f, 0.95f, 0.65f);
            Ui.At(Ui.Label(s, $"{Rep}  ·  Bankroll {Ui.Money(S.br)}  ·  {S.events} events  ·  {S.decisions} decisions", 30, Ui.Dim), 0.05f, 0.45f, 0.95f, 0.49f);
            Ui.At(Ui.Button(s, "Send feedback", () => Application.OpenURL(FeedbackUrl), Ui.Gold, Ui.Night, 48), 0.18f, 0.3f, 0.82f, 0.36f);
            Ui.At(Ui.Button(s, "Keep playing Chapter 1", () => { S.step = "ch1"; S.Write(); Hub(); }, Ui.Card, Ui.Ink, 36), 0.18f, 0.21f, 0.82f, 0.26f);
            Ui.At(Ui.Button(s, "Title", Title, Ui.Card, Ui.Dim, 32), 0.3f, 0.12f, 0.7f, 0.17f);
        }
    }
}
