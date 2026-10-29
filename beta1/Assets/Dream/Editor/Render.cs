// Offline film renders with Unity Recorder: open the scene, enter Play mode, record the Game view to an MP4 until the
// film says it is done (Take.Done) or the time limit passes. Time is locked to 30 fps, so a slow PC drops no frames.
//   remote: render <id>   -> C:\Dev\Builds\Beta1\Film\<id>.mp4   ("test" records Scenes/Test.unity for 4 s)
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dream.EditorTools
{
    [InitializeOnLoad]
    public static class Render
    {
        public static string OutDir => @"C:\Dev\Builds\Beta1\Film";
        public const int Width = 2560, Height = 1440, Fps = 30;   // supersampled: published films are scaled down (lanczos) from here
        const float Limit = 240f;

        static readonly Queue<string> queue = new Queue<string>();
        static string current; static RecorderController rec; static int frames; static float limit;

        static Render() { EditorApplication.update += Tick; EditorApplication.playModeStateChanged += OnPlay; }

        public static void Queue(string id) { queue.Enqueue(id); Remote.Log($"render: queued {id} ({queue.Count} waiting)"); }

        static void Tick()
        {
            if (current != null || queue.Count == 0 || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            current = queue.Dequeue();
            var scene = current == "test" ? "Assets/Dream/Scenes/Test.unity" : "Assets/Dream/Scenes/Film.unity";
            limit = current == "test" ? 4f : Limit;
            if (!File.Exists(scene)) { Remote.Log($"render: no {scene}"); current = null; return; }
            EditorSceneManager.OpenScene(scene);
            Take.Id = current; Take.Done = false; Take.Recording = true;
            EditorApplication.isPlaying = true;
        }

        static void OnPlay(PlayModeStateChange s)
        {
            if (current == null) return;
            if (s == PlayModeStateChange.EnteredPlayMode) Begin();
            if (s == PlayModeStateChange.EnteredEditMode) { Take.Recording = false; current = null; }
        }

        static void Begin()
        {
            Directory.CreateDirectory(OutDir);
            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            settings.SetRecordModeToManual(); settings.FrameRate = Fps; settings.CapFrameRate = true;
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "film"; movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings { EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High, Codec = CoreEncoderSettings.OutputCodec.MP4 };
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = Width, OutputHeight = Height };
            movie.CaptureAudio = true;
            movie.OutputFile = Path.Combine(OutDir, current);
            settings.AddRecorderSettings(movie);
            rec = new RecorderController(settings);
            rec.PrepareRecording(); rec.StartRecording();
            frames = 0;
            EditorApplication.update += Watch;
            Remote.Log($"render: recording {current}");
        }

        static void Watch()
        {
            if (!EditorApplication.isPlaying) { EditorApplication.update -= Watch; return; }
            frames = Time.frameCount;
            if (!Take.Done && Time.time < limit) return;
            EditorApplication.update -= Watch;
            if (rec != null && rec.IsRecording()) rec.StopRecording();
            Remote.Log($"render: {current} {Time.time:0.0}s -> {Path.Combine(OutDir, current + ".mp4")}");
            rec = null;
            EditorApplication.isPlaying = false;
        }
    }
}
