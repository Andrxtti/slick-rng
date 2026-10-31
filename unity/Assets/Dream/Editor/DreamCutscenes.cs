// Play mode only: run any cutscene without registering for an event (Dream > Cutscenes). For checking the art.
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CFDream.EditorTools
{
    public static class DreamCutscenes
    {
        static void Go(string kind, string ride)
        {
            if (!Application.isPlaying || Game.State == null) { Debug.LogWarning("[Dream] Cutscenes play in Play mode"); return; }
            var ev = kind == "online" ? DB.Events.FirstOrDefault(e => e.online) : DB.Events.FirstOrDefault(e => !e.online && !e.daily);
            if (ev == null) return;
            Game.Trip = new Trip { kind = kind, ride = ride, ev = ev }; Game.View = View.Transit; Game.Render();
        }
        [MenuItem("Dream/Cutscenes/Drive: bus", priority = 60)] static void Bus() => Go("drive", "bus");
        [MenuItem("Dream/Cutscenes/Drive: hatch", priority = 61)] static void Hatch() => Go("drive", "hatch");
        [MenuItem("Dream/Cutscenes/Drive: coupe", priority = 62)] static void Coupe() => Go("drive", "coupe");
        [MenuItem("Dream/Cutscenes/Drive: sedan", priority = 63)] static void Sedan() => Go("drive", "sedan");
        [MenuItem("Dream/Cutscenes/Drive: supercar", priority = 64)] static void Super() => Go("drive", "super");
        [MenuItem("Dream/Cutscenes/Flight", priority = 65)] static void Fly() => Go("fly", "hatch");
        [MenuItem("Dream/Cutscenes/Private jet", priority = 66)] static void Jet() => Go("fly", "jet");
        [MenuItem("Dream/Cutscenes/CF Poker (online)", priority = 67)] static void Online() => Go("online", "hatch");
    }
}
