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
            { "play", () => EditorApplication.isPlaying = true },
            { "stop", () => EditorApplication.isPlaying = false },
        };

        static DreamRemote() { EditorApplication.update += Poll; }

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
                if (!Commands.TryGetValue(c, out var run)) result = "unknown command";
                else
                {
                    try { run(); result = "done"; }
                    catch (Exception e) { result = "failed: " + e.GetBaseException().Message; }
                }
                try { File.AppendAllText(Out, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {c}: {result}\n"); } catch { }
            }
        }
    }
}
