// Which screen to draw, plus the title, the character creator and story scenes.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public static class Screens
    {
        public static VisualElement Build()
        {
            switch (Game.View)
            {
                case View.Create: return Create();
                case View.Hub: return HubView.Build();
                case View.Profile: Game.Tab = "career"; Game.View = View.Hub; return HubView.Build();
                case View.Scene: return SceneView();
                case View.Run: return RunView.Build();
                case View.Results: return ResultsView.Build();
                default: return Title();
            }
        }

        public static VisualElement Page(string cls = null) => U.Div("screen").Cls(cls);
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
            var s = Page("pad");
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
            var s = Page("title-screen");
            var glow = new VisualElement().Cls("title-glow").NoPick();
            glow.generateVisualContent += mgc =>
            {
                var r = glow.contentRect; var p = mgc.painter2D;
                for (int i = 0; i < 14; i++)
                {
                    float t = i / 13f; p.fillColor = C.A(C.Mix(C.Felt, C.Gold3, 0.2f), 0.05f * (1 - t) + 0.01f);
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

        static VisualElement Create()
        {
            var d = Game.Draft; bool re = Game.Restyle;
            var fig = re ? new Fig { look = d, eq = Game.State.eq, dye = Game.State.dye, fin = Game.State.fin } : Fig.Of(d, new State().eq);
            var s = Page("create");
            var top = U.Div("create-top");
            var portrait = new Portrait(fig, 150);
            top.Add(portrait);
            var nameCol = U.Col().Grow();
            nameCol.Add(U.Kicker(re ? "Restyle" : "Create your player"));
            var tf = new TextField { value = Game.DraftName, maxLength = 18 }; tf.AddToClassList("name-field"); Fonts.Use(tf, Fonts.Semi);
            tf.RegisterValueChangedCallback(e => Game.DraftName = e.newValue);
            nameCol.Add(U.Hint("Your name").Mt(4)); nameCol.Add(tf);
            nameCol.Add(U.Hint(re ? "Clothes stay on. Change your face, hair and body here." : "You'll earn every piece of clothing. This is just you.").Mt(6));
            top.Add(nameCol);
            s.Add(top);

            var sv = Scroll(); var body = U.Div("pad");
            void Changed() => Game.Render();
            body.Add(Section("Skin tone", Swatches(Opt("DR_SKIN"), d.skin, i => { d.skin = i; Changed(); })));
            body.Add(Section("Hair", Chips(Opt("DR_HAIR"), d.hair, v => { d.hair = v; Changed(); })));
            body.Add(Section("Hair colour", Swatches(Opt("DR_HAIRC"), d.hairC, i => { d.hairC = i; Changed(); })));
            body.Add(Section("Eye colour", Swatches(Opt("DR_EYEC"), d.eyeC, i => { d.eyeC = i; Changed(); })));
            body.Add(Section("Brows", Chips(Opt("DR_BROWS"), d.brows, v => { d.brows = v; Changed(); })));
            body.Add(Section("Face shape", Chips(Opt("DR_FACES"), d.face, v => { d.face = v; Changed(); })));
            body.Add(Section("Facial hair", Chips(Opt("DR_FACIAL"), d.facial, v => { d.facial = v; Changed(); })));
            body.Add(Section("Age", Chips(Opt("DR_AGES"), d.age, v => { d.age = v; Changed(); })));
            var fr = U.Row().Cls("wrap"); fr.Add(Toggle("Freckles", d.freck, () => { d.freck = !d.freck; Changed(); })); body.Add(Section("Details", fr));

            // sculpt: every slider redraws the portrait live without rebuilding the screen
            var sc = U.Card("sculpt");
            var head = U.Row(); head.Add(U.H3("Sculpt").Grow());
            head.Add(U.Btn("Reset", () => { d.sc.Clear(); Changed(); }, "ghost small"));
            head.Add(U.Btn("Random", () => { var r = new System.Random(); foreach (var g in Opt("DR_SCULPT")) foreach (var kv in Pairs(g)) d.sc[kv] = Math.Round(r.NextDouble() * 1.4 - 0.7, 2); Changed(); }, "ghost small"));
            sc.Add(head);
            foreach (var g in Opt("DR_SCULPT"))
            {
                var gl = g as List<object>; if (gl == null || gl.Count < 2) continue;
                sc.Add(U.Kicker(gl[0] as string).Mt(10));
                foreach (var item in gl[1] as List<object> ?? new List<object>())
                {
                    var kv = item as List<object>; if (kv == null || kv.Count < 2) continue;
                    string key = kv[0] as string, label = kv[1] as string;
                    var row = U.Row().Cls("slider-row");
                    row.Add(U.T(label, "slider-l"));
                    var sl = new Slider(-1, 1) { value = (float)(d.sc.TryGetValue(key, out var v) ? v : 0) }; sl.AddToClassList("sculpt-slider"); sl.style.flexGrow = 1;
                    sl.RegisterValueChangedCallback(e => { d.sc[key] = Math.Round(e.newValue, 2); portrait.MarkDirtyRepaint(); });
                    row.Add(sl); sc.Add(row);
                }
            }
            body.Add(sc);
            body.Add(U.Space(90));
            sv.Add(body); s.Add(sv);

            var foot = U.Row().Cls("footer");
            if (re)
            {
                foot.Add(U.Btn("Cancel", Game.CancelRestyle, "ghost grow"));
                foot.Add(U.Btn("Save look", () => Game.SaveRestyle(Game.DraftName), "gold grow"));
            }
            else
            {
                foot.Add(U.Btn("Randomize", () => { Randomize(d); Changed(); }, "ghost grow"));
                foot.Add(U.Btn("Start the story", () => Game.Begin(Game.DraftName), "gold grow"));
            }
            s.Add(foot);
            return s;
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
        static VisualElement Section(string title, VisualElement content)
        {
            var c = U.Div("section"); c.Add(U.Kicker(title)); c.Add(content); return c;
        }
        static VisualElement Swatches(List<object> colors, int sel, Action<int> pick)
        {
            var row = U.Row().Cls("wrap swatches");
            for (int i = 0; i < colors.Count; i++)
            {
                int k = i; var sw = U.Div("swatch").Bg(C.H(colors[i] as string));
                if (i == sel) sw.AddToClassList("on");
                sw.Tap(() => pick(k)); row.Add(sw);
            }
            return row;
        }
        static VisualElement Chips(List<object> opts, string sel, Action<string> pick)
        {
            var row = U.Row().Cls("wrap");
            foreach (var o in opts)
            {
                var kv = o as List<object>; if (kv == null || kv.Count < 2) continue;
                string id = kv[0] as string;
                row.Add(Toggle(kv[1] as string, id == sel, () => pick(id)));
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
            { "tv", "#3d7fd1" }, { "titan", "#8c7a4a" }, { "main", "#d9b36c" }, { "hall", "#7a5ac8" },
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
            var s = Page("scene");
            s.style.backgroundColor = C.Mix(C.Ink, tint, 0.1f);
            var glow = new VisualElement().Cls("scene-glow").NoPick();
            glow.generateVisualContent += mgc =>
            {
                var r = glow.contentRect; var p = mgc.painter2D;
                for (int i = 0; i < 12; i++) { float t = i / 11f; p.fillColor = C.A(tint, 0.05f * (1 - t) + 0.008f); p.BeginPath(); p.Arc(new Vector2(r.width * 0.5f, 0), r.width * (0.25f + 0.75f * t), Angle.Degrees(0), Angle.Degrees(180)); p.Fill(); }
            };
            s.Add(glow);

            var head = U.Div("scene-head");
            head.Add(U.Kicker(DB.Place(def.place, "sub")).Fg(C.Mix(tint, C.Cream, 0.4f)));
            head.Add(U.H2(def.title ?? DB.Place(def.place)));
            if (!string.IsNullOrEmpty(def.sub) && def.title != null) head.Add(U.Hint(def.sub));
            var skip = U.Btn(sc.replay ? "Close" : "Skip", () => { if (sc.replay) { Game.Scene = null; Game.View = View.Hub; Game.Tab = "story"; Game.Render(); } else Game.Skip(); }, "ghost small");
            skip.AddToClassList("scene-skip");
            head.Add(skip);
            s.Add(head);

            var stage = U.Div("scene-stage");
            if (b != null)
            {
                string text = Story.Text(sc.id, b, sc.i, st);
                var (name, role, col, fig) = Speaker(b.sp, b.who);
                VisualElement beat;
                if (b.sp == "n")
                {
                    beat = U.Div("narration").Cls(b.goal ? "goal" : "", b.big ? "big" : "");
                    if (b.goal) beat.Add(U.Kicker("Your goal").Fg(C.Gold));
                    beat.Add(U.T(U.Esc(text), "narr-t", b.goal ? Fonts.Semi : Fonts.Body));
                }
                else if (b.text)
                {
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
                    beat = U.Div("speech").Cls(b.sp == "you" ? "me" : "");
                    var who = U.Row().Cls("speech-who");
                    if (fig != null) who.Add(new Portrait(fig, 74));
                    var nm = U.Col(); nm.style.marginLeft = 10;
                    nm.Add(U.T(name, "speech-name", Fonts.Semi).Fg(col));
                    if (!string.IsNullOrEmpty(role)) nm.Add(U.Hint(role));
                    who.Add(nm); beat.Add(who);
                    beat.Add(U.T(U.Esc(text), "speech-t" + (b.big ? " big" : "") + (b.mood == "think" ? " think" : "")));
                }
                beat.AddToClassList("beat");
                Enter(beat);
                stage.Add(beat);

                if (sc.extra.Count == 0 && b.choice != null)
                {
                    var ch = U.Col().Cls("choices");
                    for (int i = 0; i < b.choice.Count; i++) { int k = i; ch.Add(U.Btn(b.choice[i].t, () => Game.Choose(k), i == 0 ? "gold wide" : "wide")); }
                    stage.Add(ch);
                }
                else stage.Add(U.Hint("Tap to continue").Cls("center tap-hint"));
            }
            s.Add(stage);
            if (b == null || b.choice == null || sc.extra.Count > 0) s.Tap(Game.Advance);
            var dots = U.Row().Cls("dots");
            int total = def.beats.Count; for (int i = 0; i < total; i++) dots.Add(U.Div("dot").Cls(i < sc.i ? "done" : i == sc.i ? "on" : ""));
            s.Add(dots);
            return s;
        }

        // fade-and-rise on the next frame
        public static void Enter(VisualElement e, long delayMs = 16)
        {
            e.AddToClassList("anim"); e.AddToClassList("pre");
            e.schedule.Execute(() => e.RemoveFromClassList("pre")).StartingIn(delayMs);
        }
    }
}
