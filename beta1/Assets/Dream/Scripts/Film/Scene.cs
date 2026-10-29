using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dream.Film
{
    // One scene of the script, as tools/script_build.py writes it to Resources/Film/<id>.json.
    [Serializable]
    public class Line
    {
        public int n;
        public string who, name, shot, subject, act, next, text, voice;
        public float hold;   // seconds for a silent shot (the reel); 0 = the voice length, or a card's hold
    }

    [Serializable]
    public class Direction
    {
        public int after;
        public string text;
    }

    [Serializable]
    public class Scene
    {
        public string id, title, place, time;
        public List<string> cast = new List<string>();
        public List<Line> lines = new List<Line>();
        public List<Direction> directions = new List<Direction>();

        public static Scene Load(string id)
        {
            var t = Data.Load<TextAsset>($"Film/{id}.json");
            return t == null ? null : JsonUtility.FromJson<Scene>(t.text);
        }
    }

    // Film data (scenes, voices, sound) lives in Assets/Dream/FilmData, outside Resources: films are rendered in the
    // editor and shipped as video, so none of it belongs in the phone build.
    public static class Data
    {
        public static T Load<T>(string rel) where T : UnityEngine.Object
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>("Assets/Dream/FilmData/" + rel);
#else
            return null;
#endif
        }
    }
}
