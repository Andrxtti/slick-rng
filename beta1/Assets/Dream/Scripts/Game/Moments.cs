// The moments around the hands (unity/docs/TABLE.md): the all-in screen and the cooler card (ported from the old game's
// Showdown.cs and Run.cs), the level card with Slick's tip and the final-table rival bar, the bubble, in the money,
// heads-up, table talk, and the walk away after each event. Every moment knows where it is (TABLE.md §11): live at
// Biscayne it has the room, the floor mic and the people; online at CF Tilt it has a banner, the chat box, screen names
// and the phone, and never a dealer, a cage, a trophy or anyone in person.
using System;
using System.Collections.Generic;
using System.Linq;
using Dream.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dream.Game
{
    // ------------------------------------------------------------ the all-in (old Showdown.cs, the logic verbatim)
    public class Showdown
    {
        public int[] hero, opp, board; public bool win, folded, chop; public double eq; public string oppName, oppSeat, heroHand, oppHand; public int before, after;

        // did this decision put you all in?
        public static bool AllIn(Spot sp, int j)
        {
            if (sp == null || j < 0 || j >= sp.acts.Count) return false;
            string a = sp.acts[j];
            if (a == "allin") return true;
            return a == "call" && (sp.path.Contains("allin") || (sp.cat ?? "").IndexOf("jam", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        // always at the bubble and the final table; otherwise about half the time, never two hands running, never when
        // the player turned the animations off
        public static bool Plays(Run run, Decision d)
        {
            if (PlayerPrefs.GetInt("sd_skip", 0) > 0) return false;
            string st = run.Stage;
            if (st == "final" || st == "bubble" || st == "satbubble") return true;
            if (run.showH == run.h - 1 && run.si == run.showSi) return false;
            var rng = new System.Random((int)(Rng.Hash($"sdp|{run.ev.id}|{run.si}|{run.h}|{d.j}") & 0x7fffffff));
            return rng.NextDouble() < 0.55;
        }

        // the cards are dealt to fit the result the hand already produced (good = it should hold); a cooler forces a loss
        public static Showdown Make(Run run, Decision d, bool cooler = false)
        {
            var sp = run.spot;
            var rng = new System.Random((int)(Rng.Hash($"{run.ev.id}|{run.si}|{run.h}|{run.results.Count}|{sp.hand[0]}|{sp.hand[1]}") & 0x7fffffff));
            var sd = new Showdown { hero = sp.hand.ToArray(), before = d.before, after = cooler ? 0 : d.after };
            bool good = !cooler && (d.after > d.before || (d.after == d.before && d.ok));
            int opp = -1;
            for (int i = 0; i < sp.seats.Count; i++) if (i != sp.hero && !sp.seats[i].folded && (sp.seats[i].act ?? "").ToLowerInvariant().Contains("jam")) opp = i;
            if (opp < 0) { double best = -1; for (int i = 0; i < sp.seats.Count; i++) if (i != sp.hero && !sp.seats[i].folded && sp.seats[i].put > best) { best = sp.seats[i].put; opp = i; } }
            if (opp < 0) opp = (sp.hero + 1) % Math.Max(1, sp.seats.Count);
            sd.oppSeat = sp.seats[opp].pos;
            sd.oppName = Moments.Rival(run) ?? sd.oppSeat;
            if (!cooler && sp.acts[d.j] == "allin" && good && Moments.Rival(run) == null && rng.NextDouble() < 0.45) { sd.folded = true; sd.win = true; return sd; }

            var deck = Enumerable.Range(0, 52).Where(c => c != sd.hero[0] && c != sd.hero[1]).ToList();
            int[] Draw(List<int> from, int n) { var l = from.ToList(); var o = new int[n]; for (int i = 0; i < n; i++) { int k = rng.Next(l.Count); o[i] = l[k]; l.RemoveAt(k); } return o; }
            int[] bestOpp = null, bestBoard = null; bool matched = false;
            for (int tries = 0; tries < 400 && !matched; tries++)
            {
                var o = Draw(deck, 2);
                int hi = Math.Max(o[0] >> 2, o[1] >> 2), lo = Math.Min(o[0] >> 2, o[1] >> 2); bool pair = hi == lo;
                double like = pair ? 1 : hi >= 9 ? 0.75 : hi >= 7 && lo >= 4 ? 0.45 : 0.15;
                if (rng.NextDouble() > like) continue;
                var rest = deck.Where(c => c != o[0] && c != o[1]).ToList();
                var b = Draw(rest, 5);
                long hs = Eval(sd.hero.Concat(b)), os = Eval(o.Concat(b));
                bestOpp = o; bestBoard = b;
                if ((hs > os) == good && hs != os) matched = true;
            }
            sd.opp = bestOpp; sd.board = bestBoard;
            long H = Eval(sd.hero.Concat(sd.board)), O = Eval(sd.opp.Concat(sd.board));
            sd.win = H > O; sd.chop = H == O; sd.heroHand = Name(H); sd.oppHand = Name(O);
            var rest2 = deck.Where(c => c != sd.opp[0] && c != sd.opp[1]).ToList(); int won = 0, n = 300; double ties = 0;
            for (int i = 0; i < n; i++) { var b = Draw(rest2, 5); long a = Eval(sd.hero.Concat(b)), c = Eval(sd.opp.Concat(b)); if (a > c) won++; else if (a == c) ties += 0.5; }
            sd.eq = (won + ties) / n;
            return sd;
        }

        static readonly string[] RankN = { "twos", "threes", "fours", "fives", "sixes", "sevens", "eights", "nines", "tens", "jacks", "queens", "kings", "aces" };
        static readonly string[] RankOne = { "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "jack", "queen", "king", "ace" };
        public static long Eval(IEnumerable<int> cards)
        {
            var cs = cards.ToArray(); var cnt = new int[13]; var suitCnt = new int[4]; var bySuit = new List<int>[4];
            for (int i = 0; i < 4; i++) bySuit[i] = new List<int>();
            foreach (var c in cs) { cnt[c >> 2]++; suitCnt[c & 3]++; bySuit[c & 3].Add(c >> 2); }
            long Pack(int cat, params int[] ks) { long v = cat; for (int i = 0; i < 5; i++) v = v * 16 + (i < ks.Length ? ks[i] : 0); return v; }
            int Straight(bool[] has) { for (int top = 12; top >= 4; top--) { bool ok = true; for (int k = 0; k < 5; k++) if (!has[top - k]) { ok = false; break; } if (ok) return top; } if (has[12] && has[0] && has[1] && has[2] && has[3]) return 3; return -1; }
            for (int s = 0; s < 4; s++) if (suitCnt[s] >= 5)
                {
                    var has = new bool[13]; foreach (var r in bySuit[s]) has[r] = true;
                    int sf = Straight(has); if (sf >= 0) return Pack(8, sf);
                }
            var ranksDesc = Enumerable.Range(0, 13).Reverse().ToList();
            int quad = ranksDesc.Where(r => cnt[r] == 4).DefaultIfEmpty(-1).First();
            if (quad >= 0) return Pack(7, quad, ranksDesc.First(r => r != quad && cnt[r] > 0));
            var trips = ranksDesc.Where(r => cnt[r] == 3).ToList(); var pairs = ranksDesc.Where(r => cnt[r] == 2).ToList();
            if (trips.Count > 0 && (trips.Count > 1 || pairs.Count > 0)) return Pack(6, trips[0], trips.Count > 1 ? Math.Max(trips[1], pairs.Count > 0 ? pairs[0] : -1) : pairs[0]);
            for (int s = 0; s < 4; s++) if (suitCnt[s] >= 5) return Pack(5, bySuit[s].OrderByDescending(r => r).Take(5).ToArray());
            var all = new bool[13]; for (int r = 0; r < 13; r++) all[r] = cnt[r] > 0;
            int st = Straight(all); if (st >= 0) return Pack(4, st);
            var singles = ranksDesc.Where(r => cnt[r] > 0).ToList();
            if (trips.Count > 0) return Pack(3, new[] { trips[0] }.Concat(singles.Where(r => r != trips[0]).Take(2)).ToArray());
            if (pairs.Count >= 2) return Pack(2, pairs[0], pairs[1], singles.First(r => r != pairs[0] && r != pairs[1]));
            if (pairs.Count == 1) return Pack(1, new[] { pairs[0] }.Concat(singles.Where(r => r != pairs[0]).Take(3)).ToArray());
            return Pack(0, singles.Take(5).ToArray());
        }
        public static string Name(long v)
        {
            int cat = (int)(v >> 20); int k0 = (int)((v >> 16) & 15), k1 = (int)((v >> 12) & 15);
            switch (cat)
            {
                case 0: return $"{Up(RankOne[k0])} high";
                case 1: return $"A pair of {RankN[k0]}";
                case 2: return $"Two pair, {RankN[k0]} and {RankN[k1]}";
                case 3: return $"Three {RankN[k0]}";
                case 4: return $"A straight, {RankOne[k0]} high";
                case 5: return $"A flush, {RankOne[k0]} high";
                case 6: return $"A full house, {RankN[k0]} full of {RankN[k1]}";
                case 7: return $"Four {RankN[k0]}";
                default: return k0 == 12 ? "A royal flush" : $"A straight flush, {RankOne[k0]} high";
            }
        }
        static string Up(string s) => char.ToUpperInvariant(s[0]) + s.Substring(1);
    }

    public static class Moments
    {
        public static readonly Color Lime = new Color(0.72f, 1f, 0.18f), Green = new Color(0.36f, 0.86f, 0.48f), Red = new Color(1f, 0.36f, 0.36f), Cream = new Color(0.97f, 0.94f, 0.86f);
        // where a moment can happen (TABLE.md §11); the test driver reads Log
        public static readonly List<string> Log = new List<string>();
        public static string Where(EventDef ev) => ev.online ? "online" : "live";
        public static bool Allowed(string tag, EventDef ev) => tag == "any" || tag == "story" || tag == Where(ev);
        static void Note(string tag, string what, EventDef ev)
        {
            bool ok = Allowed(tag, ev); Log.Add($"{(ok ? "ok" : "MISMATCH")} [{tag}] at {Where(ev)}: {what}");
            if (!ok) Debug.LogError($"[Dream] {tag} moment at an {Where(ev)} event: {what}");
        }

        // who from the story is at this table: Tommy at the Championship and the nightly, Mustich at the Classic. Online,
        // nobody from the story plays you in Chapter 1.
        public static string Rival(Run run)
        {
            if (run.ev.online || run.Stage != "final") return null;
            return run.ev.boss == "lou" ? "Tommy" : run.ev.boss == "trent" ? "Mustich" : null;
        }
        static string Talker(EventDef ev) => ev.online ? null : ev.id.StartsWith("classic") ? "Mustich" : "Tommy";
        static readonly string[] Screen = { "ripriver_88", "nitbox", "flopzilla", "gr1nd_city", "the_mathematician", "SunnyIsles_Sal", "luckyluke305" };
        public static string ScreenName(Run run) => Screen[(int)((Rng.Hash(run.ev.id + run.si) & 0x7fffffff) % Screen.Length)];

        // ------------------------------------------------------------ building blocks
        static RectTransform Overlay(RectTransform root, Color dim, Action tap = null, bool close = true)
        {
            var o = Ui.Panel(root, "Moment", dim); var b = o.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            if (tap != null) b.onClick.AddListener(() => { if (close) UnityEngine.Object.Destroy(o.gameObject); tap(); });   // a tap card goes away as it goes on
            return o;
        }
        public static RawImage Portrait(Transform parent, string who, float x0, float y0, float x1, float y1)
        {
            var holder = Ui.At(Ui.Box(parent, "Portrait"), x0, y0, x1, y1);   // the fitter sizes against its parent, so it gets its own
            var frame = Ui.Tile(holder, "Frame", new Color(1, 1, 1, 0.08f), 24);
            frame.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            frame.gameObject.AddComponent<RectMask2D>();
            var raw = new GameObject("Face", typeof(RectTransform)).AddComponent<RawImage>(); raw.transform.SetParent(frame, false);
            raw.texture = Resources.Load<Texture2D>("UI/" + who.ToLowerInvariant()); raw.raycastTarget = false;
            if (raw.texture != null) { var f = raw.gameObject.AddComponent<AspectRatioFitter>(); f.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; f.aspectRatio = raw.texture.width / (float)raw.texture.height; }
            if (raw.texture == null) raw.color = Color.clear;
            return raw;
        }
        static RectTransform Card(RectTransform root, float y0, float y1) => Ui.At(Ui.Tile(root, "Card", new Color(0.09f, 0.085f, 0.11f, 0.97f), 36), 0.05f, y0, 0.95f, y1);
        static TextMeshProUGUI Glow(TextMeshProUGUI t, Color c)
        {
            t.fontMaterial.EnableKeyword("UNDERLAY_ON"); t.fontMaterial.SetColor("_UnderlayColor", new Color(c.r, c.g, c.b, 0.5f));
            t.fontMaterial.SetFloat("_UnderlaySoftness", 0.45f); t.fontMaterial.SetFloat("_UnderlayDilate", 0.05f);   // a halo inside the atlas padding (more clips to a box)
            return t;
        }
        static void CardImg(Transform parent, int c, float x0, float y0, float x1, float y1, bool up = true)
        {
            var card = Ui.At(Ui.Tile(parent, "Card", up ? new Color(0.97f, 0.96f, 0.93f) : new Color(0.55f, 0.1f, 0.14f), 14), x0, y0, x1, y1);
            if (!up) { Ui.At(Ui.Tile(card, "Back", new Color(0.7f, 0.16f, 0.2f), 10), 0.1f, 0.07f, 0.9f, 0.93f); return; }
            int suit = c & 3; var col = suit == 0 ? new Color(0.1f, 0.45f, 0.2f) : suit == 1 ? new Color(0.15f, 0.35f, 0.75f) : suit == 2 ? new Color(0.78f, 0.1f, 0.12f) : new Color(0.08f, 0.08f, 0.1f);
            Ui.At(Ui.Label(card, "23456789TJQKA"[c >> 2].ToString().Replace("T", "10"), 64, col, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 0.42f, 1, 1);
            Ui.At(Ui.Label(card, "♣♦♥♠"[suit].ToString(), 56, col), 0, 0.02f, 1, 0.5f);
        }

        // ------------------------------------------------------------ 1. the all-in screen
        public static void AllIn(RectTransform root, Run run, Showdown sd, Action done, bool cooler = false)
        {
            Note("any", "all-in vs " + sd.oppName, run.ev);
            float t0 = Time.realtimeSinceStartup; bool skip = false, ended = false;
            var o = Overlay(root, new Color(0.02f, 0.02f, 0.03f, 1f), () => { if (Time.realtimeSinceStartup - t0 > 0.4f) skip = true; }, false);
            string kick = Rival(run) != null ? "FINAL TABLE · ALL IN VS " + sd.oppName.ToUpperInvariant() : run.ev.online ? "CF TILT · ALL IN" : "ALL IN";
            Ui.At(Ui.Label(o, kick, 24, Ui.Dim, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 0.86f, 1, 0.89f);
            Ui.At(Glow(Ui.Title(o, "ALL IN", 150, Lime), Lime), 0, 0.75f, 1, 0.86f);
            var go = Ui.Button(o, "See the verdict", () => { UnityEngine.Object.Destroy(o.gameObject); done(); }, Ui.Gold, Ui.Night, 42); Ui.At(go, 0.18f, 0.06f, 0.82f, 0.12f); go.gameObject.SetActive(false);
            if (sd.folded)
            {
                Ui.At(Ui.Label(o, $"{sd.oppSeat} thinks it over. Then the cards go in the muck.", 34, Ui.Ink), 0.08f, 0.55f, 0.92f, 0.65f);
                Ui.At(Ui.Title(o, "They fold. The pot is yours.", 60, Green), 0.05f, 0.42f, 0.95f, 0.54f);
                go.gameObject.SetActive(true); return;
            }
            Ui.At(Ui.Label(o, "YOU", 26, Ui.Aqua, TextAnchor.MiddleCenter, FontStyle.Bold), 0.06f, 0.69f, 0.46f, 0.72f);
            Ui.At(Ui.Label(o, sd.oppName.ToUpperInvariant(), 26, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 0.54f, 0.69f, 0.94f, 0.72f);
            CardImg(o, sd.hero[0], 0.08f, 0.56f, 0.25f, 0.68f); CardImg(o, sd.hero[1], 0.27f, 0.56f, 0.44f, 0.68f);
            Ui.At(Ui.Label(o, "vs", 30, Ui.Dim), 0.44f, 0.58f, 0.56f, 0.66f);
            var oppBox = Ui.At(Ui.Box(o, "Opp"), 0.56f, 0.56f, 0.92f, 0.68f);
            CardImg(oppBox, sd.opp[0], 0, 0, 0.47f, 1, false); CardImg(oppBox, sd.opp[1], 0.53f, 0, 1, 1, false);
            // the equity bar, when their cards turn over
            var eq = Ui.At(Ui.Box(o, "Equity"), 0.08f, 0.5f, 0.92f, 0.545f); var eqg = eq.gameObject.AddComponent<CanvasGroup>(); eqg.alpha = 0;
            var bar = Ui.At(Ui.Tile(eq, "Theirs", Ui.Gold, 8), 0, 0.62f, 1, 1); var mine = Ui.Tile(bar, "Mine", Ui.Aqua, 8); mine.anchorMax = new Vector2((float)sd.eq, 1);
            Ui.At(Ui.Num(eq, $"{Math.Round(sd.eq * 100)}% to win", 26, Ui.Aqua, TextAnchor.MiddleLeft, FontStyle.Bold), 0, 0, 0.6f, 0.55f);
            Ui.At(Ui.Num(eq, $"{Math.Round((1 - sd.eq) * 100)}%", 26, Ui.Gold, TextAnchor.MiddleRight, FontStyle.Bold), 0.6f, 0, 1, 0.55f);
            // the board, and the street
            var boardBox = Ui.At(Ui.Box(o, "Board"), 0.06f, 0.33f, 0.94f, 0.46f); var slots = new List<RectTransform>();
            for (int i = 0; i < 5; i++)
            {
                float x0 = i * 0.2f + (i >= 3 ? 0.012f * (i - 2) : 0) - 0.012f;
                var slot = Ui.At(Ui.Box(boardBox, "S" + i), x0 + 0.01f, 0, x0 + 0.19f, 1); CardImg(slot, sd.board[i], 0, 0, 1, 1);
                slot.gameObject.AddComponent<CanvasGroup>().alpha = 0; slots.Add(slot);
            }
            var street = Ui.At(Ui.Label(o, "", 30, Ui.Dim, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 0.28f, 1, 0.32f);
            var res = Ui.At(Ui.Box(o, "Result"), 0.05f, 0.13f, 0.95f, 0.28f); var resg = res.gameObject.AddComponent<CanvasGroup>(); resg.alpha = 0;
            string big = sd.chop ? "Chopped. Split pot." : sd.win ? "You hold!" : Rival(run) != null ? $"{sd.oppName} holds." : "They hold.";
            Ui.At(Ui.Title(res, big, 72, sd.chop ? Cream : sd.win ? Green : Red), 0, 0.5f, 1, 1);
            string how = sd.chop ? $"{sd.heroHand} each." : sd.heroHand == sd.oppHand ? $"{sd.heroHand} each. The kicker plays." : sd.win ? $"{sd.heroHand} beats {Lower(sd.oppHand)}." : $"{sd.oppHand} beats {Lower(sd.heroHand)}.";
            Ui.At(Ui.Label(res, how, 30, Ui.Ink), 0, 0.25f, 1, 0.5f);
            if (sd.after != sd.before) Ui.At(Ui.Num(res, $"Stack {sd.before} → {sd.after} bb", 28, sd.after > sd.before ? Green : Red), 0, 0, 1, 0.25f);
            var hint = Ui.At(Ui.Label(o, "tap to skip", 22, new Color(1, 1, 1, 0.35f)), 0, 0.015f, 1, 0.045f);
            // the timeline: flip at 0.8 s, flop 1.6, turn 3.0, "The river..." and a breath, river 4.6, the result
            float[] at = { 1.6f, 1.75f, 1.9f, 3.0f, 4.6f }; int shown = 0; bool flipped = false;
            Dream.UI.Motion.On(o).Go(Run());
            System.Collections.IEnumerator Run()
            {
                while (!ended)
                {
                    float t = skip ? 99 : Time.realtimeSinceStartup - t0;
                    if (!flipped && t >= 0.8f) { flipped = true; Ui.Clear(oppBox); CardImg(oppBox, sd.opp[0], 0, 0, 0.47f, 1); CardImg(oppBox, sd.opp[1], 0.53f, 0, 1, 1); eqg.alpha = 1; }
                    while (shown < 5 && t >= at[shown]) { slots[shown].GetComponent<CanvasGroup>().alpha = 1; Ui.Slide(slots[shown], new Vector2(0, 40), 0, 0.2f); shown++; }
                    street.text = shown == 3 ? "The flop" : shown == 4 ? (t > 3.6f ? "The river..." : "The turn") : "";
                    if (t >= 5.3f) { ended = true; street.text = ""; resg.alpha = 1; hint.text = ""; go.gameObject.SetActive(true); }
                    yield return null;
                }
            }
        }
        static string Lower(string s) => string.IsNullOrEmpty(s) ? s : char.ToLowerInvariant(s[0]) + s.Substring(1);

        // ------------------------------------------------------------ 7. the cooler card
        static readonly string[] Coolers =
        {
            "Kings into aces. All the chips go in, and you never had a chance.",
            "You flop a set. He flops the higher set. That is the whole story.",
            "Ace-king versus queens, a coin flip for your tournament. The coin lands on its edge, then on queens.",
            "Your all-in is called by the one hand in his range that beats you.",
            "Runner-runner. You did nothing wrong. The deck did.",
        };
        static readonly string[] CoolerLines =
        {
            "You played it right. That's all you control. People insist on calling it luck.",
            "Write it down and let it go. The next hand does not know about this one.",
            "Every legend has a list of these longer than their list of titles.",
            "Bad beats are the tax on getting your money in good. Pay it and keep playing.",
            "That one stings. Good. It means you care. Now go show the deck it was wrong.",
        };
        public static string CoolerStory(Run run) => Coolers[(int)((Rng.Hash(run.ev.id + run.si + "|" + run.h) & 0x7fffffff) % Coolers.Length)];
        public static void Cooler(RectTransform root, Run run, int coolersBefore, Action done)
        {
            Note("any", "cooler", run.ev);
            var o = Overlay(root, new Color(0.02f, 0.02f, 0.03f, 1f));
            Ui.At(Ui.Label(o, "COOLER", 26, Red, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 0.78f, 1, 0.81f);
            Ui.At(Ui.Title(o, "Nothing you could do", 80, Ui.Ink), 0.05f, 0.68f, 0.95f, 0.78f);
            Ui.At(Ui.Label(o, run.cooler ?? CoolerStory(run), 34, Ui.Ink), 0.1f, 0.54f, 0.9f, 0.66f);
            string line = (coolersBefore > 0 ? $"That's cooler number {coolersBefore + 1}. " : "") + CoolerLines[coolersBefore % CoolerLines.Length];
            SlickSays(o, line, run.ev, 0.3f, 0.5f);
            Ui.At(Ui.Button(o, "See where you finished", () => { UnityEngine.Object.Destroy(o.gameObject); done(); }, Ui.Gold, Ui.Night, 40), 0.15f, 0.12f, 0.85f, 0.18f);
        }

        // Slick's voice at the table: his portrait and words live; a text on the phone's bubble style online
        static void SlickSays(RectTransform o, string line, EventDef ev, float y0, float y1)
        {
            var c = Card(o, y0, y1);
            if (ev.online)
            {
                Ui.At(Ui.Label(c, "SLICK · TEXT", 22, Ui.Aqua, TextAnchor.MiddleLeft, FontStyle.Bold), 0.06f, 0.78f, 0.94f, 0.94f);
                var b = Ui.At(Ui.Tile(c, "Bubble", new Color(0.17f, 0.17f, 0.19f), 36), 0.05f, 0.08f, 0.95f, 0.74f);
                Ui.At(Ui.Label(b, line, 30, Color.white, TextAnchor.MiddleLeft), 0.05f, 0.05f, 0.95f, 0.95f);
                return;
            }
            Portrait(c, "slick", 0.04f, 0.15f, 0.26f, 0.85f);
            Ui.At(Ui.Label(c, "SLICK", 22, Ui.Aqua, TextAnchor.MiddleLeft, FontStyle.Bold), 0.3f, 0.72f, 0.95f, 0.9f);
            Ui.At(Ui.Label(c, line, 30, Ui.Ink, TextAnchor.UpperLeft), 0.3f, 0.08f, 0.95f, 0.7f);
        }

        // ------------------------------------------------------------ 2. the level card (Slick's tips, DR_TIPS verbatim)
        static readonly Dictionary<string, string[]> Tips = new Dictionary<string, string[]>
        {
            ["early"] = new[] { "Early levels: chips are cheap, mistakes aren't. Fold the junk and let them spew.", "Position is power. The button prints money; under the gun is a tax.", "You don't have to win a hand to win this level. Survive and steal." },
            ["middle"] = new[] { "The short stacks are getting desperate. Let them shove into your good hands.", "Stack sizes changed. Your opening ranges should too.", "This is where tournaments are won quietly. Steal, don't spew." },
            ["bubble"] = new[] { "Bubble. The short stacks are terrified. The big stacks are licking their lips. Which one are you?", "Near the money, a call costs more than chips. Tighten your calls, not your raises.", "Min-cash is not the goal. But busting on the bubble is a long ride home." },
            ["satbubble"] = new[] { "Satellite bubble: once you have a seat locked up, folding aces can be right. Survival is the only prize.", "Short stacks have to gamble. Big stacks don't. Know which one you are." },
            ["deep"] = new[] { "You're in the money. Ladder up, but don't play scared. Scared money never wins.", "Every pay jump matters now. Pick on the stacks that are clinging on." },
            ["final"] = new[] { "Final table. Every pay jump is real money. Every chip is a decision.", "Short stack? Your jam is your weapon. Raise-fold gives it away.", "Look around. Somebody here is scared. Make sure it isn't you." },
        };
        static readonly Dictionary<string, string> Rivals = new Dictionary<string, string>
        { ["Tommy"] = "Thirty years, kid, and I still get butterflies at a final table.", ["Mustich"] = "Final table. Dad flew in for this. Don't embarrass yourself." };
        public static string StageName(string st) => st switch { "early" => "Early levels", "middle" => "Middle levels", "bubble" => "The bubble", "satbubble" => "The bubble", "final" => "Final table", "deep" => "Deep run", "cash" => "Cash game", _ => st };

        public static void Level(RectTransform root, Run run, Action done)
        {
            var ev = run.ev; bool first = run.si == 0;
            Note(ev.online ? "online" : "live", "level card " + run.Stage, ev);
            var o = Overlay(root, new Color(0.02f, 0.02f, 0.03f, 1f), () => done());
            string top = ev.online ? $"CF TILT · LEVEL {run.si * 3 + 4} · BLINDS {Blinds(run)}" : first ? "BISCAYNE CARD ROOM · " + ev.name.ToUpperInvariant() : "NEXT LEVEL";
            Ui.At(Ui.Label(o, top, 24, ev.online ? Ui.Aqua : Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 0.04f, 0.76f, 0.96f, 0.8f);
            Ui.At(Ui.Title(o, StageName(run.Stage), 100, Ui.Ink), 0.04f, 0.65f, 0.96f, 0.76f);
            int per = Hands.Per(run);
            Ui.At(Ui.Label(o, $"{per} hand{(per == 1 ? "" : "s")} at this level" + (run.turbo ? "  ·  turbo blinds: stacks get short fast" : "") + (ev.online ? "  ·  next level 5:00" : ""), 28, Ui.Dim), 0.06f, 0.6f, 0.94f, 0.64f);
            if (Tips.TryGetValue(run.Stage, out var tips)) SlickSays(o, tips[(run.si + run.results.Count + ev.name.Length) % tips.Length], ev, 0.4f, 0.56f);
            var rival = Rival(run);
            if (rival != null)   // the rival bar: who's waiting at the final table
            {
                var c = Card(o, 0.22f, 0.37f);
                Portrait(c, rival, 0.04f, 0.15f, 0.26f, 0.85f);
                Ui.At(Ui.Label(c, "AT THE FINAL TABLE", 22, Red, TextAnchor.MiddleLeft, FontStyle.Bold), 0.3f, 0.74f, 0.95f, 0.92f);
                Ui.At(Ui.Title(c, rival == "Tommy" ? "Tommy \"The Shark\" Maio" : rival, 44, Ui.Ink, TextAnchor.MiddleLeft), 0.3f, 0.48f, 0.95f, 0.74f);
                Ui.At(Ui.Label(c, Rivals[rival], 26, Ui.Dim, TextAnchor.UpperLeft), 0.3f, 0.06f, 0.95f, 0.46f);
            }
            Ui.At(Ui.Label(o, "tap to deal", 24, new Color(1, 1, 1, 0.4f)), 0, 0.08f, 1, 0.11f);
        }
        static string Blinds(Run run) { int k = run.si * 3 + 4; int sb = (int)(25 * Math.Pow(1.45, k)) / 25 * 25; return $"{sb:N0}/{sb * 2:N0}"; }

        // ------------------------------------------------------------ 3–5. the bubble, in the money, heads-up (new)
        public static void Bubble(RectTransform root, Run run, Action done)
        {
            var ev = run.ev; int paid = Hands.Paid(ev); bool sat = ev.kind == "sat";
            Note(ev.online ? "online" : "live", "the bubble", ev);
            var o = Overlay(root, new Color(0.02f, 0.02f, 0.03f, 1f), () => done());
            Ui.At(Glow(Ui.Title(o, "THE BUBBLE", 120, Red), Red), 0, 0.72f, 1, 0.84f);
            string sub = sat ? $"{paid + 1} left · {paid} bag chips for Day 2" : $"{paid + 1} left · {paid} paid · min-cash {Ui.Money(Hands.PrizeFor(ev, paid))}";
            Ui.At(Ui.Num(o, sub, 30, Ui.Ink), 0, 0.67f, 1, 0.71f);
            if (ev.online)
            {
                Ui.At(Ui.Label(o, "HAND-FOR-HAND · every table waits for every other table", 24, Ui.Aqua, TextAnchor.MiddleCenter, FontStyle.Bold), 0.04f, 0.6f, 0.96f, 0.64f);
                Chat(o, run, new[] { "fold fold fold", "pls no bubble", "someone just shove already" }, 0.42f, 0.58f);
            }
            else
            {
                Mic(o, "Gnomey", "We are hand for hand! Dealers, stand up when your hand is done. Nobody leaves the table.", 0.52f, 0.64f);
                if (Talker(ev) == "Tommy") Speech(o, "Tommy", "Thirty years, kid, and the bubble still makes my hands sweat.", 0.4f, 0.5f);
            }
            Text(o, "Slick", "Near the money, think about what the money's worth. Not the chips.", 0.24f, 0.37f);
            Ui.At(Ui.Label(o, "tap to play the bubble", 24, new Color(1, 1, 1, 0.4f)), 0, 0.08f, 1, 0.11f);
        }

        public static void InTheMoney(RectTransform root, Run run, Action done)
        {
            var ev = run.ev; Note(ev.online ? "online" : "live", "in the money", ev);
            var o = Overlay(root, new Color(0.02f, 0.02f, 0.03f, 1f), () => done());
            Ui.At(Glow(Ui.Title(o, ev.kind == "sat" ? "CHIPS BAGGED" : "IN THE MONEY", 110, Ui.Gold), Ui.Gold), 0, 0.68f, 1, 0.8f);
            if (ev.online) { Ui.At(Ui.Label(o, "You're in the money", 30, Ui.Aqua), 0, 0.62f, 1, 0.66f); Chat(o, run, new[] { "gg bubble boy", "ty dealer" }, 0.46f, 0.6f); }
            else Mic(o, "Gnomey", "That's the bubble! Everybody left is in the money.", 0.5f, 0.62f);
            Text(o, "David", "ARE YOU IN THE MONEY?? SCREENSHOT IT", 0.3f, 0.43f);
            Ui.At(Ui.Label(o, "tap to go on", 24, new Color(1, 1, 1, 0.4f)), 0, 0.08f, 1, 0.11f);
        }

        public static void HeadsUp(RectTransform root, Run run, Action done)
        {
            var ev = run.ev; Note(ev.online ? "online" : "live", "heads-up", ev);
            var o = Overlay(root, new Color(0.02f, 0.02f, 0.03f, 1f), () => done());
            string opp = Rival(run) ?? (ev.online ? ScreenName(run) : "the last man standing");
            int you = Math.Max(20, run.stack / 2 + 20), them = Math.Max(15, 120 - you / 2);
            Ui.At(Glow(Ui.Title(o, "HEADS-UP", 130, Lime), Lime), 0, 0.72f, 1, 0.84f);
            Ui.At(Ui.Label(o, ev.online ? "CF TILT · HEADS-UP VS " + opp.ToUpperInvariant() : ev.name.ToUpperInvariant(), 24, ev.online ? Ui.Aqua : Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 0.68f, 1, 0.71f);
            if (!ev.online)
            {
                Portrait(o, "you", 0.12f, 0.5f, 0.4f, 0.66f);
                if (Rival(run) != null) Portrait(o, Rival(run), 0.6f, 0.5f, 0.88f, 0.66f);
                Ui.At(Ui.Label(o, "vs", 34, Ui.Dim), 0.4f, 0.55f, 0.6f, 0.61f);
            }
            Ui.At(Ui.Num(o, $"You {you} bb  ·  {opp} {them} bb", 30, Ui.Ink), 0, 0.45f, 1, 0.49f);
            if (!ev.online)
            {
                Mic(o, "Gnomey", $"Ladies and gentlemen, we are heads-up for {ev.name}!", 0.33f, 0.43f);
                if (Rival(run) != null) Speech(o, Rival(run), Rivals[Rival(run)], 0.2f, 0.31f);
            }
            Ui.At(Ui.Label(o, "tap to play it", 24, new Color(1, 1, 1, 0.4f)), 0, 0.08f, 1, 0.11f);
        }

        // the floor mic (live): a caption with a speaker mark
        static void Mic(RectTransform o, string who, string line, float y0, float y1)
        {
            var c = Card(o, y0, y1);
            Ui.At(Ui.Label(c, "FLOOR MIC · " + who.ToUpperInvariant(), 22, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold), 0.05f, 0.68f, 0.95f, 0.92f);
            Ui.At(Ui.Label(c, "“" + line + "”", 30, Ui.Ink, TextAnchor.UpperLeft, FontStyle.Italic), 0.05f, 0.06f, 0.95f, 0.66f);
        }
        // someone at your table says something (live): portrait and a bubble
        public static void Speech(Transform o, string who, string line, float y0, float y1)
        {
            var c = Ui.At(Ui.Tile(o, "Speech", new Color(0.97f, 0.95f, 0.9f, 0.97f), 36), 0.05f, y0, 0.95f, y1);
            Portrait(c, who, 0.03f, 0.1f, 0.2f, 0.9f);
            Ui.At(Ui.Label(c, who.ToUpperInvariant(), 20, new Color(0.45f, 0.3f, 0.1f), TextAnchor.MiddleLeft, FontStyle.Bold), 0.24f, 0.66f, 0.96f, 0.92f);
            Ui.At(Ui.Label(c, line, 28, Ui.Night, TextAnchor.UpperLeft), 0.24f, 0.06f, 0.96f, 0.64f);
        }
        // a text arriving on the phone during play: the Messages bubble, small, over the table
        static void Text(RectTransform o, string from, string line, float y0, float y1)
        {
            var c = Ui.At(Ui.Tile(o, "Text", new Color(0.12f, 0.12f, 0.14f, 0.98f), 36), 0.08f, y0, 0.92f, y1);
            Ui.At(Ui.Label(c, from.ToUpperInvariant() + "  ·  now", 20, Ui.Dim, TextAnchor.MiddleLeft, FontStyle.Bold), 0.06f, 0.72f, 0.94f, 0.92f);
            var b = Ui.At(Ui.Tile(c, "Bubble", new Color(0.2f, 0.2f, 0.23f), 32), 0.04f, 0.08f, 0.96f, 0.68f);
            Ui.At(Ui.Label(b, line, 28, Color.white, TextAnchor.MiddleLeft), 0.05f, 0.05f, 0.95f, 0.95f);
            Ui.Slide(c, new Vector2(0, -60), 0.6f, 0.3f);
        }
        // the table's chat box (online)
        public static void Chat(Transform o, Run run, IList<string> lines, float y0, float y1)
        {
            var c = Ui.At(Ui.Tile(o, "Chat", new Color(0.05f, 0.08f, 0.1f, 0.95f), 20), 0.08f, y0, 0.92f, y1);
            Ui.At(Ui.Label(c, "CHAT", 18, Ui.Aqua, TextAnchor.UpperLeft, FontStyle.Bold), 0.04f, 0.78f, 0.5f, 0.96f);
            var rng = new System.Random((int)(Rng.Hash(run.ev.id + run.si + run.h) & 0x7fffffff));
            string text = string.Join("\n", lines.Select(l => $"<color=#7FD8D2>{Screen[rng.Next(Screen.Length)]}:</color> {l}"));
            Ui.At(Ui.Num(c, text, 22, Ui.Ink, TextAnchor.UpperLeft), 0.04f, 0.04f, 0.96f, 0.76f).lineSpacing = 8;
        }

        // ------------------------------------------------------------ 6. table talk (Steven's Witness lines; live only)
        public static void Talk(RectTransform root, Run run, Decision d)
        {
            if (run.h - run.talkH < 3 && run.talkH >= 0) return;
            int swing = d.after - d.before; string line = null, who = Talker(run.ev);
            if (run.ev.online)
            {
                if (Math.Abs(swing) < 25) return;
                run.talkH = run.h; Note("online", "chat", run.ev);
                var chat = new[] { "nh", "sick", "lol ok", "ship it", "how", "gl" };
                var host = Ui.At(Ui.Box(root, "Talk"), 0, 0.58f, 1, 0.68f); Chat(host, run, new[] { chat[(run.h + run.si) % chat.Length] }, 0.55f, 1f);
                UnityEngine.Object.Destroy(host.gameObject, 3f); return;
            }
            if (who == "Mustich" && swing >= 30) line = "Enjoy it. Variance is a loan.";
            else if (who == "Tommy" && swing >= 30) line = "Kid. Don't let it go to your head. Let it go to your bankroll.";
            if (line == null) return;
            run.talkH = run.h; Note("live", "table talk: " + who, run.ev);
            var t = Ui.At(Ui.Box(root, "Talk"), 0, 0.6f, 1, 0.7f); Speech(t, who, line, 0.05f, 0.95f);
            Ui.Slide(t, new Vector2(0, 30), 0, 0.25f); UnityEngine.Object.Destroy(t.gameObject, 3.2f);
        }

        // films know where they happen (TABLE.md §11): live = Biscayne's room and people, story = fixed story beats,
        // any = Slick at the Lucky Seven at night (after any event)
        static readonly string[] LiveFilms = { "p_seat", "c1", "classic_day1", "classic_mustich", "classic_shark", "classic_other", "trentbeat", "firstwin" };
        public static string FilmTag(string id) => LiveFilms.Contains(id) || id.StartsWith("ft_") ? "live" : id.StartsWith("p_") ? "story" : "any";
        public static bool FilmOk(string id, EventDef last)
        {
            string tag = FilmTag(id); if (last == null) { Log.Add($"ok [{tag}] film {id}"); return true; }
            Note(tag, "film " + id, last); return Allowed(tag, last);
        }

        // ------------------------------------------------------------ 8. the walk away (old Story.cs after_*, Witness)
        static readonly Dictionary<string, string[]> Narr = new Dictionary<string, string[]>
        {
            ["win:live"] = new[] { "The last card falls and the room comes apart.\n{ev}. Yours.", "They push the whole thing across the felt. Every chip.\n{ev}, and nobody is sitting down.", "Phones up. Dealer standing. Floor staff grinning.\n{ev} belongs to you." },
            ["cash:live"] = new[] { "Not the trophy, but a walk to the cage.\n{cash} from {ev}.", "The cashier counts it twice. {cash}.\nYou count it four times on the way home.", "Out before the end, but with money.\n{cash}, and a plan for the next one." },
            ["bust:live"] = new[] { "Chair pushed back. Chips gone. The lights feel brighter.\n{ev} goes on without you.", "The long walk past tables that are still playing.\nYou replay it anyway: {ev}, hand by hand.", "No cash. No excuses. The felt doesn't care.\n{ev}: busted." },
            ["cooler:live"] = new[] { "Set over set. Kings into aces. Whatever you call it, it hurt.\nThe table goes quiet the way it only does for a bad beat.", "All in good. Called by worse. The river did the rest.\nSomeone mutters \"brutal\" and means it.", "You did everything right and the deck said no.\nThe dealer won't look at you." },
            ["win:online"] = new[] { "The final table window flashes gold. First of {field}. Your name at the top of the CF Tilt lobby.", "Three in the morning, alone in the apartment, and you just beat a thousand people. You say it out loud to nobody.", "The chat fills with gg and one ship it. The laptop fan is the only applause." },
            ["cash:online"] = new[] { "A cash lands in your CF Tilt balance. You refresh it twice to be sure.", "Out before the end, but paid. The lobby already has the next one starting.", "Min-cash. You close the laptop gently, like it might change its mind." },
            ["bust:online"] = new[] { "You finished {place}. The table window closes by itself.", "Busted. The tab is gone before you can argue with it.", "The lobby keeps scrolling. Nobody noticed you leave." },
            ["cooler:online"] = new[] { "Aces into kings, in a window the size of a postcard. It still hurts full size.", "The hand history says you did everything right. The balance says otherwise.", "You watch the replay twice. It doesn't change." },
        };
        static readonly Dictionary<string, string[]> Mine = new Dictionary<string, string[]>
        {
            ["win"] = new[] { "YES. YES. YES.", "I never doubted it. Not once. (Twice.)", "Text Slick. Text David. Text everyone." },
            ["cash"] = new[] { "Good. Next time, further.", "Bankroll first. Glory later.", "That's rent. That's the roll. That's enough." },
            ["bust"] = new[] { "Review the hands. Every one.", "Variance. Or me. Tomorrow I'll know which.", "Sleep. Study. Back tomorrow." },
            ["cooler"] = new[] { "...Brutal.", "Right play. Wrong deck.", "Don't tilt. Don't tilt. Don't tilt." },
        };
        // the witness: whoever is at Biscayne (live), a text from Slick or David (online). Steven's Witness pool.
        static (string who, string line, bool text) Witness(string outcome, EventDef ev, int v)
        {
            if (ev.online)
            {
                if (outcome == "win" || outcome == "cash") return ("David", (outcome == "win" ? new[] { "I SAW IT. I SAW THE WHOLE THING. DINNERS ON YOU", "FIRST PLACE?? ok im telling everyone at the bar" } : new[] { "PAID. we eating tonight", "a cash is a cash bro. proud of you" })[v % 2], true);
                return ("Slick", (outcome == "bust" ? new[] { "Review the hands. Then sleep.", "Busted is information. Use it." } : new[] { "Right play. Wrong deck. Same you tomorrow.", "I count the beats, remember. That one's on the list." })[v % 2], true);
            }
            switch (outcome)
            {
                case "win": return ("Tommy", new[] { "Thirty years in this room. I've seen three people do that. You're the fourth.", "Kid. Don't let it go to your head. Let it go to your bankroll." }[v % 2], false);
                case "cash": return ("Gnomey", new[] { "Cage is that way. Don't spend it all on the drive home.", "Nice run. Sign up for tomorrow's on your way out." }[v % 2], false);
                case "bust": return ("Mustich", new[] { "Out already? Dad's going to love this.", "Tough. Not really.", "Maybe stick to the spreadsheets." }[v % 3], false);
                default: return ("Tommy", new[] { "Brutal. Thirty years and that one still stings.", "That wasn't poker. That was weather." }[v % 2], false);
            }
        }

        public static void WalkAway(RectTransform root, EventDef ev, string outcome, int place, double prize, int v, Action done)
        {
            string where = Where(ev);
            Note(where, "walk away " + outcome, ev);
            var o = Ui.Panel(root, "WalkAway", Color.black);
            var bg = Resources.Load<Texture2D>(ev.online ? "UI/room_online" : "UI/room_live");
            if (bg != null)
            {
                var raw = new GameObject("Room", typeof(RectTransform)).AddComponent<RawImage>(); raw.transform.SetParent(o, false); raw.texture = bg; raw.color = new Color(0.4f, 0.4f, 0.42f);
                var fit = raw.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = bg.width / (float)bg.height; raw.raycastTarget = false;
            }
            string n = Narr[outcome + ":" + where][v % 3].Replace("{ev}", ev.name).Replace("{cash}", Ui.Money(prize)).Replace("{field}", ev.field.ToString("N0")).Replace("{place}", Ord(place));
            int beat = 0; var box = Ui.Box(o, "Beat"); Ui.Fill(box);
            var b = o.gameObject.AddComponent<Button>(); b.transition = Selectable.Transition.None;
            void Show()
            {
                Ui.Clear(box);
                if (beat == 0) Ui.At(Ui.Title(box, n, 54, Ui.Ink), 0.08f, 0.4f, 0.92f, 0.66f).lineSpacing = 10;
                else if (beat == 1) Ui.At(Ui.Title(box, "“" + Mine[outcome][v % 3] + "”", 64, Ui.Gold), 0.08f, 0.42f, 0.92f, 0.6f);
                else
                {
                    var w = Witness(outcome, ev, v);
                    if (w.text) Text((RectTransform)box, w.who, w.line, 0.42f, 0.56f);
                    else Speech(box, w.who, w.line, 0.42f, 0.56f);
                }
                Ui.At(Ui.Label(box, "tap to continue", 22, new Color(1, 1, 1, 0.4f)), 0, 0.06f, 1, 0.09f);
            }
            b.onClick.AddListener(() => { if (++beat > 2) { UnityEngine.Object.Destroy(o.gameObject); done(); } else Show(); });
            Show();
        }
        static string Ord(int n) => n + (n % 100 is 11 or 12 or 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" });
    }
}
