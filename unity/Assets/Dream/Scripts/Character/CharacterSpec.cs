// Everything the 3D character needs, read from the save: body, face, hair, and every piece worn with its
// colourway, metal finish and how broken in it is. The 3D body (UMA or later pro art) only ever sees this.
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CFDream.Character
{
    public class Worn
    {
        public string slot, id;
        public Color? dye;          // a colourway, or null for the original colour
        public int finish = -1;     // metal finish index (Fashion.Finishes), or -1 for the original metal
        public int level;           // 0 New, 1 Broken in, 2 Signature (monogram), 3 Mastered (light-catching finish)
        public string Key => slot + ":" + id;
    }

    public class CharacterSpec
    {
        public string body = "male";
        public Color skin, hair, eyes;
        public string hairStyle = "short", facial = "none", brows = "natural", face = "oval", age = "young";
        public bool freckles;
        public readonly Dictionary<string, float> sculpt = new Dictionary<string, float>(); // -1..1, see DR_SCULPT
        public readonly List<Worn> worn = new List<Worn>();
        public string initials = "CF";

        public Worn Get(string slot) { foreach (var w in worn) if (w.slot == slot) return w; return null; }
        public float Sculpt(string k) => sculpt.TryGetValue(k, out var v) ? v : 0;

        public static CharacterSpec From(State s) => From(s.look, s.eq, s.dye, s.fin, s, s.name);

        public static CharacterSpec From(Look l, Dictionary<string, string> eq, Dictionary<string, int> dye = null, Dictionary<string, int> fin = null, State s = null, string name = null)
        {
            var c = new CharacterSpec();
            l = l ?? new Look();
            c.skin = Palette.Get("DR_SKIN", l.skin, "#e9b48a"); c.hair = Palette.Get("DR_HAIRC", l.hairC, "#3a2416"); c.eyes = Palette.Get("DR_EYEC", l.eyeC, "#4a2f1c");
            c.hairStyle = l.hair ?? "short"; c.facial = l.facial ?? "none"; c.brows = l.brows ?? "natural"; c.face = l.face ?? "oval"; c.age = l.age ?? "young"; c.freckles = l.freck;
            foreach (var kv in l.sc) c.sculpt[kv.Key] = Mathf.Clamp((float)kv.Value, -1f, 1f);
            c.initials = Fashion.Initials(name);
            if (eq != null)
                foreach (var slot in Fashion.Wearable)
                {
                    if (!eq.TryGetValue(slot, out var id) || string.IsNullOrEmpty(id) || id == "none") continue;
                    var w = new Worn { slot = slot, id = id };
                    if (dye != null && dye.TryGetValue(id, out var di) && di > 0 && di < Fashion.Dyes.Length && Fashion.Dyes[di] != null) w.dye = Palette.Hex(Fashion.Dyes[di]);
                    if (fin != null && fin.TryGetValue(w.Key, out var fi)) w.finish = fi;
                    if (s != null) w.level = Fashion.WearLv(s, w.Key);
                    c.worn.Add(w);
                }
            return c;
        }

        // a fingerprint of the look, so the 3D stage rebuilds only when something actually changed
        public string Fingerprint()
        {
            var sb = new StringBuilder();
            sb.Append(body).Append('|').Append(ColorUtility.ToHtmlStringRGB(skin)).Append(ColorUtility.ToHtmlStringRGB(hair)).Append(ColorUtility.ToHtmlStringRGB(eyes))
              .Append('|').Append(hairStyle).Append(facial).Append(brows).Append(face).Append(age).Append(freckles ? 'f' : '-').Append('|').Append(initials);
            foreach (var kv in sculpt) sb.Append(kv.Key).Append(Mathf.RoundToInt(kv.Value * 100));
            foreach (var w in worn) sb.Append('|').Append(w.Key).Append(w.dye.HasValue ? ColorUtility.ToHtmlStringRGB(w.dye.Value) : "-").Append(w.finish).Append(w.level);
            return sb.ToString();
        }
    }

    public static class Palette
    {
        public static Color Hex(string hex, string fallback = "#808080")
        {
            if (!string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString(hex.StartsWith("#") ? hex : "#" + hex, out var c)) return c;
            ColorUtility.TryParseHtmlString(fallback, out var f); return f;
        }
        public static Color Get(string key, int i, string fallback)
        {
            var l = DB.List(key); if (l == null || l.Count == 0) return Hex(fallback);
            return Hex(l[Math.Max(0, Math.Min(l.Count - 1, i))] as string, fallback);
        }
        // the metal a piece shows: its chosen finish, else what it's made of
        public static Color Metal(Worn w)
        {
            if (w.finish >= 0 && w.finish < Fashion.Finishes.Length) return Hex(Fashion.Finishes[w.finish].c);
            return w.id == "silver" || w.id == "steel" || w.id == "iced" || w.id == "diamond" || w.id == "tennis" ? Hex("#dfe3ea") : Hex("#e3b94f");
        }
    }
}
