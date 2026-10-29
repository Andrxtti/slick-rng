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
        static Sprite rounded;
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
                    var r = Hands.Start(Lib.Event("nightly"), "spot"); r.libGid = gid; r.libIndex = ti;
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

            // the table: the kit's racetrack plate (Biscayne green live, CF Tilt teal online; the tall plate when the space
            // is taller than wide), fitted inside its area; seats on the rail, chips in front of them, the pot in the middle
            var area = Ui.At(Ui.Box(s, "TableArea"), 0.02f, 0.43f, 0.98f, 0.875f);
            Canvas.ForceUpdateCanvases();
            bool tall = area.rect.height > area.rect.width * 0.9f;
            var plateSprite = Resources.Load<Sprite>("Kit/table_" + (ev.online ? "online" : "live") + (tall ? "_portrait" : ""));
            var table = Ui.Box(area, "Plate"); Ui.Fill(table);
            var fit = table.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            if (plateSprite != null) { var pimg = table.gameObject.AddComponent<Image>(); pimg.sprite = plateSprite; pimg.raycastTarget = false; fit.aspectRatio = plateSprite.rect.width / plateSprite.rect.height; }
            else fit.aspectRatio = tall ? 0.6f : 2f;
            Canvas.ForceUpdateCanvases();
            int n = sp.seats.Count, hero = sp.hero;
            float rx = tall ? 0.4f : 0.43f, ry = tall ? 0.44f : 0.4f;   // the rail, as a share of the plate
            for (int i = 0; i < n; i++)
            {
                int k = (i - hero + n) % n;                       // you at the bottom; the next seat in order sits to your left (clockwise)
                float a = -Mathf.PI / 2 - k * 2 * Mathf.PI / n;
                float x = 0.5f + Mathf.Cos(a) * rx, y = 0.5f + Mathf.Sin(a) * ry;
                Seat(table, sp.seats[i], i == hero, x, y, answered == null, ev.online, tall, fit.aspectRatio);
            }
            float aspect = fit.aspectRatio;
            Chips(table, sp.pot, 0.5f, 0.53f, tall ? 0.09f : 0.05f, aspect);   // the pot
            Ui.At(Ui.Num(table, $"pot {Bb(sp.pot)} bb", 30, Ui.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), 0.3f, 0.44f, 0.7f, 0.5f);
            Ui.At(Ui.Label(table, sp.cat, 24, new Color(1, 1, 1, 0.6f)), 0.2f, 0.39f, 0.8f, 0.44f);

            // your cards: large, slightly fanned, with the kit's soft shadow
            var cards = Ui.At(Ui.Box(s, "Cards"), 0.3f, 0.295f, 0.7f, 0.445f);
            var c0 = CardFace(cards, sp.hand[0], 0.27f, 0.53f, 6f); var c1 = CardFace(cards, sp.hand[1], 0.47f, 0.73f, -6f);
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

        static readonly string[] Screens = { "ripriver_88", "nitbox", "flopzilla", "gr1nd_city", "the_mathematician", "SunnyIsles_Sal", "luckyluke305", "acehigh_andy", "bluffcatcher" };
        static Image Kit(Transform parent, string path, float x0, float y0, float x1, float y1, Color? tint = null, bool sliced = false)
        {
            var img = new GameObject(path, typeof(RectTransform)).AddComponent<Image>(); img.transform.SetParent(parent, false);
            img.sprite = Resources.Load<Sprite>("Kit/" + path); img.raycastTarget = false; img.color = tint ?? Color.white;
            if (sliced) { img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1.4f; } else img.preserveAspect = true;
            Ui.At(img, x0, y0, x1, y1); return img;
        }

        // a seat on the rail: the kit's dark-glass plate, the position (or a screen name and two-letter avatar online),
        // the stack in IBM Plex Mono; your seat in gold with the glow while it's your move
        static void Seat(RectTransform table, SeatView seat, bool you, float x, float y, bool deal, bool online, bool tall, float aspect)
        {
            float w = tall ? 0.15f : 0.095f, h = tall ? 0.045f : 0.075f;
            if (you && deal)
            {
                var glow = Kit(table, "ui/glow", x - w * 1.45f, y - h * 1.6f, x + w * 1.45f, y + h * 1.6f, new Color(0.95f, 0.76f, 0.35f, 0.5f), true);
                Ui.Pulse(glow, 0.15f, 0.6f);
            }
            var plate = Kit(table, "ui/seat_plate", x - w, y - h, x + w, y + h, you ? new Color(0.95f, 0.76f, 0.35f, 0.97f) : new Color(0.04f, 0.04f, 0.06f, seat.folded ? 0.5f : 0.86f), true);
            var box = plate.rectTransform; box.name = seat.pos;
            var fg = you ? Ui.Night : seat.folded ? Ui.Dim : Ui.Ink;
            string name = you ? "YOU · " + seat.pos : online ? Screens[(seat.pos.GetHashCode() & 0x7fff) % Screens.Length] : seat.pos;
            if (online && !you)   // a two-letter avatar, no faces online
            {
                var av = Ui.At(Ui.Tile(box, "Avatar", new Color(0.18f, 0.55f, 0.55f), 999), -0.18f, 0.2f, 0.06f, 0.8f);
                av.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                Ui.Label(av, name.Substring(0, 2).ToUpperInvariant(), 18, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            Ui.At(Ui.Label(box, name, online && !you ? 18 : 22, fg, TextAnchor.MiddleCenter, FontStyle.Bold), 0.04f, 0.5f, 0.96f, 0.95f);
            Ui.At(Ui.Num(box, seat.stack - seat.put <= 0.01 ? "all in" : Bb(seat.stack - seat.put) + " bb", 22, fg), 0, 0.06f, 1, 0.5f);
            if (!string.IsNullOrEmpty(seat.act) || seat.put > 0)
            {
                // the action already carries its size ("Jam 7.9", "Raise 2.3"); otherwise the amount in (blinds), or both
                string act = seat.act ?? "", bet = act.Any(char.IsDigit) || act.StartsWith("Fold") ? act : (act + (seat.put > 0 ? " " + Bb(seat.put) : "")).Trim();
                float bx = x + (0.5f - x) * 0.38f, by = y + (0.5f - y) * 0.42f;
                var chips = Ui.Box(table, "Bet"); Ui.Fill(chips);
                if (seat.put > 0 && !act.StartsWith("Fold")) Chips(chips, seat.put, bx, by + (tall ? 0.008f : 0.015f), tall ? 0.075f : 0.042f, aspect);
                var label = Ui.At(Ui.Num(chips, bet, 20, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), bx - 0.12f, by - (tall ? 0.03f : 0.05f), bx + 0.12f, by - (tall ? 0.008f : 0.012f));
                if (deal) { var sz = table.rect.size; Ui.Slide(chips, new Vector2(-(0.5f - x) * 0.38f * sz.x, -(0.5f - y) * 0.42f * sz.y), 0.05f, 0.35f); }   // the bet goes out from the seat
            }
            if (seat.pos == "BTN")   // the dealer button: toward the middle, to one side of the bet
            {
                float dx = 0.5f - x, dy = 0.5f - y, px = x + dx * 0.3f - dy * 0.25f, py = y + dy * 0.3f + dx * 0.25f, r = tall ? 0.03f : 0.02f;
                Kit(table, "chips/dealer_button", px - r, py - r * (tall ? 0.6f : 2f), px + r, py + r * (tall ? 0.6f : 2f));
            }
        }

        // a stack of side-view chips for an amount in big blinds (denomination by size, one chip per step, ~9 px apart)
        // w is in plate widths; aspect (plate width / height) turns it into plate heights so the chip keeps its shape
        static void Chips(RectTransform parent, double bb, float x, float y, float w, float aspect)
        {
            string den = bb < 2 ? "5" : bb < 8 ? "25" : bb < 25 ? "100" : bb < 60 ? "500" : "1K";
            int n = Mathf.Clamp((int)Math.Round(bb < 2 ? bb * 2 : bb < 8 ? bb : bb / 4), 1, 9);
            float hh = w * aspect * 0.42f, step = hh * 0.36f;
            for (int i = 0; i < n; i++) Kit(parent, $"chips/chip_{den}_side", x - w / 2, y + i * step, x + w / 2, y + i * step + hh);
        }

        // a card from the kit (four-colour faces, "As", "Td"), on its soft shadow, turned by `tilt` degrees
        public static RectTransform CardFace(RectTransform parent, int c, float x0, float x1, float tilt = 0)
        {
            var holder = Ui.At(Ui.Box(parent, "Card"), x0, 0, x1, 1);   // the slot takes the card's shape, so the shadow hugs it
            var slot = Ui.Box(holder, "Face"); Ui.Fill(slot);
            var af = slot.gameObject.AddComponent<AspectRatioFitter>(); af.aspectMode = AspectRatioFitter.AspectMode.FitInParent; af.aspectRatio = 360f / 504f;
            Kit(slot, "ui/card_shadow", -0.08f, -0.1f, 1.08f, 1.06f, new Color(0, 0, 0, 0.55f), true);
            var face = Kit(slot, "cards/" + Hands.Card(c), 0, 0, 1, 1);
            slot.localRotation = Quaternion.Euler(0, 0, tilt);
            return holder;
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
