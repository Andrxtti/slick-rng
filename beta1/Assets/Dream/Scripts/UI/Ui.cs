using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dream.UI
{
    public enum Face { Body, Display, Mono }

    // Small uGUI builders: every screen is code, laid out for a phone held upright (1080 x 1920 reference).
    // Type is TextMeshPro in the brand fonts (all OFL, Resources/Fonts, licences in Assets/Dream/Fonts-Licenses):
    // Gloock for display (titles, big numbers in a story beat), Inter for everything you read, IBM Plex Mono for money
    // and counts. The scale, at the 1080-wide reference: Display 132 / 96 / 72 / 56, Body 44 lead / 36 / 30 / 26 small /
    // 22 micro, labels in caps get +8 tracking. Bold asks for Inter SemiBold (Bold only at 56 and up).
    public static class Ui
    {
        static readonly Dictionary<string, TMP_FontAsset> fonts = new Dictionary<string, TMP_FontAsset>();
        public static TMP_FontAsset Font(string file)
        {
            if (fonts.TryGetValue(file, out var f)) return f;
            var src = Resources.Load<Font>("Fonts/" + file);
            f = src == null ? TMP_Settings.defaultFontAsset : TMP_FontAsset.CreateFontAsset(src, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
            f.name = file; fonts[file] = f;
            // symbols the brand fonts don't draw (★ ✓ suits) fall back to DejaVu; Gloock falls back to Inter first
            if (src != null && file != "DejaVuSans-Bold")
            {
                f.fallbackFontAssetTable = new List<TMP_FontAsset>();
                if (file.StartsWith("Gloock")) f.fallbackFontAssetTable.Add(Font("Inter-Regular"));
                f.fallbackFontAssetTable.Add(Font("DejaVuSans-Bold"));
            }
            return f;
        }
        // emoji have no font on the web: the grade icons get plain glyphs
        public static string Icon(string icon) => icon == "👍" ? "✓✓" : icon;
        public static TMP_FontAsset Body => Font("Inter-Regular"); public static TMP_FontAsset Semi => Font("Inter-SemiBold");
        public static TMP_FontAsset Heavy => Font("Inter-Bold"); public static TMP_FontAsset Display => Font("Gloock-Regular");
        public static TMP_FontAsset Mono => Font("IBMPlexMono-Regular"); public static TMP_FontAsset MonoBold => Font("IBMPlexMono-Bold");
        public static readonly Color Ink = new Color(0.95f, 0.93f, 0.88f), Dim = new Color(0.62f, 0.6f, 0.56f),
            Gold = new Color(0.95f, 0.76f, 0.35f), Night = new Color(0.07f, 0.065f, 0.09f), Card = new Color(0.13f, 0.12f, 0.15f),
            Felt = new Color(0.12f, 0.35f, 0.23f), Neon = new Color(1f, 0.31f, 0.64f), Aqua = new Color(0.18f, 0.83f, 0.78f);

        public static Canvas Root(string name)
        {
            var go = new GameObject(name);
            var c = go.AddComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 5;
            var s = go.AddComponent<CanvasScaler>(); s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1080, 1920); s.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            return c;
        }

        public static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            var r = (RectTransform)go.transform; Fill(r); return r;
        }

        // a white rounded rectangle, 9-sliced, drawn once by code (anti-aliased edge): tint it with Image.color.
        // radius: corner radius in reference pixels
        static Sprite round;
        public static Sprite Round
        {
            get
            {
                if (round != null) return round;
                const int N = 128, R = 60;
                var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color32[N * N];
                for (int y = 0; y < N; y++)
                    for (int x = 0; x < N; x++)
                    {
                        float cx = Mathf.Clamp(x + 0.5f, R, N - R), cy = Mathf.Clamp(y + 0.5f, R, N - R);
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                        px[y * N + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(R - d + 0.5f) * 255));
                    }
                tex.SetPixels32(px); tex.Apply();
                return round = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
            }
        }
        public static Image Rounded(Image img, float radius)
        {
            img.sprite = Round; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 60f / Mathf.Max(1f, radius);
            return img;
        }
        public static RectTransform Tile(Transform parent, string name, Color color, float radius = 28)
        {
            var r = Panel(parent, name, color); Rounded(r.GetComponent<Image>(), radius); return r;
        }

        public static RectTransform Box(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Fill(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

        // place by anchors in 0..1 of the parent (x0, y0, x1, y1), y up
        public static T At<T>(T c, float x0, float y0, float x1, float y1) where T : Component
        {
            var r = (RectTransform)c.transform; r.anchorMin = new Vector2(x0, y0); r.anchorMax = new Vector2(x1, y1); r.offsetMin = r.offsetMax = Vector2.zero;
            return c;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal, Face face = Face.Body)
        {
            var go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            bool bold = style == FontStyle.Bold || style == FontStyle.BoldAndItalic;
            t.font = face == Face.Display ? Display : face == Face.Mono ? (bold ? MonoBold : Mono) : bold ? (size >= 56 ? Heavy : Semi) : Body;
            t.text = text; t.fontSize = size; t.color = color; t.alignment = Align(align); t.raycastTarget = false;
            if (style == FontStyle.Italic || style == FontStyle.BoldAndItalic) t.fontStyle = FontStyles.Italic;
            t.textWrappingMode = TextWrappingModes.Normal; t.overflowMode = TextOverflowModes.Overflow;
            if (face == Face.Body && size <= 30 && text.Length > 1 && text.Length < 40 && text == text.ToUpperInvariant() && text.IndexOfAny("ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray()) >= 0)
                t.characterSpacing = 8;   // caps labels breathe
            if (face == Face.Display) t.characterSpacing = -1;
            if (face == Face.Body && size <= 36) t.lineSpacing = 6;
            Fill((RectTransform)go.transform); return t;
        }
        public static TextMeshProUGUI Title(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter) => Label(parent, text, size, color, align, FontStyle.Normal, Face.Display);
        public static TextMeshProUGUI Num(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal) => Label(parent, text, size, color, align, style, Face.Mono);

        static TextAlignmentOptions Align(TextAnchor a) => a switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft, TextAnchor.UpperCenter => TextAlignmentOptions.Top, TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left, TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft, TextAnchor.LowerCenter => TextAlignmentOptions.Bottom, TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.Center,
        };

        public static Button Button(Transform parent, string text, Action onClick, Color bg, Color fg, int size = 46)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            Rounded(go.GetComponent<Image>(), 34).color = bg;   // soft corners (a 9-slice bigger than the button stretches into an ellipse)
            var b = go.GetComponent<Button>(); b.onClick.AddListener(() => onClick());
            var cb = b.colors; cb.highlightedColor = new Color(1, 1, 1, 0.92f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f); b.colors = cb;
            Label(go.transform, text, size, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            return b;
        }

        // a text field in the display face: a rounded card, the text and its placeholder clipped inside it
        public static TMP_InputField Input(Transform parent, string placeholder, int size)
        {
            var bg = Tile(parent, "Input", Card, 28);
            var area = Box(bg, "Text Area"); Fill(area); area.gameObject.AddComponent<RectMask2D>(); area.offsetMin = new Vector2(32, 6); area.offsetMax = new Vector2(-32, -6);
            var ph = Label(area, placeholder, size, new Color(1, 1, 1, 0.25f), TextAnchor.MiddleLeft, FontStyle.Normal, Face.Display);
            var tx = Label(area, "", size, Ink, TextAnchor.MiddleLeft, FontStyle.Normal, Face.Display);
            ph.textWrappingMode = tx.textWrappingMode = TextWrappingModes.NoWrap;
            var f = bg.gameObject.AddComponent<TMP_InputField>();
            f.textViewport = area; f.textComponent = tx; f.placeholder = ph; f.fontAsset = Display; f.pointSize = size;
            f.caretColor = Gold; f.customCaretColor = true; f.caretWidth = 4; f.selectionColor = new Color(0.95f, 0.76f, 0.35f, 0.35f);
            return f;
        }

        // small motion, cheap on phones: a UI piece slides in from an offset (reference pixels) and fades up; or pulses
        public static void Slide(RectTransform r, Vector2 from, float delay, float time = 0.3f) => Motion.On(r).Go(Motion.SlideIn(r, from, delay, time));
        public static void Pulse(Graphic g, float lo, float hi, float period = 1.4f) => Motion.On(g.rectTransform).Go(Motion.PulseLoop(g, lo, hi, period));

        public static void Clear(Transform t) { for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject); }

        public static string Money(double v) => "$" + Math.Round(v).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }

    public class Motion : MonoBehaviour
    {
        public static Motion On(RectTransform r) => r.GetComponent<Motion>() ?? r.gameObject.AddComponent<Motion>();
        public void Go(System.Collections.IEnumerator e) => StartCoroutine(e);
        public static System.Collections.IEnumerator SlideIn(RectTransform r, Vector2 from, float delay, float time)
        {
            var cg = r.GetComponent<CanvasGroup>() ?? r.gameObject.AddComponent<CanvasGroup>(); var end = r.anchoredPosition;
            cg.alpha = 0; r.anchoredPosition = end + from;
            for (float t = 0; t < delay; t += Time.unscaledDeltaTime) yield return null;
            for (float t = 0; t < time; t += Time.unscaledDeltaTime)
            {
                float k = 1 - Mathf.Pow(1 - t / time, 3); cg.alpha = Mathf.Clamp01(t / (time * 0.6f)); r.anchoredPosition = end + from * (1 - k);
                yield return null;
            }
            cg.alpha = 1; r.anchoredPosition = end;
        }
        public static System.Collections.IEnumerator PulseLoop(Graphic g, float lo, float hi, float period)
        {
            var c = g.color;
            for (float t = 0; ; t += Time.unscaledDeltaTime) { c.a = Mathf.Lerp(lo, hi, 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2 / period)); g.color = c; yield return null; }
        }
    }
}
