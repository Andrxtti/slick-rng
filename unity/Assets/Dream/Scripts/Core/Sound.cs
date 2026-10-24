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

        AudioSource[] amb = new AudioSource[2], mus = new AudioSource[2]; AudioSource fx; int ambOn, musOn;
        string ambName, musName; float ambVol = 0.55f, musVol = 0.38f;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        void Setup()
        {
            for (int i = 0; i < 2; i++) { amb[i] = Src(true); mus[i] = Src(true); }
            fx = Src(false);
        }
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
            if (fades.Count == 0) return;
            float k = Time.unscaledDeltaTime / 1.6f; var done = new List<AudioSource>();
            foreach (var kv in fades)
            {
                var a = kv.Key; a.volume = Mathf.MoveTowards(a.volume, kv.Value, k);
                if (Mathf.Approximately(a.volume, kv.Value)) { if (kv.Value <= 0f) a.Stop(); done.Add(a); }
            }
            foreach (var a in done) fades.Remove(a);
        }
        void Apply()
        {
            bool on = On;
            fades[amb[ambOn]] = on && ambName != null ? ambVol : 0f; fades[mus[musOn]] = on && musName != null ? musVol : 0f;
            if (on) { if (ambName != null && !amb[ambOn].isPlaying) amb[ambOn].Play(); if (musName != null && !mus[musOn].isPlaying) mus[musOn].Play(); }
        }
    }
}
