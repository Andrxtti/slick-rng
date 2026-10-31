// File check for the Daz content library, run through the PC worker (the worker starts "node solve_full.js <cfg> <secs> <evs> <out>").
// Reads the tail of Daz Studio's own log, lists the Genesis 9 morph folders, and for each character preset lists the files it
// references and which of them are missing from the content folders. No Daz needed (Daz may be busy or stuck in a dialog).
const fs = require('fs'), path = require('path'), zlib = require('zlib'), os = require('os');
const OUT = process.argv[5];
const LIBS = ['C:/Users/Public/Documents/My DAZ 3D Library', path.join(os.homedir(), 'OneDrive/Documents/DAZ 3D/Studio/My Library'), path.join(os.homedir(), 'Documents/DAZ 3D/Studio/My Library')];
const res = { home: os.homedir(), libs: LIBS.map((l) => [l, fs.existsSync(l)]) };
const readDuf = (p) => { const b = fs.readFileSync(p); return (b[0] === 0x1f && b[1] === 0x8b ? zlib.gunzipSync(b) : b).toString('utf8'); };
const exists = (rel) => LIBS.some((l) => fs.existsSync(path.join(l, rel)));
try {
  const logs = [path.join(os.homedir(), 'AppData/Roaming/DAZ 3D/Studio4/log.txt'), path.join(os.homedir(), 'AppData/Roaming/DAZ 3D/Studio4 Public Build/log.txt')];
  for (const l of logs) if (fs.existsSync(l)) { const t = fs.readFileSync(l, 'utf8'); res.log = { file: l, tail: t.slice(-1500) }; break; }
} catch (e) { res.logErr = String(e); }
try {
  const md = 'data/DAZ 3D/Genesis 9/Base/Morphs';
  res.morphDirs = {};
  for (const l of LIBS) { const p = path.join(l, md); if (!fs.existsSync(p)) continue; for (const v of fs.readdirSync(p)) { const vp = path.join(p, v); if (fs.statSync(vp).isDirectory()) res.morphDirs[v] = fs.readdirSync(vp); } }
  const bc = 'data/DAZ 3D/Genesis 9/Base/Morphs/Daz 3D/Base Characters 9';
  for (const l of LIBS) { const p = path.join(l, bc); if (fs.existsSync(p)) res.baseChars = fs.readdirSync(p); }
} catch (e) { res.morphErr = String(e); }
res.presets = {};
for (const ch of ['Matt', 'Ty', 'Fabrice']) {
  const rel = `People/Genesis 9/Characters/${ch} for Genesis 9.duf`;
  try {
    const lib = LIBS.find((l) => fs.existsSync(path.join(l, rel))); if (!lib) { res.presets[ch] = 'preset not found'; continue; }
    const txt = readDuf(path.join(lib, rel)), refs = new Set();
    for (const m of txt.matchAll(/"(?:url|source)"\s*:\s*"([^"#]+)/g)) { let u = m[1]; try { u = decodeURIComponent(u); } catch (e) {} if (u.startsWith('/')) refs.add(u.slice(1)); }
    const list = [...refs].map((r) => ({ r, ok: exists(r) }));
    let anim = [], mods = [];
    try { const j = JSON.parse(txt), sc = j.scene || {};
      anim = (sc.animations || []).map((a) => [decodeURIComponent(a.url || ''), JSON.stringify(a.keys || a.value)]);
      mods = (sc.modifiers || []).map((m) => decodeURIComponent(m.url || m.id || ''));
    } catch (e) { anim = ['parse error ' + e]; }
    res.presets[ch] = { bytes: txt.length, refs: list.length, missing: list.filter((x) => !x.ok).map((x) => x.r), anim, mods };
  } catch (e) { res.presets[ch] = 'error ' + e; }
}
fs.writeFileSync(OUT, JSON.stringify(res, null, 1));
