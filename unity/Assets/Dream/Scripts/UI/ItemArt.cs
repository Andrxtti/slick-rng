// A picture of every closet item: clothes as flat-lays, hats and glasses from the front, jewellery, watches,
// patches, homes and rides. Drawn as vectors in an 80 x 70 box, in the piece's own colourway and metal.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace CFDream.UI
{
    public class ItemArt : VisualElement
    {
        const string Tee = "M22 8L31 4Q40 11 49 4L58 8L74 22L66 32L59 27V66H21V27L14 32L6 22Z";
        const string Long = "M22 10L30 6Q40 13 50 6L58 10L72 28V58H63L60 32V66H20V32L17 58H8V28Z";
        const string VestP = "M26 6Q40 16 54 6L58 10Q56 22 60 30V66H20V30Q24 22 22 10Z";
        static readonly HashSet<string> TeeShape = new HashSet<string> { "tee", "jersey" };
        static readonly HashSet<string> Suit = new HashSet<string> { "blazer", "suit", "designer", "tux" };

        readonly string slot, id; readonly Color? dye; readonly Color metal; readonly Dictionary<string, object> patch;
        Vector2 o; float k;

        public ItemArt(string slot, string id, Color? dye, Color metal, float w, float h)
        {
            this.slot = slot; this.id = id ?? "none"; this.dye = dye; this.metal = metal;
            style.width = w; style.height = h; style.flexShrink = 0; pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
            if ((slot == "patch" || slot == "sleeve") && this.id != "none")
            {
                patch = J.O(DB.Obj("DR_PATCH"), this.id);
                var t = U.T(J.S(patch, "t", this.id.ToUpperInvariant()), "patch-t", Fonts.Bold).NoPick();
                t.style.position = Position.Absolute; t.style.left = 0; t.style.right = 0; t.style.top = 0; t.style.bottom = 0;
                t.style.unityTextAlign = TextAnchor.MiddleCenter; t.style.fontSize = Mathf.Max(7, w * 0.1f);
                var bg = C.H(J.S(patch, "c", "#d9b36c")); t.style.color = bg.grayscale > 0.55f ? C.Ink : C.Cream;
                Add(t);
            }
        }

        // ------------------------------------------------------------ drawing helpers
        Vector2 M(Vector2 v) => o + v * k;
        void F(Painter2D p, string d, Color c) { p.BeginPath(); VPath.Trace(p, d, M); p.fillColor = c; p.Fill(); }
        void S(Painter2D p, string d, Color c, float w) { p.BeginPath(); VPath.Trace(p, d, M); p.strokeColor = c; p.lineWidth = w * k; p.lineCap = LineCap.Round; p.lineJoin = LineJoin.Round; p.Stroke(); }
        void Dot(Painter2D p, float x, float y, float r, Color c) { p.BeginPath(); p.Arc(M(new Vector2(x, y)), r * k, Angle.Degrees(0), Angle.Degrees(360)); p.fillColor = c; p.Fill(); }
        void Ring(Painter2D p, float x, float y, float r, Color c, float w) { p.BeginPath(); p.Arc(M(new Vector2(x, y)), r * k, Angle.Degrees(0), Angle.Degrees(360)); p.strokeColor = c; p.lineWidth = w * k; p.Stroke(); }
        void Oval(Painter2D p, float x, float y, float rx, float ry, Color? fill, Color? line = null, float w = 1)
        {
            const float m = 0.5523f;
            p.BeginPath();
            p.MoveTo(M(new Vector2(x + rx, y)));
            p.BezierCurveTo(M(new Vector2(x + rx, y + ry * m)), M(new Vector2(x + rx * m, y + ry)), M(new Vector2(x, y + ry)));
            p.BezierCurveTo(M(new Vector2(x - rx * m, y + ry)), M(new Vector2(x - rx, y + ry * m)), M(new Vector2(x - rx, y)));
            p.BezierCurveTo(M(new Vector2(x - rx, y - ry * m)), M(new Vector2(x - rx * m, y - ry)), M(new Vector2(x, y - ry)));
            p.BezierCurveTo(M(new Vector2(x + rx * m, y - ry)), M(new Vector2(x + rx, y - ry * m)), M(new Vector2(x + rx, y)));
            p.ClosePath();
            if (fill.HasValue) { p.fillColor = fill.Value; p.Fill(); }
            if (line.HasValue) { p.strokeColor = line.Value; p.lineWidth = w * k; p.Stroke(); }
        }
        void Box(Painter2D p, float x, float y, float w, float h, float r, Color c) { p.BeginPath(); VPath.RoundRect(p, M(new Vector2(x, y)), w * k, h * k, r * k); p.fillColor = c; p.Fill(); }
        static Color Dk(Color c, float t = 0.28f) => Color.Lerp(c, Color.black, t);
        static Color Lt(Color c, float t = 0.25f) => Color.Lerp(c, Color.white, t);
        static Vector2 Quad(Vector2 a, Vector2 b, Vector2 c, float t) => (1 - t) * (1 - t) * a + 2 * (1 - t) * t * b + t * t * c;

        void Draw(MeshGenerationContext mgc)
        {
            var r = contentRect; if (r.width < 4 || r.height < 4) return;
            k = Mathf.Min(r.width / 80f, r.height / 70f); o = new Vector2(r.x + (r.width - 80 * k) / 2, r.y + (r.height - 70 * k) / 2);
            var p = mgc.painter2D;
            if (id == "none") { Ring(p, 40, 35, 16, C.Faint, 2); S(p, "M29 46L51 24", C.Faint, 2); return; }
            switch (slot)
            {
                case "outfit": Outfit(p); break;
                case "head": Hat(p); break;
                case "eyes": Glasses(p); break;
                case "ear": Earrings(p); break;
                case "chain": Chain(p); break;
                case "ring": RingArt(p); break;
                case "wrist": Bracelet(p); break;
                case "watch": Watch(p); break;
                case "patch": Box(p, 12, 18, 56, 34, 6, C.H(J.S(patch, "c", "#d9b36c"))); Box(p, 15, 21, 50, 28, 4, C.A(Color.white, 0.08f)); break;
                case "sleeve": F(p, "M40 6L62 14V32C62 48 52 60 40 66C28 60 18 48 18 32V14Z", C.H(J.S(patch, "c", "#d9b36c"))); break;
                case "home": Home(p); break;
                case "ride": Ride(p); break;
            }
        }

        // ------------------------------------------------------------ clothes
        void Outfit(Painter2D p)
        {
            Color c = dye ?? C.H(Portrait.OutfitBase.TryGetValue(id, out var h) ? h : "#3b3f47"), d = Dk(c), l = Lt(c), white = C.H("#efece6");
            if (id == "vampire")
            {
                F(p, "M28 6Q40 14 52 6L62 10L76 66H4L18 10Z", c);
                F(p, "M30 12L40 16L50 12L58 66H22Z", C.H("#5e1b2d"));
                F(p, "M22 0L28 10L40 14L52 10L58 0L50 6Q40 10 30 6Z", d);
                return;
            }
            if (id == "vest") { F(p, Long, white); F(p, VestP, c); S(p, "M31 7L40 26L49 7", d, 1.5f); Dot(p, 40, 36, 1.6f, d); Dot(p, 40, 46, 1.6f, d); Dot(p, 40, 56, 1.6f, d); return; }
            F(p, TeeShape.Contains(id) ? Tee : Long, c);
            switch (id)
            {
                case "tee": S(p, "M31 4Q40 11 49 4", l, 1.5f); break;
                case "jersey": S(p, "M31 4Q40 11 49 4", white, 2); S(p, "M33 30H47L38 56", white, 4); break;
                case "hoodie":
                    F(p, "M28 8Q40 21 52 8Q48 1 40 1Q32 1 28 8Z", d); F(p, "M29 46H51L54 60H26Z", Dk(c, 0.14f));
                    S(p, "M37 12L36 26M43 12L44 26", white, 1.2f); break;
                case "fleece": F(p, "M30 3H50V11H30Z", d); S(p, "M40 6V40", l, 1.5f); S(p, "M26 44L30 54M54 44L50 54", d, 1.5f); break;
                case "track":
                    S(p, "M40 7V66", white, 1.5f);
                    S(p, "M11 30L10 56M14 30L13 56M69 30L70 56M66 30L67 56", white, 1.6f); break;
                case "flannel":
                    var ln = C.A(Color.black, 0.25f);
                    S(p, "M28 12V66M40 14V66M52 12V66M20 26H60M20 40H60M20 54H60M12 34L16 34M64 34L68 34", ln, 1.6f);
                    F(p, "M31 5L40 12L35 16Z", d); F(p, "M49 5L40 12L45 16Z", d); break;
                case "cardigan": S(p, "M31 6L40 30L49 6", d, 2); Dot(p, 40, 38, 1.7f, l); Dot(p, 40, 48, 1.7f, l); Dot(p, 40, 58, 1.7f, l); break;
                case "office": F(p, "M31 5L40 12L35 16Z", Dk(c, 0.12f)); F(p, "M49 5L40 12L45 16Z", Dk(c, 0.12f)); F(p, "M38 12H42L44 40L40 46L36 40Z", C.H("#24427e")); break;
                case "denim":
                    F(p, "M30 5L40 12L50 5L52 12L40 16L28 12Z", d); S(p, "M40 16V66", l, 1.2f);
                    S(p, "M26 22H36V30H26ZM44 22H54V30H44Z", l, 1.2f); break;
                case "varsity":
                    F(p, "M22 10L8 28V58H17L20 32Z", C.H("#efe3c8")); F(p, "M58 10L72 28V58H63L60 32Z", C.H("#efe3c8"));
                    S(p, "M8 54H17M63 54H72M20 62H60", d, 2.5f); S(p, "M31 7Q40 13 49 7", white, 2); break;
                case "leather": F(p, "M30 6L36 24L40 14L44 24L50 6L44 9L40 12L36 9Z", d); S(p, "M44 24L40 62", metal, 1.4f); break;
                case "bomber": Box(p, 20, 60, 40, 6, 1, d); Box(p, 8, 54, 9, 4, 1, d); Box(p, 63, 54, 9, 4, 1, d); S(p, "M40 8V60", metal, 1.4f); F(p, "M31 5Q40 12 49 5L48 9Q40 14 32 9Z", d); break;
                case "polo": F(p, "M31 3H49V11H31Z", d); S(p, "M40 6V30", C.H("#c9c3b8"), 1.5f); Box(p, 38.6f, 28, 2.8f, 5, 1, C.H("#c9c3b8")); break;
                case "ugly":
                    S(p, "M20 30L26 36L32 30L38 36L44 30L50 36L56 30L60 33", C.H("#efe3c8"), 2);
                    S(p, "M20 46H60", C.H("#b3262c"), 3); Dot(p, 28, 54, 1.6f, C.H("#efe3c8")); Dot(p, 40, 56, 1.6f, C.H("#efe3c8")); Dot(p, 52, 54, 1.6f, C.H("#efe3c8")); break;
                default:
                    if (Suit.Contains(id))
                    {
                        F(p, "M33 6L40 26L47 6Z", white);
                        if (id == "tux") F(p, "M35 10L40 13L45 10V16L40 13L35 16Z", C.H("#0a0a0a"));
                        else if (id != "blazer") F(p, "M38.5 9H41.5L42.5 26L40 29L37.5 26Z", C.H(id == "designer" ? "#c9a03e" : "#7a1f2b"));
                        F(p, "M33 6L38 30L40 26L36 8Z", d); F(p, "M47 6L42 30L40 26L44 8Z", d);
                        Dot(p, 40, 40, 1.6f, d); Dot(p, 40, 50, 1.6f, d);
                    }
                    else S(p, "M31 7Q40 13 49 7", d, 1.5f);
                    break;
            }
        }

        // ------------------------------------------------------------ headwear
        void Hat(Painter2D p)
        {
            Color c = dye ?? C.H(Portrait.HatBase.TryGetValue(id, out var h) ? h : "#222222"), d = Dk(c, 0.3f);
            switch (id)
            {
                case "cap": F(p, "M14 46C14 24 26 14 40 14C54 14 66 24 66 46Z", c); F(p, "M10 44H70Q76 50 66 54H14Q4 50 10 44Z", d); Dot(p, 40, 14, 2.6f, d); S(p, "M40 16V44", Dk(c, 0.15f), 1.2f); break;
                case "snap": F(p, "M14 44C14 22 26 12 40 12C54 12 66 22 66 44Z", c); F(p, "M8 42H72V49H8Z", d); Dot(p, 40, 12, 2.6f, d); break;
                case "beanie": F(p, "M16 50C16 26 26 10 40 10C54 10 64 26 64 50Z", c); Box(p, 13, 42, 54, 16, 4, d); S(p, "M22 46V54M30 46V54M38 46V54M46 46V54M54 46V54", Dk(c, 0.45f), 1); break;
                case "bucket": F(p, "M20 40L24 14H56L60 40Z", c); F(p, "M6 50L20 38H60L74 50Z", d); break;
                case "cowboy": Oval(p, 40, 46, 37, 8, d); F(p, "M22 46L24 18Q32 12 40 18Q48 12 56 18L58 46Z", c); Box(p, 23, 36, 34, 5, 1, C.H("#3a2416")); break;
                case "fedora": Oval(p, 40, 48, 31, 7, d); F(p, "M22 48L25 22Q40 13 55 22L58 48Z", c); Box(p, 23, 39, 34, 6, 1, C.H("#101010")); break;
                case "phones":
                    S(p, "M14 44C14 14 66 14 66 44", C.H("#202024"), 5);
                    Oval(p, 14, 46, 8, 12, C.H("#202024")); Oval(p, 66, 46, 8, 12, C.H("#202024"));
                    Oval(p, 14, 46, 4, 6, metal); Oval(p, 66, 46, 4, 6, metal); break;
                case "visor": Box(p, 14, 28, 52, 7, 2, c); F(p, "M10 35H70Q76 44 64 46H16Q4 44 10 35Z", d); break;
                case "pumpkin":
                    Oval(p, 40, 42, 27, 21, C.H("#d98a2b"));
                    S(p, "M40 22V62M28 24Q22 42 28 60M52 24Q58 42 52 60", C.H("#a8621a"), 1.6f); Box(p, 37, 12, 6, 10, 2, C.H("#3c6b2a")); break;
                case "pilgrim":
                    Oval(p, 40, 54, 31, 6, C.H("#141414")); F(p, "M24 54L28 12H52L56 54Z", C.H("#1b1b1b"));
                    Box(p, 26, 42, 28, 7, 1, C.H("#2b2b2b")); S(p, "M36 41H44V50H36Z", C.H("#e3b94f"), 1.6f); break;
                case "santa":
                    F(p, "M16 50L44 12Q56 10 58 20L64 50Z", C.H("#c8242c")); Box(p, 11, 45, 58, 11, 5, C.H("#f3f1ea")); Dot(p, 58, 18, 6, C.H("#f3f1ea")); break;
                default: Oval(p, 40, 40, 24, 16, c); break;
            }
        }

        // ------------------------------------------------------------ eyewear
        void Glasses(Painter2D p)
        {
            Color fr = C.H("#151515"); Color? lens = C.A(Color.white, 0.08f); float w = 2;
            switch (id)
            {
                case "readers": fr = C.H("#8a6a45"); w = 1.4f; break;
                case "round": fr = metal; w = 1.4f; break;
                case "thick": w = 3.6f; break;
                case "tinted": fr = C.H("#5a5f68"); w = 1.4f; lens = C.A(C.H("#d07a3a"), 0.55f); break;
                case "aviator": fr = C.H("#d8c27a"); w = 1.4f; lens = C.A(C.H("#2b3a32"), 0.85f); break;
                case "shades": w = 3.2f; lens = C.H("#0d0d10"); break;
                case "amber": fr = C.H("#5a3a22"); w = 3f; lens = C.A(C.H("#d98a2b"), 0.75f); break;
            }
            switch (id)
            {
                case "sport":
                    F(p, "M6 30Q40 22 74 30Q72 46 56 46Q46 46 40 40Q34 46 24 46Q8 46 6 30Z", C.A(C.H("#2a54b8"), 0.9f));
                    S(p, "M6 30Q40 22 74 30Q72 46 56 46Q46 46 40 40Q34 46 24 46Q8 46 6 30Z", C.H("#e9e7e1"), 2.4f);
                    S(p, "M18 30Q24 28 30 30", C.A(Color.white, 0.6f), 1.5f); return;
                case "snow":
                    S(p, "M2 34H8M72 34H78", C.H("#24427e"), 5);
                    F(p, "M8 24Q40 16 72 24V44Q40 52 8 44Z", C.H("#e07a2c")); S(p, "M8 24Q40 16 72 24V44Q40 52 8 44Z", C.H("#efeee9"), 3);
                    S(p, "M16 30Q30 26 44 28", C.A(Color.white, 0.55f), 2); return;
                case "masq":
                    F(p, "M4 30Q20 18 40 28Q60 18 76 30Q74 48 58 48Q46 48 40 40Q34 48 22 48Q6 48 4 30Z", C.H("#3a1458"));
                    Oval(p, 24, 35, 9, 6, C.Card); Oval(p, 56, 35, 9, 6, C.Card);
                    S(p, "M4 30Q20 18 40 28Q60 18 76 30", C.H("#c9a03e"), 1.6f); return;
            }
            foreach (int sx in new[] { -1, 1 })
            {
                float x = 40 + sx * 16;
                if (id == "aviator")
                {
                    string drop = sx < 0 ? "M12 26H36Q38 44 26 48Q12 48 12 26Z" : "M44 26H68Q68 48 54 48Q42 44 44 26Z";
                    if (lens.HasValue) F(p, drop, lens.Value); S(p, drop, fr, w);
                }
                else if (id == "thick" || id == "shades")
                {
                    if (lens.HasValue) Box(p, x - 14, 26, 28, 20, 6, lens.Value);
                    p.BeginPath(); VPath.RoundRect(p, M(new Vector2(x - 14, 26)), 28 * k, 20 * k, 6 * k); p.strokeColor = fr; p.lineWidth = w * k; p.Stroke();
                }
                else
                {
                    float rx = id == "round" ? 12 : 13, ry = id == "round" ? 12 : id == "readers" ? 8 : 10;
                    Oval(p, x, 36, rx, ry, lens, fr, w);
                }
            }
            S(p, id == "aviator" ? "M36 28Q40 25 44 28M36 32Q40 30 44 32" : "M35 33Q40 29 45 33", fr, Mathf.Max(1.4f, w * 0.7f));
            S(p, "M2 30L10 32M78 30L70 32", fr, Mathf.Max(1.4f, w * 0.7f));
        }

        // ------------------------------------------------------------ jewellery and watches
        void Earrings(Painter2D p)
        {
            foreach (float x in new[] { 26f, 54f })
            {
                if (id == "hoop") Ring(p, x, 38, 10, metal, 2.6f);
                else if (id == "diamond")
                {
                    bool left = x < 40;
                    F(p, left ? "M26 24L33 33L26 44L19 33Z" : "M54 24L61 33L54 44L47 33Z", C.H("#dff1ff"));
                    S(p, left ? "M19 33H33M26 24V44" : "M47 33H61M54 24V44", C.A(Color.white, 0.8f), 1);
                }
                else { Dot(p, x, 34, 5, metal); Dot(p, x - 1.5f, 32.5f, 1.6f, C.A(Color.white, 0.7f)); }
            }
        }

        void Chain(Painter2D p)
        {
            Vector2 a = new Vector2(12, 8), b = new Vector2(40, 72), c = new Vector2(68, 8);
            string u = "M12 8Q40 72 68 8";
            switch (id)
            {
                case "bones":
                case "lights":
                    S(p, u, C.H("#2a2a30"), 1.2f);
                    Color[] bulbs = { C.H("#e8433a"), C.H("#3cbc5a"), C.H("#f2c230"), C.H("#3a7fe0") };
                    for (int i = 1; i < 12; i++)
                    {
                        var q = Quad(a, b, c, i / 12f);
                        if (id == "bones") Box(p, q.x - 4, q.y - 1.6f, 8, 3.2f, 1.5f, C.H("#ece6d6"));
                        else Dot(p, q.x, q.y, 2.8f, bulbs[i % 4]);
                    }
                    return;
                case "layered": S(p, "M14 6Q40 52 66 6", metal, 1.8f); S(p, "M10 8Q40 80 70 8", metal, 2.4f); return;
                case "wishbone": S(p, u, metal, 1.4f); S(p, "M34 40L40 54L46 40", metal, 2.4f); return;
                case "iced":
                    S(p, u, C.H("#eef6ff"), 5);
                    for (int i = 1; i < 14; i++) { var q = Quad(a, b, c, i / 14f); Dot(p, q.x, q.y, 1.5f, Color.white); }
                    return;
                default: S(p, u, metal, id == "cuban" ? 5.5f : id == "rope" ? 3.6f : 2.2f); if (id == "rope") S(p, u, Dk(metal, 0.25f), 1); return;
            }
        }

        void RingArt(Painter2D p)
        {
            bool pinky = id == "pinky", champ = id == "champ";
            float rx = pinky ? 15 : 20, ry = pinky ? 9 : 12;
            Oval(p, 40, 44, rx, ry, null, metal, champ ? 7 : 5);
            Oval(p, 40, 44, rx, ry, null, C.A(Color.white, 0.25f), 1);
            if (id == "signet" || pinky) Box(p, 40 - (pinky ? 6 : 9), 26, pinky ? 12 : 18, 10, 3, metal);
            if (champ) { Dot(p, 40, 26, 11, metal); Dot(p, 40, 26, 7, C.H("#5aa9de")); Dot(p, 37.5f, 23.5f, 2, C.A(Color.white, 0.8f)); }
        }

        void Bracelet(Painter2D p)
        {
            if (id == "beads") { for (int i = 0; i < 14; i++) { float t = i / 14f * Mathf.PI * 2; Dot(p, 40 + Mathf.Cos(t) * 26, 36 + Mathf.Sin(t) * 18, 4.6f, C.H("#6b4a2e")); } return; }
            Oval(p, 40, 36, 26, 18, null, metal, id == "tennis" ? 3 : 4.5f);
            if (id == "tennis") for (int i = 0; i < 18; i++) { float t = i / 18f * Mathf.PI * 2; Dot(p, 40 + Mathf.Cos(t) * 26, 36 + Mathf.Sin(t) * 18, 1.7f, Color.white); }
        }

        void Watch(Painter2D p)
        {
            bool dig = id == "digital", iced = id == "iced", hyper = id == "hyper";
            Color strap = dig || hyper ? C.H("#18181b") : iced ? C.H("#eef6ff") : metal;
            Box(p, 31, 0, 18, 70, 3, strap);
            if (dig) { Box(p, 24, 20, 32, 30, 6, C.H("#202024")); Box(p, 28, 25, 24, 18, 2, C.H("#5ad2c8")); return; }
            Dot(p, 40, 35, 16, hyper ? C.H("#2e2e34") : iced ? C.H("#eef6ff") : metal);
            Dot(p, 40, 35, 12.5f, id == "steel" ? C.H("#1d2a3d") : id == "gold" ? C.H("#f1e6c8") : hyper ? C.H("#0d0d0f") : C.H("#e9eef5"));
            Color hand = id == "gold" ? C.H("#1a1a1a") : hyper ? C.H("#e8433a") : metal;
            S(p, "M40 35V26M40 35L46 38", hand, 1.8f);
            if (iced) for (int i = 0; i < 12; i++) { float t = i / 12f * Mathf.PI * 2; Dot(p, 40 + Mathf.Cos(t) * 14.2f, 35 + Mathf.Sin(t) * 14.2f, 1.2f, Color.white); }
        }

        // ------------------------------------------------------------ homes and rides
        void Home(Painter2D p)
        {
            Color wall = C.H("#3b3f47"), win = C.H("#e8c46a"), dk = C.H("#262a31");
            void Windows(float x0, float y0, int cols, int rows, float dx, float dy) { for (int i = 0; i < cols; i++) for (int j = 0; j < rows; j++) Box(p, x0 + i * dx, y0 + j * dy, 4, 4, 0.6f, (i + j) % 3 == 0 ? C.A(win, 0.35f) : win); }
            switch (id)
            {
                case "studio": Box(p, 22, 28, 36, 36, 2, wall); Windows(27, 34, 3, 3, 10, 9); Box(p, 36, 54, 8, 10, 1, dk); break;
                case "apt": Box(p, 18, 12, 44, 54, 2, wall); Windows(23, 17, 4, 5, 10, 9); break;
                case "condo": Box(p, 26, 4, 28, 62, 2, wall); Windows(30, 9, 3, 6, 8, 9); break;
                case "penthouse": Box(p, 26, 10, 28, 56, 2, wall); Box(p, 22, 4, 36, 8, 2, C.H("#c9a03e")); Windows(30, 16, 3, 5, 8, 9); break;
                case "mansion": F(p, "M6 34L40 12L74 34V64H6Z", C.H("#e9e3d6")); S(p, "M20 36V62M30 36V62M50 36V62M60 36V62", C.H("#b9b1a0"), 2.4f); Box(p, 35, 46, 10, 18, 1, dk); break;
                case "island":
                    F(p, "M0 58Q20 52 40 56Q60 60 80 54V70H0Z", C.H("#2f8fbf")); F(p, "M18 58Q40 44 64 58Z", C.H("#e7d3a1"));
                    S(p, "M42 52Q44 32 40 22", C.H("#7a5a32"), 3); F(p, "M40 22Q28 18 22 26Q32 22 40 24Q46 14 58 16Q48 18 42 24Q52 26 56 34Q46 28 40 24Z", C.H("#3c8a3a")); break;
                default: Box(p, 22, 28, 36, 36, 2, wall); break;
            }
        }

        void Ride(Painter2D p)
        {
            Color body = C.H(id == "bus" ? "#2f6fb0" : id == "coupe" ? "#b3262c" : id == "sedan" ? "#1b2442" : id == "super" ? "#d98a2b" : id == "jet" ? "#e9e7e1" : "#8a8f98");
            Color glass = C.H("#9fc3df");
            void Wheel(float x, float y) { Dot(p, x, y, 7, C.H("#121214")); Dot(p, x, y, 3, C.H("#8a8f98")); }
            switch (id)
            {
                case "bus": Box(p, 6, 18, 68, 36, 4, body); for (int i = 0; i < 5; i++) Box(p, 10 + i * 12, 23, 9, 10, 1.5f, glass); Wheel(18, 54); Wheel(62, 54); break;
                case "hatch": F(p, "M8 50V40L18 28H44L54 38H70Q74 40 74 46V50Z", body); F(p, "M20 31H42L50 38H17Z", glass); Wheel(22, 50); Wheel(60, 50); break;
                case "coupe": F(p, "M6 50V44Q14 36 30 34L42 27H54L68 38Q74 40 74 46V50Z", body); F(p, "M33 35L43 30H52L60 37Z", glass); Wheel(20, 50); Wheel(60, 50); break;
                case "sedan": F(p, "M6 50V42L16 34L26 27H50L60 34H70Q74 36 74 44V50Z", body); F(p, "M28 30H49L56 35H22Z", glass); Wheel(20, 50); Wheel(60, 50); break;
                case "super": F(p, "M4 50V46L24 37L44 31H52L76 44V50Z", body); F(p, "M36 36L45 33H51L58 38Z", C.H("#1b1b1f")); F(p, "M70 40L78 34V40Z", body); Wheel(18, 50); Wheel(62, 50); break;
                case "jet":
                    F(p, "M4 40L60 34Q74 34 77 38Q74 42 60 42Z", body); F(p, "M30 38L46 22H52L44 38Z", Dk(body, 0.1f)); F(p, "M30 40L44 56H50L42 40Z", Dk(body, 0.1f));
                    F(p, "M8 40L4 28H10L18 38Z", Dk(body, 0.1f)); for (int i = 0; i < 6; i++) Dot(p, 30 + i * 5, 37, 1.1f, C.H("#3a5a7a")); break;
                default: Box(p, 10, 30, 60, 20, 6, body); Wheel(22, 50); Wheel(58, 50); break;
            }
        }
    }
}
