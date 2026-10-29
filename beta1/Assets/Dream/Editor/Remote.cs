// Hands-free editor commands: write one command per line into Logs/dream_cmd.txt and the open editor runs it within a
// second or two, appending what happened to Logs/dream_cmd_out.txt. Only the commands listed here run.
//   refresh | setup | resolve | play | stop | render <id> | status
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dream.EditorTools
{
    [InitializeOnLoad]
    public static class Remote
    {
        static readonly string Dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
        static string Cmd => Path.Combine(Dir, "dream_cmd.txt");
        static string Out => Path.Combine(Dir, "dream_cmd_out.txt");
        static double next;

        static readonly Dictionary<string, Action> Commands = new Dictionary<string, Action>
        {
            { "refresh", () => AssetDatabase.Refresh() },
            { "setup", Setup.Run },
            { "resolve", () => UnityEditor.PackageManager.Client.Resolve() },   // re-read Packages/manifest.json
            { "play", () => EditorApplication.isPlaying = true },
            { "stop", () => EditorApplication.isPlaying = false },
            { "status", () => Log($"status: playing {EditorApplication.isPlaying} compiling {EditorApplication.isCompiling} scene {UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}") },
        };

        static Remote() { EditorApplication.update += Poll; }

        public static void Log(string m)
        {
            Debug.Log("[Dream] " + m);
            try { Directory.CreateDirectory(Dir); File.AppendAllText(Out, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {m}\n"); } catch { }
        }

        static void Poll()
        {
            if (EditorApplication.timeSinceStartup < next) return;
            next = EditorApplication.timeSinceStartup + 1.0;
            if (BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling || !File.Exists(Cmd)) return;
            string[] lines;
            try { lines = File.ReadAllLines(Cmd); File.Delete(Cmd); } catch { return; }
            foreach (var raw in lines)
            {
                var c = raw.Trim(); if (c.Length == 0) continue;
                var sp = c.IndexOf(' ');
                var verb = (sp < 0 ? c : c.Substring(0, sp)).ToLowerInvariant();
                var arg = sp < 0 ? "" : c.Substring(sp + 1).Trim();
                string result;
                try
                {
                    if (verb == "render" && arg.Length > 0) { foreach (var id in arg.Split(' ')) Render.Queue(id); result = "queued"; }
                    else if (Extra.TryGetValue(verb, out var run1)) { run1(arg); result = "done"; }
                    else if (arg.Length == 0 && Commands.TryGetValue(verb, out var run)) { run(); result = "done"; }
                    else result = "unknown command";
                }
                catch (Exception e) { result = "failed: " + e.GetBaseException().Message; }
                Log($"{c}: {result}");
            }
        }

        // Commands with an argument, registered by other editor scripts (e.g. Film tools).
        public static readonly Dictionary<string, Action<string>> Extra = new Dictionary<string, Action<string>>();
    }
}
