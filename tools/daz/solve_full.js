// Daz Studio runner for the PC worker (the worker starts "node solve_full.js <job.json> <secs> <evs> <out>" in the job's
// cwd; this folder's copy runs a Daz script instead of a solve). The job file names the script; this launches Daz Studio
// with it, waits for the script to write its "done" marker (or the time limit), closes Daz, and writes the script's log
// to <out> so the job shows up in out/ like any other.
//   job json: { "script": "probe.dsa", "secs": 600 }   (script path relative to this folder)
const fs = require('fs'), path = require('path'), { spawn, execSync } = require('child_process');
const [jobFile, secsArg, , OUT] = process.argv.slice(2);
const job = JSON.parse(fs.readFileSync(jobFile, 'utf8'));
const here = __dirname, script = path.resolve(here, job.script), secs = +(job.secs || secsArg || 600);
const logFile = path.join(here, 'run.log'), doneFile = path.join(here, 'run.done');
for (const f of [logFile, doneFile]) try { fs.unlinkSync(f); } catch (e) {}
const candidates = [process.env.DAZ_EXE, 'C:\\Program Files\\DAZ 3D\\DAZStudio4\\DAZStudio.exe', 'C:\\Program Files\\DAZ 3D\\DAZStudio4 Public Build\\DAZStudio.exe'].filter(Boolean);
const exe = candidates.find((p) => fs.existsSync(p));
const report = (extra) => { let log = ''; try { log = fs.readFileSync(logFile, 'utf8'); } catch (e) {} fs.writeFileSync(OUT, JSON.stringify({ exe: exe || null, script, done: fs.existsSync(doneFile), extra, log })); };
if (!exe) { report('Daz Studio not found in: ' + candidates.join(' | ')); process.exit(0); }
// the script reads its paths from here (DAZ Script has no argv): log, done marker, and the folder of this runner
fs.writeFileSync(path.join(here, 'run.cfg.json'), JSON.stringify({ log: logFile, done: doneFile, dir: here, args: job.args || {} }));
const p = spawn(exe, ['-noPrompt', script], { detached: false, windowsHide: false });
const t0 = Date.now();
const tick = setInterval(() => {
  const done = fs.existsSync(doneFile), late = Date.now() - t0 > secs * 1000;
  if (!done && !late) return;
  clearInterval(tick);
  setTimeout(() => { try { execSync(`taskkill /PID ${p.pid} /T /F`); } catch (e) {} report(late && !done ? 'time limit reached' : 'ok'); process.exit(0); }, done ? 3000 : 0);
}, 2000);
