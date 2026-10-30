using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Dream.Game
{
    // Voices for the lines the game shows outside the films (Steven, 15:48: "voice everything that's unspoken"): phone
    // texts in the sender's voice, a touch quieter, after the message tone; Yarn's plain narration in the narrator's; the
    // storyboards. Clips are made by tools/voice.py (masters in beta1/Voice/Lines), key = Key(who, text), the same FNV hash
    // in Python and here, and stream as StreamingAssets/Voice/<key>.mp3 (not in the first download: Milestone B, 25 MB).
    // Preload() fetches them all in the background from the title on; a line whose clip hasn't arrived reads silently.
    // A line with a {value} in it has no clip and stays silent.
    public static class Voice
    {
        static AudioSource src;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        static bool preloading;
        public static string Last = "";   // the last line asked for and whether it had a clip (test driver)

        public static string Key(string who, string text) => Rng.Hash(who.ToLowerInvariant() + "|" + text).ToString("x8");

        static string Dir { get { var sa = Application.streamingAssetsPath; return (sa.Contains("://") ? sa : "file://" + sa) + "/Voice/"; } }
        [System.Serializable] class Index { public List<string> keys; }

        // every clip, one after another, in the background (started once by the game)
        public static void Preload(MonoBehaviour host) { if (!preloading) { preloading = true; host.StartCoroutine(Load()); } }
        static IEnumerator Load()
        {
            var req = UnityWebRequest.Get(Dir + "index.json"); yield return req.SendWebRequest();
            var keys = req.result == UnityWebRequest.Result.Success ? JsonUtility.FromJson<Index>(req.downloadHandler.text).keys : new List<string>();
            req.Dispose();
            foreach (var k in keys)
            {
                if (clips.ContainsKey(k)) continue;
                var r = UnityWebRequestMultimedia.GetAudioClip(Dir + k + ".mp3", AudioType.MPEG); yield return r.SendWebRequest();
                if (r.result == UnityWebRequest.Result.Success) { var c = DownloadHandlerAudioClip.GetContent(r); c.name = k; clips[k] = c; }
                r.Dispose();
            }
            Debug.Log($"[Dream] voices preloaded: {clips.Count} of {keys.Count}");
        }

        // plays the line if its clip is here; returns its length in seconds (0 when silent)
        public static float Say(string who, string text, float volume = 1f, bool tone = false)
        {
            if (string.IsNullOrEmpty(text) || text.Contains("{")) return 0;
            clips.TryGetValue(Key(who, text), out var clip);
            if (src == null) { var go = new GameObject("Voice"); Object.DontDestroyOnLoad(go); src = go.AddComponent<AudioSource>(); src.playOnAwake = false; }
            src.Stop();
            float wait = 0;
            if (tone) { var t = Resources.Load<AudioClip>("Voice/text_tone"); if (t != null) { src.PlayOneShot(t, 0.55f); wait = t.length * 0.8f; } }
            Last = $"{who}: {text} -> {(clip != null ? clip.name + $" {clip.length:0.0}s" : "no clip")}{(tone ? " (tone)" : "")}";
            if (clip == null) return wait;
            src.clip = clip; src.volume = volume; src.PlayDelayed(wait);
            return wait + clip.length;
        }

        public static void Hush() { if (src != null) src.Stop(); }
    }
}
