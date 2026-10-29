using UnityEngine;

namespace Dream.Game
{
    // Voices for the lines the game shows outside the films (Steven, 15:48: "voice everything that's unspoken"): phone
    // texts in the sender's voice, a touch quieter, after the message tone; Yarn's plain narration in the narrator's.
    // Clips are made by tools/voice.py into Resources/Voice/Lines/<key>.wav, key = Key(who, text), the same FNV hash in
    // Python and here. A line with a {value} in it has no clip and stays silent.
    public static class Voice
    {
        static AudioSource src;
        public static string Last = "";   // the last line asked for and whether it had a clip (test driver)

        public static string Key(string who, string text) => Rng.Hash(who.ToLowerInvariant() + "|" + text).ToString("x8");

        // plays the line if it has a clip; returns its length in seconds (0 when silent)
        public static float Say(string who, string text, float volume = 1f, bool tone = false)
        {
            if (string.IsNullOrEmpty(text) || text.Contains("{")) return 0;
            var clip = Resources.Load<AudioClip>("Voice/Lines/" + Key(who, text));
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
