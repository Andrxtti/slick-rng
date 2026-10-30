// Phone-size stills from the editor (Milestone A): set the Game view to a size and show a screen to capture.
//   remote: res <w> <h>        -> the Game view at w x h (a custom fixed size, added once)
//   remote: allin [win|lose]   -> the all-in screen over the table, a made-up showdown (AK vs QQ) with the river already out
using System;
using System.Linq;
using System.Reflection;
using Dream.Game;
using UnityEditor;
using UnityEngine;

namespace Dream.EditorTools
{
    [InitializeOnLoad]
    public static class PhoneShots
    {
        static PhoneShots()
        {
            Remote.Extra["res"] = a => { var p = a.Split(' '); Remote.Log("res: " + SetSize(int.Parse(p[0]), int.Parse(p[1]))); };
            Remote.Extra["home"] = a => { Flow.I.TestHome(a.Trim()); Remote.Log("home: " + a); };
            Remote.Extra["allin"] = a =>
            {
                bool win = a.Trim() != "lose";
                var root = (RectTransform)UnityEngine.Object.FindAnyObjectByType<Canvas>().transform;
                var run = Hands.Start(Lib.Event("nightly"), "allin-shot");
                int C(string s) => "23456789TJQKA".IndexOf(s[0]) * 4 + "cdhs".IndexOf(s[1]);
                var sd = new Showdown
                {
                    hero = new[] { C("As"), C("Kd") }, opp = new[] { C("Qc"), C("Qh") }, board = new[] { C("Ks"), C("7h"), C("2c"), C("9d"), win ? C("3s") : C("Qs") },
                    win = win, eq = 0.43, oppName = "CO", oppSeat = "CO", heroHand = win ? "A pair of kings" : "A pair of kings", oppHand = win ? "a pair of queens" : "Three queens", before = 24, after = win ? 49 : 0,
                };
                Moments.AllIn(root, run, sd, () => { });
                Remote.Log("allin: shown " + (win ? "win" : "lose"));
            };
        }

        // the Game view's size list is internal: add a fixed custom size if missing, then select it
        static string SetSize(int w, int h)
        {
            var asm = typeof(Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var single = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(single, new object[] { (int)GameViewSizeGroupType.Standalone });
            var groupType = group.GetType();
            int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null), idx = -1;
            for (int i = 0; i < total; i++)
            {
                var sz = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                var t = sz.GetType();
                if ((int)t.GetProperty("width").GetValue(sz) == w && (int)t.GetProperty("height").GetValue(sz) == h && t.GetProperty("sizeType").GetValue(sz).ToString() == "FixedResolution") { idx = i; break; }
            }
            if (idx < 0)
            {
                var sizeType = asm.GetType("UnityEditor.GameViewSize"); var kind = asm.GetType("UnityEditor.GameViewSizeType");
                var ctor = sizeType.GetConstructor(new[] { kind, typeof(int), typeof(int), typeof(string) });
                var size = ctor.Invoke(new object[] { Enum.Parse(kind, "FixedResolution"), w, h, $"phone {w}x{h}" });
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                idx = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null) - 1;
            }
            var gvType = asm.GetType("UnityEditor.GameView");
            var gv = EditorWindow.GetWindow(gvType, false, null, false);
            var prop = gvType.GetProperty("selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null) prop.SetValue(gv, idx);
            else gvType.GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(gv, new object[] { idx, null });
            gv.Repaint();
            return $"{w}x{h} (index {idx})";
        }
    }
}
