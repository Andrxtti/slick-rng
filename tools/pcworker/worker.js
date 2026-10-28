// Confirmed Fire Poker · PC solve worker. Start it with start-worker.bat (or: node worker.js) and leave the window open.
// Claude drops solve jobs into jobs\, this runs them in parallel on this PC and writes the results to out\.
const fs = require('fs'), path = require('path'), os = require('os'), { spawn } = require('child_process');
const R = __dirname, J = (d) => path.join(R, d);
for (const d of ['jobs', 'running', 'done', 'failed', 'out', 'logs']) fs.mkdirSync(J(d), { recursive: true });
const CPUS = os.cpus().length, TOTAL_MB = Math.round(os.totalmem() / 1e6);
// the PC is dedicated to solving: one solve per thread (each needs about 0.3 GB of memory)
const SLOTS = +(process.env.SLOTS || Math.max(1, Math.min(CPUS, Math.floor((TOTAL_MB - 2500) / 350))));
let running = 0; const live = new Map();
const log = (m) => { const l = `${new Date().toLocaleString()}  ${m}\n`; process.stdout.write(l); fs.appendFileSync(J('logs/worker.log'), l); };
const beat = () => fs.writeFileSync(J('heartbeat.json'), JSON.stringify({ t: Date.now(), slots: SLOTS, running, jobs: [...live.keys()], cpus: CPUS,
  cpu: os.cpus()[0].model, freeMB: Math.round(os.freemem() / 1e6), totalMB: TOTAL_MB, node: process.version, load: os.loadavg() }));
function next() {
  while (running < SLOTS) {
    const f = fs.readdirSync(J('jobs')).filter((x) => x.endsWith('.json')).sort()[0]; if (!f) return;
    try { fs.renameSync(J('jobs/' + f), J('running/' + f)); } catch (e) { return; }
    let job; try { job = JSON.parse(fs.readFileSync(J('running/' + f), 'utf8')); } catch (e) { fs.renameSync(J('running/' + f), J('failed/' + f)); continue; }
    const out = J('out/' + job.id + '.json'), tmp = out + '.part';
    running++; live.set(job.id, Date.now()); log(`start ${job.id}`);
    const p = spawn(process.execPath, ['solve_full.js', job.cfg, String(job.secs), String(job.evs || 60), tmp],
      { cwd: J(job.cwd || 'tools/pf'), env: { ...process.env, ...(job.env || {}) }, stdio: ['ignore', 'pipe', 'pipe'], windowsHide: true });
    // above normal: ahead of everything else on the PC, but Windows and the Claude app (the link to this PC) stay responsive
    try { os.setPriority(p.pid, os.constants.priority.PRIORITY_ABOVE_NORMAL); } catch (e) {}
    const lf = fs.createWriteStream(J('logs/' + job.id + '.log')); p.stdout.pipe(lf); p.stderr.pipe(lf);
    p.on('exit', (code) => {
      running--; const mins = ((Date.now() - live.get(job.id)) / 60000).toFixed(1); live.delete(job.id);
      if (code === 0 && fs.existsSync(tmp)) { fs.renameSync(tmp, out); fs.renameSync(J('running/' + f), J('done/' + f)); log(`done  ${job.id} (${mins} min)`); }
      else {
        job.tries = (job.tries || 0) + 1; fs.writeFileSync(J('running/' + f), JSON.stringify(job));
        fs.renameSync(J('running/' + f), J((job.tries >= 2 ? 'failed/' : 'jobs/') + f)); log(`FAIL  ${job.id} (exit ${code}, try ${job.tries})`);
      }
      beat(); next();
    });
  }
}
for (const f of fs.readdirSync(J('running'))) fs.renameSync(J('running/' + f), J('jobs/' + f));   // restarted: unfinished jobs go back in the queue
log(`worker up: ${SLOTS} solves at a time on ${CPUS} threads (${os.cpus()[0].model}), ${TOTAL_MB} MB memory, Node ${process.version}`);
beat(); next(); setInterval(() => { beat(); next(); }, 15000);
