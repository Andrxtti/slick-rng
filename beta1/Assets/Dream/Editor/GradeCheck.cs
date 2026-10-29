// The same-grade check (BETA1 §5): every hand class in every situation of every library table, every possible answer,
// graded by the old project's LabelFor (OldGrading, copied as text) and by beta1's Hands.Grade. Any difference is listed.
//   remote: grade-check   -> Logs/grade_check.txt
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dream.Game;
using UnityEditor;
using UnityEngine;

namespace Dream.EditorTools
{
    [InitializeOnLoad]
    public static class GradeCheck
    {
        static GradeCheck() { Remote.Extra["grade-check"] = _ => Run(); }

        public static void Run()
        {
            Lib.Load();
            int spots = 0, answers = 0, diff = 0; var lines = new List<string>(); var byLab = new Dictionary<string, int>();
            var rng = new Rng("grade-check");
            foreach (var kv in Lib.Spots)
            {
                var tables = kv.Value as List<object>; if (tables == null) continue;
                for (int ti = 0; ti < tables.Count; ti++)
                {
                    var table = (Dictionary<string, object>)tables[ti];
                    var nodes = J.L(table, "n");
                    for (int ni = 0; ni < nodes.Count; ni++)
                    {
                        var node = (Dictionary<string, object>)nodes[ni];
                        foreach (var r in J.L(node, "r"))
                        {
                            var sp = Hands.BuildSpot(kv.Key, ti, table, node, ni, (List<object>)r, 0.5, rng); spots++;
                            var ev = sp.ev != null && sp.evScale != 1 ? sp.ev.Select(x => x * sp.evScale).ToList() : sp.ev;
                            for (int j = 0; j < sp.acts.Count; j++)
                            {
                                var a = OldGrading.LabelFor(sp.strat, ev, j, sp.unit == "bbicm" ? "bb" : sp.unit, sp.hard, sp.acts[j]);
                                var b = Hands.Grade(sp, j); answers++;
                                byLab[b.lab] = (byLab.TryGetValue(b.lab, out var c) ? c : 0) + 1;
                                if (a.lab != b.lab || a.bestIdx != b.bestIdx || a.loss != b.loss)
                                {
                                    diff++;
                                    if (lines.Count < 50) lines.Add($"{kv.Key} t{ti} n{ni} cls{sp.cls} j{j}: old {a.lab}/{a.loss}/{a.bestIdx} new {b.lab}/{b.loss}/{b.bestIdx}");
                                }
                            }
                        }
                    }
                }
            }
            var head = $"grade-check: {spots} hands, {answers} answers graded by old and new code, {diff} differences. Labels: " + string.Join(", ", byLab.OrderBy(k => k.Key).Select(k => $"{k.Key} {k.Value}"));
            File.WriteAllLines(Path.Combine(Application.dataPath, "..", "Logs", "grade_check.txt"), new[] { head }.Concat(lines));
            Remote.Log(head);
        }
    }
}
