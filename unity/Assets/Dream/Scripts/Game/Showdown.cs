// All in: when your decision puts your stack in the middle (you shove, or you call a shove), the hand plays out
// on screen. Their cards flip, the board runs out one street at a time, and the result matches what the hand did
// to your stack: a good all-in usually holds, a bad one usually doesn't. The cards are dealt to fit that result,
// so the drama never contradicts the grade.
using System;
using System.Collections.Generic;
using System.Linq;

namespace CFDream
{
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

        // does this all-in play out on screen? Always at the bubble and the final table; otherwise about half the time,
        // never two hands running, and never if the player turned the animations off in Settings.
        public static bool Plays(Run run, Decision d)
        {
            var s = Game.State; if (s != null && s.FlagN("sd_skip") > 0) return false;
            string st = run.Stage;
            if (st == "final" || st == "bubble" || st == "satbubble") return true;
            if (run.showH == run.h - 1 && run.si == run.showSi) return false;
            var rng = new System.Random((int)(Rng.Hash($"sdp|{run.ev.id}|{run.si}|{run.h}|{d.j}") & 0x7fffffff));
            return rng.NextDouble() < 0.55;
        }

        public static Showdown Make(Run run, Decision d)
        {
            var sp = run.spot; var heroSeat = sp.seats[sp.hero];
            var rng = new System.Random((int)(Rng.Hash($"{run.ev.id}|{run.si}|{run.h}|{run.results.Count}|{sp.hand[0]}|{sp.hand[1]}") & 0x7fffffff));
            var sd = new Showdown { hero = sp.hand.ToArray(), before = d.before, after = d.after };
            bool good = d.after > d.before || (d.after == d.before && d.ok);
            // who's in it with you: whoever shoved before you, or someone left to act who calls
            int opp = -1;
            for (int i = 0; i < sp.seats.Count; i++) if (i != sp.hero && !sp.seats[i].folded && (sp.seats[i].act ?? "").Contains("allin")) opp = i;
            if (opp < 0) { double best = -1; for (int i = 0; i < sp.seats.Count; i++) if (i != sp.hero && !sp.seats[i].folded && sp.seats[i].put > best) { best = sp.seats[i].put; opp = i; } }
            if (opp < 0) opp = (sp.hero + 1) % Math.Max(1, sp.seats.Count);
            sd.oppSeat = opp >= 0 && opp < sp.seats.Count ? sp.seats[opp].pos : "Villain";
            bool finalTable = run.Stage == "final" && run.boss != null && run.drill == null;
            sd.oppName = finalTable ? Story.NameOf(run.boss) : sd.oppSeat;
            // you shoved and it was right: half the time they just fold
            if (sp.acts[d.j] == "allin" && good && !finalTable && rng.NextDouble() < 0.45) { sd.folded = true; sd.win = true; return sd; }

            var deck = Enumerable.Range(0, 52).Where(c => c != sd.hero[0] && c != sd.hero[1]).ToList();
            int[] Draw(List<int> from, int n) { var l = from.ToList(); var o = new int[n]; for (int i = 0; i < n; i++) { int k = rng.Next(l.Count); o[i] = l[k]; l.RemoveAt(k); } return o; }
            int[] bestOpp = null, bestBoard = null; bool matched = false;
            for (int tries = 0; tries < 400 && !matched; tries++)
            {
                var o = Draw(deck, 2);
                // a believable range: pairs and big cards more often than rags
                int hi = Math.Max(o[0] >> 2, o[1] >> 2), lo = Math.Min(o[0] >> 2, o[1] >> 2); bool pair = hi == lo;
                double like = pair ? 1 : hi >= 9 ? 0.75 : hi >= 7 && lo >= 4 ? 0.45 : 0.15;
                if (rng.NextDouble() > like) continue;
                var rest = deck.Where(c => c != o[0] && c != o[1]).ToList();
                var b = Draw(rest, 5);
                long hs = Eval(sd.hero.Concat(b)), os = Eval(o.Concat(b));
                bool w = hs > os;
                bestOpp = o; bestBoard = b;
                if (w == good && hs != os) matched = true;
            }
            sd.opp = bestOpp; sd.board = bestBoard;
            long H = Eval(sd.hero.Concat(sd.board)), O = Eval(sd.opp.Concat(sd.board));
            sd.win = H > O; sd.chop = H == O; sd.heroHand = Name(H); sd.oppHand = Name(O);
            // how often you'd win this all-in from here
            var rest2 = deck.Where(c => c != sd.opp[0] && c != sd.opp[1]).ToList(); int won = 0, n = 300; double ties = 0;
            for (int i = 0; i < n; i++) { var b = Draw(rest2, 5); long a = Eval(sd.hero.Concat(b)), c = Eval(sd.opp.Concat(b)); if (a > c) won++; else if (a == c) ties += 0.5; }
            sd.eq = (won + ties) / n;
            return sd;
        }

        // ------------------------------------------------------------ the best five of seven
        static readonly string[] Cat = { "High card", "A pair", "Two pair", "Three of a kind", "A straight", "A flush", "A full house", "Four of a kind", "A straight flush" };
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
}
