using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dream.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Dream.Game
{
    // A scene with no film yet (or one Steven edited since its film) plays as a storyboard: styled cards on the film screen,
    // one per line: the place's still (StreamingAssets/Sets, when there is one), the speaker's portrait and name, the
    // words, the voice when a clip exists. Tap or wait to go on. Made from Resources/Story/scenes.json
    // (tools/storyboard_build.py, from SCRIPT_ALL.md), so a script edit shows up after a rebuild with no render.
    public static class Storyboard
    {
        [Serializable] public class Line { public string who, name, mode, text, part; }
        [Serializable] public class Option { public int n; public string text, go, who, name, mode, say; }
        [Serializable] public class Scene { public string id, title, place, set; public bool edited, film, storyboard; public List<Line> lines; public List<Option> options; }
        [Serializable] class Book { public List<Scene> scenes; }
        static Dictionary<string, Scene> scenes;
        static readonly string[] Portraits = { "slick", "tommy", "mustich", "gnomey", "you" };

        public static Scene Get(string id)
        {
            if (scenes == null)
            {
                var t = Resources.Load<TextAsset>("Story/scenes");
                scenes = t == null ? new Dictionary<string, Scene>() : JsonUtility.FromJson<Book>(t.text).scenes.ToDictionary(s => s.id);
            }
            if (scenes.TryGetValue(id, out var sc)) return sc;
            // a part of a scene with a choice: "id@a" the lines before it, "id@b" after it, "id@2" option 2's answer
            int at = id.IndexOf('@'); if (at < 0 || !scenes.TryGetValue(id.Substring(0, at), out var whole)) return null;
            string part = id.Substring(at + 1);
            var cut = new Scene { id = id, title = whole.title, place = whole.place, set = whole.set, edited = whole.edited, film = false, storyboard = true, options = new List<Option>() };
            if (part.StartsWith("L") && int.TryParse(part.Substring(1), out int ln))   // "id@L3": the scene's line 3 alone
                cut.lines = ln >= 1 && ln <= whole.lines.Count ? new List<Line> { whole.lines[ln - 1] } : new List<Line>();
            else if (int.TryParse(part, out int n))
            {
                var o = whole.options.FirstOrDefault(x => x.n == n);
                cut.lines = o == null || string.IsNullOrEmpty(o.say) ? new List<Line>() : new List<Line> { new Line { who = o.who, name = o.name, mode = o.mode, text = o.say, part = part } };
            }
            else cut.lines = whole.lines.Where(l => (l.part ?? "a") == part).ToList();
            scenes[id] = cut; return cut;
        }
        public static string OptionText(string id, int n) => Get(id)?.options?.FirstOrDefault(o => o.n == n)?.text ?? "";
        public static IEnumerable<Scene> All() { Get(""); return scenes.Values; }
        public static bool Prefer(string id) { var sc = Get(id); return sc != null && sc.storyboard && sc.lines.Count > 0; }

        static bool skip;
        public static void Skip() => skip = true;

        // play the scene's cards inside `screen` (the film screen's black panel); done() when the last one is over
        public static IEnumerator Play(RectTransform screen, string id, Action done)
        {
            var sc = Get(id); skip = false;
            var root = Ui.Box(screen, "Storyboard"); Ui.Fill(root);
            Canvas.ForceUpdateCanvases();
            bool wide = root.rect.width > root.rect.height;
            Texture2D still = null;
            if (!string.IsNullOrEmpty(sc.set))
            {
                var sa = Application.streamingAssetsPath; var url = (sa.Contains("://") ? sa : "file://" + sa) + $"/Sets/{sc.set}_{(wide ? "landscape" : "portrait")}.jpg";
                var req = UnityWebRequestTexture.GetTexture(url); yield return req.SendWebRequest();
                if (req.result == UnityWebRequest.Result.Success) still = DownloadHandlerTexture.GetContent(req);
                req.Dispose();
            }
            var bg = new GameObject("Still", typeof(RectTransform)).AddComponent<RawImage>(); bg.transform.SetParent(root, false); bg.raycastTarget = false;
            if (still != null) { bg.texture = still; var f = bg.gameObject.AddComponent<AspectRatioFitter>(); f.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; f.aspectRatio = (float)still.width / still.height; bg.color = new Color(0.55f, 0.55f, 0.6f); }
            else { Ui.Fill(bg.rectTransform); bg.color = new Color(0.07f, 0.06f, 0.09f); }
            Ui.At(Ui.Label(root, (sc.place ?? "").ToUpperInvariant(), 24, Ui.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 0.05f, 0.92f, 0.95f, 0.96f);
            Ui.At(Ui.Label(root, "STORYBOARD · " + sc.title, 20, new Color(1, 1, 1, 0.45f), TextAnchor.MiddleCenter), 0.05f, 0.885f, 0.95f, 0.92f);
            var tap = root.gameObject.AddComponent<Button>(); bool next = false; tap.onClick.AddListener(() => next = true);
            var hint = Ui.At(Ui.Label(root, "tap to continue", 22, new Color(1, 1, 1, 0.4f)), 0, 0.02f, 1, 0.05f);
            foreach (var line in sc.lines)
            {
                if (skip) break;
                var card = Ui.Box(root, "Card"); Ui.Fill(card);
                bool narr = line.who == "narrator", text = line.mode == "text";
                if (narr)
                {
                    var t = Ui.At(Ui.Title(card, line.text, wide ? 54 : 58, new Color(0.97f, 0.95f, 0.9f)), 0.08f, 0.3f, 0.92f, 0.82f); t.lineSpacing = 10;
                }
                else
                {
                    var box = Ui.At(Ui.Tile(card, "Line", new Color(0.05f, 0.05f, 0.07f, 0.88f), 32), 0.05f, wide ? 0.08f : 0.1f, 0.95f, wide ? 0.42f : 0.36f);
                    var pic = Ui.At(Ui.Box(box, "Pic"), 0.03f, 0.2f, 0.03f, 0.8f);   // a square as tall as 60% of the card, from its left edge
                    pic.pivot = new Vector2(0, 0.5f);   // grows rightwards from the card's left edge
                    pic.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                    if (Portraits.Contains(line.who))
                    {
                        var raw = new GameObject("Face", typeof(RectTransform)).AddComponent<RawImage>(); raw.transform.SetParent(pic, false); Ui.Fill(raw.rectTransform);
                        raw.texture = Resources.Load<Texture2D>("UI/" + line.who); raw.raycastTarget = false;
                    }
                    else
                    {
                        var av = Ui.Tile(pic, "Avatar", new Color(0.18f, 0.2f, 0.26f), 999); Ui.Fill(av);
                        string ini = new string((line.name ?? "?").Split(' ').Where(w => w.Length > 0 && char.IsLetter(w[0])).Select(w => w[0]).Take(2).ToArray());
                        Ui.Label(av, ini.ToUpperInvariant(), 44, Ui.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                    }
                    float x0 = wide ? 0.2f : 0.42f;   // the words start clear of the portrait
                    Ui.At(Ui.Label(box, (line.name ?? "").ToUpperInvariant() + (text ? "  ·  TEXT" : ""), 24, Ui.Gold, TextAnchor.MiddleLeft, FontStyle.Bold), x0, 0.72f, 0.97f, 0.92f);
                    Ui.At(Ui.Label(box, line.text, wide ? 34 : 36, Ui.Ink, TextAnchor.UpperLeft), x0, 0.08f, 0.97f, 0.7f);
                }
                float said = Voice.Say(line.who, line.text, text ? 0.8f : 1f, text);
                float hold = Mathf.Max(said + 0.6f, 1.6f + line.text.Length * 0.045f), t0 = Time.unscaledTime;
                next = false;
                while (!next && !skip && Time.unscaledTime - t0 < hold) yield return null;
                Voice.Hush();
                UnityEngine.Object.Destroy(card.gameObject);
            }
            UnityEngine.Object.Destroy(root.gameObject);
            done();
        }
    }
}
