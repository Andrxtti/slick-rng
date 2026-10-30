using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dream.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Dream.Game
{
    // The rendered films (StreamingAssets/Films/<id>.mp4, made by tools/films_publish.py): played one after another,
    // letterboxed on a black screen. Phones only ever play video. ONE film screen and ONE VideoPlayer for the whole
    // session (on the web, one <video> element): made on the first film, hidden between films, reused by changing the
    // url, so the sound the first tap unlocked stays unlocked. Taps during a film belong to the web page (index.html
    // swallows them and shows its own "Skip scene" pill); Skip() stays for the autoplay drivers. The words are not in the picture: each film
    // has <id>.json (when each narrator card and spoken line starts, and for how long), drawn here in the brand fonts:
    // the narrator in Gloock, subtitles in Inter with the speaker's name. Held upright, they sit under the picture.
    public class Films : MonoBehaviour
    {
        Queue<string> queue; Action done; VideoPlayer vp; RawImage img; RenderTexture rt; GameObject screen;
        float started; RectTransform board; bool boarding;   // a storyboard playing in place of a film (Storyboard.cs)
        static Films inst;
        public static Films I => inst;
        public VideoPlayer Player => vp;
        public static bool Playing => inst != null && inst.queue != null;   // a film queue is running
        public static readonly List<string> Played = new List<string>();   // every film started this session (test driver)
        public static Func<string, string> Fill = k => "";                 // a card's {key}, filled in by the game

        [Serializable] class Caption { public float t, len; public string who, name, text; }
        [Serializable] class Captions { public List<Caption> lines = new List<Caption>(); }
        Captions caps; RectTransform capBox; TextMeshProUGUI narrator, subtitle; CanvasGroup capGroup; Caption showing;

        public static void Play(IEnumerable<string> ids, Action then)
        {
            var q = new Queue<string>(ids);
            if (q.Count == 0) { then(); return; }
            if (inst == null)
            {
                var go = new GameObject("Films"); DontDestroyOnLoad(go);
                inst = go.AddComponent<Films>(); inst.Build();
            }
            inst.queue = q; inst.done = then; inst.screen.SetActive(true);
            inst.Next();
        }

        void Build()
        {
            var c = Ui.Root("FilmScreen"); c.sortingOrder = 50; screen = c.gameObject; screen.transform.SetParent(transform);
            var bg = Ui.Panel(c.transform, "Black", Color.black);   // blocks taps to the game underneath; no skip here
            board = bg;
            img = new GameObject("Film", typeof(RectTransform)).AddComponent<RawImage>(); img.transform.SetParent(bg, false);
            var fit = img.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 16f / 9f;
            img.raycastTarget = false;
            capBox = Ui.Box(bg, "Captions"); capGroup = capBox.gameObject.AddComponent<CanvasGroup>(); capGroup.alpha = 0;
            narrator = Ui.Title(capBox, "", 50, new Color(0.97f, 0.95f, 0.9f)); narrator.lineSpacing = 10;
            subtitle = Ui.Label(capBox, "", 40, new Color(0.97f, 0.95f, 0.9f));
            foreach (var t in new[] { narrator, subtitle })   // a soft dark edge so the words read over any frame
            {
                t.fontMaterial.EnableKeyword("UNDERLAY_ON"); t.fontMaterial.SetFloat("_UnderlaySoftness", 0.6f);
                t.fontMaterial.SetFloat("_UnderlayDilate", 0.35f); t.fontMaterial.SetColor("_UnderlayColor", new Color(0, 0, 0, 0.85f));
            }
            rt = new RenderTexture(1280, 720, 0); img.texture = rt;
            vp = gameObject.AddComponent<VideoPlayer>(); vp.playOnAwake = false; vp.renderMode = VideoRenderMode.RenderTexture; vp.targetTexture = rt;
            vp.audioOutputMode = VideoAudioOutputMode.Direct; vp.loopPointReached += _ => Next();
            vp.errorReceived += (_, e) => { Debug.LogWarning("[Dream] film: " + e); Next(); };
        }

        void Next()
        {
            caps = null; showing = null; capGroup.alpha = 0;
            if (queue == null) return;   // a late end or error event after the queue finished
            if (queue.Count == 0)   // the queue is over: hide the screen, keep the player for the next film
            {
                var d = done; done = null; queue = null; vp.Stop(); screen.SetActive(false);
                d?.Invoke(); return;
            }
            var id = queue.Dequeue(); Played.Add(id);
            if (Storyboard.Prefer(id))   // no film yet, or Steven edited the scene since: its storyboard
            {
                vp.Stop(); img.enabled = false; boarding = true;
                StartCoroutine(Storyboard.Play(board, id, () => { boarding = false; img.enabled = true; Next(); }));
                return;
            }
            var sa = Application.streamingAssetsPath; var dir = (sa.Contains("://") ? sa : "file://" + sa) + "/Films/";
            vp.source = VideoSource.Url;
            vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, "Films", id + ".mp4");
            vp.prepareCompleted -= Started; vp.prepareCompleted += Started; vp.Prepare(); started = Time.unscaledTime; lastClock = -1; lastFrame = Time.realtimeSinceStartup; stuckSince = started;
            var req = UnityWebRequest.Get(dir + id + ".json");   // works on the web too (no file access there)
            req.SendWebRequest().completed += _ =>
            {
                if (req.result == UnityWebRequest.Result.Success) try { caps = JsonUtility.FromJson<Captions>(req.downloadHandler.text); } catch { }
                req.Dispose();
            };
        }
        void Started(VideoPlayer p) { p.prepareCompleted -= Started; p.Play(); }

        public void Skip() { if (boarding) { Storyboard.Skip(); return; } if (queue == null || Time.unscaledTime - started < 0.4f) return; vp.Stop(); Next(); }

        // the page is back (Resume): a film the browser paused plays on, and the watchdogs start counting from now
        public void Wake()
        {
            if (queue == null || vp == null || boarding) return;
            started = stuckSince = Time.unscaledTime; lastClock = -1;
            // the browser paused the <video> behind Unity's back, so isPlaying can't be trusted: pause and play again
            if (vp.isPrepared) { vp.Pause(); vp.Play(); }
            else vp.Prepare();
            Debug.Log($"[Dream] page back: film {System.IO.Path.GetFileName(vp.url)} resumed at {vp.time:0.0}s");
        }

        float lastClock = -1, stuckSince, lastFrame;
        void Update()
        {
            // watchdogs: a film that never starts (missing, unreadable) or whose clock stops is treated as over, so the
            // story never hangs on a black screen
            if (boarding) return;
            if (queue != null && vp != null)
            {
                // a frame over a second long: the page was asleep (another app, tab or the lock screen). The game loop can
                // run before the browser's "visible" event arrives, so wake the film here too, before the watchdogs look
                if (Resume.Hidden) { stuckSince = started = Time.unscaledTime; lastFrame = Time.realtimeSinceStartup; return; }
                float gap = Time.realtimeSinceStartup - lastFrame; lastFrame = Time.realtimeSinceStartup;
                if (gap > 1f) { Wake(); return; }
                if (!vp.isPrepared && Time.unscaledTime - started > 6f) { Debug.LogWarning("[Dream] film never started: " + vp.url); Next(); return; }
                if (vp.isPrepared && Time.unscaledTime - started > 1f)
                {
                    if (Mathf.Abs((float)vp.time - lastClock) > 0.001f) { lastClock = (float)vp.time; stuckSince = Time.unscaledTime; }
                    else if (Time.unscaledTime - stuckSince > 4f) { Debug.LogWarning($"[Dream] film stalled at {vp.time:0.0}s (stuck {Time.unscaledTime - stuckSince:0.0}s, playing {vp.isPlaying}): {vp.url}"); Next(); return; }
                }
            }
            if (caps == null || vp == null || !vp.isPlaying) return;
            float now = (float)vp.time; Caption cur = null;
            foreach (var c in caps.lines) if (now >= c.t && now < c.t + c.len) cur = c;
            if (cur != showing)
            {
                showing = cur;
                if (cur != null)
                {
                    bool card = cur.who == "card";
                    var text = Regex.Replace(cur.text, @"\{(\w+)\}", m => Fill(m.Groups[1].Value));
                    narrator.text = card ? text : ""; subtitle.text = card ? "" : $"<size=70%><color=#F2C25A>{Cap(cur.name)}</color></size>\n{text}";
                }
            }
            // fade in and out over a third of a second at each end of the line
            float a = cur == null ? 0 : Mathf.Clamp01(Mathf.Min(now - cur.t, cur.t + cur.len - now) / 0.33f);
            capGroup.alpha = Mathf.MoveTowards(capGroup.alpha, a, Time.deltaTime * 4f);
            Layout();
        }

        static string Cap(string name) => string.IsNullOrEmpty(name) ? "" : name.ToUpperInvariant() == "STRANGER" ? "STRANGER" : name.ToUpperInvariant();

        // held upright the picture is a band in the middle: the words go in the black under it; on a wide screen they sit
        // in the bottom of the picture
        void Layout()
        {
            var pic = (RectTransform)img.transform; var full = ((RectTransform)pic.parent).rect;
            float picH = pic.rect.height / full.height, below = (1 - picH) / 2;
            if (below > 0.16f) { capBox.anchorMin = new Vector2(0.06f, below - 0.15f); capBox.anchorMax = new Vector2(0.94f, below - 0.01f); }
            else { capBox.anchorMin = new Vector2(0.12f, below + 0.04f); capBox.anchorMax = new Vector2(0.88f, below + picH * 0.3f); }
            capBox.offsetMin = capBox.offsetMax = Vector2.zero;
            narrator.alignment = subtitle.alignment = below > 0.16f ? TextAlignmentOptions.Top : TextAlignmentOptions.Bottom;
        }
    }
}
