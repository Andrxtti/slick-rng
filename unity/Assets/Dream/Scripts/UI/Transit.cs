// Between registering and sitting down: a short cutscene. Live events show you driving your ride along the bay
// at sunset (painted layers scrolling at different speeds, your 3D ride crossing the screen); out-of-town majors
// and the private jet fly; online events open the CF Poker client. Tap anywhere to skip.
using System;
using System.Collections.Generic;
using CFDream.Character;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public static class TransitView
    {
        class Layer { public string name; public float y, h, speed; public bool tile; public Texture2D tex; }
        static List<Layer> layers; static float laneY = 1108, W0 = 2048, H0 = 1152;
        static readonly Dictionary<string, Texture2D> tex = new Dictionary<string, Texture2D>();

        public static VisualElement Build()
        {
            var trip = Game.Trip;
            var root = U.Div("transit");
            if (trip == null) { root.schedule.Execute(Game.Arrive).StartingIn(10); return root; }
            Screens.Backdrop = Color.black;
            bool done = false;
            void Finish() { if (done) return; done = true; RideStage.Get().End(); Game.Arrive(); }
            root.RegisterCallback<PointerUpEvent>(e => Finish());
            root.RegisterCallback<DetachFromPanelEvent>(e => { if (!done) RideStage.Get().End(); });
            bool built = false;
            root.RegisterCallback<GeometryChangedEvent>(e =>
            {
                if (built || root.layout.width < 10) return; built = true;
                float cw = root.layout.width, ch = root.layout.height;
                switch (trip.kind)
                {
                    case "online": Online(root, trip, cw, ch, Finish); break;
                    case "fly": Fly(root, trip, cw, ch, Finish); break;
                    default: Drive(root, trip, cw, ch, Finish); break;
                }
                Hint(root);
            });
            return root;
        }

        // ------------------------------------------------------------ the drive
        static void Drive(VisualElement root, Trip trip, float cw, float ch, Action finish)
        {
            Load();
            float s = Mathf.Max(cw / W0, ch / H0);                // cover the screen
            float offX = (cw - W0 * s) / 2, offY = ch - H0 * s;  // centre on the sun, keep the road in view
            float lw = W0 * s;
            var moving = new List<(VisualElement e, Layer l)>();
            var stage = RideStage.Get();
            VisualElement car = null, shadow = null, beam = null; float carW = 0, carH = 0, carTop = 0;
            foreach (var l in layers)
            {
                int copies = l.tile ? 3 : 1;
                for (int k = 0; k < copies; k++)
                {
                    var e = Img(l.tex, (l.tile ? offX - lw : offX) + k * lw, offY + l.y * s, lw, l.h * s);
                    if (l.name == "palms") e.name = "palms";
                    root.Add(e); moving.Add((e, l));
                }
                if (l.name == "road") AddCar();
            }
            void AddCar()
            {
                string ride = trip.ride;
                stage.Show(ride); stage.Begin();
                float metres = stage.Rig.bounds.size.x;
                float lenPx = metres * 100 * s;                    // the painting's scale: about 100 px a metre
                lenPx = Mathf.Min(lenPx, cw * (ride == "bus" ? 0.95f : 0.72f));   // on a tall phone the painting is huge; keep the ride to the screen
                carW = lenPx / Mathf.Max(0.2f, stage.LengthU); carH = carW / 2;
                float lane = offY + laneY * s;
                carTop = lane - (1 - stage.GroundV) * carH;
                shadow = Img(Tex("shadow"), 0, lane - lenPx * 0.05f, lenPx * 1.1f, lenPx * 0.12f);
                root.Add(shadow);
                car = new VisualElement().NoPick(); car.style.position = Position.Absolute;
                car.style.width = carW; car.style.height = carH; car.style.top = carTop; car.style.left = 0;
                car.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(stage.Texture));
                root.Add(car);
                beam = Img(Tex("beam"), 0, lane - lenPx * 0.28f, lenPx * 1.6f, lenPx * 0.45f); beam.style.opacity = 0.85f;
                root.Add(beam);
            }
            Letterbox(root, ch);
            Title(root, trip.kicker ?? "Heading to", trip.place ?? DB.Place(trip.ev?.venue), trip.sub ?? Sub(trip), RideName(trip.ride), cw, ch);

            float dur = trip.ride == "bus" ? 4.6f : 3.9f;
            float roadPx = 1500 * s;                               // the road's speed on screen, px/s
            stage.Speed = roadPx / (100 * s) * 0.9f;
            float t0 = Time.realtimeSinceStartup;
            root.schedule.Execute(() =>
            {
                float t = Time.realtimeSinceStartup - t0, u = Mathf.Clamp01(t / dur);
                foreach (var (e, l) in moving)
                {
                    float dx = l.tile ? -((t * roadPx * l.speed) % lw) : -t * roadPx * l.speed;
                    e.style.translate = new Translate(dx, 0);
                }
                if (car != null)
                {
                    // across the screen: quicker at the edges, easing through the middle
                    float v = u - 0.5f, p = 0.5f + 0.7f * v + 1.2f * v * v * v;
                    float lenPx = carW * stage.LengthU;
                    float x = Mathf.Lerp(-carW * 0.95f, cw + carW * 0.05f, p);
                    car.style.translate = new Translate(x, 0);
                    float rideL = x + (carW - lenPx) / 2;
                    shadow.style.translate = new Translate(rideL - lenPx * 0.05f, 0);
                    beam.style.translate = new Translate(rideL + lenPx * 0.9f, 0);
                }
                if (t >= dur) finish();
            }).Every(16);
        }

        // ------------------------------------------------------------ the flight
        static void Fly(VisualElement root, Trip trip, float cw, float ch, Action finish)
        {
            Load();
            float s = Mathf.Max(cw / W0, ch / H0) * 1.15f;
            float offX = (cw - W0 * s) / 2, offY = (ch - H0 * s) / 2 + ch * 0.18f;   // horizon low: we're up high
            float lw = W0 * s;
            var sky = layers.Find(l => l.name == "sky"); var city = layers.Find(l => l.name == "city");
            var skyE = Img(sky.tex, offX, offY + sky.y * s, lw, sky.h * s); root.Add(skyE);
            var cityE = Img(city.tex, offX, offY + city.y * s + ch * 0.06f, lw, city.h * s * 0.8f); cityE.style.opacity = 0.75f; root.Add(cityE);
            var stage = RideStage.Get(); stage.Show("jet", 0.86f); stage.Begin();
            float planeW = Mathf.Min(cw * 0.9f, ch * 1.3f), planeH = planeW / 2;
            var plane = new VisualElement().NoPick(); plane.style.position = Position.Absolute;
            plane.style.width = planeW; plane.style.height = planeH; plane.style.top = ch * 0.56f - planeH * 0.5f;
            plane.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(stage.Texture));
            root.Add(plane);
            Letterbox(root, ch);
            bool own = trip.ride == "jet";
            Title(root, trip.kicker ?? (own ? "Wheels up" : "Flying to"), trip.place ?? DB.Place(trip.ev?.venue), trip.sub ?? Sub(trip), own ? "Your private jet" : "Coach, window seat", cw, ch);
            float dur = 4.2f, t0 = Time.realtimeSinceStartup;
            root.schedule.Execute(() =>
            {
                float t = Time.realtimeSinceStartup - t0, u = Mathf.Clamp01(t / dur);
                float v = u - 0.5f, p = 0.5f + 0.75f * v + v * v * v;
                plane.style.translate = new Translate(Mathf.Lerp(-planeW * 0.9f, cw - planeW * 0.1f, p), -u * ch * 0.06f);
                skyE.style.translate = new Translate(-t * 12, 0); cityE.style.translate = new Translate(-t * 40, 0);
                stage.Bank = Mathf.Sin(u * Mathf.PI) * -4f;
                if (t >= dur) finish();
            }).Every(16);
        }

        // ------------------------------------------------------------ online: the CF Poker client
        static void Online(VisualElement root, Trip trip, float cw, float ch, Action finish)
        {
            root.AddToClassList("cf-client");
            var glow = new VisualElement().NoPick(); glow.AddToClassList("cf-glow"); root.Add(glow);
            glow.generateVisualContent += mgc =>
            {
                var r = glow.contentRect; var p = mgc.painter2D; var c = new Vector2(r.center.x, r.height * 0.42f);
                for (int i = 0; i < 26; i++)
                {
                    float t = i / 25f; p.fillColor = new Color(1f, 0.32f, 0.12f, 0.035f * (1 - t) + 0.002f);
                    p.BeginPath(); p.Arc(c, Mathf.Min(r.width, r.height) * (0.08f + 0.5f * t), Angle.Degrees(0), Angle.Degrees(360)); p.Fill();
                }
            };
            var col = U.Col().Cls("cf-center").NoPick();
            var mark = new VisualElement().NoPick(); mark.AddToClassList("cf-mark");
            float spin = 0;
            mark.generateVisualContent += mgc => DrawMark(mgc, mark.contentRect, spin);
            col.Add(mark);
            var word = U.Row().Cls("cf-word").NoPick();
            word.Add(U.T("CF", "cf-w1", Fonts.Bold).NoPick()); word.Add(U.T("POKER", "cf-w2", Fonts.Semi).NoPick());
            col.Add(word);
            var bar = U.Div("cf-bar").NoPick(); var fill = U.Div("cf-fill").NoPick(); bar.Add(fill); col.Add(bar);
            var status = U.T("Connecting…", "cf-status", Fonts.Medium).NoPick(); col.Add(status);
            var evl = U.T(trip.ev.name, "cf-ev", Fonts.Semi).NoPick(); col.Add(evl);
            root.Add(col);
            int seat = 1 + Mathf.Abs((trip.ev.id ?? "").GetHashCode()) % 9, table = 3 + Mathf.Abs((trip.ev.name ?? "").GetHashCode()) % 40;
            string[] lines = { "Connecting to CF Poker…", "Signing in as " + Game.State.name + "…", "Finding your table…", $"Table {table} · Seat {seat}. Good luck." };
            float dur = 2.8f, t0 = Time.realtimeSinceStartup;
            root.schedule.Execute(() =>
            {
                float t = Time.realtimeSinceStartup - t0, u = Mathf.Clamp01(t / dur);
                float eased = 1 - Mathf.Pow(1 - u, 2.2f);
                fill.style.width = Length.Percent(Mathf.Min(100, eased * 104));
                status.text = lines[Mathf.Min(lines.Length - 1, (int)(u * lines.Length))];
                spin = t * 220; mark.MarkDirtyRepaint();
                if (t >= dur + 0.25f) finish();
            }).Every(16);
        }

        // the CF mark: a flame inside a turning chip
        static void DrawMark(MeshGenerationContext mgc, Rect r, float spin)
        {
            var p = mgc.painter2D; var c = r.center; float R = Mathf.Min(r.width, r.height) / 2 - 2;
            p.fillColor = new Color(0.09f, 0.06f, 0.05f); p.BeginPath(); p.Arc(c, R, Angle.Degrees(0), Angle.Degrees(360)); p.Fill();
            p.lineWidth = R * 0.14f; p.lineCap = LineCap.Butt;
            for (int k = 0; k < 8; k++)
            {
                float a0 = spin + k * 45;
                p.strokeColor = k % 2 == 0 ? new Color(1f, 0.38f, 0.1f) : new Color(0.96f, 0.92f, 0.86f);
                p.BeginPath(); p.Arc(c, R * 0.88f, Angle.Degrees(a0), Angle.Degrees(a0 + 30)); p.Stroke();
            }
            p.strokeColor = new Color(1f, 0.55f, 0.2f, 0.6f); p.lineWidth = 1.5f;
            p.BeginPath(); p.Arc(c, R * 0.7f, Angle.Degrees(0), Angle.Degrees(360)); p.Stroke();
            // flame
            float k2 = R / 30f; Vector2 P(float x, float y) => c + new Vector2(x, y) * k2;
            p.fillColor = new Color(1f, 0.36f, 0.08f);
            p.BeginPath(); p.MoveTo(P(0, -17)); p.BezierCurveTo(P(10, -6), P(13, 3), P(9, 10)); p.BezierCurveTo(P(6, 15), P(-6, 15), P(-9, 10));
            p.BezierCurveTo(P(-12, 4), P(-8, -2), P(-4, -5)); p.BezierCurveTo(P(-4, 0), P(-2, 3), P(0, 3)); p.BezierCurveTo(P(2, -3), P(-2, -10), P(0, -17)); p.ClosePath(); p.Fill();
            p.fillColor = new Color(1f, 0.82f, 0.35f);
            p.BeginPath(); p.MoveTo(P(0, -2)); p.BezierCurveTo(P(5, 3), P(5, 9), P(2, 11)); p.BezierCurveTo(P(0, 12), P(-3, 11), P(-4, 8)); p.BezierCurveTo(P(-5, 5), P(-2, 3), P(0, -2)); p.ClosePath(); p.Fill();
        }

        // ------------------------------------------------------------ pieces
        static void Letterbox(VisualElement root, float ch)
        {
            foreach (bool top in new[] { true, false })
            {
                var b = U.Div("lbox").NoPick(); b.style.height = ch * 0.07f;
                if (top) b.style.top = 0; else b.style.bottom = 0;
                root.Add(b);
            }
            var vig = U.Div("vignette").NoPick(); var vt = Tex("vignette");
            if (vt != null) { vig.style.backgroundImage = new StyleBackground(vt); vig.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover); }
            root.Add(vig);
        }

        static void Title(VisualElement root, string kicker, string place, string sub, string ride, float cw, float ch)
        {
            bool land = cw > ch;
            var t = U.Col().Cls("trip-title", land ? "land" : "port").NoPick();
            t.style.top = ch * (land ? 0.13f : 0.12f);
            t.Add(U.T(kicker.ToUpperInvariant(), "trip-k", Fonts.Semi).NoPick());
            var big = U.T(place, "trip-place", Fonts.Bold).NoPick();
            big.style.textShadow = new TextShadow { offset = Vector2.zero, blurRadius = 14, color = new Color(1f, 0.25f, 0.65f, 0.95f) };
            t.Add(big);
            if (!string.IsNullOrEmpty(sub)) t.Add(U.T(sub, "trip-sub", Fonts.Medium).NoPick());
            root.Add(t);
            if (!string.IsNullOrEmpty(ride))
            {
                var r = U.Row().Cls("trip-ride").NoPick(); r.style.bottom = ch * 0.07f + 10;
                r.Add(new Icon("ride", 15, new Color(0.5f, 0.92f, 1f))); r.Add(U.T(ride, "trip-ride-t", Fonts.Semi).NoPick());
                root.Add(r);
            }
            // fade the card in (the USS transition does the easing)
            t.schedule.Execute(() => t.AddToClassList("in")).StartingIn(250);
        }

        static void Hint(VisualElement root)
        {
            var h = U.T("Tap to skip", "skip-hint", Fonts.Medium).NoPick(); root.Add(h);
        }

        static string Sub(Trip trip)
        {
            var ev = trip.ev; if (ev == null) return null;
            return ev.free ? ev.name + " · freeroll" : $"{ev.name} · {U.Money(ev.buyin)}";
        }
        static string RideName(string id) => DB.Item("ride", id)?.name ?? id;

        static VisualElement Img(Texture2D t, float x, float y, float w, float h)
        {
            var e = new VisualElement().NoPick(); e.style.position = Position.Absolute;
            e.style.left = x; e.style.top = y; e.style.width = w; e.style.height = h;
            if (t != null) { e.style.backgroundImage = new StyleBackground(t); e.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover); }
            return e;
        }

        static Texture2D Tex(string n)
        {
            if (tex.TryGetValue(n, out var t) && t != null) return t;
            t = Resources.Load<Texture2D>("Dream/Drive/" + n); tex[n] = t; return t;
        }

        static void Load()
        {
            if (layers != null) return;
            layers = new List<Layer>();
            var ta = Resources.Load<TextAsset>("Dream/Drive/drive");
            var d = ta != null ? Json.Parse(ta.text) as Dictionary<string, object> : null;
            if (d != null)
            {
                W0 = (float)J.N(d, "w", 2048); H0 = (float)J.N(d, "h", 1152); laneY = (float)J.N(d, "laneY", 1108);
                if (d.TryGetValue("layers", out var lo) && lo is List<object> ll)
                    foreach (var o in ll)
                        if (o is Dictionary<string, object> m)
                            layers.Add(new Layer { name = J.S(m, "name"), y = (float)J.N(m, "y"), h = (float)J.N(m, "h"), speed = (float)J.N(m, "speed"), tile = J.B(m, "tile") });
            }
            foreach (var l in layers) l.tex = Tex(l.name);
        }
    }
}
