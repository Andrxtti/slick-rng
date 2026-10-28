// Offline cutscene renders: play a story scene in the editor and record the Game view to an MP4 with Unity Recorder,
// advancing each line by itself (voice length + a beat, choices take the first option). This is how cutscenes ship
// on phones: rendered once here at full quality, played back as video.
//   remote: render <sceneId>      -> C:\Dev\Builds\Cutscenes\<sceneId>.mp4
//           render-all            -> every scene with a shot list, in order
//   Dream > Render > ...          (same, from the menu, in Play mode or not: it enters Play mode itself)
// Needs com.unity.recorder (Packages/manifest.json). Output: 1920x1080, 30 fps, H.264 high quality.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace CFDream.EditorTools
{
    [InitializeOnLoad]
    public static class DreamRender
    {
        public static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Builds", "Cutscenes"));
        public const int Width = 1920, Height = 1080, Fps = 30;
        public const float Beat = 0.9f, MinLine = 2.2f, MaxLine = 14f;   // seconds after the voice ends; floor and ceiling per line

        static readonly Queue<string> queue = new Queue<string>();
        static string current; static RecorderController rec; static double lineStart, started; static int lastIndex = -1; static string lastId;
        static bool starting;

        static DreamRender() { EditorApplication.update += Tick; }

        [MenuItem("Dream/Render/Diner (p_diner)")] static void MenuDiner() => Render("p_diner");
        [MenuItem("Dream/Render/Office (p_office)")] static void MenuOffice() => Render("p_office");
        [MenuItem("Dream/Render/All scenes")] static void MenuAll() => RenderAll();

        public static void Render(string id) { queue.Enqueue(id); Log($"queued {id} ({queue.Count} waiting)"); }
        public static void RenderAll()
        {
            var dir = Path.Combine(Application.dataPath, "Dream", "Resources", "Dream", "direction.json");
            var ids = new List<string>();
            try { foreach (var k in (CFDream.Json.Parse(File.ReadAllText(dir)) as Dictionary<string, object>).Keys) ids.Add(k); } catch (Exception e) { Log("direction.json: " + e.Message); return; }
            foreach (var id in ids) queue.Enqueue(id);
            Log($"queued {ids.Count} scenes");
        }

        static void Log(string m) { Debug.Log("[Dream] render: " + m); try { File.AppendAllText(Path.Combine(Application.dataPath, "..", "Logs", "dream_render.txt"), $"{DateTime.Now:HH:mm:ss} {m}\n"); } catch { } }

        static void Tick()
        {
            if (current == null)
            {
                if (queue.Count == 0 || starting) return;
                if (!EditorApplication.isPlaying) { starting = true; EditorApplication.isPlaying = true; EditorApplication.delayCall += () => starting = false; return; }
                if (CFDream.Game.State == null)
                {   // a throwaway career so every scene can be replayed
                    var s = new CFDream.State { made = true, name = "Render", ch = 7, job = "quit", br = 250000, peak = 250000 };
                    CFDream.Game.State = s; foreach (var it in CFDream.DB.Items) s.owned.Add(it.Key);
                }
                Begin(queue.Dequeue()); return;
            }
            // recording: advance lines by voice length, pick the first choice, stop when the scene is over
            var sc = CFDream.Game.Scene;
            bool over = sc == null || sc.id != current || CFDream.Game.View != CFDream.View.Scene;
            if (over) { End(); return; }
            int idx = sc.i * 100 + sc.extra.Count;
            if (idx != lastIndex) { lastIndex = idx; lineStart = EditorApplication.timeSinceStartup; return; }
            double held = EditorApplication.timeSinceStartup - lineStart;
            bool speaking = CFDream.Sound.Speaking;
            if (held < MinLine || speaking && held < MaxLine) return;
            var b = CFDream.Game.CurrentBeat();
            if (b != null && b.choice != null && sc.extra.Count == 0) CFDream.Game.Choose(0); else CFDream.Game.Advance();
            lineStart = EditorApplication.timeSinceStartup + Beat;   // the beat after the voice: the next line starts after it
        }

        static void Begin(string id)
        {
            if (!CFDream.DB.Scenes.ContainsKey(id)) { Log($"no scene {id}"); return; }
            Directory.CreateDirectory(OutDir);
            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            settings.SetRecordModeToManual(); settings.FrameRate = Fps; settings.CapFrameRate = true;
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "cutscene"; movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings { EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High, Codec = CoreEncoderSettings.OutputCodec.MP4 };
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = Width, OutputHeight = Height };
            movie.CaptureAudio = true;
            movie.OutputFile = Path.Combine(OutDir, id);
            settings.AddRecorderSettings(movie);
            rec = new RecorderController(settings);
            rec.PrepareRecording(); rec.StartRecording();
            current = id; lastIndex = -1; lineStart = EditorApplication.timeSinceStartup; started = lineStart;
            CFDream.Game.PlayScene(id, true);
            Log($"recording {id}");
        }

        static void End()
        {
            if (rec != null && rec.IsRecording()) rec.StopRecording();
            Log($"{current}: {EditorApplication.timeSinceStartup - started:0.0}s -> {Path.Combine(OutDir, current + ".mp4")}");
            rec = null; current = null;
            if (queue.Count == 0) EditorApplication.isPlaying = false;
        }
    }
}
