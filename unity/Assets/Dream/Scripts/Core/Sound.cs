// The game's sound: an ambience bed for wherever you are, a score cue for the mood, stingers for moments.
// Clips live in Resources/Dream/Audio (made by unity/tools/art/audio.py). Two sources per layer so a change
// cross-fades instead of cutting. One toggle mutes everything; it is remembered on the device.
using System.Collections.Generic;
using UnityEngine;

namespace CFDream
{
    public class Sound : MonoBehaviour
    {
        static Sound inst;
        public static Sound I { get { if (inst == null) { var go = new GameObject("Sound"); DontDestroyOnLoad(go); inst = go.AddComponent<Sound>(); inst.Setup(); } return inst; } }
        public static bool On { get => PlayerPrefs.GetInt("cfdream.sound", 1) == 1; set { PlayerPrefs.SetInt("cfdream.sound", value ? 1 : 0); PlayerPrefs.Save(); if (inst != null) inst.Apply(); } }

        AudioSource[] amb = new AudioSource[2], mus = new AudioSource[2]; AudioSource fx, voice; int ambOn, musOn;
        string ambName, musName; float ambVol = 0.55f, musVol = 0.38f;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        // the voice: one line at a time; the score and the room duck under it
        public static bool Speaking => inst != null && inst.voice != null && inst.voice.isPlaying;
        public static float SpeakingFor => Speaking ? inst.voice.clip.length : 0f;
        float duck = 1f;

        void Setup()
        {
            for (int i = 0; i < 2; i++) { amb[i] = Src(true); mus[i] = Src(true); }
            fx = Src(false); voice = Src(false); voice.volume = 1f;
        }
        // say a line (Resources/Dream/Voice/v_<key>, made by unity/tools/voice.py). True when there is a clip for it.
        public static bool Say(string key)
        {
            var I_ = I; I_.voice.Stop();
            if (string.IsNullOrEmpty(key) || !On) return false;
            var c = Resources.Load<AudioClip>("Dream/Voice/v_" + key); if (c == null) return false;
            I_.voice.clip = c; I_.voice.Play(); return true;
        }
        public static void Hush() { if (inst != null && inst.voice != null) inst.voice.Stop(); }
        AudioSource Src(bool loop) { var a = gameObject.AddComponent<AudioSource>(); a.loop = loop; a.playOnAwake = false; a.volume = 0; a.spatialBlend = 0; return a; }
        AudioClip Clip(string name)
        {
            if (clips.TryGetValue(name, out var c)) return c;
            c = Resources.Load<AudioClip>("Dream/Audio/" + name); clips[name] = c; return c;
        }

        // ------------------------------------------------------------ what plays where
        public static void Place(string place) => I.Ambience(AmbFor(place));
        public static string AmbFor(string place)
        {
            switch (place)
            {
                case "office": case "diner": case "cardroom": case "casino": case "vegas": case "tv": case "titan": case "main": case "hall": return "amb_" + place;
                case "cftilt": case "home": case null: case "": return "amb_home";
                default: return "amb_cardroom";
            }
        }
        public static void Music(string cue) => I.Score(cue == null ? null : "music_" + cue);
        public static void Play(string sfx, float vol = 1f) { var c = I.Clip("sfx_" + sfx); if (c != null && On) I.fx.PlayOneShot(c, vol); }
        public static void Quiet() { I.Ambience(null); I.Score(null); }

        void Ambience(string name)
        {
            if (name == ambName) return; ambName = name;
            Cross(amb, ref ambOn, name == null ? null : Clip(name), ambVol);
        }
        void Score(string name)
        {
            if (name == musName) return; musName = name;
            Cross(mus, ref musOn, name == null ? null : Clip(name), musVol);
        }
        // fade the live source out and the other one in with the new clip
        void Cross(AudioSource[] pair, ref int on, AudioClip clip, float vol)
        {
            int next = 1 - on; var a = pair[next];
            a.Stop(); a.clip = clip; a.volume = 0; if (clip != null) a.Play();
            fades[a] = clip != null && On ? vol : 0f; fades[pair[on]] = 0f; on = next;
        }
        readonly Dictionary<AudioSource, float> fades = new Dictionary<AudioSource, float>();
        void Update()
        {
            // the bed ducks while someone speaks, quickly down and gently back
            float want = voice != null && voice.isPlaying ? 0.45f : 1f;
            duck = Mathf.MoveTowards(duck, want, Time.unscaledDeltaTime / (want < duck ? 0.25f : 0.9f));
            if (fades.Count > 0)
            {
                float k = Time.unscaledDeltaTime / 1.6f; var done = new List<AudioSource>();
                foreach (var kv in fades)
                {
                    var a = kv.Key; a.volume = Mathf.MoveTowards(a.volume, kv.Value, k);
                    if (Mathf.Approximately(a.volume, kv.Value)) { if (kv.Value <= 0f) a.Stop(); done.Add(a); }
                }
                foreach (var a in done) fades.Remove(a);
            }
            // settled sources follow the duck
            if (On && musName != null && !fades.ContainsKey(mus[musOn])) mus[musOn].volume = musVol * duck;
            if (On && ambName != null && !fades.ContainsKey(amb[ambOn])) amb[ambOn].volume = ambVol * Mathf.Lerp(0.7f, 1f, (duck - 0.45f) / 0.55f);
        }
        void Apply()
        {
            bool on = On; if (!on && voice != null) voice.Stop();
            fades[amb[ambOn]] = on && ambName != null ? ambVol : 0f; fades[mus[musOn]] = on && musName != null ? musVol : 0f;
            if (on) { if (ambName != null && !amb[ambOn].isPlaying) amb[ambOn].Play(); if (musName != null && !mus[musOn].isPlaying) mus[musOn].Play(); }
        }
    }
}
