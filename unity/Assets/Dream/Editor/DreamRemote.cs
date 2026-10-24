// Hands-free editor commands for when nobody's at the keyboard: write one command per line into
// Logs/dream_cmd.txt (outside Assets, so writing it doesn't trigger an import) and the editor runs it within a
// second or two, then writes what happened to Logs/dream_cmd_out.txt. Only the commands listed here run.
//   refresh        re-scan Assets (picks up script and file changes without clicking into Unity)
//   build-web      Dream > Build Web
//   web-textures   switch the web build's textures to ASTC
//   play / stop    enter or leave Play mode
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CFDream.EditorTools
{
    [InitializeOnLoad]
    public static class DreamRemote
    {
        static readonly string Dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
        static string Cmd => Path.Combine(Dir, "dream_cmd.txt");
        static string Out => Path.Combine(Dir, "dream_cmd_out.txt");
        static double next;

        static readonly Dictionary<string, Action> Commands = new Dictionary<string, Action>
        {
            { "refresh", () => AssetDatabase.Refresh() },
            { "build-web", () => { DreamBuild.Quiet = true; try { DreamBuild.BuildWeb(); } finally { DreamBuild.Quiet = false; } } },
            { "web-textures", DreamBuild.WebTextures },
#if DREAM_UMA
            { "export-uvs", DreamUMATools.ExportUVs }, { "build-clothes", DreamClothes.Build },
            { "uma-rebuild", DreamUMATools.Rebuild }, { "uma-trim", DreamUMATools.TrimApply }, { "uma-shrink", DreamUMATools.ShrinkForWeb },
#endif
            { "play", () => EditorApplication.isPlaying = true },
            { "stop", () => EditorApplication.isPlaying = false },
            // play-mode shortcuts for testing: give every pet and move one in, go home
            { "cheat-pets", () => { var s = CFDream.Game.State; if (s == null) return; foreach (var it in CFDream.DB.Items) if (it.slot == "pet") s.owned.Add(it.Key); s.Save(); CFDream.Game.Render(); } },
            { "pet-cat", () => Adopt("cat") }, { "pet-pup", () => Adopt("pup") }, { "pet-frenchie", () => Adopt("frenchie") }, { "pet-doberman", () => Adopt("doberman") }, { "pet-none", () => Adopt("none") },
            { "start", () => { var s = new CFDream.State { made = true, name = "Tester", ch = 1, job = "quit", br = 5000, peak = 5000 }; CFDream.Game.State = s; s.story.Add("p_after"); CFDream.Career.Unlocks(s); s.newItems.Clear(); s.Save(); CFDream.Game.SetTab("home"); } },
            { "bling", () => { var s = CFDream.Game.State; if (s == null) return; foreach (var it in CFDream.DB.Items) s.owned.Add(it.Key); foreach (var kv in new Dictionary<string, string> { { "eyes", "aviator" }, { "head", "snap" }, { "ear", "diamond" }, { "chain", "cuban" }, { "ring", "gold" }, { "wrist", "gold" }, { "watch", "gold" }, { "outfit", "jersey" }, { "bottom", "joggers" }, { "shoes", "highs" } }) s.eq[kv.Key] = kv.Value; s.Save(); CFDream.Game.SetTab("closet"); } },
            { "eq", null },   // placeholder, see Poll: "eq slot id"
            { "restyle", () => { if (CFDream.Game.State != null) CFDream.Game.StartRestyle(); } },
            { "go-home", () => { if (CFDream.Game.State != null) CFDream.Game.SetTab("home"); } },
            { "status", () => { var s = CFDream.Game.State; var st = CFDream.Character.CharacterStage.Instance; Debug.Log($"[Dream] status: view {CFDream.Game.View} tab {CFDream.Game.Tab} made {s?.made} pet {s?.Eq("pet")} home {s?.Eq("home")} bodies {CFDream.Character.Bodies.Available} stage {(st != null)} inRoom {st?.InRoom} hasBody {st?.HasBody} petId {st?.Pets?.Id}"); } },
            { "snap", () => CFDream.Character.CharacterStage.Instance?.SnapNow() },
            { "where", () => { var st = CFDream.Character.CharacterStage.Instance; if (st != null && st.Pivot != null) Debug.Log($"[Dream] where: ({st.Pivot.localPosition.x:0.00}, {st.Pivot.localPosition.z:0.00}) yaw {st.Pivot.localEulerAngles.y:0}"); } },
        };

        static DreamRemote() { EditorApplication.update += Poll; }
        static void Adopt(string id) { var s = CFDream.Game.State; if (s == null) return; if (id != "none") s.owned.Add("pet:" + id); s.eq["pet"] = id; s.Save(); CFDream.Game.Render(); }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + 1.0;
            if (BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling || !File.Exists(Cmd)) return;
            string[] lines;
            try { lines = File.ReadAllLines(Cmd); File.Delete(Cmd); } catch { return; }
            foreach (var raw in lines)
            {
                var c = raw.Trim().ToLowerInvariant(); if (c.Length == 0) continue;
                string result;
                if (c.StartsWith("eq "))   // eq <slot> <id>: wear something (test only)
                {
                    var p = c.Split(' '); var s = CFDream.Game.State;
                    if (p.Length == 3 && s != null) { s.owned.Add(p[1] + ":" + p[2]); s.eq[p[1]] = p[2]; s.Save(); CFDream.Game.Render(); result = "done"; } else result = "usage: eq slot id";
                }
                else if (c.StartsWith("use "))   // use <id>: walk to a thing in the room and use it (fire, piano, pool, tv, bed, ...)
                {
                    var life = CFDream.Character.CharacterStage.Instance?.Life;
                    if (life == null) result = "no room"; else { life.Use(c.Substring(4).Trim()); result = "done"; }
                }
                else if (c.StartsWith("snap "))   // snap <name> [bust|face|full|room]: render the character stage to Logs/<name>.png
                {
                    var p = c.Split(' '); var st = CFDream.Character.CharacterStage.Instance;
                    if (st == null) result = "no stage"; else { st.SnapTo(p[1], p.Length > 2 ? p[2] : null); result = "done"; }
                }
                else if (!Commands.TryGetValue(c, out var run) || run == null) result = "unknown command";
                else
                {
                    try { run(); result = "done"; }
                    catch (Exception e) { result = "failed: " + e.GetBaseException().Message; }
                }
                try { File.AppendAllText(Out, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {c}: {result}\n"); } catch { }
                Debug.Log($"[Dream] remote {c}: {result}");
            }
        }
    }
}
