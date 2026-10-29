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
            var t = Resources.Load<TextAsset>("Film/" + id);
            return t == null ? null : JsonUtility.FromJson<Scene>(t.text);
        }
    }
}
