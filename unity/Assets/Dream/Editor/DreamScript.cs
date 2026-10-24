// The script as one readable file, for editing dialogue without touching code.
//   Dream > Export script  (remote "script-export")  ->  Logs/script.md
//   edit the text, then  python3 unity/tools/script_import.py Logs/script.md Assets/Dream/Resources/Dream/script_edits.json
//   (DB.Load applies script_edits.json over the scenes: text, speaker name and choice labels, by scene and line number)
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CFDream.EditorTools
{
    public static class DreamScript
    {
        [MenuItem("Dream/Export script")]
        public static void Export()
        {
            DB.Load();
            var sb = new StringBuilder();
            sb.Append("# The Dream — script\n\n");
            sb.Append("Edit the words after the colon. Keep the numbers and the speaker ids in brackets as they are; a line that starts with `Choice:` is a button the player taps (the arrow says where it leads; after `|` is the reply it gets).\n");
            sb.Append("`[n]` is the narrator, `[you]` is the player. `/` in a line is a line break on screen. Texts on the phone are marked (text).\n\n");
            foreach (var kv in DB.Scenes.OrderBy(k => Order(k.Key)))
            {
                var sc = kv.Value;
                sb.Append($"## {kv.Key} — {sc.title ?? ""}{(sc.sub != null ? " · " + sc.sub : "")} ({sc.place})\n\n");
                for (int i = 0; i < sc.beats.Count; i++)
                {
                    var b = sc.beats[i];
                    sb.Append($"{i + 1}. {Who(b)}{(b.text ? " (text)" : "")}{(b.mood != null ? " (" + b.mood + ")" : "")}: {(b.fn ? "(built at runtime)" : One(b.t))}\n");
                    if (b.choice != null) foreach (var c in b.choice) sb.Append($"    - Choice: {One(c.t)}{(c.go != null ? " → " + c.go : "")}{(c.line != null ? " | " + Who(c.line) + ": " + One(c.line.t) : "")}\n");
                }
                sb.Append("\n");
            }
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs")); Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "script.md"), sb.ToString(), new UTF8Encoding(false));
            Debug.Log($"[Dream] script with {DB.Scenes.Count} scenes written to Logs/script.md");
        }
        static string Who(Beat b)
        {
            if (b.sp == "n") return "Narrator [n]";
            if (b.sp == "you") return "You [you]";
            string name = b.who;
            if (name == null)
            {
                if (b.sp != null && b.sp.StartsWith("L:")) { var lg = DB.Legend(b.sp.Substring(2)); name = lg != null ? J.S(lg, "name", b.sp) : b.sp; }
                else { var p = DB.Person(b.sp); name = p != null ? J.S(p, "name", b.sp) : b.sp; }
            }
            return $"{name} [{b.sp}]";
        }
        static string One(string t) => (t ?? "").Replace("\r", "").Replace("\n", " / ");
        // the story order first, then the rest alphabetically
        static readonly string[] First = { "p_office", "p_diner", "p_after", "p_quit", "p_keep", "c1", "trentbeat", "c2", "c3", "c4", "c5", "c6", "c7" };
        static string Order(string id) { int i = System.Array.IndexOf(First, id); return (i >= 0 ? i.ToString("00") : "99") + id; }
    }
}
