// Balance by numbers: play an event many times on the real engine (Hands: deal, grade, stack, coolers, Place, payouts),
// no UI, with a player who picks the solver's best play with probability q and another play otherwise.
//   remote: sim <q> <n>   -> Logs/sim.txt: per Chapter 1 event, the measured accuracy, P(cash), P(win), P(bust) and mean net
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dream.Game;
using UnityEditor;
using UnityEngine;

namespace Dream.EditorTools
{
    [InitializeOnLoad]
    public static class Sim
    {
        static Sim()
        {
            Remote.Extra["place"] = a => { var p = a.Split(' ').Select(x => double.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray(); Hands.PlaceBase = p[0]; Hands.PlaceSlope = p[1]; Hands.PlaceLuck = p[2]; if (p.Length > 3) Hands.PlaceBand = p[3]; if (p.Length > 4) Hands.PlaceOnline = p[4]; Remote.Log($"place: base {p[0]} slope {p[1]} luck {p[2]} band {Hands.PlaceBand}"); };
            Remote.Extra["sim"] = a =>
            {
                var p = a.Split(' '); double q = double.Parse(p[0], System.Globalization.CultureInfo.InvariantCulture); int n = int.Parse(p[1]);
                var lines = new List<string>(); Lib.Load(); Hands.Chapter = 1;
                foreach (var ev in Lib.Events.Where(e => e.id != "local"))
                {
                    var rng = new System.Random(ev.id.GetHashCode() ^ (int)(q * 1000));
                    double net = 0, accSum = 0; int cash = 0, win = 0, bust = 0, decs = 0; var nets = new List<double>();
                    for (int i = 0; i < n; i++)
                    {
                        var run = Hands.Start(ev, $"sim|{ev.id}|{q}|{i}");
                        while (true)
                        {
                            var sp = Hands.Deal(run); int pick = Hands.Grade(sp, 0).bestIdx;
                            if (rng.NextDouble() > q && sp.acts.Count > 1) { var o = Enumerable.Range(0, sp.acts.Count).Where(j => j != pick).ToList(); pick = o[rng.Next(o.Count)]; }
                            Hands.Answer(run, pick);
                            var what = Hands.Next(run);
                            if (what == "finish" || what == "cooler") break;
                        }
                        accSum += Hands.Accuracy(run.results) * run.results.Count; decs += run.results.Count;
                        double prize;
                        if (ev.kind == "cash") prize = ev.buyin + (run.bust ? -ev.buyin : Hands.CashNet(run, (run.rng.Next() - 0.5) * 0.3 + ev.soft * 0.5));
                        else { int place = Hands.Place(run); int paid = Hands.Paid(ev); prize = place <= paid ? Hands.PrizeFor(ev, place) : 0; if (place == 1) win++; if (place <= paid) cash++; }
                        if (run.bust) bust++;
                        double nt = prize - (ev.free ? 0 : ev.buyin); net += nt; nets.Add(nt);
                    }
                    nets.Sort();
                    lines.Add($"[{Hands.PlaceBase}/{Hands.PlaceSlope}/{Hands.PlaceLuck}/{Hands.PlaceBand}] {ev.id,-9} q {q:0.00}  acc {accSum / System.Math.Max(1, decs):0}%  cash {100.0 * cash / n:0}%  win {100.0 * win / n:0.0}%  bust {100.0 * bust / n:0}%  mean net {net / n:+0;-0}  median {nets[n / 2]:+0;-0}  (buy-in {ev.buyin}, field {ev.field})");
                }
                File.AppendAllLines(Path.Combine(Application.dataPath, "..", "Logs", "sim.txt"), lines);
                foreach (var l in lines) Remote.Log("sim: " + l);
            };
        }
    }
}
