//! `slick` command line: solve a spot file and print / save the results.
//!
//!   slick solve spot.json [--out result.json] [--path 0,1]

use engine::cards::*;
use engine::json::Json;
use engine::solver::{NodeReport, Solver};
use engine::spot::parse_spot;
use std::collections::BTreeMap;

fn main() {
    let args: Vec<String> = std::env::args().collect();
    if args.len() < 3 || args[1] != "solve" {
        eprintln!("usage: slick solve <spot.json> [--out result.json] [--path 0,1,...]");
        std::process::exit(2);
    }
    let text = std::fs::read_to_string(&args[2]).unwrap_or_else(|e| fail(&format!("cannot read {}: {e}", args[2])));
    let mut out_path = None;
    let mut path: Vec<usize> = Vec::new();
    let mut i = 3;
    while i < args.len() {
        match args[i].as_str() {
            "--out" => {
                out_path = args.get(i + 1).cloned();
                i += 2;
            }
            "--path" => {
                path = args
                    .get(i + 1)
                    .map(|s| s.split(',').filter(|x| !x.is_empty()).map(|x| x.parse().unwrap()).collect())
                    .unwrap_or_default();
                i += 2;
            }
            _ => fail(&format!("unknown option {}", args[i])),
        }
    }

    if let Ok(v) = std::env::var("SLICK_VARIANT") {
        let mut it = v.split(',');
        let rp = it.next().unwrap() == "1";
        let ap: f32 = it.next().unwrap().parse().unwrap();
        engine::solver::set_variant(rp, ap);
    }
    let (spot, settings) = parse_spot(&text).unwrap_or_else(|e| fail(&e));
    eprintln!(
        "Slick RNG | {} players | board {} | tree: {} nodes ({} decision points)",
        spot.names.len(),
        spot.board.iter().map(|&c| card_str(c)).collect::<String>(),
        spot.tree.nodes.len(),
        spot.tree.action_node_count()
    );
    if spot.turn_buckets + spot.river_buckets > 0 {
        eprintln!("abstraction: turn {} x4 buckets, river {} buckets", spot.turn_buckets, spot.river_buckets);
    }
    let solver = Solver::new(spot, settings.memory_bytes).unwrap_or_else(|e| fail(&e));
    eprintln!("solving with {} threads (max {:.0}s, {} deals)...", settings.threads, settings.seconds, settings.iterations);
    let mut last = -1.0;
    solver.solve(settings.iterations, settings.seconds, settings.threads, |it, el, bytes| {
        if el - last >= 2.0 {
            last = el;
            eprintln!("  {:>6.1}s  {:>11} deals  {:>8.1} MB", el, it, bytes as f64 / 1e6);
        }
    });
    if solver.out_of_memory() {
        eprintln!("warning: memory limit reached; results are partial. Reduce bet sizes or use more buckets.");
    }
    let iters = solver.iterations.load(std::sync::atomic::Ordering::Relaxed);
    eprintln!("done: {} deals, {} info sets", iters, solver.table.infosets());

    let evs = solver.estimate_ev(200_000, 42);
    println!();
    for (p, (m, se)) in evs.iter().enumerate() {
        println!("EV {:<6} {:>9.3} (±{:.3})", solver.spot.names[p], m, 1.96 * se);
    }
    let rep = solver.node_report(&path).unwrap_or_else(|e| fail(&e));
    print_report(&solver, &rep);

    if let Some(op) = out_path {
        let j = report_json(&solver, &rep, &evs, iters);
        std::fs::write(&op, j.to_string()).unwrap_or_else(|e| fail(&format!("cannot write {op}: {e}")));
        eprintln!("saved {op}");
    }
}

fn fail(msg: &str) -> ! {
    eprintln!("error: {msg}");
    std::process::exit(1)
}

fn by_class(rep: &NodeReport) -> BTreeMap<String, (f64, Vec<f64>)> {
    let na = rep.actions.len();
    let mut m: BTreeMap<String, (f64, Vec<f64>)> = BTreeMap::new();
    for c in &rep.combos {
        let e = m.entry(combo_class(c.combo)).or_insert((0.0, vec![0.0; na]));
        e.0 += c.reach;
        for a in 0..na {
            e.1[a] += c.reach * c.strategy[a];
        }
    }
    for v in m.values_mut() {
        if v.0 > 0.0 {
            for x in v.1.iter_mut() {
                *x /= v.0;
            }
        }
    }
    m
}

fn print_report(solver: &Solver, rep: &NodeReport) {
    println!(
        "\n{} to act (pot {}):",
        solver.spot.names[rep.player],
        engine::tree::fmt_chips(rep.pot)
    );
    for (a, f) in rep.actions.iter().zip(&rep.overall) {
        println!("  {:<16} {:>5.1}%", a, 100.0 * f);
    }
    let classes = by_class(rep);
    let mut rows: Vec<_> = classes.into_iter().filter(|(_, v)| v.0 > 1e-6).collect();
    rows.sort_by(|a, b| b.1 .0.partial_cmp(&a.1 .0).unwrap().then(a.0.cmp(&b.0)));
    println!("\n  {:<6} {}", "hand", rep.actions.iter().map(|a| format!("{:>12}", a)).collect::<String>());
    for (cls, (_, s)) in rows.iter().take(40) {
        println!("  {:<6} {}", cls, s.iter().map(|x| format!("{:>11.0}%", 100.0 * x)).collect::<String>());
    }
    if rows.len() > 40 {
        println!("  ... {} more hand classes in the saved results", rows.len() - 40);
    }
}

fn report_json(solver: &Solver, rep: &NodeReport, evs: &[(f64, f64)], iters: u64) -> Json {
    let num = Json::Num;
    let s = |x: &str| Json::Str(x.to_string());
    let classes = by_class(rep);
    Json::Obj(vec![
        ("players".into(), Json::Arr(solver.spot.names.iter().map(|n| s(n)).collect())),
        ("deals".into(), num(iters as f64)),
        ("ev".into(), Json::Arr(evs.iter().map(|(m, se)| Json::Arr(vec![num(*m), num(*se)])).collect())),
        ("node".into(), Json::Obj(vec![
            ("player".into(), num(rep.player as f64)),
            ("pot".into(), num(rep.pot)),
            ("actions".into(), Json::Arr(rep.actions.iter().map(|a| s(a)).collect())),
            ("overall".into(), Json::Arr(rep.overall.iter().map(|x| num(*x)).collect())),
            ("classes".into(), Json::Obj(classes.into_iter().map(|(k, (w, st))| {
                (k, Json::Obj(vec![("weight".into(), num(w)), ("strategy".into(), Json::Arr(st.into_iter().map(num).collect()))]))
            }).collect())),
            ("combos".into(), Json::Obj(rep.combos.iter().map(|c| {
                (combo_str(c.combo), Json::Obj(vec![("reach".into(), num(c.reach)), ("strategy".into(), Json::Arr(c.strategy.iter().map(|x| num(*x)).collect()))]))
            }).collect())),
        ])),
    ])
}
