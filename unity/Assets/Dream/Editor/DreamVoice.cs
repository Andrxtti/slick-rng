// Voice lines: every spoken beat in the story (after the Unity overrides) listed for the voice tool.
//   Dream > Export voice lines   (or the remote command "voice-list")  ->  Logs/voice_lines.json
//   then: python3 unity/tools/voice.py Logs/voice_lines.json Assets/Dream/Resources/Dream/Voice
// A line's clip is Resources/Dream/Voice/v_<key>, key = Voice.Key(speaker, text), so a text edit simply
// means a new clip and an old one stops matching; text messages and lines built at runtime (fn) have no clip.
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CFDream.EditorTools
{
    public static class DreamVoice
    {
        [MenuItem("Dream/Export voice lines")]
        public static void Export()
        {
            DB.Load();
            var sb = new StringBuilder("[\n"); int n = 0; var seen = new HashSet<string>();
            void Line(string sceneId, Beat b)
            {
                if (b == null || b.text || b.fn || string.IsNullOrEmpty(b.t) || string.IsNullOrEmpty(b.sp)) return;
                string key = Voice.Key(b.sp, b.t); if (!seen.Add(key)) return;
                if (n > 0) sb.Append(",\n");
                sb.Append("{\"scene\":").Append(Q(sceneId)).Append(",\"sp\":").Append(Q(b.sp)).Append(",\"who\":").Append(Q(b.who ?? "")).Append(",\"mood\":").Append(Q(b.mood ?? "")).Append(",\"key\":").Append(Q(key)).Append(",\"t\":").Append(Q(b.t)).Append("}");
                n++;
            }
            foreach (var kv in DB.Scenes)
                foreach (var b in kv.Value.beats)
                {
                    Line(kv.Key, b);
                    if (b.choice != null) foreach (var c in b.choice) Line(kv.Key, c.line);
                }
            foreach (var (sp, t) in Story.WitnessLines()) Line("after", new Beat { sp = sp, t = t });   // the aftermath witnesses (David's and Slick's are texts, left out by Line)
            sb.Append("\n]\n");
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs")); Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "voice_lines.json"), sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"[Dream] {n} voice lines written to Logs/voice_lines.json");
        }
        static string Q(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\"";
    }
}
