// Peek at the local Claude Code session working in C:\Dev\slick-rng (PC worker: "node solve_full.js <cfg> <secs> <evs> <out>"):
// running processes, the newest transcript's last steps (assistant text and tool calls, trimmed), and the repo state.
const fs = require('fs'), path = require('path'), os = require('os'), { spawnSync } = require('child_process');
const OUT = process.argv[5], res = {};
const run = (cmd, args) => { const r = spawnSync(cmd, args, { encoding: 'utf8', windowsHide: true, timeout: 60000 }); return ((r.stdout || '') + (r.stderr || '')).trim(); };
res.procs = run('tasklist', ['/FO', 'CSV', '/NH']).split(/\r?\n/).filter((l) => /claude|unity|blender|daz|ffmpeg|python/i.test(l)).map((l) => l.split('","').slice(0, 5).join(' | ').replace(/"/g, ''));
const dir = path.join(os.homedir(), '.claude', 'projects', 'C--Dev-slick-rng');
try {
  const files = fs.readdirSync(dir).filter((f) => f.endsWith('.jsonl')).map((f) => ({ f, t: fs.statSync(path.join(dir, f)).mtimeMs, s: fs.statSync(path.join(dir, f)).size })).sort((a, b) => b.t - a.t);
  res.transcripts = files.slice(0, 3).map((x) => [x.f, new Date(x.t).toISOString(), x.s]);
  if (files.length) {
    const lines = fs.readFileSync(path.join(dir, files[+(process.env.PEEK_IDX||0)].f), 'utf8').trim().split(/\n/);
    res.lines = lines.length; const steps = [];
    for (const l of lines.slice(-60)) {
      let d; try { d = JSON.parse(l); } catch (e) { continue; }
      const c = d.message && d.message.content; const who = d.type;
      if (typeof c === 'string') steps.push(`${d.timestamp || ''} ${who}: ${c.slice(0, 200)}`);
      else if (Array.isArray(c)) for (const b of c) {
        if (b.type === 'text') steps.push(`${d.timestamp || ''} ${who} text: ${b.text.slice(0, 300)}`);
        else if (b.type === 'tool_use') steps.push(`${d.timestamp || ''} tool ${b.name}: ${JSON.stringify(b.input).slice(0, 220)}`);
        else if (b.type === 'tool_result') steps.push(`${d.timestamp || ''} result: ${JSON.stringify(b.content).slice(0, 160)}`);
      }
    }
    res.steps = steps.slice(-30);
  }
} catch (e) { res.err = String(e); }
const git = 'C:\\Program Files\\Git\\cmd\\git.exe';
res.git = run(git, ['-C', 'C:\\Dev\\slick-rng', 'log', '-5', '--oneline', '--all']) + '\n' + run(git, ['-C', 'C:\\Dev\\slick-rng', 'status', '--short']).slice(0, 800);
try { res.beta1 = fs.readdirSync('C:\\Dev\\slick-rng\\beta1'); } catch (e) { res.beta1 = String(e); }
fs.writeFileSync(OUT, JSON.stringify(res, null, 1));
