// The first time you reach a new place, the trip ends on an arrival shot: Silver Strike's neon tower, the Strip
// at dusk with searchlights, the ON AIR light at the TV studio, the private terminal for Titan, the Main Event
// ballroom, the Hall of Legends. Painted live (Painter2D), a slow push-in, a title card. Tap skips it.
using System;
using System.Collections.Generic;
using CFDream.Character;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public static class ArrivalView
    {
        // the places that get one, and how you get there
        public static readonly Dictionary<string, string> Venues = new Dictionary<string, string>
        { { "casino", "drive" }, { "vegas", "fly" }, { "tv", "fly" }, { "titan", "fly" }, { "main", "fly" }, { "hall", "drive" } };

        // the arrival for this venue if you've never had it (and mark it had)
        public static string Claim(State s, string venue)
        {
            if (s == null || string.IsNullOrEmpty(venue) || !Venues.ContainsKey(venue) || s.FlagN("arr_" + venue) > 0) return null;
            s.SetFlag("arr_" + venue); s.Save(); return venue;
        }

        static readonly Dictionary<string, (string kicker, string place, string sub)> Cards = new Dictionary<string, (string, string, string)>
        {
            { "casino", ("Arriving", "Silver Strike Casino", "The Circuit · every event gives a ring") },
            { "vegas", ("Touchdown", "Las Vegas", "The WCS Summer Series") },
            { "tv", ("Studio 4", "The Feature Table", "Royal Poker Tour · live on stream") },
            { "titan", ("Private terminal", "Titan Super High Roller", "Invitation only") },
            { "main", ("The ballroom", "The Main Event", "World Card Series · every seat filled") },
            { "hall", ("By invitation", "The Hall of Legends", "Every era. One room.") },
        };

        public static void Play(VisualElement root, string venue, float cw, float ch, Action done)
        {
            root.Clear(); root.style.backgroundColor = Color.black;
            var art = new VisualElement().NoPick(); art.style.position = Position.Absolute;
            art.style.left = 0; art.style.top = 0; art.style.width = cw; art.style.height = ch;
            root.Add(art);
            float t = 0; var labels = new List<(Label l, Func<float, float> alpha)>();
            Painter paint = venue == "vegas" ? (Painter)Vegas : venue == "tv" ? Studio : venue == "titan" ? Terminal : venue == "main" ? Ballroom : venue == "hall" ? (Painter)Hall : Casino;
            art.generateVisualContent += mgc => paint(mgc.painter2D, cw, ch, t);

            // words that live in the picture (neon letters, the ON AIR box)
            void Word(string text, float x, float y, float size, Color c, Font f, Func<float, float> alpha, bool center = true, float glow = 18)
            {
                var l = U.T(text, null, f).NoPick(); l.style.position = Position.Absolute;
                l.style.fontSize = size; l.style.color = c; l.style.unityTextAlign = center ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
                l.style.left = center ? x - cw : x; l.style.width = center ? cw * 2 : StyleKeyword.Auto; l.style.top = y - size * 0.7f;
                l.style.textShadow = new TextShadow { offset = Vector2.zero, blurRadius = glow, color = new Color(c.r, c.g, c.b, 0.95f) };
                l.style.letterSpacing = size * 0.06f;
                art.Add(l); labels.Add((l, alpha));
            }
            float u = Mathf.Min(cw, ch);
            Func<float, float> on = _ => 1;
            switch (venue)
            {
                case "casino":
                    {
                        // the tower sign: SILVER STRIKE down the column, flickering on
                        string word = "SILVERSTRIKE"; float x = cw * 0.86f, y0 = ch * 0.25f, step = ch * 0.047f;
                        for (int i = 0; i < word.Length; i++)
                        {
                            int k = i; float gap = i >= 6 ? step * 0.6f : 0;
                            Word(word[i].ToString(), x, y0 + i * step + gap, step * 0.82f, new Color(0.62f, 0.95f, 1f), Fonts.Bold, tt => Flicker(tt, 0.35f + k * 0.05f));
                        }
                        break;
                    }
                case "vegas":
                    Word("WELCOME TO", cw * 0.5f, ch * 0.555f, u * 0.035f, new Color(1f, 0.95f, 0.8f), Fonts.Semi, tt => Flicker(tt, 0.4f));
                    Word("LAS VEGAS", cw * 0.5f, ch * 0.615f, u * 0.085f, new Color(1f, 0.35f, 0.62f), Fonts.Bold, tt => Flicker(tt, 0.6f), true, 26);
                    Word("THE SUMMER SERIES", cw * 0.5f, ch * 0.672f, u * 0.03f, new Color(0.55f, 0.95f, 1f), Fonts.Semi, tt => Flicker(tt, 0.9f));
                    break;
                case "tv":
                    Word("ON AIR", cw * 0.5f, ch * 0.36f, u * 0.09f, Color.white, Fonts.Bold, tt => tt < OnAir ? 0.25f : 1f, true, 30);
                    break;
                case "titan":
                    Word("TITAN", cw * 0.5f, ch * 0.3f, u * 0.075f, new Color(1f, 0.83f, 0.45f), Fonts.Bold, tt => Mathf.Clamp01((tt - 0.5f) * 1.5f), true, 22);
                    break;
                case "main":
                    Word("THE MAIN EVENT", cw * 0.5f, ch * 0.2f, u * 0.06f, new Color(1f, 0.86f, 0.5f), Fonts.Bold, tt => Mathf.Clamp01((tt - 0.3f) * 1.4f), true, 24);
                    break;
                case "hall":
                    Word("HALL OF LEGENDS", cw * 0.5f, ch * 0.19f, u * 0.055f, new Color(1f, 0.86f, 0.5f), Fonts.Bold, tt => Mathf.Clamp01((tt - 0.4f) * 1.4f), true, 22);
                    break;
            }

            // the private jet waits on the apron
            VisualElement jet = null; RideStage stage = null;
            if (venue == "titan")
            {
                stage = RideStage.Get(); stage.Show("jet", 0.86f); stage.Begin(); stage.Speed = 0; stage.Bank = 0;
                float jw = Mathf.Min(cw * 0.95f, ch * 1.2f);
                jet = new VisualElement().NoPick(); jet.style.position = Position.Absolute;
                jet.style.width = jw; jet.style.height = jw / 2; jet.style.left = (cw - jw) / 2; jet.style.top = ch * 0.68f - jw / 4;
                jet.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(stage.Texture));
                art.Add(jet);
            }

            var c = Cards.TryGetValue(venue, out var cc) ? cc : ("Arriving", DB.Place(venue), null);
            TransitCard(root, c.Item1, c.Item2, c.Item3, cw, ch);

            float dur = 3.6f, t0 = Time.realtimeSinceStartup; bool flashed = false, swelled = false;
            Sound.Play(venue == "tv" ? "hush" : "swell", 0.7f);
            art.schedule.Execute(() =>
            {
                t = Time.realtimeSinceStartup - t0;
                float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / dur));
                float zoom = venue == "main" || venue == "hall" ? 1f + 0.16f * k : 1f + 0.06f * k;   // the long walk in, or a slow push
                art.style.scale = new Scale(new Vector3(zoom, zoom, 1));
                foreach (var (l, a) in labels) l.style.opacity = a(t);
                if (jet != null) { float s2 = 0.92f + 0.08f * k; jet.style.scale = new Scale(new Vector3(s2, s2, 1)); }
                if (venue == "tv" && !flashed && t >= OnAir) { flashed = true; Sound.Play("flash", 0.8f); }
                if (venue == "vegas" && !swelled && t >= 0.6f) { swelled = true; Sound.Play("crowd", 0.35f); }
                art.MarkDirtyRepaint();
                if (t >= dur) done();
            }).Every(16);
        }
        const float OnAir = 1.3f;

        // neon coming on: a stutter, then steady
        static float Flicker(float t, float at)
        {
            if (t < at) return 0.12f;
            float d = t - at;
            if (d < 0.08f) return 1; if (d < 0.16f) return 0.2f; if (d < 0.22f) return 1; if (d < 0.3f) return 0.35f;
            return 0.92f + 0.08f * Mathf.Sin(t * 23f) * Mathf.Sin(t * 7f);
        }

        static void TransitCard(VisualElement root, string kicker, string place, string sub, float cw, float ch)
        {
            foreach (bool top in new[] { true, false })
            {
                var b = U.Div("lbox").NoPick(); b.style.height = ch * 0.07f;
                if (top) b.style.top = 0; else b.style.bottom = 0;
                root.Add(b);
            }
            bool land = cw > ch;
            var t = U.Col().Cls("trip-title", land ? "land" : "port").NoPick();
            t.style.top = StyleKeyword.Auto; t.style.bottom = ch * 0.07f + 18;
            t.Add(U.T(kicker.ToUpperInvariant(), "trip-k", Fonts.Semi).NoPick());
            var big = U.T(place, "trip-place", Fonts.Bold).NoPick();
            big.style.textShadow = new TextShadow { offset = Vector2.zero, blurRadius = 14, color = new Color(1f, 0.25f, 0.65f, 0.95f) };
            t.Add(big);
            if (!string.IsNullOrEmpty(sub)) t.Add(U.T(sub, "trip-sub", Fonts.Medium).NoPick());
            root.Add(t);
            t.schedule.Execute(() => t.AddToClassList("in")).StartingIn(900);
        }

        // ------------------------------------------------------------ painting
        delegate void Painter(Painter2D p, float W, float H, float t);

        static float Hash(int i) { float x = Mathf.Sin(i * 12.9898f + 78.233f) * 43758.5453f; return x - Mathf.Floor(x); }
        static void Rect(Painter2D p, float x, float y, float w, float h, Color c)
        {
            p.fillColor = c; p.BeginPath(); p.MoveTo(new Vector2(x, y)); p.LineTo(new Vector2(x + w, y)); p.LineTo(new Vector2(x + w, y + h)); p.LineTo(new Vector2(x, y + h)); p.ClosePath(); p.Fill();
        }
        static void Poly(Painter2D p, Color c, params Vector2[] pts)
        {
            p.fillColor = c; p.BeginPath(); p.MoveTo(pts[0]); for (int i = 1; i < pts.Length; i++) p.LineTo(pts[i]); p.ClosePath(); p.Fill();
        }
        static void Dot(Painter2D p, float x, float y, float r, Color c) { p.fillColor = c; p.BeginPath(); p.Arc(new Vector2(x, y), Mathf.Max(0.5f, r), Angle.Degrees(0), Angle.Degrees(360)); p.Fill(); }
        static void Glow(Painter2D p, float x, float y, float r, Color c, int rings = 6)
        {
            for (int i = rings; i >= 1; i--) Dot(p, x, y, r * (1 + i * 0.55f), new Color(c.r, c.g, c.b, c.a * 0.07f));
            Dot(p, x, y, r, c);
        }
        static void Ellipse(Painter2D p, float cx, float cy, float rx, float ry, Color c)
        {
            p.fillColor = c; p.BeginPath();
            for (int i = 0; i <= 28; i++) { float a = i / 28f * Mathf.PI * 2; var v = new Vector2(cx + Mathf.Cos(a) * rx, cy + Mathf.Sin(a) * ry); if (i == 0) p.MoveTo(v); else p.LineTo(v); }
            p.ClosePath(); p.Fill();
        }
        static void Sky(Painter2D p, float W, float H, float to, Color top, Color bottom)
        {
            int n = 28; float h = H * to / n;
            for (int i = 0; i < n; i++) Rect(p, 0, i * h, W, h + 1, Color.Lerp(top, bottom, i / (float)(n - 1)));
        }
        static void Stars(Painter2D p, float W, float H, float t, int n, float below)
        {
            for (int i = 0; i < n; i++)
            {
                float a = 0.35f + 0.35f * Mathf.Sin(t * (1.5f + Hash(i + 900) * 3) + i);
                Dot(p, Hash(i) * W, Hash(i + 300) * H * below, 0.6f + Hash(i + 600) * 1.1f, new Color(1, 1, 1, a));
            }
        }
        static void Windows(Painter2D p, float x, float y, float w, float h, int cols, int rows, int seed, Color lit, float on = 0.62f)
        {
            float cwid = w / cols, rh = h / rows;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float hsh = Hash(seed + r * 31 + c * 7); if (hsh > on) continue;
                    Rect(p, x + c * cwid + cwid * 0.22f, y + r * rh + rh * 0.25f, cwid * 0.56f, rh * 0.5f, new Color(lit.r, lit.g, lit.b, 0.45f + 0.55f * hsh / on));
                }
        }
        static void Palm(Painter2D p, float x, float ground, float h, Color c)
        {
            float lean = h * 0.12f;
            Poly(p, c, new Vector2(x - h * 0.018f, ground), new Vector2(x + h * 0.018f, ground), new Vector2(x + lean + h * 0.01f, ground - h), new Vector2(x + lean - h * 0.01f, ground - h));
            var top = new Vector2(x + lean, ground - h);
            for (int i = 0; i < 7; i++)
            {
                float a = Mathf.Lerp(-170, -10, i / 6f) * Mathf.Deg2Rad, len = h * (0.34f + 0.06f * Mathf.Sin(i * 2.1f));
                var tip = top + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.55f + 0.35f) * len;
                var side = new Vector2(-(tip - top).y, (tip - top).x).normalized * h * 0.03f;
                Poly(p, c, top, (top + tip) / 2 + side, tip, (top + tip) / 2 - side * 0.3f);
            }
        }

        // Silver Strike: a casino block, a chasing marquee, the neon tower and a lightning bolt over the door
        static void Casino(Painter2D p, float W, float H, float t)
        {
            float g = H * 0.86f;
            Sky(p, W, H, 0.86f, new Color(0.05f, 0.03f, 0.12f), new Color(0.27f, 0.08f, 0.33f));
            Stars(p, W, H, t, 50, 0.45f);
            Glow(p, W * 0.2f, H * 0.16f, H * 0.035f, new Color(1f, 0.95f, 0.85f, 0.9f), 8);   // the moon
            float bx = W * 0.08f, bw = W * 0.68f, by = H * 0.4f;
            Rect(p, bx, by, bw, g - by, new Color(0.09f, 0.05f, 0.12f));
            Rect(p, bx, by - H * 0.006f, bw, H * 0.006f, new Color(1f, 0.35f, 0.7f, 0.9f));
            Windows(p, bx + bw * 0.04f, by + H * 0.03f, bw * 0.92f, (g - by) * 0.55f, 12, 6, 11, new Color(1f, 0.8f, 0.45f));
            // the canopy and its chasing bulbs
            float cy = g - (g - by) * 0.3f, chh = (g - by) * 0.12f;
            Rect(p, bx - W * 0.02f, cy, bw + W * 0.04f, chh, new Color(0.18f, 0.08f, 0.2f));
            int bulbs = 22; int step = (int)(t * 10);
            for (int i = 0; i < bulbs; i++)
            {
                bool lit = (i + step) % 3 == 0; float x = bx + bw * (i + 0.5f) / bulbs;
                Dot(p, x, cy, H * 0.005f, lit ? new Color(1f, 0.92f, 0.6f) : new Color(0.5f, 0.35f, 0.2f));
                Dot(p, x, cy + chh, H * 0.005f, !lit ? new Color(1f, 0.92f, 0.6f) : new Color(0.5f, 0.35f, 0.2f));
            }
            // the doors, warm
            Rect(p, bx + bw * 0.38f, cy + chh, bw * 0.24f, g - cy - chh, new Color(1f, 0.72f, 0.38f, 0.75f));
            // the bolt over the canopy: silver strike
            float bolt = Flicker(t, 0.25f);
            var bc = new Color(0.75f, 0.97f, 1f, bolt);
            float mx = bx + bw * 0.5f, my = by - H * 0.02f, s = H * 0.11f;
            for (int i = 3; i >= 0; i--)
            {
                float k = 1 + i * 0.25f; var gc = i == 0 ? bc : new Color(0.4f, 0.85f, 1f, bolt * 0.12f);
                Poly(p, gc, new Vector2(mx + s * 0.1f * k, my - s * k * 0.9f), new Vector2(mx - s * 0.25f * k, my - s * 0.05f), new Vector2(mx + s * 0.02f, my - s * 0.12f),
                    new Vector2(mx - s * 0.12f * k, my + s * 0.6f * k), new Vector2(mx + s * 0.28f * k, my - s * 0.25f), new Vector2(mx + s * 0.02f, my - s * 0.2f));
            }
            // the tower sign
            float tx = W * 0.8f, tw = W * 0.12f, ty = H * 0.2f;
            Rect(p, tx, ty, tw, g - ty, new Color(0.06f, 0.05f, 0.09f));
            p.strokeColor = new Color(0.4f, 0.9f, 1f, 0.25f * Flicker(t, 0.3f)); p.lineWidth = 8; p.BeginPath(); p.MoveTo(new Vector2(tx, ty)); p.LineTo(new Vector2(tx + tw, ty)); p.LineTo(new Vector2(tx + tw, g)); p.LineTo(new Vector2(tx, g)); p.ClosePath(); p.Stroke();
            p.strokeColor = new Color(0.7f, 0.97f, 1f, Flicker(t, 0.3f)); p.lineWidth = 2; p.Stroke();
            // the street, and the neon in it
            Rect(p, 0, g, W, H - g, new Color(0.04f, 0.03f, 0.06f));
            Rect(p, bx, g + H * 0.01f, bw, H * 0.02f, new Color(1f, 0.4f, 0.7f, 0.12f));
            Rect(p, tx, g + H * 0.01f, tw, H * 0.05f, new Color(0.5f, 0.9f, 1f, 0.1f * Flicker(t, 0.3f)));
            var dark = new Color(0.02f, 0.01f, 0.03f);
            Palm(p, W * 0.03f, g, H * 0.42f, dark); Palm(p, W * 0.72f, g, H * 0.36f, dark); Palm(p, W * 0.97f, g, H * 0.46f, dark);
        }

        // the Strip at dusk: towers, searchlights, traffic, and the sign
        static void Vegas(Painter2D p, float W, float H, float t)
        {
            float g = H * 0.8f;
            Sky(p, W, H, 0.8f, new Color(0.1f, 0.05f, 0.24f), new Color(1f, 0.46f, 0.36f));
            Stars(p, W, H, t, 26, 0.3f);
            // searchlights
            foreach (var (bx, ph) in new[] { (W * 0.25f, 0f), (W * 0.74f, 1.7f) })
            {
                float a = (-90 + 24 * Mathf.Sin(t * 0.9f + ph)) * Mathf.Deg2Rad, len = H * 1.1f, sp = 0.05f;
                var b0 = new Vector2(bx, g - H * 0.3f);
                Poly(p, new Color(1f, 1f, 0.9f, 0.09f), b0, b0 + new Vector2(Mathf.Cos(a - sp), Mathf.Sin(a - sp)) * len, b0 + new Vector2(Mathf.Cos(a + sp), Mathf.Sin(a + sp)) * len);
            }
            // towers
            for (int i = 0; i < 13; i++)
            {
                float w = W * (0.05f + Hash(i + 40) * 0.06f), h = H * (0.18f + Hash(i + 80) * 0.42f), x = W * (i / 13f) + (Hash(i + 120) - 0.5f) * W * 0.03f;
                var col = Color.Lerp(new Color(0.08f, 0.05f, 0.16f), new Color(0.16f, 0.08f, 0.22f), Hash(i + 160));
                Rect(p, x, g - h, w, h, col);
                Windows(p, x + w * 0.05f, g - h + H * 0.012f, w * 0.9f, h * 0.9f, 4, Mathf.Max(3, (int)(h / (H * 0.03f))), i * 97, Hash(i + 200) > 0.5f ? new Color(1f, 0.85f, 0.5f) : new Color(0.7f, 0.9f, 1f), 0.55f);
                if (Hash(i + 240) > 0.55f) Rect(p, x, g - h - H * 0.004f, w, H * 0.004f, new Color(1f, 0.35f, 0.7f, 0.8f));
            }
            // the sign: a diamond panel on a post
            float sx = W * 0.5f, sy = H * 0.61f, sw = Mathf.Min(W * 0.34f, H * 0.3f), sh = H * 0.1f;
            Rect(p, sx - W * 0.01f, sy + sh * 0.9f, W * 0.02f, g - sy - sh * 0.9f, new Color(0.15f, 0.12f, 0.12f));
            var panel = new[] { new Vector2(sx, sy - sh * 1.2f), new Vector2(sx + sw * 0.5f, sy - sh * 0.4f), new Vector2(sx + sw * 0.5f, sy + sh * 0.6f), new Vector2(sx, sy + sh * 1.05f), new Vector2(sx - sw * 0.5f, sy + sh * 0.6f), new Vector2(sx - sw * 0.5f, sy - sh * 0.4f) };
            Poly(p, new Color(0.96f, 0.93f, 0.85f), panel);
            p.strokeColor = new Color(1f, 0.35f, 0.62f, Flicker(t, 0.3f)); p.lineWidth = 3; p.BeginPath(); p.MoveTo(panel[0]); for (int i = 1; i < panel.Length; i++) p.LineTo(panel[i]); p.ClosePath(); p.Stroke();
            for (int i = 0; i < 16; i++) { var a = panel[i * panel.Length / 16 % panel.Length]; var b = panel[(i * panel.Length / 16 + 1) % panel.Length]; float f = (i % 2) * 0.5f; var v = Vector2.Lerp(a, b, f); Dot(p, v.x, v.y, H * 0.004f, ((i + (int)(t * 8)) % 2 == 0) ? new Color(1f, 0.95f, 0.6f) : new Color(0.6f, 0.45f, 0.3f)); }
            // the boulevard and its traffic
            Rect(p, 0, g, W, H - g, new Color(0.05f, 0.03f, 0.07f));
            for (int i = 0; i < 18; i++)
            {
                float lane = i % 2 == 0 ? g + (H - g) * 0.35f : g + (H - g) * 0.6f, dir = i % 2 == 0 ? 1 : -1;
                float x = ((Hash(i + 500) * W + dir * t * W * 0.35f) % (W * 1.2f) + W * 1.2f) % (W * 1.2f) - W * 0.1f;
                Rect(p, x, lane, W * 0.05f, H * 0.004f, dir > 0 ? new Color(1f, 0.2f, 0.25f, 0.85f) : new Color(1f, 0.95f, 0.85f, 0.85f));
            }
        }

        // studio 4: dark set, the lights come on one by one, then ON AIR goes red
        static void Studio(Painter2D p, float W, float H, float t)
        {
            float g = H * 0.78f;
            Rect(p, 0, 0, W, g, new Color(0.06f, 0.07f, 0.1f));
            Rect(p, 0, g, W, H - g, new Color(0.03f, 0.035f, 0.05f));
            // back wall panels
            for (int i = 0; i < 9; i++) Rect(p, W * (i / 9f) + 2, H * 0.14f, W / 9f - 4, g - H * 0.14f, new Color(0.08f, 0.09f, 0.13f));
            // the truss and its lamps
            Rect(p, 0, H * 0.08f, W, H * 0.025f, new Color(0.18f, 0.18f, 0.2f));
            for (int i = 0; i < 5; i++)
            {
                float x = W * (0.12f + i * 0.19f), on = t > 0.2f + i * 0.18f ? 1 : 0;
                Rect(p, x - W * 0.02f, H * 0.105f, W * 0.04f, H * 0.035f, new Color(0.12f, 0.12f, 0.14f));
                if (on > 0)
                {
                    Poly(p, new Color(1f, 0.92f, 0.75f, 0.07f), new Vector2(x - W * 0.015f, H * 0.14f), new Vector2(x + W * 0.015f, H * 0.14f), new Vector2(x + W * 0.13f, g), new Vector2(x - W * 0.13f, g));
                    Ellipse(p, x, g + H * 0.01f, W * 0.13f, H * 0.025f, new Color(1f, 0.9f, 0.7f, 0.08f));
                    Glow(p, x, H * 0.14f, H * 0.008f, new Color(1f, 0.95f, 0.8f, 1f), 4);
                }
            }
            // the feature table on set
            Ellipse(p, W * 0.5f, g + H * 0.06f, W * 0.3f, H * 0.06f, new Color(0.12f, 0.08f, 0.05f));
            Ellipse(p, W * 0.5f, g + H * 0.055f, W * 0.27f, H * 0.048f, new Color(0.05f, 0.3f, 0.2f));
            // ON AIR
            bool lit = t >= OnAir; float bw = Mathf.Min(W * 0.5f, H * 0.36f), bh = H * 0.11f, bx = W * 0.5f - bw / 2, by = H * 0.36f - bh / 2;
            if (lit) for (int i = 4; i >= 1; i--) Rect(p, bx - i * 6, by - i * 6, bw + i * 12, bh + i * 12, new Color(1f, 0.1f, 0.15f, 0.06f));
            Rect(p, bx, by, bw, bh, lit ? new Color(0.9f, 0.06f, 0.14f) : new Color(0.22f, 0.04f, 0.05f));
            // a camera in the foreground
            float cx = W * 0.12f, cyy = H * 0.66f;
            p.strokeColor = new Color(0.02f, 0.02f, 0.03f); p.lineWidth = Mathf.Max(3, W * 0.006f);
            foreach (var dx in new[] { -0.08f, 0f, 0.08f }) { p.BeginPath(); p.MoveTo(new Vector2(cx, cyy)); p.LineTo(new Vector2(cx + W * dx, H)); p.Stroke(); }
            Rect(p, cx - W * 0.07f, cyy - H * 0.07f, W * 0.14f, H * 0.07f, new Color(0.03f, 0.03f, 0.04f));
            Rect(p, cx + W * 0.07f, cyy - H * 0.055f, W * 0.04f, H * 0.04f, new Color(0.03f, 0.03f, 0.04f));
            if (lit) Dot(p, cx - W * 0.05f, cyy - H * 0.06f, H * 0.005f, new Color(1f, 0.1f, 0.15f));
        }

        // Titan: the runway at night, the terminal glowing, the rabbit lights running toward the jet
        static void Terminal(Painter2D p, float W, float H, float t)
        {
            float hz = H * 0.48f;
            Sky(p, W, H, 0.48f, new Color(0.01f, 0.02f, 0.06f), new Color(0.1f, 0.14f, 0.26f));
            Stars(p, W, H, t, 60, 0.42f);
            Rect(p, 0, hz, W, H - hz, new Color(0.04f, 0.05f, 0.07f));
            // the terminal
            Rect(p, W * 0.02f, hz - H * 0.09f, W * 0.36f, H * 0.09f, new Color(0.07f, 0.08f, 0.1f));
            Rect(p, W * 0.04f, hz - H * 0.065f, W * 0.32f, H * 0.035f, new Color(1f, 0.78f, 0.42f, 0.8f));
            Rect(p, W * 0.66f, hz - H * 0.13f, W * 0.3f, H * 0.13f, new Color(0.06f, 0.07f, 0.09f));
            Rect(p, W * 0.68f, hz - H * 0.11f, W * 0.26f, H * 0.012f, new Color(1f, 0.82f, 0.5f, 0.6f));
            // runway in perspective
            var vp = new Vector2(W * 0.5f, hz);
            Poly(p, new Color(0.08f, 0.09f, 0.11f), vp + new Vector2(-W * 0.02f, 0), vp + new Vector2(W * 0.02f, 0), new Vector2(W * 0.95f, H), new Vector2(W * 0.05f, H));
            for (int i = 0; i < 9; i++)
            {
                float z0 = Mathf.Pow((i + ((t * 0.6f) % 1)) / 9f, 2.2f), z1 = Mathf.Pow((i + 0.45f + ((t * 0.6f) % 1)) / 9f, 2.2f);
                float y0 = Mathf.Lerp(hz, H, z0), y1 = Mathf.Lerp(hz, H, z1), w0 = Mathf.Lerp(W * 0.002f, W * 0.012f, z0), w1 = Mathf.Lerp(W * 0.002f, W * 0.012f, z1);
                Poly(p, new Color(0.85f, 0.85f, 0.8f, 0.7f), new Vector2(W * 0.5f - w0, y0), new Vector2(W * 0.5f + w0, y0), new Vector2(W * 0.5f + w1, y1), new Vector2(W * 0.5f - w1, y1));
            }
            // edge lights, and the rabbit running in
            int n = 14; int rabbit = (int)(t * 9) % n;
            for (int i = 0; i < n; i++)
            {
                float z = Mathf.Pow((i + 0.5f) / n, 2f), y = Mathf.Lerp(hz, H, z), half = Mathf.Lerp(W * 0.02f, W * 0.45f, z), r = Mathf.Lerp(1, H * 0.008f, z);
                var c = i == n - 1 - rabbit ? new Color(1f, 1f, 1f) : new Color(0.45f, 0.65f, 1f, 0.85f);
                Glow(p, W * 0.5f - half, y, r, c, 3); Glow(p, W * 0.5f + half, y, r, c, 3);
            }
        }

        // the Main Event ballroom: rows of tables to the vanishing point, chandeliers, the long walk in
        static void Ballroom(Painter2D p, float W, float H, float t)
        {
            float hz = H * 0.36f; var vp = new Vector2(W * 0.5f, hz);
            Rect(p, 0, 0, W, hz, new Color(0.05f, 0.04f, 0.04f));
            Rect(p, 0, hz, W, H - hz, new Color(0.12f, 0.06f, 0.07f));
            // carpet pattern lines toward the vanishing point
            p.strokeColor = new Color(0.25f, 0.12f, 0.1f, 0.5f); p.lineWidth = 1;
            for (int i = -10; i <= 10; i++) { p.BeginPath(); p.MoveTo(vp); p.LineTo(new Vector2(W * 0.5f + i * W * 0.12f, H)); p.Stroke(); }
            // chandeliers receding
            for (int i = 0; i < 7; i++)
            {
                float z = 1 - i / 7f, y = Mathf.Lerp(hz - H * 0.02f, H * 0.06f, z * z), r = Mathf.Lerp(H * 0.004f, H * 0.03f, z * z);
                foreach (var side in new[] { -1f, 1f }) { float x = W * 0.5f + side * Mathf.Lerp(W * 0.03f, W * 0.32f, z * z); Glow(p, x, y, r, new Color(1f, 0.88f, 0.6f, 0.9f), 5); }
            }
            // tables: far to near so the near ones cover
            for (int row = 0; row < 8; row++)
            {
                float z = Mathf.Pow((row + 1) / 8f, 1.7f), y = Mathf.Lerp(hz + H * 0.01f, H * 1.02f, z), rx = Mathf.Lerp(W * 0.012f, W * 0.18f, z), ry = rx * 0.33f;
                foreach (var col in new[] { -2f, -1f, 1f, 2f })
                {
                    float x = W * 0.5f + col * Mathf.Lerp(W * 0.03f, W * 0.42f, z) - Mathf.Sign(col) * Mathf.Lerp(0, W * 0.12f, z);
                    Ellipse(p, x, y + ry * 0.25f, rx * 1.05f, ry * 1.1f, new Color(0.05f, 0.03f, 0.02f));
                    Ellipse(p, x, y, rx, ry, new Color(0.07f, 0.33f, 0.2f));
                    for (int s2 = 0; s2 < 6; s2++) { float a = s2 / 6f * Mathf.PI * 2 + 0.3f; Dot(p, x + Mathf.Cos(a) * rx * 1.15f, y + Mathf.Sin(a) * ry * 1.25f, rx * 0.08f, new Color(0.03f, 0.02f, 0.03f)); }
                }
            }
            // a spotlight on the aisle
            Poly(p, new Color(1f, 0.9f, 0.7f, 0.06f), new Vector2(W * 0.48f, 0), new Vector2(W * 0.52f, 0), new Vector2(W * 0.62f, H), new Vector2(W * 0.38f, H));
        }

        // the Hall of Legends: a marble gallery, gold frames on both walls, light at the far end
        static void Hall(Painter2D p, float W, float H, float t)
        {
            var vp = new Vector2(W * 0.5f, H * 0.45f);
            Rect(p, 0, 0, W, H, new Color(0.08f, 0.06f, 0.1f));
            Poly(p, new Color(0.16f, 0.13f, 0.17f), vp + new Vector2(-W * 0.06f, H * 0.06f), vp + new Vector2(W * 0.06f, H * 0.06f), new Vector2(W, H), new Vector2(0, H));   // the floor
            Glow(p, vp.x, vp.y, H * 0.03f, new Color(1f, 0.9f, 0.7f, 0.9f), 9);
            Rect(p, vp.x - W * 0.06f, vp.y - H * 0.08f, W * 0.12f, H * 0.14f, new Color(1f, 0.92f, 0.75f, 0.35f));
            for (int i = 0; i < 6; i++)
            {
                float z = Mathf.Pow((i + 1) / 6f, 1.6f);
                foreach (var side in new[] { -1f, 1f })
                {
                    float x = vp.x + side * Mathf.Lerp(W * 0.07f, W * 0.48f, z), top = Mathf.Lerp(vp.y - H * 0.08f, H * 0.02f, z), bot = Mathf.Lerp(vp.y + H * 0.06f, H * 0.98f, z), cw2 = Mathf.Lerp(W * 0.006f, W * 0.05f, z);
                    Rect(p, x - cw2 / 2, top, cw2, bot - top, new Color(0.22f, 0.19f, 0.22f));   // a column
                    float fx = x - side * Mathf.Lerp(W * 0.02f, W * 0.13f, z), fw = Mathf.Lerp(W * 0.012f, W * 0.1f, z), fy = Mathf.Lerp(vp.y - H * 0.04f, H * 0.3f, z), fh = fw * 1.25f;
                    Rect(p, fx - fw / 2 - 2, fy - 2, fw + 4, fh + 4, new Color(0.85f, 0.66f, 0.3f));   // a frame
                    Rect(p, fx - fw / 2, fy, fw, fh, new Color(0.12f, 0.1f, 0.14f));
                    Glow(p, fx, fy - H * 0.01f, Mathf.Max(1, fw * 0.08f), new Color(1f, 0.9f, 0.7f, 0.8f), 3);
                }
            }
        }
    }
}
