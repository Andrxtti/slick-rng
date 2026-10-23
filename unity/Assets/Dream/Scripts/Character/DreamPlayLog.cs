// Editor only: copies every Play-mode log line to Logs/dream_play.txt in the project, so problems can be read
// from the file instead of the console window. Not compiled into builds.
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace CFDream.Character
{
    static class DreamPlayLog
    {
        static StreamWriter w;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Start()
        {
            try
            {
                Application.logMessageReceived -= OnLog;
                w?.Dispose();
                var path = Path.Combine(Application.dataPath, "..", "Logs", "dream_play.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                w = new StreamWriter(path, false) { AutoFlush = true };
                w.WriteLine("play " + DateTime.Now.ToString("HH:mm:ss"));
                Application.logMessageReceived += OnLog;
            }
            catch (Exception) { }
        }

        static void OnLog(string msg, string stack, LogType type)
        {
            if (w == null) return;
            try
            {
                if (msg.Contains("_AdditionalShadowParams")) return; // known URP editor noise
                w.WriteLine($"[{type}] {msg}");
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    foreach (var l in stack.Split('\n')) if (l.Length > 0) w.WriteLine("    " + l);
            }
            catch (Exception) { }
        }
    }
}
#endif
