using System;
using System.Collections.Generic;
using System.Linq;
using Dream.UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Dream.Game
{
    // Home (Milestone A2 / B5): after the Prologue the game lives in your Brickell studio, not the card room. The apartment
    // at night is a Cycles still (tools/blender/home.py --still, StreamingAssets/Home, loaded when the screen opens). From
    // home: the Spacepad (the planner: the week, the money, today's games; online games are played from it, live ones are a
    // drive to Biscayne), the closet (the old game's wardrobe, data.js DR_ITEMS, unlocked by milestones), and sleep.
    public partial class Flow
    {
        // hotfix (Steven 11:00): a screen that throws costs that screen, never the career. The error is logged (and kept for
        // the test menu); the player lands home, or on a plain card if home itself failed. The save is never touched.
        public static string LastError = "";
        void Safe(string where, Action build)
        {
            try { build(); }
            catch (Exception e)
            {
                LastError = $"{where}: {e.GetType().Name}: {e.Message}";
                Debug.LogError("[Dream] " + LastError + "\n" + e.StackTrace);
                if (where != "home") { Hub("That screen didn't open. Your career is saved."); return; }
                var s = Screen(Ui.Night);
                Ui.At(Ui.Title(s, "Something broke here.", 64, Ui.Ink), 0.08f, 0.55f, 0.92f, 0.66f);
                Ui.At(Ui.Label(s, "Your career is saved. " + LastError, 26, Ui.Dim), 0.08f, 0.42f, 0.92f, 0.54f);
                Ui.At(Ui.Button(s, "Open the Spacepad", Spacepad, Ui.Gold, Ui.Night, 36), 0.18f, 0.28f, 0.82f, 0.34f);
                Ui.At(Ui.Button(s, "Title", Title, Ui.Card, Ui.Ink, 30), 0.3f, 0.19f, 0.7f, 0.24f);
            }
        }

        // ------------------------------------------------------------ the closet's items (old dream/data.js, Chapter 1 subset of tests)
        class Item { public string slot, id, name, req; public Func<Save, bool> got; }
        static bool Start_(Save s) => true;
        static readonly Item[] Items =
        {
            It("outfit", "tee", "Plain black tee", "Where it all started", Start_),
            It("outfit", "office", "Clearance-rack office fit", "Where it all started", Start_),
            It("outfit", "hoodie", "The lucky hoodie", "Play your first tournament", s => s.events >= 1),
            It("outfit", "polo", "Card-room quarter-zip", "Reach Chapter 1", s => s.step != "new" && s.step != "prologue" && s.step != "choice"),
            It("outfit", "cardigan", "Study-group cardigan", "Play 3 sessions", s => s.events >= 3),
            It("outfit", "jersey", "Lucky number 7 jersey", "Make 100 decisions", s => s.decisions >= 100),
            It("outfit", "bomber", "Black bomber jacket", "Win any tournament", s => s.titles >= 1),
            It("outfit", "vest", "Argyle sweater vest", "Peak bankroll of $10,000", s => s.peak >= 10000),
            It("outfit", "blazer", "Circuit blazer", "Reach the Circuit", s => s.step == "ch2" || s.step == "done" || s.step == "end"),
            It("outfit", "leather", "Worn leather jacket", "Peak bankroll of $50,000", s => s.peak >= 50000),
            It("outfit", "suit", "Tailored suit", "Win a gold bracelet", s => false),
            It("outfit", "tux", "Main Event tuxedo", "Make the Main Event final table", s => false),
            It("eyes", "none", "No eyewear", "", Start_),
            It("eyes", "readers", "Blue-light readers", "Where it all started", Start_),
            It("eyes", "round", "Wire round frames", "Make 50 decisions", s => s.decisions >= 50),
            It("eyes", "thick", "Thick black frames", "Play 2 sessions", s => s.events >= 2),
            It("eyes", "sport", "Wraparound sport shades", "Peak bankroll of $25,000", s => s.peak >= 25000),
            It("eyes", "shades", "Slick's 8-bit shades", "Win any title", s => s.titles >= 1),
            It("eyes", "aviator", "Gold aviators", "Win a Circuit ring", s => false),
            It("head", "none", "Nothing", "", Start_),
            It("head", "cap", "Backwards ballcap", "Play 5 sessions", s => s.events >= 5),
            It("head", "snap", "Flat-brim snapback", "Reach Chapter 1", s => s.step != "new" && s.step != "prologue" && s.step != "choice"),
            It("head", "bucket", "Cream bucket hat", "Play 10 sessions", s => s.events >= 10),
            It("head", "phones", "Noise-cancelling cans", "Make 100 decisions", s => s.decisions >= 100),
            It("head", "cowboy", "Texas hat", "Win a Circuit ring", s => false),
            It("head", "fedora", "High-roller fedora", "Peak bankroll of $500,000", s => s.peak >= 500000),
            It("chain", "none", "No chain", "", Start_),
            It("chain", "silver", "Thin silver chain", "Peak bankroll of $10,000", s => s.peak >= 10000),
            It("chain", "cuban", "Gold Cuban link", "Peak bankroll of $100,000", s => s.peak >= 100000),
            It("chain", "layered", "Layered chains", "Win a gold bracelet", s => false),
            It("ride", "bus", "Monthly bus pass", "Where it all started", Start_),
            It("ride", "hatch", "'09 hatchback", "Peak bankroll of $5,000", s => s.peak >= 5000),
            It("ride", "coupe", "Used sports coupe", "Peak bankroll of $50,000", s => s.peak >= 50000),
            It("ride", "sedan", "German super-sedan", "Peak bankroll of $250,000", s => s.peak >= 250000),
            It("home", "studio", "Brickell studio", "Where it all started", Start_),
            It("home", "apt", "Brickell one-bedroom", "Peak bankroll of $25,000", s => s.peak >= 25000),
            It("home", "condo", "Bayfront high-rise condo", "Peak bankroll of $150,000", s => s.peak >= 150000),
        };
        static Item It(string slot, string id, string name, string req, Func<Save, bool> got) => new Item { slot = slot, id = id, name = name, req = req, got = got };
        static readonly (string slot, string label)[] Slots = { ("outfit", "Outfit"), ("eyes", "Glasses"), ("head", "Hat"), ("chain", "Chain"), ("ride", "Ride"), ("home", "Home") };
        static readonly Dictionary<string, string> Defaults = new Dictionary<string, string> { ["outfit"] = "tee", ["eyes"] = "none", ["head"] = "none", ["chain"] = "none", ["ride"] = "bus", ["home"] = "studio" };

        string Worn(string slot)
        {
            var v = S.eq.FirstOrDefault(e => e.StartsWith(slot + ":"));
            return v != null ? v.Substring(slot.Length + 1) : Defaults[slot];
        }
        Item WornItem(string slot) { var id = Worn(slot); return Items.First(i => i.slot == slot && (i.id == id || i.id == Defaults[slot])); }
        void Wear(Item it) { S.eq.RemoveAll(e => e.StartsWith(it.slot + ":")); S.eq.Add(it.slot + ":" + it.id); S.Write(); }

        // ------------------------------------------------------------ the apartment still, loaded once per orientation
        static readonly Dictionary<string, Texture2D> stills = new Dictionary<string, Texture2D>();
        void Backdrop(RectTransform s, string name)
        {
            bool wide = s.rect.width > s.rect.height;
            string file = $"{name}_{(wide ? "landscape" : "portrait")}.jpg";
            var img = new GameObject("Still", typeof(RectTransform)).AddComponent<RawImage>(); img.transform.SetParent(s, false);
            img.raycastTarget = false; img.color = new Color(1, 1, 1, 0);
            var fit = img.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            void Show(Texture2D t) { if (img == null) return; img.texture = t; fit.aspectRatio = (float)t.width / t.height; img.color = Color.white; }
            if (stills.TryGetValue(file, out var have) && have != null) { Show(have); return; }   // (a texture can be gone after the editor leaves play mode)
            var sa = Application.streamingAssetsPath; var url = (sa.Contains("://") ? sa : "file://" + sa) + "/Home/" + file;
            var req = UnityWebRequestTexture.GetTexture(url);
            req.SendWebRequest().completed += _ =>
            {
                if (req.result == UnityWebRequest.Result.Success) { var t = DownloadHandlerTexture.GetContent(req); stills[file] = t; Show(t); }
                else Debug.LogWarning("[Dream] home still: " + req.error);
                req.Dispose();
            };
        }
        static void Shade(RectTransform s, float y0, float y1, float a) => Ui.At(Ui.Panel(s, "Shade", new Color(0.02f, 0.02f, 0.04f, a)), 0, y0, 1, y1);

        // ------------------------------------------------------------ home (the hub)
        public void Hub(string note = null) => Safe("home", () => HubBody(note));
        void HubBody(string note)
        {
            var s = Screen(Ui.Night); Canvas.ForceUpdateCanvases();
            bool wide = s.rect.width > s.rect.height;
            Backdrop(s, "home");
            Shade(s, wide ? 0.72f : 0.8f, 1, 0.72f); Shade(s, 0, wide ? 0.24f : 0.27f, 0.72f);
            Ui.At(Ui.Label(s, "HOME  ·  " + DateStr(S.day).ToUpperInvariant(), 26, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold), 0.06f, 0.94f, 0.94f, 0.975f);
            Ui.At(Ui.Num(s, Ui.Money(S.br), 96, Ui.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), 0.06f, wide ? 0.82f : 0.87f, 0.94f, 0.94f);
            Ui.At(Ui.Label(s, "BANKROLL  ·  " + (string.IsNullOrEmpty(S.name) ? "" : S.name.ToUpperInvariant() + "  ·  ") + Rep.ToUpperInvariant(), 22, Ui.Dim, TextAnchor.MiddleLeft, FontStyle.Bold), 0.06f, wide ? 0.78f : 0.845f, 0.94f, wide ? 0.82f : 0.87f);
            if (note != null) Ui.At(Ui.Label(s, note, 28, Ui.Aqua, TextAnchor.MiddleLeft), 0.06f, wide ? 0.72f : 0.8f, 0.94f, wide ? 0.78f : 0.845f);
            var today = Today().ToList();
            bool stuck = today.All(e => CantPlay(e) != null);
            float y0 = wide ? 0.13f : 0.15f, y1 = wide ? 0.215f : 0.235f;
            Ui.At(Ui.Button(s, "Open the Spacepad", Spacepad, Ui.Gold, Ui.Night, 40), 0.06f, y0, wide ? 0.4f : 0.94f, y1);
            if (wide)
            {
                Ui.At(Ui.Button(s, "Closet", Closet, Ui.Card, Ui.Ink, 34), 0.42f, y0, 0.66f, y1);
                Ui.At(Ui.Button(s, stuck ? (S.job == "nights" ? "Sleep until you can play" : "Sleep") : "Call it a night", Sleep, Ui.Aqua, Ui.Night, 34), 0.68f, y0, 0.94f, y1);
            }
            else
            {
                Ui.At(Ui.Button(s, "Closet", Closet, Ui.Card, Ui.Ink, 34), 0.06f, 0.075f, 0.48f, 0.135f);
                Ui.At(Ui.Button(s, stuck ? "Sleep" : "Call it a night", Sleep, Ui.Aqua, Ui.Night, 34), 0.52f, 0.075f, 0.94f, 0.135f);
            }
            if (S.step == "ch1" && S.br >= Goal && S.day < ClassicDay - 1 && !S.classicDone)   // B4: the stake is made; the Classic can come now
                Ui.At(Ui.Button(s, "Skip to the Biscayne Classic · Sat, Feb 14", SkipToClassic, new Color(0.95f, 0.76f, 0.35f, 0.25f), Ui.Gold, 28), 0.06f, wide ? 0.235f : 0.245f, 0.94f, wide ? 0.295f : 0.29f);
            float yb = wide ? 0.035f : 0.02f;
            Ui.At(Ui.Button(s, "Replay the reel", () => Films.Play(new[] { "reel" }, () => Hub()), new Color(1, 1, 1, 0.08f), Ui.Dim, 24), 0.06f, yb, 0.46f, yb + 0.045f);
            Ui.At(Ui.Button(s, "Title", Title, new Color(1, 1, 1, 0.08f), Ui.Dim, 24), 0.54f, yb, 0.94f, yb + 0.045f);
        }

        // the test command "home <hub|spacepad|closet|drive>": a Chapter 1 save mid-month if none is loaded
        public void TestHome(string screen)
        {
            var w = screen.Split(' '); screen = w[0];
            if (S == null || S.step != "ch1" || w.Length > 1) { S = new Save { step = "ch1", job = "nights", day = 20, br = 3190, peak = 3400, events = 6, decisions = 140, name = "Rookie" }; }
            if (w.Length > 1) { S.br = S.peak = double.Parse(w[1], System.Globalization.CultureInfo.InvariantCulture); S.Write(); }
            if (screen == "spacepad") Spacepad(); else if (screen == "closet") Closet(); else if (screen == "drive") Drive("nightly"); else Hub("Payday +$1,250.  Rent and living −$600.");
        }

        // B4, the goal fast-forward: the calendar jumps to the night before the Classic and sleeps into it. Every Friday on
        // the way pays and charges as usual; the weekly beats you passed are skipped; the night before still plays.
        void SkipToClassic()
        {
            double before = S.br; int from = S.day;
            for (int d = S.day + 1; d < ClassicDay; d++)
                if (Dow(d) == 5) { if (S.job == "nights") S.br += Salary; S.br = Math.Max(0, S.br - Bills); }
            foreach (var v in new[] { "$wk2", "$wk3", "$wk3d", "$wk4a" })
            {
                if (S.story.Any(x => x == v + "=true")) continue;
                S.story.RemoveAll(x => x.StartsWith(v + "=")); S.story.Add(v + "=true");
            }
            S.day = ClassicDay - 1; S.Write();
            Debug.Log($"[Dream] skip to the Classic: day {from} -> {S.day}, bankroll {before:0} -> {S.br:0}");
            Sleep();
        }

        // ------------------------------------------------------------ the Spacepad: the planner
        void Spacepad() => Safe("the Spacepad", SpacepadBody);
        void SpacepadBody()
        {
            var s = Screen(Ui.Night); Canvas.ForceUpdateCanvases();
            bool wide = s.rect.width > s.rect.height;
            Backdrop(s, "home"); Shade(s, 0, 1, 0.55f);
            var pad = Ui.At(Ui.Tile(s, "Spacepad", new Color(0.06f, 0.065f, 0.08f, 0.97f), 40), wide ? 0.12f : 0.03f, 0.03f, wide ? 0.88f : 0.97f, 0.97f);
            var p = Ui.At(Ui.Box(pad, "In"), 0.04f, 0.02f, 0.96f, 0.98f);
            Ui.At(Ui.Label(p, "SPACEPAD", 24, Ui.Aqua, TextAnchor.MiddleLeft, FontStyle.Bold), 0, 0.955f, 0.5f, 0.99f);
            Ui.At(Ui.Label(p, DateStr(S.day), 24, Ui.Ink, TextAnchor.MiddleRight), 0.4f, 0.955f, 1, 0.99f);
            Ui.At(Ui.Num(p, Ui.Money(S.br), 64, Ui.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), 0, 0.885f, 0.6f, 0.955f);
            Ui.At(Ui.Label(p, Countdown(), 24, Ui.Gold, TextAnchor.MiddleRight, FontStyle.Bold), 0.5f, 0.885f, 1, 0.955f);
            var bar = Ui.At(Ui.Tile(p, "Stake", Ui.Card, 10), 0, 0.865f, 1, 0.878f);
            var fill = Ui.Tile(bar, "Fill", Ui.Gold, 10); fill.anchorMax = new Vector2(Mathf.Clamp01(GoalFill), 1);
            int toFri = (5 - Dow(S.day) + 7) % 7;
            string money = (toFri == 0 ? "Friday: " : toFri == 1 ? "Tomorrow, Friday: " : $"Friday, in {toFri} days: ") + $"rent and living −{Ui.Money(Bills)}" + (S.job == "nights" ? $", pay +{Ui.Money(Salary)}" : "")
                + (S.debt > 0 ? $"  ·  owed to Slick {Ui.Money(S.debt)}" : "");
            Ui.At(Ui.Label(p, GoalLine + "  ·  " + money, 21, Ui.Dim, TextAnchor.MiddleLeft), 0, 0.825f, 1, 0.86f);
            // the week: Monday to Sunday, today lit, what each day allows
            int monday = S.day - (Dow(S.day) + 6) % 7;
            for (int i = 0; i < 7; i++)
            {
                int d = monday + i; bool now = d == S.day, past = d < S.day;
                var cell = Ui.At(Ui.Tile(p, "D" + i, now ? Ui.Gold : new Color(1, 1, 1, past ? 0.03f : 0.07f), 14), i / 7f + 0.004f, 0.715f, (i + 1) / 7f - 0.004f, 0.815f);
                var fg = now ? Ui.Night : past ? Ui.Dim : Ui.Ink;
                Ui.At(Ui.Label(cell, Date(d).ToString("ddd", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant(), 18, fg, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 0.68f, 1, 0.95f);
                Ui.At(Ui.Num(cell, Date(d).Day.ToString(), 30, fg, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 0.33f, 1, 0.7f);
                string tag = DayTag(d); bool big = tag == "CLASSIC" || tag == "MAIN" || tag == "RING";
                Ui.At(Ui.Label(cell, tag, 14, now ? Ui.Night : big ? Ui.Gold : tag == "RENT" ? Ui.Neon : Ui.Aqua, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 0.05f, 1, 0.33f);
            }
            // today's games
            Ui.At(Ui.Label(p, "TONIGHT", 22, Ui.Dim, TextAnchor.MiddleLeft, FontStyle.Bold), 0, 0.665f, 1, 0.7f);
            float y = 0.66f; float h = wide ? 0.16f : 0.12f;
            foreach (var ev in Today())
            {
                string why = CantPlay(ev); bool afford = (ev.free || S.ticket == ev.id || S.br >= ev.buyin) && why == null;
                var row = Ui.At(Ui.Tile(p, ev.id, Ui.Card, 26), 0, y - h, 1, y - 0.01f);
                string where = ev.online ? "ONLINE · CF TILT · FROM THE SPACEPAD" : ev.id.StartsWith("classic") ? "BISCAYNE · THE CLASSIC · A DRIVE" : ev.venue == "casino" ? "SILVER STRIKE · THE CIRCUIT · A DRIVE" : "BISCAYNE · LIVE · A DRIVE";
                Ui.At(Ui.Label(row, where, 17, ev.online ? Ui.Aqua : Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold), 0.04f, 0.7f, 0.75f, 0.94f);
                Ui.At(Ui.Label(row, ev.name, 32, afford ? Ui.Ink : Ui.Dim, TextAnchor.MiddleLeft, FontStyle.Bold), 0.04f, 0.38f, 0.72f, 0.72f);
                bool red = why != null || (!ev.free && S.br < ev.buyin);
                Ui.At(Ui.Label(row, why ?? (!ev.free && S.br < ev.buyin ? "Not in your roll" : ev.blurb), 20, red ? Ui.Neon : Ui.Dim, TextAnchor.UpperLeft), 0.04f, 0.04f, 0.72f, 0.38f);
                string price = ev.free ? (ev.id == "classic2" ? "Chips bagged" : "Free") : S.ticket == ev.id ? "Your seat" : ev.kind == "cash" ? Ui.Money(ev.buyin) + " in" : Ui.Money(ev.buyin);
                Ui.At(Ui.Num(row, price, 32, Ui.Gold, TextAnchor.MiddleRight, FontStyle.Bold), 0.68f, 0.48f, 0.96f, 0.86f);
                Ui.At(Ui.Label(row, ev.kind == "sat" ? $"{ev.field:N0} players · {ev.seats} return" : ev.kind == "cash" ? "cash game · 9-handed" : $"{ev.field:N0} players", 19, Ui.Dim, TextAnchor.MiddleRight), 0.6f, 0.12f, 0.96f, 0.45f);
                var b = row.gameObject.AddComponent<Button>(); var id = ev.id; b.interactable = afford;
                b.onClick.AddListener(() => { if (Lib.Event(id).online) Enter(id); else Drive(id); });
                y -= h + 0.005f;
            }
            Ui.At(Ui.Button(p, "Back home", () => Hub(), new Color(1, 1, 1, 0.08f), Ui.Ink, 30), 0.2f, 0.0f, 0.8f, 0.055f);
        }

        // ------------------------------------------------------------ a live game: the drive over the causeway (a card for now)
        void Drive(string id)
        {
            var s = Screen(Color.black);
            var ride = WornItem("ride");
            bool strike = Lib.Event(id).venue == "casino";   // Chapter 2: north to the Circuit
            string how = strike ? (ride.id == "bus" ? "Two buses and a casino shuttle. The Circuit doesn't care how you got here." : $"The {ride.name.ToLowerInvariant()}, forty minutes north. The sign is visible from the highway.") :
                         ride.id == "bus" ? "The 7 bus over the causeway. The bay is black on both sides." :
                         ride.id == "hatch" ? "The '09 hatchback coughs once, then takes the causeway like it means it." :
                         $"The {ride.name.ToLowerInvariant()} eats the causeway in four minutes.";
            Ui.At(Ui.Label(s, strike ? "NORTH ON I-95" : "OFF THE CAUSEWAY", 26, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 0.06f, 0.6f, 0.94f, 0.64f);
            Ui.At(Ui.Title(s, strike ? "Silver Strike Casino" : "Biscayne Card Room", 80, Ui.Ink), 0.06f, 0.5f, 0.94f, 0.6f);
            Ui.At(Ui.Label(s, how, 32, Ui.Dim), 0.1f, 0.4f, 0.9f, 0.49f);
            bool gone = false; void Go() { if (gone) return; gone = true; Enter(id); }
            var b = s.gameObject.AddComponent<Button>(); b.onClick.AddListener(Go);
            Ui.At(Ui.Label(s, "tap to go in", 24, new Color(1, 1, 1, 0.4f)), 0, 0.06f, 1, 0.09f);
            Dream.UI.Motion.On(s).Go(Wait()); System.Collections.IEnumerator Wait() { yield return new WaitForSeconds(3.2f); Go(); }
        }

        // ------------------------------------------------------------ the closet
        string closetSlot = "outfit";
        void Closet() => Safe("the closet", ClosetBody);
        void ClosetBody()
        {
            var s = Screen(Ui.Night); Canvas.ForceUpdateCanvases();
            bool wide = s.rect.width > s.rect.height;
            Ui.At(Ui.Label(s, "CLOSET", 26, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold), 0.06f, 0.94f, 0.5f, 0.975f);
            Ui.At(Ui.Label(s, string.Join("  ·  ", new[] { "outfit", "eyes", "head", "chain" }.Select(WornItem).Where(i => i.id != "none").Select(i => i.name)), 22, Ui.Dim, TextAnchor.MiddleLeft), 0.06f, 0.9f, 0.94f, 0.94f);
            for (int i = 0; i < Slots.Length; i++)
            {
                var (slot, label) = Slots[i]; bool on = slot == closetSlot;
                var tab = Ui.Button(s, label, () => { closetSlot = slot; Closet(); }, on ? Ui.Gold : Ui.Card, on ? Ui.Night : Ui.Ink, 26);
                Ui.At(tab, 0.06f + i * 0.88f / Slots.Length + 0.004f, 0.84f, 0.06f + (i + 1) * 0.88f / Slots.Length - 0.004f, 0.885f);
            }
            var list = Items.Where(it => it.slot == closetSlot).ToList();
            int cols = wide ? 2 : 1, perCol = Mathf.CeilToInt(list.Count / (float)cols);
            float top = 0.83f, rowH = Mathf.Min(wide ? 0.12f : 0.065f, 0.72f / perCol);
            for (int i = 0; i < list.Count; i++)
            {
                var it = list[i]; bool got = it.got(S), wearing = Worn(it.slot) == it.id;
                int c = i / perCol, r = i % perCol; float x0 = 0.06f + c * 0.45f, x1 = x0 + (cols == 1 ? 0.88f : 0.43f);
                var row = Ui.At(Ui.Tile(s, it.id, wearing ? new Color(0.95f, 0.76f, 0.35f, 0.18f) : Ui.Card, 20), x0, top - (r + 1) * rowH, x1, top - r * rowH - 0.006f);
                Ui.At(Ui.Label(row, it.name, 28, got ? Ui.Ink : new Color(1, 1, 1, 0.3f), TextAnchor.MiddleLeft, FontStyle.Bold), 0.05f, 0.45f, 0.75f, 0.95f);
                Ui.At(Ui.Label(row, got ? (wearing ? (it.slot == "home" ? "You live here" : it.slot == "ride" ? "Your ride" : "Wearing") : "Tap to wear") : "Locked · " + it.req, 20, wearing ? Ui.Gold : got ? Ui.Dim : new Color(1, 1, 1, 0.3f), TextAnchor.MiddleLeft), 0.05f, 0.05f, 0.95f, 0.45f);
                if (got && !wearing) { var b = row.gameObject.AddComponent<Button>(); b.onClick.AddListener(() => { Wear(it); Closet(); }); }
            }
            Ui.At(Ui.Button(s, "Back home", () => Hub(), Ui.Aqua, Ui.Night, 32), 0.25f, 0.025f, 0.75f, 0.08f);
        }
    }
}
