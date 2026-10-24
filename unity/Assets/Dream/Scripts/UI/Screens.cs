// Which screen to draw, plus the title, the character creator and story scenes.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using CFDream.Character;

namespace CFDream.UI
{
    public static class Screens
    {
        public static Color Backdrop = C.Ink;

        public static VisualElement Build()
        {
            Backdrop = C.Ink;
            switch (Game.View)
            {
                case View.Create: return Create();
                case View.Hub: return HubView.Build();
                case View.Profile: Game.Tab = "career"; Game.View = View.Hub; return HubView.Build();
                case View.Scene: return SceneView();
                case View.Run: return RunView.Build();
                case View.Results: return ResultsView.Build();
                case View.Transit: return TransitView.Build();
                default: return Title();
            }
        }

        public static VisualElement Page(string cls = null) => U.Div("screen").Cls(cls);
        // in landscape, screens that read like a page stay a centred phone-width column
        public static VisualElement Narrow(string cls = null) => Page(cls).Cls("narrow");
        public static ScrollView Scroll()
        {
            var sv = new ScrollView(ScrollViewMode.Vertical) { name = "main" };
            sv.AddToClassList("main-scroll");
            sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            sv.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            return sv;
        }

        public static VisualElement ErrorView(Exception e)
        {
            var s = Narrow("pad");
            s.Add(U.Kicker("Something broke"));
            s.Add(U.H2("The Dream hit an error"));
            s.Add(U.P(U.Esc(e.GetBaseException().Message)).Mt(8));
            s.Add(U.Hint("The full error is in the Unity Console. Send it to Claude.").Mt(6));
            s.Add(U.Btn("Back to the hub", () => { try { Game.Hub(); } catch { } }, "ghost").Mt(16));
            s.Add(U.Btn("Erase save and restart", () => DreamApp.Confirm("Erase your career?", "This deletes your save on this device. It can't be undone.", "Erase", () => { State.Erase(); Game.State = new State(); Game.View = View.Title; Game.Render(); }), "ghost small"));
            return s;
        }

        // ------------------------------------------------------------ title
        static VisualElement Title()
        {
            var s = Narrow("title-screen");
            var glow = new VisualElement().Cls("title-glow").NoPick();
            glow.generateVisualContent += mgc =>
            {
                var r = glow.contentRect; var p = mgc.painter2D;
                for (int i = 0; i < 36; i++)
                {
                    float t = i / 35f; p.fillColor = C.A(C.Mix(C.Felt, C.Gold3, 0.2f), 0.02f * (1 - t) + 0.004f);
                    p.BeginPath(); p.Arc(new Vector2(r.center.x, r.height * 0.42f), r.width * (0.2f + 0.5f * t), Angle.Degrees(0), Angle.Degrees(360)); p.Fill();
                }
            };
            s.Add(glow);
            var fan = U.Row().Cls("title-fan");
            var a = CardView.Face(51, 74); a.style.rotate = new Rotate(Angle.Degrees(-9)); a.style.marginRight = -14;
            var k = CardView.Face(46, 74); k.style.rotate = new Rotate(Angle.Degrees(7)); k.style.marginTop = 6;
            fan.Add(a); fan.Add(k);
            s.Add(U.Spring());
            s.Add(fan);
            s.Add(U.Kicker("Confirmed Fire").Cls("center").Mt(26));
            s.Add(U.T("The Dream", "title-h", Fonts.Disp));
            s.Add(U.P("From a cubicle on Floor 34 to the final table of the Main Event. Every decision graded by a solver.").Cls("center title-sub"));
            s.Add(U.Spring());
            var st = Game.State;
            if (st != null && st.made)
            {
                s.Add(U.Btn("Continue", () => Game.Hub(), "gold wide", true, $"{st.name} · {Career.Rep(st)} · {U.Money(st.br)}"));
                s.Add(U.Btn("New career", () => DreamApp.Confirm("Start over?", $"This erases {st.name}'s career on this device.", "Start over", () => { State.Erase(); Game.State = new State(); Game.NewCareer(); }), "ghost wide"));
            }
            else s.Add(U.Btn("Start your career", Game.NewCareer, "gold wide"));
            s.Add(U.Hint("Version " + Application.version).Cls("center").Mt(14));
            return s;
        }

        // ------------------------------------------------------------ the character creator
        static List<object> Opt(string key) => DB.List(key) ?? new List<object>();

        public static string CreateCat = "hair", HairMode = "style";
        static readonly (string id, string label, string icon)[] CreateCats = { ("skin", "Skin", "skin"), ("face", "Face", "face"), ("hair", "Hair", "hair"), ("eyes", "Eyes", "eye"), ("beard", "Beard", "beard"), ("sculpt", "Sculpt", "sculpt") };

        static VisualElement Create()
        {
            var d = Game.Draft; bool re = Game.Restyle; var st = Game.State;
            var eq = re ? st.eq : new State().eq;
            var fig = re ? new Fig { look = d, eq = st.eq, dye = st.dye, fin = st.fin } : Fig.Of(d, eq);
            bool land = DreamApp.Landscape;
            var s = Page("create");
            void Changed() => Game.Render();

            // your name, and a dice for a random start
            var head = U.Row().Cls("create-head");
            var nameCol = U.Col().Grow();
            nameCol.Add(U.T(re ? "Restyle" : "Create your player", "create-k", Fonts.Semi));
            var nr = U.Row().Cls("name-row");
            var tf = new TextField { value = Game.DraftName, maxLength = 18 }; tf.AddToClassList("name-field"); tf.AddToClassList("name-big"); Fonts.Use(tf, Fonts.Disp);
            tf.RegisterValueChangedCallback(e => Game.DraftName = e.newValue);
            tf.style.flexGrow = 1;
            nr.Add(tf); nr.Add(new Icon("pencil", 18, C.Mute));
            nameCol.Add(nr);
            head.Add(nameCol);
            if (!re) head.Add(Bits.IconBtn("dice", () => { Randomize(d); Changed(); }, "Randomize your look"));

            // the stage: close on the face while you work on the head
            Func<Character.CharacterSpec> spec = () => Character.CharacterSpec.From(d, eq, re ? st.dye : null, re ? st.fin : null, re ? st : null, Game.DraftName);
            var stage = StageView.Make(fig, spec, land ? -1 : 270, CreateCat == "skin" ? "bust" : "face", out var refresh);

            // what you're changing
            var rail = U.Row().Cls("cat-rail");
            foreach (var (id, label, icon) in CreateCats)
            {
                bool on = id == CreateCat;
                var b = U.Col().Cls("cat-b", on ? "on" : "");
                var ci = U.Div("cat-i").NoPick(); ci.Add(new Icon(icon, 22, on ? C.Ink : C.Mute)); b.Add(ci);
                b.Add(U.T(label, "cat-t", on ? Fonts.Semi : Fonts.Medium).NoPick());
                string k = id; b.Tap(() => { CreateCat = k; Changed(); });
                rail.Add(b);
            }

            // the sheet: one category at a time, as pictures where it helps
            var sheet = U.Col().Cls("sheet");
            var sv = Scroll(); var body = U.Div("sheet-body");
            switch (CreateCat)
            {
                case "skin":
                    body.Add(SheetLabel("Skin tone")); body.Add(Swatches(Opt("DR_SKIN"), d.skin, i => { d.skin = i; Changed(); }));
                    body.Add(SheetLabel("Age")); body.Add(Bits.Segmented(Options("DR_AGES"), d.age, v => { d.age = v; Changed(); }));
                    body.Add(SheetLabel("Details")); var fr = U.Row().Cls("wrap"); fr.Add(Toggle("Freckles", d.freck, () => { d.freck = !d.freck; Changed(); })); body.Add(fr);
                    break;
                case "face":
                    body.Add(SheetLabel("Face shape")); body.Add(LookTiles(d, eq, "DR_FACES", d.face, (l, v) => l.face = v, v => { d.face = v; Changed(); }));
                    body.Add(SheetLabel("Brows")); body.Add(LookTiles(d, eq, "DR_BROWS", d.brows, (l, v) => l.brows = v, v => { d.brows = v; Changed(); }));
                    break;
                case "eyes":
                    body.Add(SheetLabel("Eye colour")); body.Add(Swatches(Opt("DR_EYEC"), d.eyeC, i => { d.eyeC = i; Changed(); }));
                    body.Add(SheetLabel("Shape")); body.Add(Sliders(d, "Eyes", refresh));
                    break;
                case "beard":
                    body.Add(SheetLabel("Facial hair")); body.Add(LookTiles(d, eq, "DR_FACIAL", d.facial, (l, v) => l.facial = v, v => { d.facial = v; Changed(); }));
                    break;
                case "sculpt":
                    {
                        var hd = U.Row().Mt(2); hd.Add(U.Hint("Drag to shape the face. Small moves go a long way.").Grow());
                        hd.Add(U.Btn("Reset", () => { d.sc.Clear(); Changed(); }, "small"));
                        hd.Add(U.Btn("Random", () => { var r = new System.Random(); foreach (var g in Opt("DR_SCULPT")) foreach (var kv in Pairs(g)) d.sc[kv] = Math.Round(r.NextDouble() * 1.4 - 0.7, 2); Changed(); }, "small"));
                        body.Add(hd);
                        foreach (var g in Opt("DR_SCULPT")) if (g is List<object> gl && gl.Count >= 2 && gl[0] as string != "Eyes") { body.Add(SheetLabel(gl[0] as string)); body.Add(Sliders(d, gl[0] as string, refresh)); }
                        break;
                    }
                default:
                    body.Add(Bits.Segmented(new[] { ("style", "Style"), ("colour", "Color") }, HairMode, v => { HairMode = v; Changed(); }));
                    if (HairMode == "colour") body.Add(Swatches(Opt("DR_HAIRC"), d.hairC, i => { d.hairC = i; Changed(); }).Mt(14));
                    else body.Add(LookTiles(d, eq, "DR_HAIR", d.hair, (l, v) => l.hair = v, v => { d.hair = v; Changed(); }).Mt(14));
                    break;
            }
            body.Add(U.Space(12));
            sv.Add(body); sheet.Add(sv);
            var foot = U.Row().Cls("sheet-foot");
            if (re) { foot.Add(U.Btn("Cancel", Game.CancelRestyle, "grow")); foot.Add(U.Btn("Save look", () => Game.SaveRestyle(Game.DraftName), "gold grow")); }
            else foot.Add(U.Btn("Start the story", () => Game.Begin(Game.DraftName), "gold grow"));
            sheet.Add(foot);

            if (land)
            {
                s.AddToClassList("two-pane");
                var left = U.Col().Cls("pane-left"); left.Add(head); left.Add(stage); s.Add(left);
                var right = U.Col().Cls("grow"); right.Add(rail); right.Add(sheet); s.Add(right);
            }
            else { s.Add(head); s.Add(stage); s.Add(rail); s.Add(sheet); }
            return s;
        }

        static Label SheetLabel(string t) => U.T(t, "sheet-label", Fonts.Semi);
        static List<(string, string)> Options(string key)
        {
            var l = new List<(string, string)>();
            foreach (var o in Opt(key)) if (o is List<object> kv && kv.Count >= 2) l.Add((kv[0] as string, kv[1] as string));
            return l;
        }
        // one tile per option, each showing your face with that option
        static VisualElement LookTiles(Look d, Dictionary<string, string> eq, string key, string sel, Action<Look, string> apply, Action<string> pick)
        {
            string outfit = "tee";
            if (eq != null && eq.TryGetValue("outfit", out var worn) && !string.IsNullOrEmpty(worn) && worn != "none") outfit = worn;
            var wear = new Dictionary<string, string> { { "outfit", outfit } };
            var tiles = new List<VisualElement>();
            foreach (var (id, label) in Options(key))
            {
                var l = d.Clone(); apply(l, id);
                var pic = new Portrait(Fig.Of(l, wear), 66, false);
                string v = id; tiles.Add(Bits.Tile(pic, label, id == sel, () => pick(v)));
            }
            return Bits.Tiles(tiles);
        }
        // sculpt sliders for one group; they reshape the face live without rebuilding the screen
        static VisualElement Sliders(Look d, string group, Action refresh)
        {
            var col = U.Col().Cls("sliders");
            foreach (var g in Opt("DR_SCULPT"))
            {
                if (!(g is List<object> gl) || gl.Count < 2 || gl[0] as string != group) continue;
                foreach (var item in gl[1] as List<object> ?? new List<object>())
                {
                    if (!(item is List<object> kv) || kv.Count < 2) continue;
                    string key = kv[0] as string, label = kv[1] as string;
                    var row = U.Row().Cls("slider-row");
                    row.Add(U.T(label, "slider-l", Fonts.Medium));
                    var sl = new Slider(-1, 1) { value = (float)(d.sc.TryGetValue(key, out var v) ? v : 0) }; sl.AddToClassList("sculpt-slider"); sl.style.flexGrow = 1;
                    sl.RegisterValueChangedCallback(e => { d.sc[key] = Math.Round(e.newValue, 2); refresh?.Invoke(); });
                    row.Add(sl); col.Add(row);
                }
            }
            return col;
        }
        static IEnumerable<string> Pairs(object g)
        {
            var gl = g as List<object>; if (gl == null || gl.Count < 2) yield break;
            foreach (var item in gl[1] as List<object> ?? new List<object>()) if (item is List<object> kv && kv.Count > 0) yield return kv[0] as string;
        }
        static void Randomize(Look d)
        {
            var r = new System.Random();
            string Pick(string key) { var l = Opt(key); return l.Count == 0 ? null : (l[r.Next(l.Count)] as List<object>)?[0] as string; }
            d.skin = r.Next(Opt("DR_SKIN").Count); d.hairC = r.Next(Math.Min(9, Opt("DR_HAIRC").Count)); d.eyeC = r.Next(Opt("DR_EYEC").Count);
            d.hair = Pick("DR_HAIR") ?? d.hair; d.facial = Pick("DR_FACIAL") ?? d.facial; d.brows = Pick("DR_BROWS") ?? d.brows; d.face = Pick("DR_FACES") ?? d.face;
            d.freck = r.NextDouble() < 0.2;
        }
        static VisualElement Swatches(List<object> colors, int sel, Action<int> pick)
        {
            var row = U.Row().Cls("wrap swatches");
            for (int i = 0; i < colors.Count; i++)
            {
                int k = i; var sw = U.Div("swatch", "big").Bg(C.H(colors[i] as string));
                if (i == sel) { sw.AddToClassList("on"); sw.Add(new Icon("check", 16, C.H(colors[i] as string).grayscale > 0.6f ? C.Ink : C.Cream, 2.6f)); }
                sw.Tap(() => pick(k)); row.Add(sw);
            }
            return row;
        }
        public static VisualElement Toggle(string label, bool on, Action a)
        {
            var b = U.Div("opt").Cls(on ? "on" : "");
            b.Add(U.T(label, "opt-t", Fonts.Medium).NoPick());
            return b.Tap(a);
        }

        // ------------------------------------------------------------ story scenes
        static readonly Dictionary<string, string> PlaceTint = new Dictionary<string, string>
        {
            { "office", "#5d7896" }, { "diner", "#c2413a" }, { "cardroom", "#2f8a5e" }, { "casino", "#c79a3c" }, { "vegas", "#c2479a" },
            { "tv", "#3d7fd1" }, { "titan", "#8c7a4a" }, { "main", "#d9b36c" }, { "hall", "#7a5ac8" }, { "home", "#6b4f8a" },
        };
        public static Color Tint(string place) => C.H(PlaceTint.TryGetValue(place ?? "", out var h) ? h : "#d9b36c");

        public static (string name, string role, Color c, Fig fig) Speaker(string sp, string who)
        {
            var s = Game.State;
            if (sp == "you") return (s.name, Career.Rep(s), C.Cream, Fig.Of(s));
            if (sp != null && sp.StartsWith("L:"))
            {
                var id = sp.Substring(2); var lg = DB.Legend(id);
                return (who ?? J.S(lg, "name", id), J.S(lg, "nick", "Legend"), C.Gold2, Fig.For(id));
            }
            var p = DB.Person(sp);
            if (p != null) return (who ?? J.S(p, "name", sp), who != null ? "" : J.S(p, "role", ""), C.H(J.S(p, "c", "#d9b36c")), Fig.For(sp));
            return (who ?? sp, "", C.Gold, null);
        }

        static VisualElement SceneView()
        {
            var sc = Game.Scene; if (sc == null) { Game.View = View.Hub; return HubView.Build(); }
            var def = DB.Scenes[sc.id]; var b = Game.CurrentBeat(); var st = Game.State;
            var tint = Tint(def.place);
            var s = Narrow("scene");
            s.style.backgroundColor = C.Mix(C.Ink, tint, 0.1f); Backdrop = C.Mix(C.Ink, tint, 0.1f);
            var glow = new VisualElement().Cls("scene-glow").NoPick();
            glow.generateVisualContent += mgc =>
            {
                var r = glow.contentRect; var p = mgc.painter2D;
                for (int i = 0; i < 32; i++) { float t = i / 31f; p.fillColor = C.A(tint, 0.02f * (1 - t) + 0.003f); p.BeginPath(); p.Arc(new Vector2(r.width * 0.5f, 0), r.width * (0.25f + 0.75f * t), Angle.Degrees(0), Angle.Degrees(180)); p.Fill(); }
            };
            s.Add(glow);

            var head = U.Div("scene-head");
            head.Add(U.Kicker(DB.Place(def.place, "sub")).Fg(C.Mix(tint, C.Cream, 0.4f)));
            head.Add(U.H2(def.title ?? DB.Place(def.place)));
            if (!string.IsNullOrEmpty(def.sub) && def.title != null) head.Add(U.Hint(def.sub));
            var skip = U.Btn(sc.replay ? "Close" : "Skip", () => { if (sc.replay) { Game.Scene = null; Game.View = View.Hub; Game.Tab = "story"; Game.Render(); } else Game.Skip(); }, "ghost small");
            skip.AddToClassList("scene-skip");
            head.Add(SoundToggle()); head.Add(skip);
            s.Add(head);

            var stage = U.Div("scene-stage"); var bottom = U.Div("scene-bottom");
            if (b != null)
            {
                string text = Story.Text(sc.id, b, sc.i, st);
                var (name, role, col, fig) = Speaker(b.sp, b.who);
                VisualElement beat;
                // the movie: the set for this place, you and whoever you're talking to in 3D, a camera that moves
                if (Bodies.Available)
                {
                    string other = b.sp != "you" && b.sp != "n" && !b.text && fig != null ? b.sp : sc.lastOther;
                    // nobody yet (the opening narration): whoever speaks first in this scene is already in the room
                    if (other == null) for (int k = sc.i; k < def.beats.Count; k++) { var nb = def.beats[k]; if (nb.sp != "you" && nb.sp != "n" && !nb.text && Speaker(nb.sp, null).fig != null) { other = nb.sp; break; } }
                    if (other != null) sc.lastOther = other;
                    var ofig = other != null ? (other == b.sp ? fig : Speaker(other, null).fig) : null;
                    bool speakerYou = b.sp == "you", speech = !b.text && b.sp != "n" && fig != null;
                    string shot = sc.i == 0 || b.sp == "n" ? "wide" : !speech ? "wide" : other == null ? "cu" : sc.i % 3 == 2 ? "ots" : "cu";
                    if (b.mood == "win" || b.mood == "bust" || b.mood == "cooler") shot = sc.i == 0 ? "wide" : "cu";
                    string setTier = def.place == "home" ? st.Eq("home") : "set_" + (def.place ?? "cardroom");
                    var film = new CharacterView(CharacterSpec.From(st), null, 0, 0, "scene") { Tier = setTier, NpcSpec = ofig != null ? CharacterSpec.From(ofig.look, ofig.eq, ofig.dye, ofig.fin) : null, ShotKind = shot, SpeakerIsYou = speakerYou || other == null, Narration = b.sp == "n" || b.text, Mood = b.mood };
                    film.AddToClassList("film");
                    Film(film, def, sc, tint);
                    stage.Add(film); stage.AddToClassList("movie");
                }
                if (b.sp == "n")
                {
                    beat = U.Div("narration").Cls(b.goal ? "goal" : "", b.big ? "big" : "");
                    if (b.goal) beat.Add(U.Kicker("Your goal").Fg(C.Gold));
                    var nl = U.T(U.Esc(text), "narr-t", b.goal ? Fonts.Semi : Fonts.Body); beat.Add(nl); if (Bodies.Available) Type(nl, U.Esc(text));
                }
                else if (b.text)
                {
                    if (!sc.replay || true) Sound.Play("text", 0.7f);
                    beat = U.Div("phone");
                    var ph = U.Row().Cls("phone-head");
                    if (fig != null) ph.Add(new Portrait(fig, 34, false));
                    ph.Add(U.T(name, "phone-name", Fonts.Semi));
                    ph.Add(U.Spring()); ph.Add(U.T("now", "phone-time"));
                    beat.Add(ph);
                    var bub = U.Div(b.sp == "you" ? "bubble me" : "bubble"); bub.Add(U.T(U.Esc(text), "bubble-t")); beat.Add(bub);
                }
                else
                {
                    // the speaker stands on the stage; what they say sits at the bottom (the drawn figure only without 3D)
                    if (fig != null && !Bodies.Available)
                    {
                        stage.AddToClassList("speaking");
                        var spot = U.Div("scene-spot").NoPick();
                        var big = new Portrait(fig, 230, false); big.AddToClassList("scene-figure"); spot.Add(big);
                        if (Game.Scene != null && Game.Scene.i > 0 && sc.lastSp == b.sp) stage.Add(spot); else { Enter(spot); stage.Add(spot); }
                    }
                    beat = U.Div("speech").Cls(b.sp == "you" ? "me" : "");
                    var nm = U.Row().Cls("speech-who");
                    nm.Add(U.T(name, "speech-name", Fonts.Semi).Fg(col));
                    if (!string.IsNullOrEmpty(role)) nm.Add(U.T(role, "speech-role"));
                    beat.Add(nm);
                    var sl = U.T(U.Esc(text), "speech-t" + (b.big ? " big" : "") + (b.mood == "think" ? " think" : "")); beat.Add(sl); if (Bodies.Available) Type(sl, U.Esc(text));
                }
                beat.AddToClassList("beat");
                Enter(beat);
                if ((b.sp == "n" || b.text) && !Bodies.Available) stage.Add(beat); else bottom.Add(beat);
                sc.lastSp = b.sp;

                if (sc.extra.Count == 0 && b.choice != null)
                {
                    var ch = U.Col().Cls("choices");
                    // story choices are neutral: no right or wrong colour, just one blue and one green (then they alternate)
                    for (int i = 0; i < b.choice.Count; i++) { int k = i; ch.Add(U.Btn(b.choice[i].t, () => Game.Choose(k), (i % 2 == 0 ? "pick-blue" : "pick-green") + " wide")); }
                    bottom.Add(ch);
                }
                else bottom.Add(U.Hint("Tap to continue").Cls("center tap-hint"));
            }
            s.Add(stage); s.Add(bottom);
            if (b == null || b.choice == null || sc.extra.Count > 0) s.Tap(() => { if (typing != null) typing(); else Game.Advance(); });   // a tap mid-line finishes the line first
            var dots = U.Row().Cls("dots");
            int total = def.beats.Count; for (int i = 0; i < total; i++) dots.Add(U.Div("dot").Cls(i < sc.i ? "done" : i == sc.i ? "on" : ""));
            s.Add(dots);
            return s;
        }

        // lines type themselves out like subtitles landing; a tap while typing shows the whole line
        static Action typing;
        static void Type(Label l, string full, int perTick = 2)
        {
            if (l == null || full.Length < 10) return;
            // the rest of the line is there but transparent, so the box is its final size from the first frame
            int n = 0; l.enableRichText = true; l.text = "<alpha=#00>" + full; IVisualElementScheduledItem it = null; Action done = null;
            done = () => { l.text = full; if (typing == done) typing = null; it?.Pause(); };
            it = l.schedule.Execute(() => { n = Math.Min(full.Length, n + perTick); l.text = full.Substring(0, n) + "<alpha=#00>" + full.Substring(n); if (n >= full.Length) done(); }).Every(40);
            typing = done;
            l.RegisterCallback<DetachFromPanelEvent>(e => { if (typing == done) typing = null; it?.Pause(); });
        }

        public static VisualElement SoundToggle()
        {
            var b = U.Div("icon-btn snd"); b.Add(new Icon(Sound.On ? "sound" : "mute", 18, C.Mute));
            b.Tap(() => { Sound.On = !Sound.On; Game.Render(); });
            return b;
        }
        // the film look over a scene shot: colour grade, vignette, grain, letterbox, a dip to black on every cut,
        // a fade in from black with a title card when the scene opens
        static Texture2D grainTex;
        static void Film(VisualElement film, Scene def, ScenePlay sc, Color tint)
        {
            var grade = U.Div("film-grade").NoPick(); grade.style.backgroundColor = C.A(tint, 0.10f); film.Add(grade);
            var vig = U.Div("film-vignette").NoPick();
            vig.generateVisualContent += mgc =>
            {
                var r = vig.contentRect; var p = mgc.painter2D; float R = Mathf.Max(r.width, r.height) * 0.78f; var cen = new Vector2(r.width / 2, r.height / 2);
                for (int i = 0; i < 14; i++)
                {
                    float t = i / 13f; p.fillColor = new Color(0, 0, 0, 0.035f + 0.02f * t); p.strokeColor = Color.clear;
                    p.BeginPath(); p.Arc(cen, R * (1.25f - 0.55f * t), Angle.Degrees(0), Angle.Degrees(360)); p.Arc(cen, R * 2.5f, Angle.Degrees(360), Angle.Degrees(0), ArcDirection.CounterClockwise); p.ClosePath(); p.Fill(FillRule.OddEven);
                }
            };
            film.Add(vig);
            if (grainTex == null)
            {
                grainTex = new Texture2D(96, 96, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
                var px = new Color32[96 * 96]; var rng = new System.Random(7);
                for (int i = 0; i < px.Length; i++) { byte v = (byte)rng.Next(0, 256); px[i] = new Color32(v, v, v, (byte)rng.Next(14, 40)); }
                grainTex.SetPixels32(px); grainTex.Apply();
            }
            var grain = U.Div("film-grain").NoPick(); grain.style.backgroundImage = new StyleBackground(grainTex); film.Add(grain);
            var bars = U.Div("film-bars").NoPick(); film.Add(bars);
            var fade = U.Div("film-fade").NoPick(); film.Add(fade);
            bool opening = sc.i == 0 && sc.extra.Count == 0;
            float t0 = Time.realtimeSinceStartup;
            if (opening)
            {
                var title = U.Div("film-title").NoPick();
                title.Add(U.T(DB.Place(def.place, "sub").ToUpperInvariant(), "film-title-k", Fonts.Semi));
                title.Add(U.T(def.title != null && def.sub != null ? def.sub : (def.title ?? ""), "film-title-t", Fonts.Disp));
                film.Add(title);
                title.schedule.Execute(() => title.AddToClassList("in")).StartingIn(900);
                title.schedule.Execute(() => title.RemoveFromClassList("in")).StartingIn(4200);
            }
            // every frame: the grain crawls, the fade follows the cuts (and the opening fade-in)
            film.schedule.Execute(() =>
            {
                var st = CharacterStage.Instance; float el = Time.realtimeSinceStartup - t0;
                float f = st != null ? st.CutFade : 0; if (opening) f = Mathf.Max(f, 1f - el / 1.1f);
                fade.style.opacity = Mathf.Clamp01(f);
                grain.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Left, UnityEngine.Random.Range(0, 96)); grain.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Top, UnityEngine.Random.Range(0, 96));
            }).Every(50);
        }

        // fade-and-rise on the next frame
        public static void Enter(VisualElement e, long delayMs = 16)
        {
            e.AddToClassList("anim"); e.AddToClassList("pre");
            e.schedule.Execute(() => e.RemoveFromClassList("pre")).StartingIn(delayMs);
        }
    }
}
