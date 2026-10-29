// The table (BETA1 task 13): one solver spot at a time on a felt drawn in code (phone-light: no 3D, two generated
// textures), your cards, the seats, the options, then the grade the way a chess engine grades a move.
using System;
using System.Linq;
using Dream.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Dream.Game
{
    public static class Table
    {
        static Sprite felt, rounded;
        static RectTransform root; static Run run; static Action<bool> done; static bool mistake;
        // a new level is about to be dealt: the game may play something first (the final-table film) and then go on
        public static Func<Run, Action, bool> OnLevel;

        public static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.white;

        public static void Show(RectTransform parent, Run r, Action<bool> onDone, Spot preset = null)
        {
            root = parent; run = r; done = onDone; mistake = false;
            if (preset != null) { run.spot = preset; Draw(null); return; }
            Hands.Deal(run); Draw(null);
            Moments.Level(root, run, () => Draw(null));   // the first level: the room (or CF Tilt), the stage, Slick's tip
        }

        // test command "spot <pos> [gid]": a library spot where you sit in that seat, someone has acted, and there's a BTN
        public static string ShowSeat(RectTransform parent, string pos, string gid)
        {
            Lib.Load(); var rng = new Rng("spot|" + pos);
            var tables = Lib.Tables(gid);
            for (int ti = 0; ti < tables.Count; ti++)
            {
                var table = (System.Collections.Generic.Dictionary<string, object>)tables[ti]; var nodes = J.L(table, "n");
                for (int ni = 0; ni < nodes.Count; ni++)
                {
                    var node = (System.Collections.Generic.Dictionary<string, object>)nodes[ni];
                    var sp = Hands.BuildSpot(gid, ti, table, node, ni, (System.Collections.Generic.List<object>)J.L(node, "r")[0], 0.5, rng);
                    if (sp.seats[sp.hero].pos != pos || sp.seats.Count < 8 || !sp.seats.Any(v => !string.IsNullOrEmpty(v.act) && v.act.Any(char.IsDigit))) continue;
                    var r = Hands.Start(Lib.Event("deep"), "spot"); r.libGid = gid; r.libIndex = ti;
                    Show(parent, r, _ => { }, sp);
                    return string.Join(" ", sp.seats.Select(v => v.pos + (string.IsNullOrEmpty(v.act) ? "" : "(" + v.act + ")")));
                }
            }
            return "no spot with hero " + pos;
        }

        static void Draw(Decision answered)
        {
            Ui.Clear(root);
            var s = Ui.Panel(root, "Table", Ui.Night);
            var sp = run.spot; var ev = run.ev;
            // top: the event, the level, the hand, your tournament life
            Ui.At(Ui.Label(s, ev.name.ToUpperInvariant(), 32, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold), 0.05f, 0.94f, 0.95f, 0.975f);
            int per = Hands.Per(run);
            Ui.At(Ui.Label(s, $"{StageName(run.Stage)}  ·  hand {run.h + 1} of {per}", 28, Ui.Dim, TextAnchor.MiddleLeft), 0.05f, 0.91f, 0.7f, 0.94f);
            var life = Ui.At(Ui.Panel(s, "Life", Ui.Card), 0.05f, 0.885f, 0.95f, 0.9f);
            var lf = Ui.Panel(life, "Fill", run.stack >= 100 ? Ui.Aqua : run.stack >= 50 ? Ui.Gold : Ui.Neon);
            lf.anchorMax = new Vector2(Mathf.Clamp01(run.stack / 200f), 1);
            Ui.At(Ui.Label(s, ev.kind == "cash" ? "your stack" : "tournament life", 22, Ui.Dim, TextAnchor.MiddleRight), 0.5f, 0.9f, 0.95f, 0.93f);

            // the felt and the seats
            var table = Ui.At(Ui.Box(s, "Felt"), 0.04f, 0.44f, 0.96f, 0.86f);
            var fimg = table.gameObject.AddComponent<Image>(); fimg.sprite = Felt(); fimg.preserveAspect = false;
            int n = sp.seats.Count, hero = sp.hero;
            for (int i = 0; i < n; i++)
            {
                int k = (i - hero + n) % n;                       // you at the bottom; the next seat in order sits to your left (clockwise)
                float a = -Mathf.PI / 2 - k * 2 * Mathf.PI / n;
                float x = 0.5f + Mathf.Cos(a) * 0.43f, y = 0.5f + Mathf.Sin(a) * 0.41f;
                Seat(table, sp.seats[i], i == hero, x, y, answered == null);
            }
            Ui.At(Ui.Num(table, $"pot {Bb(sp.pot)} bb", 34, Ui.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), 0.3f, 0.48f, 0.7f, 0.56f);
            Ui.At(Ui.Label(table, sp.cat, 26, new Color(1, 1, 1, 0.6f)), 0.25f, 0.42f, 0.75f, 0.48f);

            // your cards
            var cards = Ui.At(Ui.Box(s, "Cards"), 0.3f, 0.31f, 0.7f, 0.43f);
            var c0 = CardFace(cards, sp.hand[0], 0.02f, 0.48f); var c1 = CardFace(cards, sp.hand[1], 0.52f, 0.98f);
            if (answered == null) { Ui.Slide(c0, new Vector2(60, 420), 0.15f); Ui.Slide(c1, new Vector2(-40, 420), 0.27f); }   // dealt from the middle

            Waiting = answered == null;
            if (answered == null)
            {
                Ui.At(Ui.Label(s, "Your move", 34, Ui.Dim), 0.05f, 0.26f, 0.95f, 0.3f);
                int m = sp.acts.Count; float w = 0.9f / m;
                for (int j = 0; j < m; j++)
                {
                    int jj = j; string act = sp.acts[j];
                    var col = act == "fold" ? Ui.Card : act == "allin" ? Ui.Neon : Ui.Gold;
                    var b = Ui.Button(s, sp.Label(j), () => Answer(jj), col, act == "fold" ? Ui.Ink : Ui.Night, 40);
                    Ui.At(b, 0.05f + j * w + 0.01f, 0.13f, 0.05f + (j + 1) * w - 0.01f, 0.23f);
                    b.GetComponent<Image>().sprite = Rounded(); b.GetComponent<Image>().type = Image.Type.Sliced;
                }
            }
            else Grade(s, answered);
        }

        // for the test driver (Editor/AutoPlay): answer the spot on screen with the solver's best play, or go on
        public static bool Waiting;
        public static void AutoStep(bool worst = false)
        {
            if (run == null) return;
            if (!Waiting) { Next(); return; }
            var sp = run.spot; int pick = Hands.Grade(sp, 0).bestIdx;
            if (worst) { var order = Enumerable.Range(0, sp.acts.Count).OrderBy(j => Lib.Labels[Hands.Grade(sp, j).lab].score).ToList(); pick = order[0]; }
            Answer(pick);
        }

        static void Answer(int j)
        {
            var r = Hands.Answer(run, j);
            if (r.tier == "bad") mistake = true;
            void Verdict() { Draw(r); Moments.Talk(root, run, r); }
            if (Showdown.AllIn(run.spot, j) && Showdown.Plays(run, r))   // the all-in plays out before the verdict
            {
                run.showH = run.h; run.showSi = run.si;
                Moments.AllIn(root, run, Showdown.Make(run, r), Verdict);
            }
            else Verdict();
        }

        static void Grade(RectTransform s, Decision r)
        {
            var L = Lib.Labels[r.lab]; var col = Hex(L.c);
            var panel = Ui.At(Ui.Panel(s, "Grade", Ui.Card), 0.04f, 0.04f, 0.96f, 0.3f);
            panel.GetComponent<Image>().sprite = Rounded(); panel.GetComponent<Image>().type = Image.Type.Sliced;
            Ui.At(Ui.Title(panel, $"{Ui.Icon(L.icon)}  {L.n}", 68, col, TextAnchor.MiddleLeft), 0.05f, 0.72f, 0.7f, 0.95f);
            int dl = r.after - r.before;
            Ui.At(Ui.Num(panel, (dl >= 0 ? "+" : "") + dl, 52, dl >= 0 ? Ui.Aqua : Ui.Neon, TextAnchor.MiddleRight, FontStyle.Bold), 0.6f, 0.72f, 0.95f, 0.95f);
            Ui.At(Ui.Label(panel, L.line, 30, Ui.Ink, TextAnchor.UpperLeft), 0.05f, 0.5f, 0.95f, 0.7f);
            var sp = r.spot;
            string often = $" ({Math.Round(sp.strat[r.bestIdx] * 100)}% of the time)";
            string why = r.mine == r.best ? "The solver agrees." : $"You: {r.mine}.  The solver: {r.best}{often}.";
            if (r.loss.HasValue && r.loss.Value > 0.0001) why += $"  Cost: {r.loss.Value:0.00} {(sp.unit == "icm" ? "% of the prize pool" : "bb")}.";
            Ui.At(Ui.Label(panel, why, 28, Ui.Dim, TextAnchor.UpperLeft), 0.05f, 0.3f, 0.95f, 0.5f);
            Ui.At(Ui.Label(panel, "Coaching by Confirmed Fire", 20, new Color(1, 1, 1, 0.35f), TextAnchor.LowerLeft), 0.05f, 0.03f, 0.5f, 0.12f);
            var b = Ui.Button(panel, "Next", Next, Ui.Gold, Ui.Night, 44); Ui.At(b, 0.55f, 0.05f, 0.95f, 0.27f);
            b.GetComponent<Image>().sprite = Rounded(); b.GetComponent<Image>().type = Image.Type.Sliced;
        }

        static void Next()
        {
            string was = run.Stage; var what = Hands.Next(run);
            var d = done;
            if (what == "finish") { run = null; d(mistake); return; }
            if (what == "cooler")   // the all-in that goes wrong, then the cooler card (old Run.cs)
            {
                var r0 = run; var last = r0.results.LastOrDefault(); r0.cooler = Moments.CoolerStory(r0);
                int before = Flow.I != null ? Flow.I.S.coolers : 0; if (Flow.I != null) Flow.I.S.coolers++;
                void Card() => Moments.Cooler(root, r0, before, () => { run = null; d(mistake); });
                if (last != null && PlayerPrefs.GetInt("sd_skip", 0) == 0) Moments.AllIn(root, r0, Showdown.Make(r0, last, true), Card); else Card();
                return;
            }
            // a new level: the final-table film, in the money, the level card, the bubble; heads-up before the last hand
            void Deal() { Hands.Deal(run); Draw(null); }
            void HeadsUp() { if (run.Stage == "final" && run.h == Hands.Per(run) - 1 && !run.huShown && run.ev.kind == "mtt") { run.huShown = true; Moments.HeadsUp(root, run, Deal); } else Deal(); }
            if (what != "level") { HeadsUp(); return; }
            void Bubble() { if ((run.Stage == "bubble" || run.Stage == "satbubble") && !run.bubbleShown) { run.bubbleShown = true; Moments.Bubble(root, run, HeadsUp); } else HeadsUp(); }
            void LevelCard() => Moments.Level(root, run, Bubble);
            void Money() { if ((was == "bubble" || was == "satbubble") && !run.itmShown) { run.itmShown = true; Moments.InTheMoney(root, run, LevelCard); } else LevelCard(); }
            if (OnLevel != null && OnLevel(run, Money)) return;
            Money();
        }

        static string StageName(string st) => st switch { "early" => "Early levels", "middle" => "Middle levels", "bubble" => "The bubble", "final" => "Final table", "deep" => "Deep run", "cash" => "Cash game", _ => st };
        static string Bb(double v) => Math.Abs(v - Math.Round(v)) < 0.01 ? Math.Round(v).ToString("0") : v.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);

        static void Seat(RectTransform table, SeatView seat, bool you, float x, float y, bool deal = false)
        {
            if (you && deal)   // a soft gold glow round your seat while it's your move
            {
                var glow = Ui.At(Ui.Tile(table, "Glow", new Color(0.95f, 0.76f, 0.35f, 0.3f), 40), x - 0.115f, y - 0.075f, x + 0.115f, y + 0.075f);
                Ui.Pulse(glow.GetComponent<Image>(), 0.08f, 0.45f);
            }
            var box = Ui.At(Ui.Panel(table, seat.pos, you ? new Color(0.95f, 0.76f, 0.35f, 0.95f) : new Color(0.05f, 0.05f, 0.07f, seat.folded ? 0.45f : 0.85f)), x - 0.1f, y - 0.06f, x + 0.1f, y + 0.06f);
            box.GetComponent<Image>().sprite = Rounded(); box.GetComponent<Image>().type = Image.Type.Sliced;
            var fg = you ? Ui.Night : seat.folded ? Ui.Dim : Ui.Ink;
            Ui.At(Ui.Label(box, you ? "YOU · " + seat.pos : seat.pos, 24, fg, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 0.5f, 1, 0.95f);
            Ui.At(Ui.Num(box, seat.stack - seat.put <= 0.01 ? "all in" : Bb(seat.stack - seat.put) + " bb", 24, fg), 0, 0.05f, 1, 0.5f);
            if (!string.IsNullOrEmpty(seat.act) || seat.put > 0)
            {
                // the action already carries its size ("Jam 7.9", "Raise 2.3"); otherwise the amount in (blinds), or both
                string act = seat.act ?? "", bet = act.Any(char.IsDigit) || act.StartsWith("Fold") ? act : (act + (seat.put > 0 ? " " + Bb(seat.put) : "")).Trim();
                var chip = Ui.At(Ui.Num(table, bet, 22, Ui.Gold), x - 0.12f + (0.5f - x) * 0.35f, y - 0.03f + (0.5f - y) * 0.45f, x + 0.12f + (0.5f - x) * 0.35f, y + 0.03f + (0.5f - y) * 0.45f);
                if (deal) { var sz = table.rect.size; Ui.Slide(chip.rectTransform, new Vector2(-(0.5f - x) * 0.35f * sz.x, -(0.5f - y) * 0.45f * sz.y), 0.05f, 0.35f); }   // the bet goes out from the seat
            }
            if (seat.pos == "BTN")   // the dealer button: toward the middle, to one side of the bet
            {
                float dx = 0.5f - x, dy = 0.5f - y, px = x + dx * 0.32f - dy * 0.22f, py = y + dy * 0.32f + dx * 0.22f;
                var puck = Ui.At(new GameObject("Dealer", typeof(RectTransform)).AddComponent<Image>(), 0, 0, 1, 1);
                puck.transform.SetParent(table, false); Ui.At(puck, px - 0.022f, py - 0.04f, px + 0.022f, py + 0.04f);
                puck.sprite = Circle(); puck.preserveAspect = true; puck.color = new Color(0.97f, 0.96f, 0.93f); puck.raycastTarget = false;
                Ui.Label(puck.transform, "D", 26, Ui.Night, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }

        static RectTransform CardFace(RectTransform parent, int c, float x0, float x1)
        {
            var card = Ui.At(Ui.Panel(parent, "Card", new Color(0.97f, 0.96f, 0.93f)), x0, 0, x1, 1);
            card.GetComponent<Image>().sprite = Rounded(); card.GetComponent<Image>().type = Image.Type.Sliced;
            int suit = c & 3; var col = suit == 0 ? new Color(0.1f, 0.45f, 0.2f) : suit == 1 ? new Color(0.15f, 0.35f, 0.75f) : suit == 2 ? new Color(0.78f, 0.1f, 0.12f) : new Color(0.08f, 0.08f, 0.1f);   // four-colour deck
            Ui.At(Ui.Label(card, "23456789TJQKA"[c >> 2].ToString().Replace("T", "10"), 96, col, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 0.42f, 1, 1);
            var pip = Ui.At(new GameObject("Suit", typeof(RectTransform)).AddComponent<Image>(), 0.25f, 0.06f, 0.75f, 0.46f);
            pip.transform.SetParent(card, false); Ui.At(pip, 0.25f, 0.06f, 0.75f, 0.46f);
            pip.sprite = Suit(suit); pip.color = col; pip.preserveAspect = true; pip.raycastTarget = false;
            return card;
        }

        // generated art: the felt (worn centre, dark rail) and a rounded rectangle for sliced panels
        static Sprite Felt()
        {
            if (felt != null) return felt;
            int W = 512, H = 384; var t = new Texture2D(W, H, TextureFormat.RGBA32, false); var px = new Color32[W * H];
            var rng = new System.Random(7);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W * 2 - 1, v = (y + 0.5f) / H * 2 - 1, d = Mathf.Sqrt(u * u + v * v);
                    Color c;
                    if (d > 0.99f) c = new Color(0, 0, 0, 0);
                    else if (d > 0.9f) c = Color.Lerp(new Color(0.09f, 0.06f, 0.05f), new Color(0.03f, 0.025f, 0.02f), (d - 0.9f) / 0.09f);   // the rail
                    else
                    {
                        var felt0 = new Color(0.16f, 0.42f, 0.28f); var edge = new Color(0.07f, 0.22f, 0.14f);
                        c = Color.Lerp(felt0, edge, Mathf.SmoothStep(0, 1, d / 0.9f)) * (0.95f + 0.1f * (float)rng.NextDouble());
                        c.a = 1;
                    }
                    px[y * W + x] = c;
                }
            t.SetPixels32(px); t.Apply();
            return felt = Sprite.Create(t, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f));
        }

        // suit pips drawn from shapes (the built-in font has no suit glyphs on the web): 0 clubs, 1 diamonds, 2 hearts, 3 spades
        static readonly Sprite[] suits = new Sprite[4];
        static Sprite Suit(int k)
        {
            if (suits[k] != null) return suits[k];
            int S = 128; var t = new Texture2D(S, S, TextureFormat.RGBA32, false); var px = new Color32[S * S];
            bool Circle(float x, float y, float cx, float cy, float r) => (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r;
            bool Heart(float x, float y) => Circle(x, y, -0.25f, 0.18f, 0.27f) || Circle(x, y, 0.25f, 0.18f, 0.27f) || (y <= 0.2f && y >= -0.75f && Mathf.Abs(x) <= (y + 0.75f) * 0.55f);
            for (int yy = 0; yy < S; yy++)
                for (int xx = 0; xx < S; xx++)
                {
                    float x = (xx + 0.5f) / S * 2 - 1, y = (yy + 0.5f) / S * 2 - 1; bool on;
                    if (k == 1) on = Mathf.Abs(x) / 0.62f + Mathf.Abs(y) / 0.9f <= 1;
                    else if (k == 2) on = Heart(x, y + 0.08f);
                    else if (k == 3) on = Heart(x, -y + 0.2f) || (y < -0.35f && y > -0.85f && Mathf.Abs(x) <= (-0.35f - y) * 0.45f + 0.05f);
                    else on = Circle(x, y, 0, 0.38f, 0.28f) || Circle(x, y, -0.32f, -0.05f, 0.28f) || Circle(x, y, 0.32f, -0.05f, 0.28f) || (y < 0 && y > -0.85f && Mathf.Abs(x) <= (-y) * 0.3f + 0.05f);
                    px[yy * S + xx] = on ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            t.SetPixels32(px); t.Apply();
            return suits[k] = Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
        }

        static Sprite circle;
        static Sprite Circle()
        {
            if (circle != null) return circle;
            int S = 64; var t = new Texture2D(S, S, TextureFormat.RGBA32, false); var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - S / 2f) * (x + 0.5f - S / 2f) + (y + 0.5f - S / 2f) * (y + 0.5f - S / 2f));
                    px[y * S + x] = new Color(1, 1, 1, Mathf.Clamp01(S / 2f - 1 - d + 0.5f));
                }
            t.SetPixels32(px); t.Apply();
            return circle = Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f));
        }

        static Sprite Rounded()
        {
            if (rounded != null) return rounded;
            int S = 64, R = 20; var t = new Texture2D(S, S, TextureFormat.RGBA32, false); var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(R - x - 0.5f, x + 0.5f - (S - R))), dy = Mathf.Max(0, Mathf.Max(R - y - 0.5f, y + 0.5f - (S - R)));
                    float a = Mathf.Clamp01(R - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    px[y * S + x] = new Color(1, 1, 1, a);
                }
            t.SetPixels32(px); t.Apply();
            return rounded = Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
        }
    }
}
