// First-time tutorial pop-ups. The screen dims except the part being explained (a lime ring around it), a bubble
// says what it is, and the player taps Got it, or does the thing (taps inside the ring). Each tip shows once per
// career; Skip tutorial turns them all off, and the Story tab can bring them back.
// Key words in a tip are written *like this* and drawn in lime.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public static class Tips
    {
        class Rule
        {
            public string id, title, target;                 // target: a USS class (".x") or element name; null = the middle of the screen
            public Func<State, bool> when;
            public Func<State, string> text;
            public bool doIt;                                // the player can also do the thing inside the ring to move on
            public Action done;
        }

        static VisualElement layer;
        static string showing; static bool pending;
        public static bool Busy => showing != null || pending;
        static readonly Color Dim = new Color(0.02f, 0.02f, 0.03f, 0.72f);
        const string Lime = "#b8ff2e", Blue = "#7fbde6";

        public static bool Off(State s) => s != null && s.FlagN("tips_off") > 0;
        public static bool Seen(State s, string id) => s != null && s.FlagN("tip_" + id) > 0;
        static void Mark(string id) { var s = Game.State; if (s == null) return; s.SetFlag("tip_" + id); s.Save(); }
        public static void Reset(State s)
        {
            foreach (var k in s.flags.Keys.Where(k => k.StartsWith("tip_") || k == "tips_off").ToList()) s.flags.Remove(k);
            s.Save();
        }

        public static void Attach(VisualElement root)
        {
            layer = U.Div("tips-layer"); layer.pickingMode = PickingMode.Ignore;
            layer.style.position = Position.Absolute; layer.style.left = 0; layer.style.top = 0; layer.style.right = 0; layer.style.bottom = 0;
            root.Add(layer);
        }
        // a new screen is being drawn: whatever was pointed at is gone
        public static void Clear() { if (layer == null) return; layer.Clear(); layer.pickingMode = PickingMode.Ignore; showing = null; pending = false; }
        public static void Later(int ms, Action a) { if (layer != null) layer.schedule.Execute(() => { try { a(); } catch (Exception e) { Debug.LogException(e); } }).StartingIn(ms); }

        // ------------------------------------------------------------ what to teach, and where
        static string Path(State s) => s.job == "nights" ? "keep" : "quit";
        static bool In(View v) => Game.View == v;
        static bool Hub(string tab) => Game.View == View.Hub && Game.Tab == tab;
        static bool Has(Func<Run, bool> f) => Game.Run != null && f(Game.Run);
        static List<Rule> rules;
        static List<Rule> Rules()
        {
            if (rules != null) return rules;
            rules = new List<Rule>
            {
                // the story
                R("t1", "The story", null, s => In(View.Scene) && Game.Scene != null && !Game.Scene.replay && Game.CurrentBeat()?.choice == null,
                    s => "Tap anywhere to keep the story going. Tap while someone is talking to see the *whole line*."),
                R("t2", "Story choices", ".choices", s => In(View.Scene) && Game.Scene != null && !Game.Scene.replay && Game.Scene.id != "p_offer",
                    s => "Story choices change what people say to you. There's *no wrong answer* here. Tap one.", doIt: true),
                // the first hand
                R("t3", "Your cards", ".hero-cards", s => In(View.Run) && Has(r => r.state == "play" && r.drill == null),
                    s => "These *two cards* are yours. Nobody else at the table can see them."),
                R("t4", "Chips and blinds", ".run-head", s => In(View.Run) && Has(r => r.state == "play" && r.drill == null),
                    s => "*Stack* is your chips, counted in *big blinds*: the forced bet everyone takes turns paying. Blinds go up every *level*, so stacks shrink if you wait."),
                R("t5", "Your moves", ".act-row", s => In(View.Run) && Has(r => r.state == "play" && r.drill == null),
                    s => "*Fold*: throw your hand away. *Call*: match the bet. *Raise*: bet more. When you're not sure, folding costs nothing. Tap a move.", doIt: true),
                R("t6", "The grade", ".verdict", s => In(View.Run) && Has(r => r.state == "answered" && r.drill == null),
                    s => "Every decision is graded by the *solver*, the math of perfect poker. Green means you made the money play; red means it cost you. Results come and go. *Good decisions pay over time.*"),
                R("t7", "The odds", "odds", s => In(View.Run) && Has(r => r.state == "play"),
                    s => "This is the price of a call: what you pay, what you can win, and how often you need to win for the call to pay off."),
                R("t8", "The bubble", ".run-head", s => In(View.Run) && Has(r => (r.Stage == "bubble" || r.Stage == "satbubble") && r.drill == null),
                    s => "Only the top finishers get paid. You're close to that line: the *bubble*. Near it, *staying alive* is worth more than winning chips."),
                R("t17", "Cash games", ".intro", s => In(View.Run) && Has(r => r.state == "ready" && r.ev.kind == "cash"),
                    s => "A *cash game* isn't a tournament. The chips are real money, and you can *leave whenever you like* with what's in front of you."),
                R("t25", "Series and rings", ".intro", s => In(View.Run) && Has(r => r.state == "ready" && r.ev.title == "ring"),
                    s => "A *series* is a poker festival: a run of events over several days. Every Circuit event gives its winner a *ring*."),
                R("t34", "Two days", ".intro", s => In(View.Run) && Has(r => r.state == "ready" && r.ev.id == "classic"),
                    s => "The Biscayne Classic runs *two days*. Survive Saturday and you *bag your chips* for Sunday's *Day 2*, where the money and the trophy are."),
                R("t26", "Satellites", ".intro", s => In(View.Run) && Has(r => r.state == "ready" && r.ev.kind == "sat" && r.ev.id != "classic"),
                    s => "A *satellite* is a cheaper tournament where the prize is a *seat* in a bigger one."),
                R("t28", "Travel", ".intro", s => In(View.Run) && Has(r => r.state == "ready" && r.travel > 0),
                    s => "Away events cost *hotel and travel* on top of the buy-in. You pay it once per trip day."),
                R("t29", "Bracelets", ".intro", s => In(View.Run) && Has(r => r.state == "ready" && r.ev.title == "bracelet"),
                    s => "WCS events run twice a week, all summer. Each winner gets a *gold bracelet*."),
                R("t30", "On TV", ".intro", s => In(View.Run) && Has(r => r.state == "ready" && r.ev.venue == "tv"),
                    s => "On TV the *audience sees your cards*. Your opponents don't. Play the hand you'd play in an empty room."),
                R("t31", "Short Deck", ".intro", s => In(View.Run) && Has(r => r.state == "ready" && r.ev.id == "t25k"),
                    s => "*Short Deck* uses 36 cards: no 2s to 5s. A *flush beats a full house*, and big hands hit more often."),
                // results
                R("t9", "How it went", ".res-hero", s => In(View.Results) && Game.Last != null && Game.Last.ev.kind != "drill",
                    s => "Where you finished, what you won, and how well you played. Your *bankroll* is all the money you have: for poker, and for rent."),
                R("t27", "Re-entry", "reenter", s => In(View.Results),
                    s => "Busted early in a big event? While registration is open you can *buy in again*. Only do it if your roll can take it."),
                // home
                R("t10", "Moving around", ".stick", s => Hub("home") && Game.Scene == null,
                    s => "Push the *stick* to walk. Tap anything with a *label* to use it.", doIt: true),
                R("t18", "Payday", ".morning", s => Hub("calendar") && Morning("Payday"),
                    s => "Payday. Lou's *$2,000* just landed. It lands every Friday while you keep the job."),
                R("t19", "Rent", ".morning", s => Hub("calendar") && Morning("Rent and bills paid"),
                    s => "*Rent and bills* came out on the 1st. This is what's left to play with."),
                // the Spacepad
                R("t15", "Tonight", ".sec-row", s => Hub("calendar") && s.ch >= 1 && s.story.Contains("c1") && s.FlagN("needplan") <= 0,
                    TonightText),
                R("t16", "Rent and bills", ".bills", s => Hub("calendar") && s.ch >= 1 && Seen(s, "t15"),
                    s => "*Rent and bills* come out on the 1st of every month. Green means covered. Never buy into a game that could leave you short."),
                R("t20", "Study", ".study", s => Hub("calendar") && s.story.Contains("lesson1"),
                    s => "Every hand you got wrong is saved here. *Drill them* until the right play is boring."),
                R("t32", "An invite", ".invite", s => Hub("calendar") && s.cal.inv != null,
                    s => "About once a month someone invites you to a *private game*. The stakes fit your roll, but sometimes they're too big. *Saying no is allowed.*"),
                // the closet
                R("t21", "Your Closet", ".fit-chip", s => Hub("closet") && string.IsNullOrEmpty(Game.ClosetCat),
                    s => "This is your *Closet*. What you wear gets a *fit rating*. Better pieces come from what you win and where you've been."),
                R("t22", "Slots", ".slot-tile", s => Hub("closet") && string.IsNullOrEmpty(Game.ClosetCat),
                    s => "Each box is a *slot*: outfit, shoes, watch and more. Tap one to see everything you own for it.", doIt: true),
                R("t24", "Looks", ".looks", s => Hub("closet") && string.IsNullOrEmpty(Game.ClosetCat),
                    s => "Keep *two looks* and flip between them in one tap. *Shuffle* throws a random fit together."),
                R("t23", "Your pieces", ".wearing", s => Hub("closet") && !string.IsNullOrEmpty(Game.ClosetCat) && Game.ClosetCat != "home" && Game.ClosetCat != "ride",
                    s => "Tap *Details* for a piece's story: its grade, who made it, where you earned it, how broken in it is. Tap *Equip* on anything you own to wear it."),
                R("t33", "Home and ride", null, s => Hub("closet") && (Game.ClosetCat == "home" || Game.ClosetCat == "ride"),
                    s => "Your *home* and *ride* live here too. Upgrading changes your apartment and your trips, and raises your *monthly bills*."),
            };
            var walk = rules.First(r => r.id == "t10"); walk.done = () => Game.HomeTipDone();
            return rules;
        }
        static Rule R(string id, string title, string target, Func<State, bool> when, Func<State, string> text, bool doIt = false) => new Rule { id = id, title = title, target = target, when = when, text = text, doIt = doIt };
        static bool Morning(string start) => Game.Morning != null && Game.Morning.Any(m => m.text.StartsWith(start));

        static string TonightText(State s)
        {
            var sl = Calendar.SlotsLeft(s); int lv = Calendar.LiveLeft(s);
            string games = s.job == "nights"
                ? $"You have *{sl.left} online game{(sl.left == 1 ? "" : "s")}* left this week" + (lv > 0 ? " and *1 live game* this weekend." : ".")
                : lv > 0 && sl.used == 0 ? "You can play *3 online games* today, or *1 live game plus 1 online*: the Spacepad comes with you to the card room."
                : $"You can play *{sl.left} more online game{(sl.left == 1 ? "" : "s")}* today" + (lv > 0 ? " and *1 live game*." : ".");
            return "Below is *tonight's schedule*. " + games + " The *Daily Freeroll* is free and doesn't count, so play it every day. Scroll down for the full *poker calendar*. When you're done, *go to bed* to move to your next night.";
        }

        // ------------------------------------------------------------ after every screen is drawn
        public static void AfterRender(VisualElement column)
        {
            if (layer == null || column == null) return;
            var s = Game.State; if (s == null || !s.made || Off(s)) return;
            if (Game.View == View.Transit || Game.View == View.Title || Game.View == View.Create) return;
            Later(140, () => Check(column));
        }
        static void Check(VisualElement column)
        {
            var s = Game.State; if (s == null || Off(s) || Busy || column.panel == null) return;
            if (DreamApp.I != null && DreamApp.I.Modal.resolvedStyle.display == DisplayStyle.Flex) return;   // a sheet is up
            foreach (var r in Rules())
            {
                if (Seen(s, r.id)) continue;
                bool ok; try { ok = r.when(s); } catch { ok = false; }
                if (!ok) continue;
                VisualElement t = null;
                if (r.target != null) { t = Find(column, r.target); if (t == null || t.resolvedStyle.display == DisplayStyle.None) continue; }
                var rule = r;
                Show(r.id, r.title, r.text(s), t, () => { rule.done?.Invoke(); Later(60, () => Check(column)); }, r.doIt);
                return;
            }
        }
        public static VisualElement OnScreen(string sel) { var r = layer?.parent; return r == null ? null : Find(r, sel); }
        static VisualElement Find(VisualElement root, string sel) => sel.StartsWith(".") ? root.Q(className: sel.Substring(1)) : root.Q(sel);

        // ------------------------------------------------------------ one tip
        // Shows the tip unless it's been seen or tips are off; `then` always runs (right away when nothing shows).
        public static void Show(string id, string title, string text, VisualElement target, Action then = null, bool doIt = false, Action<VisualElement> body = null)
        {
            var s = Game.State;
            if (layer == null || s == null || Off(s) || Seen(s, id)) { then?.Invoke(); return; }
            pending = true;
            // a target inside a scroll view is brought into view first
            var sv = target?.GetFirstAncestorOfType<ScrollView>();
            if (sv != null) { try { sv.ScrollTo(target); } catch { } }
            Later(sv != null ? 140 : 60, () => Draw(id, title, text, target, then, doIt, body));
        }
        public static void ShowId(string id, string title, string text, string target, VisualElement within, Action then = null)
            => Show(id, title, text, target == null || within == null ? null : Find(within, target), then);

        static void Draw(string id, string title, string text, VisualElement target, Action then, bool doIt, Action<VisualElement> body)
        {
            pending = false;
            if (target != null && target.panel == null) { then?.Invoke(); return; }   // the screen changed underneath
            layer.Clear(); showing = id; layer.pickingMode = PickingMode.Ignore;
            float W = layer.layout.width, H = layer.layout.height;
            if (float.IsNaN(W) || W < 10) { W = Screen.width; H = Screen.height; }
            bool closed = false;
            void Close(bool skipAll)
            {
                if (closed) return; closed = true;
                if (skipAll && Game.State != null) { Game.State.SetFlag("tips_off"); Game.State.Save(); }
                Mark(id); layer.Clear(); showing = null; then?.Invoke();
            }

            Rect hole = Rect.zero;
            if (target != null)
            {
                var wb = target.worldBound; var tl = layer.WorldToLocal(new Vector2(wb.xMin, wb.yMin)); var br = layer.WorldToLocal(new Vector2(wb.xMax, wb.yMax));
                const float pad = 8;
                hole = Rect.MinMaxRect(Mathf.Max(4, tl.x - pad), Mathf.Max(4, tl.y - pad), Mathf.Min(W - 4, br.x + pad), Mathf.Min(H - 4, br.y + pad));
                if (hole.width < 8 || hole.height < 8) target = null;
            }
            if (target == null)
            {
                layer.Add(Shade(0, 0, W, H));
            }
            else
            {
                layer.Add(Shade(0, 0, W, hole.yMin));
                layer.Add(Shade(0, hole.yMax, W, H - hole.yMax));
                layer.Add(Shade(0, hole.yMin, hole.xMin, hole.height));
                layer.Add(Shade(hole.xMax, hole.yMin, W - hole.xMax, hole.height));
                var ring = U.Div("tip-ring").NoPick(); Place(ring, hole.x, hole.y, hole.width, hole.height); layer.Add(ring);
                ring.schedule.Execute(() => ring.ToggleInClassList("pulse")).Every(700);
                // doing the thing inside the ring counts as getting it
                if (doIt) target.RegisterCallback<PointerDownEvent>(e => { if (showing == id && !closed) Close(false); }, TrickleDown.TrickleDown);
            }

            var bub = U.Col().Cls("tip-bubble");
            bub.Add(U.T("TIP", "tip-k", Fonts.Bold).NoPick());
            bub.Add(U.T(title, "tip-title", Fonts.Semi).NoPick());
            bub.Add(U.T(Rich(text), "tip-text").NoPick());
            body?.Invoke(bub);
            var row = U.Row().Cls("tip-row");
            var skip = U.T("Skip tutorial", "tip-skip", Fonts.Medium); skip.Tap(() => Close(true)); row.Add(skip);
            row.Add(U.Spring());
            row.Add(U.Btn("Got it", () => Close(false), "pick-green small"));
            bub.Add(row);
            float bw = Mathf.Min(380, W - 32); bub.style.width = bw; bub.style.position = Position.Absolute;
            if (target == null) { bub.style.left = (W - bw) / 2; bub.style.top = H * 0.3f; }
            else
            {
                float below = H - hole.yMax, above = hole.yMin;
                bub.style.left = Mathf.Clamp(hole.center.x - bw / 2, 16, W - bw - 16);
                if (below >= 190 || below >= above) bub.style.top = Mathf.Min(hole.yMax + 12, H - 200);
                else bub.style.bottom = Mathf.Min(H - hole.yMin + 12, H - 200);
            }
            layer.Add(bub);
            Sound.Play("tick", 0.5f);
            bub.AddToClassList("in");
        }
        static VisualElement Shade(float x, float y, float w, float h)
        {
            var e = new VisualElement(); e.style.backgroundColor = Dim; Place(e, x, y, Mathf.Max(0, w), Mathf.Max(0, h));
            e.pickingMode = PickingMode.Position;   // the screen underneath can't be tapped by accident
            return e;
        }
        static void Place(VisualElement e, float x, float y, float w, float h)
        {
            e.style.position = Position.Absolute; e.style.left = x; e.style.top = y; e.style.width = w; e.style.height = h;
        }
        // *key words* in lime
        public static string Rich(string t)
        {
            var parts = (t ?? "").Split('*'); var sb = new System.Text.StringBuilder();
            for (int i = 0; i < parts.Length; i++) sb.Append(i % 2 == 1 ? $"<color={Lime}>{parts[i]}</color>" : parts[i]);
            return sb.ToString();
        }

        // ------------------------------------------------------------ the job decision, side by side
        public static void JobCard(VisualElement bub)
        {
            var row = U.Row().Cls("tip-compare");
            row.Add(Side("Quit", "#4b9cd3", new[] { "Slick gives you *$5,000* once", "No paycheck", "*3 online games a day*", "Or *1 live + 1 online* on a live day", "Faster, riskier" }));
            row.Add(Side("Keep the job", "#b8ff2e", new[] { "*$2,000* from Lou every Friday", "Days at the office", "*3 online games a week*", "*1 live game* on the weekend", "Slower, safer" }));
            bub.Add(row);
            bub.Add(U.T(Rich("Rent is due on the 1st either way. You can change your mind later: keep the job now and *quit any time* from the Spacepad."), "tip-text").NoPick());
        }
        static VisualElement Side(string head, string col, string[] lines)
        {
            var c = U.Col().Cls("tip-side"); c.style.borderTopColor = C.H(col);
            c.Add(U.T(head, "tip-side-h", Fonts.Semi).Fg(C.H(col)).NoPick());
            foreach (var l in lines) c.Add(U.T(Rich(l), "tip-side-t").NoPick());
            return c;
        }
    }
}
