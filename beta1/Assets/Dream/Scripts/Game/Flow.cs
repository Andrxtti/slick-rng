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
        public string step = "new";           // new, prologue, ch1, done
        public double br;                     // bankroll
        public bool reelSeen, roomTitle, lessonSeen, needleSeen, classicSeen, classicLoseSeen;
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
        public const double Goal = 10000, Savings = 1000, Stake = 1000;
        public const string FeedbackUrl = "https://github.com/Andrxtti/slick-rng/issues/new?title=The%20Dream%20Beta%201.0%20feedback";

        void Start()
        {
            I = this; Application.targetFrameRate = 60;
            var cam = new GameObject("UiCamera").AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Ui.Night;
            canvas = Ui.Root("Dream"); root = (RectTransform)canvas.transform;
            Lib.Load();
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
            S = new Save { step = "prologue", br = Savings, reelSeen = true }; S.Write();
            var films = new List<string>(); if (!seen) films.Add("reel");
            films.AddRange(new[] { "p_office", "p_diner", "p_seat" });
            Films.Play(films, () => Play("local"));
        }

        void Resume()
        {
            if (S.step == "prologue") { Films.Play(new[] { "p_seat" }, () => Play("local")); return; }
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
            Ui.At(Ui.Label(s, "bankroll", 28, Ui.Dim, TextAnchor.MiddleLeft), 0.06f, 0.765f, 0.5f, 0.79f);
            // goal
            var bar = Ui.At(Ui.Panel(s, "GoalBar", Ui.Card), 0.06f, 0.715f, 0.94f, 0.735f);
            var fill = Ui.Panel(bar, "Fill", Ui.Gold); fill.anchorMax = new Vector2(Mathf.Clamp01((float)(S.br / Goal)), 1);
            Ui.At(Ui.Label(s, "Goal: win the Biscayne Classic, or build a $10,000 bankroll", 28, Ui.Dim, TextAnchor.MiddleLeft), 0.06f, 0.68f, 0.94f, 0.71f);
            if (note != null) Ui.At(Ui.Label(s, note, 32, Ui.Aqua, TextAnchor.MiddleLeft), 0.06f, 0.63f, 0.94f, 0.675f);
            float y = 0.6f;
            foreach (var ev in Lib.Events.Where(e => e.ch == 1))
            {
                bool afford = S.br >= ev.buyin;
                var row = Ui.At(Ui.Panel(s, ev.id, Ui.Card), 0.06f, y - 0.1f, 0.94f, y);
                Ui.At(Ui.Label(row, ev.name, 40, afford ? Ui.Ink : Ui.Dim, TextAnchor.UpperLeft, FontStyle.Bold), 0.05f, 0.45f, 0.7f, 0.9f);
                Ui.At(Ui.Label(row, ev.blurb, 26, Ui.Dim, TextAnchor.UpperLeft), 0.05f, 0.08f, 0.7f, 0.45f);
                string price = (ev.kind == "cash" ? "Sit with " : "Buy-in ") + Ui.Money(ev.buyin) + (ev.field > 0 ? $"\n{ev.field} players" : "\n100 bb deep");
                Ui.At(Ui.Label(row, price, 26, Ui.Gold, TextAnchor.MiddleRight), 0.62f, 0.1f, 0.95f, 0.9f);
                var b = row.gameObject.AddComponent<Button>(); var id = ev.id;
                b.interactable = afford; b.onClick.AddListener(() => Enter(id));
                y -= 0.115f;
            }
            Ui.At(Ui.Button(s, "Replay the reel", () => Films.Play(new[] { "reel" }, () => Hub()), Ui.Card, Ui.Dim, 30), 0.06f, 0.025f, 0.46f, 0.07f);
            Ui.At(Ui.Button(s, "Title", Title, Ui.Card, Ui.Dim, 30), 0.54f, 0.025f, 0.94f, 0.07f);
        }

        void Enter(string id)
        {
            var ev = Lib.Event(id);
            if (id == "classic" && !S.classicSeen) { S.classicSeen = true; S.Write(); Films.Play(new[] { "classic" }, () => Play(id)); return; }
            Play(id);
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
            S.br += prize; S.events++; S.decisions += n; S.perfect += run.results.Count(r => r.tier == "perfect");
            if (prize > 0) S.cashes++;
            bool won = ev.kind != "cash" && place == 1;
            if (won && ev.title == "room") S.roomTitle = true;
            S.log.Insert(0, ev.kind == "cash" ? $"{ev.name}: {(prize - ev.buyin >= 0 ? "+" : "")}{Ui.Money(prize - ev.buyin)}" : $"{ev.name}: {Ord(place)} of {ev.field}" + (prize > 0 ? $", {Ui.Money(prize)}" : ""));
            // the story films this result calls for
            var films = new List<string>();
            if (ev.id == "classic") { if (won) films.Add("classic_win"); else if (!S.classicLoseSeen) { films.Add("classic_lose"); S.classicLoseSeen = true; } }
            if (hadMistake && !S.needleSeen && S.step == "ch1") { films.Add("needle"); S.needleSeen = true; }
            bool bust = ev.kind == "cash" ? prize < ev.buyin * 0.5 : prize <= 0;
            if (bust && !S.lessonSeen && S.step == "ch1") { films.Add("lesson1"); S.lessonSeen = true; }
            string note = null;
            if (ev.id == "local") { films.Add("p_after"); films.Add("c1"); S.step = "ch1"; }
            if (S.step == "ch1" && (S.roomTitle || S.br >= Goal)) { films.Add("goal"); films.Add("end"); S.step = "done"; }
            if (S.step == "ch1" && S.br < 60) { S.br += Stake; note = "Slick staked you $1,000. Half your profits go back until it's repaid."; }
            S.Write();
            Results(run, place, paid, prize, () => Films.Play(films, () => { if (S.step == "done") EndCard(); else Hub(note); }));
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
            Ui.At(Ui.Label(s, $"Bankroll {Ui.Money(S.br)}  ·  {S.events} events  ·  {S.decisions} decisions", 30, Ui.Dim), 0.05f, 0.45f, 0.95f, 0.49f);
            Ui.At(Ui.Button(s, "Send feedback", () => Application.OpenURL(FeedbackUrl), Ui.Gold, Ui.Night, 48), 0.18f, 0.3f, 0.82f, 0.36f);
            Ui.At(Ui.Button(s, "Keep playing Chapter 1", () => { S.step = "ch1"; S.Write(); Hub(); }, Ui.Card, Ui.Ink, 36), 0.18f, 0.21f, 0.82f, 0.26f);
            Ui.At(Ui.Button(s, "Title", Title, Ui.Card, Ui.Dim, 32), 0.3f, 0.12f, 0.7f, 0.17f);
        }
    }
}
