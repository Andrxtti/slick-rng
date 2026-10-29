using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dream.UI
{
    // Small uGUI builders: every screen is code, laid out for a phone held upright (1080 x 1920 reference).
    public static class Ui
    {
        public static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var go = new GameObject("Text", typeof(RectTransform)); go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>(); t.font = Font; t.text = text; t.fontSize = size; t.color = color; t.alignment = align; t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            Fill((RectTransform)go.transform); return t;
        }

        public static Button Button(Transform parent, string text, Action onClick, Color bg, Color fg, int size = 46)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = bg;
            var b = go.GetComponent<Button>(); b.onClick.AddListener(() => onClick());
            var cb = b.colors; cb.highlightedColor = new Color(1, 1, 1, 0.92f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f); b.colors = cb;
            Label(go.transform, text, size, fg, TextAnchor.MiddleCenter, FontStyle.Bold);
            return b;
        }

        public static void Clear(Transform t) { for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject); }

        public static string Money(double v) => "$" + Math.Round(v).ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
    }
}
