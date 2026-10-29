using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dream.UI
{
    // Your phone: a Messages-style conversation over everything else. Yarn's #text lines arrive one at a time (Story.cs):
    // the other side "types" (three dots) and the bubble rises in; your own texts are typed into the field and sent.
    // Choices asked while the phone is up become reply chips at the bottom. On a wide screen it is a phone in the middle
    // of a dimmed frame; on a phone it is the whole screen.
    public class Phone : MonoBehaviour
    {
        public static Phone I;
        public static string Clock = "11:20 PM";

        static readonly Color Bg = new Color(0.035f, 0.035f, 0.045f), Them = new Color(0.17f, 0.17f, 0.19f),
            Me = new Color(0.16f, 0.47f, 0.96f), Line = new Color(1, 1, 1, 0.08f), Meta = new Color(0.56f, 0.56f, 0.6f);
        // who's who: the name on the thread and the avatar colour (the old game's cast colours)
        static readonly Dictionary<string, (string name, Color c)> Contacts = new Dictionary<string, (string, Color)>
        {
            ["Slick"] = ("Slick", new Color(0.35f, 0.66f, 0.87f)), ["David"] = ("David Pec", new Color(0.36f, 0.75f, 0.39f)),
            ["Lou"] = ("Lou (work)", new Color(0.6f, 0.65f, 0.7f)), ["Mustich"] = ("Mustich", new Color(0.75f, 0.52f, 0.99f)),
        };

        RectTransform device, content, footer; TextMeshProUGUI title, avatarInitial, field, hint; Image avatar;
        string thread; float y; bool tapped;

        public static Phone Open()
        {
            if (I != null) return I;
            var c = Ui.Root("Phone"); c.sortingOrder = 40;
            I = c.gameObject.AddComponent<Phone>(); I.Build(c.transform);
            return I;
        }

        public static void Close() { if (I != null) { Destroy(I.gameObject); I = null; } }

        void Build(Transform root)
        {
            var dim = Ui.Panel(root, "Dim", new Color(0, 0, 0, 0.92f));
            dim.gameObject.AddComponent<Button>().onClick.AddListener(() => tapped = true);
            device = Ui.Tile(dim, "Device", Bg, 0);
            var fit = device.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 1080f / 1920f;
            device.gameObject.AddComponent<RectMask2D>();
            // status bar
            Ui.At(Ui.Label(device, Clock, 30, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold), 0.07f, 0.955f, 0.5f, 0.99f);
            Ui.At(Ui.Label(device, "5G", 26, Color.white, TextAnchor.MiddleRight, FontStyle.Bold), 0.5f, 0.955f, 0.84f, 0.99f);
            var batt = Ui.At(Ui.Tile(device, "Battery", new Color(1, 1, 1, 0.35f), 8), 0.86f, 0.963f, 0.93f, 0.981f);
            Ui.At(Ui.Tile(batt, "Charge", Color.white, 6), 0.06f, 0.12f, 0.62f, 0.88f);
            // the thread's header: back chevron, avatar, name
            var head = Ui.At(Ui.Panel(device, "Header", new Color(0.06f, 0.06f, 0.075f, 0.96f)), 0, 0.855f, 1, 0.955f);
            Ui.At(Ui.Label(head, "‹", 84, Me, TextAnchor.MiddleLeft), 0.04f, 0.1f, 0.2f, 0.95f);
            avatar = Ui.At(Ui.Tile(head, "Avatar", Color.gray, 999).GetComponent<Image>(), 0.45f, 0.42f, 0.55f, 0.98f);
            avatar.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            avatarInitial = Ui.Label(avatar.transform, "", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            title = Ui.At(Ui.Label(head, "", 26, Color.white), 0.2f, 0.04f, 0.8f, 0.4f);
            Ui.At(Ui.Panel(head, "Rule", Line), 0, 0, 1, 0.012f);
            // the conversation, laid out top-down from under the header
            var view = Ui.At(Ui.Box(device, "View"), 0.04f, 0.13f, 0.96f, 0.85f);
            content = Ui.Box(view, "Content"); content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            // the input field, or the reply chips when there's a choice
            footer = Ui.At(Ui.Box(device, "Footer"), 0.04f, 0.015f, 0.96f, 0.125f);
            Field();
            hint = Ui.At(Ui.Label(device, "", 24, new Color(1, 1, 1, 0.4f)), 0, 0.125f, 1, 0.15f);
        }

        void Field()
        {
            Ui.Clear(footer); footer.anchorMax = new Vector2(0.96f, 0.125f);
            var bar = Ui.At(Ui.Tile(footer, "Field", new Color(1, 1, 1, 0.07f), 999), 0, 0.22f, 0.86f, 0.74f);
            field = Ui.At(Ui.Label(bar, "Text Message", 32, Meta, TextAnchor.MiddleLeft), 0.07f, 0, 0.93f, 1);
            var send = Ui.At(Ui.Tile(footer, "Send", Me, 999), 0.89f, 0.25f, 1f, 0.71f);
            send.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            Ui.Label(send, "↑", 40, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        void Thread(string who)
        {
            if (thread == who) return;
            thread = who; Ui.Clear(content); y = 0;
            (string name, Color c) c = Contacts.TryGetValue(who, out var k) ? k : (who, Color.gray);
            title.text = c.name + "  ›"; avatar.color = c.c; avatarInitial.text = c.name.Substring(0, 1);
            Stamp("Today " + Clock);
        }

        void Stamp(string text)
        {
            var t = Ui.Label(content, text, 24, Meta); Place((RectTransform)t.transform, 0, 1, 60);
            int sp = text.IndexOf(' '); if (sp > 0) t.text = "<b>" + text.Substring(0, sp) + "</b>" + text.Substring(sp);
        }

        // rows go down the content box; when it runs past the view, the conversation scrolls up
        void Place(RectTransform r, float x0, float x1, float h)
        {
            r.anchorMin = new Vector2(x0, 1); r.anchorMax = new Vector2(x1, 1); r.pivot = new Vector2(0.5f, 1);
            r.offsetMin = new Vector2(0, -y - h); r.offsetMax = new Vector2(0, -y); y += h + 14;
            var view = (RectTransform)content.parent; float over = y - view.rect.height;
            if (over > 0) StartCoroutine(Scroll(over));
        }
        IEnumerator Scroll(float to)
        {
            float from = content.anchoredPosition.y;
            for (float t = 0; t < 0.3f; t += Time.unscaledDeltaTime) { content.anchoredPosition = new Vector2(0, Mathf.Lerp(from, to, t / 0.3f)); yield return null; }
            content.anchoredPosition = new Vector2(0, to);
        }

        RectTransform Bubble(string text, bool mine)
        {
            float maxW = ((RectTransform)content.parent).rect.width * 0.74f;
            var g = new GameObject("Bubble", typeof(RectTransform), typeof(Image)); g.transform.SetParent(content, false);
            var img = Ui.Rounded(g.GetComponent<Image>(), 40); img.color = mine ? Me : Them;
            var t = Ui.Label(g.transform, text, 36, Color.white, TextAnchor.MiddleLeft);
            var size = t.GetPreferredValues(text, maxW - 56, 0); float w = Mathf.Min(maxW, size.x + 58), h = size.y + 40;
            var r = (RectTransform)g.transform; r.anchorMin = r.anchorMax = new Vector2(mine ? 1 : 0, 1); r.pivot = new Vector2(mine ? 1 : 0, 1);
            r.sizeDelta = new Vector2(w, h); r.anchoredPosition = new Vector2(0, -y);
            var tr = (RectTransform)t.transform; tr.offsetMin = new Vector2(28, 16); tr.offsetMax = new Vector2(-28, -18);
            y += h + 10;
            var view = (RectTransform)content.parent; float over = y - view.rect.height;
            if (over > 0) StartCoroutine(Scroll(over));
            StartCoroutine(Rise(r));
            return r;
        }
        IEnumerator Rise(RectTransform r)
        {
            var cg = r.gameObject.AddComponent<CanvasGroup>(); var p = r.anchoredPosition;
            for (float t = 0; t < 0.22f; t += Time.unscaledDeltaTime) { float k = t / 0.22f; cg.alpha = k; r.anchoredPosition = p - new Vector2(0, 24 * (1 - k)); yield return null; }
            cg.alpha = 1; r.anchoredPosition = p;
        }

        // ------------------------------------------------------------ what Story.cs asks for
        // a text arrives (from someone) or is sent (by you, to someone); done when it's on screen and has been read
        public void Text(string from, string to, string text, Action done) => StartCoroutine(Run(from, to, text, done));
        IEnumerator Run(string from, string to, string text, Action done)
        {
            bool mine = from == "You";
            Thread(mine ? to ?? thread ?? "Slick" : from);
            if (field == null) Field();
            hint.text = ""; tapped = false;
            if (mine)
            {
                for (int i = 1; i <= text.Length && !tapped; i++) { field.text = text.Substring(0, i); field.color = Color.white; yield return new WaitForSecondsRealtime(0.035f); }
                field.text = text; yield return Wait(0.35f);
                field.text = "Text Message"; field.color = Meta;
                Bubble(text, true);
                var d = Ui.Label(content, "Delivered", 22, Meta, TextAnchor.MiddleRight); var dr = (RectTransform)d.transform;
                dr.anchorMin = dr.anchorMax = new Vector2(1, 1); dr.pivot = new Vector2(1, 1); dr.sizeDelta = new Vector2(300, 30); dr.anchoredPosition = new Vector2(-6, -y + 4); y += 26;
            }
            else
            {
                var dots = Bubble("<alpha=#FF>•<alpha=#55>•<alpha=#55>•", false);
                var dt = dots.GetComponentInChildren<TextMeshProUGUI>();
                float typing = Mathf.Clamp(0.6f + text.Length / 60f, 0.8f, 1.8f);
                for (float t = 0; t < typing && !tapped; t += Time.unscaledDeltaTime)
                {
                    int k = (int)(t / 0.22f) % 3;
                    dt.text = string.Concat(new[] { 0, 1, 2 }.Select(i => i == k ? "<alpha=#FF>•" : "<alpha=#55>•"));
                    yield return null;
                }
                y -= dots.sizeDelta.y + 10; Destroy(dots.gameObject);
                Bubble(text, false);
            }
            tapped = false;
            yield return Wait(Mathf.Clamp(0.9f + text.Length / 22f, 1.4f, 4.5f));
            done();
        }
        IEnumerator Wait(float s) { for (float t = 0; t < s && !tapped; t += Time.unscaledDeltaTime) yield return null; tapped = false; }

        // a choice on the phone: an optional heading (e.g. "Decision time."), then one chip per option, hint under each
        public void Choose(string heading, List<string> options, List<string> hints, Action<int> pick)
        {
            Ui.Clear(footer); hint.text = ""; field = null;
            var f = (RectTransform)footer; f.anchorMax = new Vector2(0.96f, 0.13f + 0.125f * options.Count + (heading != null ? 0.05f : 0));
            float step = 1f / (options.Count + (heading != null ? 0.45f : 0));
            float top = 1;
            if (heading != null) { Ui.At(Ui.Label(footer, heading.ToUpperInvariant(), 28, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 0, 1 - step * 0.45f, 1, 1); top -= step * 0.45f; }
            for (int i = 0; i < options.Count; i++)
            {
                int k = i; float y1 = top - i * step, y0 = y1 - step * 0.9f;
                var b = Ui.Button(footer, options[i], () => { field = null; Field(); pick(k); }, i == 0 ? Me : new Color(1, 1, 1, 0.1f), Color.white, 34);
                Ui.At(b, 0.02f, y0 + step * 0.3f, 0.98f, y1);
                if (i < hints.Count) Ui.At(Ui.Label(footer, hints[i], 24, Meta), 0, y0, 1, y0 + step * 0.3f);
            }
        }

        // the end of a conversation: wait for a tap before whatever comes next takes the screen
        public void Done(Action next) => StartCoroutine(Tap(next));
        IEnumerator Tap(Action next)
        {
            hint.text = "tap to continue"; tapped = false;
            while (!tapped) yield return null;
            Close(); next();
        }
    }
}
