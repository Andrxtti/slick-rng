using System;
using System.Collections.Generic;
using Dream.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Dream.Game
{
    // The rendered films (StreamingAssets/Films/<id>.mp4, made by tools/films_publish.py): played one after another,
    // letterboxed on a black screen, tap to skip. Phones only ever play video.
    public class Films : MonoBehaviour
    {
        Queue<string> queue; Action done; VideoPlayer vp; RawImage img; RenderTexture rt; GameObject screen; Text skip;
        float started;

        public static void Play(IEnumerable<string> ids, Action then)
        {
            var go = new GameObject("Films"); var f = go.AddComponent<Films>();
            f.queue = new Queue<string>(ids); f.done = then;
            if (f.queue.Count == 0) { Destroy(go); then(); return; }
            f.Build(); f.Next();
        }

        void Build()
        {
            var c = Ui.Root("FilmScreen"); c.sortingOrder = 50; screen = c.gameObject; screen.transform.SetParent(transform);
            var bg = Ui.Panel(c.transform, "Black", Color.black);
            var b = bg.gameObject.AddComponent<Button>(); b.onClick.AddListener(Skip);
            img = new GameObject("Film", typeof(RectTransform)).AddComponent<RawImage>(); img.transform.SetParent(bg, false);
            var fit = img.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio = 16f / 9f;
            img.raycastTarget = false;
            skip = Ui.At(Ui.Label(bg, "tap to skip", 28, new Color(1, 1, 1, 0.45f)), 0, 0.02f, 1, 0.06f);
            rt = new RenderTexture(1280, 720, 0); img.texture = rt;
            vp = gameObject.AddComponent<VideoPlayer>(); vp.playOnAwake = false; vp.renderMode = VideoRenderMode.RenderTexture; vp.targetTexture = rt;
            vp.audioOutputMode = VideoAudioOutputMode.Direct; vp.loopPointReached += _ => Next();
            vp.errorReceived += (_, e) => { Debug.LogWarning("[Dream] film: " + e); Next(); };
        }

        void Next()
        {
            if (queue.Count == 0) { var d = done; Destroy(gameObject); rt.Release(); d?.Invoke(); return; }
            var id = queue.Dequeue();
            vp.source = VideoSource.Url;
            vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, "Films", id + ".mp4");
            vp.Prepare(); vp.prepareCompleted += Started; started = Time.time;
        }
        void Started(VideoPlayer p) { p.prepareCompleted -= Started; p.Play(); }

        void Skip() { if (Time.time - started < 0.4f) return; vp.Stop(); Next(); }
    }
}
