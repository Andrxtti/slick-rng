// ================================================================ 3D head: a lofted skull, sculpted face, painted skin
// The head is a stack of rounded cross-sections (crown to chin) so face shapes are real shapes, then the features
// (brow, nose, lips, chin) are sculpted along the surface normals. Eyes sit behind almond openings cut in the skin.
const dr3Ss = (a, b, x) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };
const dr3G2 = (x, y, cx, cy, sx, sy) => Math.exp(-(((x - cx) / sx) ** 2 + ((y - cy) / sy) ** 2));
// face shapes: half-width knots down the face (y, width), where the chin cap begins, how far the chin recedes
const DR3_SHAPE = {
  oval: { k: [[0.1, 0.89], [-0.15, 0.88], [-0.4, 0.82], [-0.6, 0.72], [-0.78, 0.56], [-0.9, 0.4], [-0.97, 0.2]], yc: -0.97, rec: 0.2 },
  round: { k: [[0.1, 0.92], [-0.15, 0.92], [-0.4, 0.84], [-0.6, 0.7], [-0.78, 0.54], [-0.9, 0.4], [-0.97, 0.24]], yc: -0.97, rec: 0.12 },
  angular: { k: [[0.1, 0.89], [-0.15, 0.88], [-0.4, 0.82], [-0.62, 0.72], [-0.8, 0.58], [-0.92, 0.45], [-0.97, 0.35]], yc: -0.97, rec: 0.24 },
};
function dr3Mono(pts, y) { // monotone cubic through (y, w) points sorted by descending y
  const n = pts.length, xs = pts.map((p) => -p[0]), ys = pts.map((p) => p[1]), x = -y; if (x <= xs[0]) return ys[0]; if (x >= xs[n - 1]) return ys[n - 1];
  const d = [], m = [];
  for (let i = 0; i < n - 1; i++) d.push((ys[i + 1] - ys[i]) / (xs[i + 1] - xs[i]));
  m[0] = d[0]; m[n - 1] = d[n - 2]; for (let i = 1; i < n - 1; i++) m[i] = d[i - 1] * d[i] <= 0 ? 0 : (d[i - 1] + d[i]) / 2;
  let i = 0; while (x > xs[i + 1]) i++; const h = xs[i + 1] - xs[i], u = (x - xs[i]) / h, u2 = u * u, u3 = u2 * u;
  return (2 * u3 - 3 * u2 + 1) * ys[i] + (u3 - 2 * u2 + u) * h * m[i] + (-2 * u3 + 3 * u2) * ys[i + 1] + (u3 - u2) * h * m[i + 1];
}
function dr3Ring(S, y) {
  const k0 = S.k[0][1]; let wx, zf, zb;
  if (y >= 0.1) { const s = Math.sqrt(Math.max(0, 1 - ((y - 0.1) / 0.9) ** 2)); wx = k0 * s; zf = 0.97 * s; zb = 0.98 * s; }
  else {
    const tt = Math.min(1, (0.05 - y) / 1.05); zf = 0.97 * (1 - S.rec * tt * tt); zb = 0.98 * (1 - 0.4 * Math.pow(Math.max(0, tt), 1.4));
    if (y >= S.yc) wx = dr3Mono(S.k, y);
    else { const u = (S.yc - y) / (S.yc + 1), c = Math.sqrt(Math.max(0, 1 - u * u)); wx = S.k[S.k.length - 1][1] * c; zf *= c; zb *= c; }
  }
  return { wx, zf, zb };
}
// face sculpting: look.sc holds sliders in -1..1; every feature reads its numbers from here
function dr3Feat(look) {
  const sc = (look && look.sc) || {}, k = (n) => Math.max(-1, Math.min(1, +sc[n] || 0));
  return { fw: k('fw'), jw: k('jw'), ch: k('ch'), cb: k('cb'), cf: k('cf'), nw: 1 + 0.36 * k('nw'), nl: 1.4 * k('nl'), nb: k('nb'), lp: 1 + 0.45 * k('lp'), mw: 1 + 0.16 * k('mw'),
    es: 1 + 0.12 * k('es'), ex: 0.34 + 0.032 * k('ep'), tilt: 0.06 * k('et'), bh: 0.035 * k('bh'), er: 1 + 0.24 * k('er') };
}
function dr3Head(look, opt) {
  const S0 = DR3_SHAPE[look.face] || DR3_SHAPE.oval, F = dr3Feat(look), age = look.age || 'young', old = age === 'old', yng = age === 'young';
  // the face's own shape with width and jaw applied, so painting and hair follow the sculpted head
  const S = { k: S0.k.map(([y, w]) => [y, w * (1 + 0.06 * F.fw) * (1 + 0.13 * F.jw * dr3Ss(-0.35, -0.85, y))]), yc: S0.yc, rec: S0.rec * (1 - 0.45 * F.ch) };
  const C = 144, R = 118, N = 2.3, pos = new Float32Array((C + 1) * (R + 1) * 3), uv = new Float32Array((C + 1) * (R + 1) * 2), idx = [];
  for (let j = 0; j <= R; j++) {
    const y = -Math.cos(Math.PI * j / R), r = dr3Ring(S, y);
    for (let i = 0; i <= C; i++) {
      const ph = (i / C - 0.5) * Math.PI * 2, s = Math.sin(ph), c = Math.cos(ph), k = j * (C + 1) + i;
      pos[k * 3] = r.wx * Math.sign(s) * Math.pow(Math.abs(s), 2 / N); pos[k * 3 + 1] = y; pos[k * 3 + 2] = (c > 0 ? r.zf : r.zb) * Math.sign(c) * Math.pow(Math.abs(c), 2 / N);
      uv[k * 2] = i / C; uv[k * 2 + 1] = (y + 1) / 2;
    }
  }
  for (let j = 0; j < R; j++) for (let i = 0; i < C; i++) { const a = j * (C + 1) + i, b = a + 1, c2 = a + C + 1, d = c2 + 1; idx.push(a, b, c2, b, d, c2); }
  const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.BufferAttribute(pos, 3)); g.setAttribute('uv', new THREE.BufferAttribute(uv, 2)); g.setIndex(idx);
  const weld = () => { // smooth the seam at the back and the poles
    g.computeVertexNormals(); const n = g.attributes.normal;
    for (let j = 0; j <= R; j++) { const a = j * (C + 1), b = a + C, nx = n.getX(a) + n.getX(b), ny = n.getY(a) + n.getY(b), nz = n.getZ(a) + n.getZ(b), l = Math.hypot(nx, ny, nz) || 1; n.setXYZ(a, nx / l, ny / l, nz / l); n.setXYZ(b, nx / l, ny / l, nz / l); }
    for (const j of [0, R]) { let sx = 0, sy = 0, sz = 0; for (let i = 0; i <= C; i++) { const k = j * (C + 1) + i; sx += n.getX(k); sy += n.getY(k); sz += n.getZ(k); } const l = Math.hypot(sx, sy, sz) || 1; for (let i = 0; i <= C; i++) n.setXYZ(j * (C + 1) + i, sx / l, sy / l, sz / l); }
  };
  weld();
  // the mouth line and its lift: smile, smug (one side), frown, flat
  const mouth = (opt && opt.mouth) || (opt && opt.smile ? 'smile' : 'smug');
  const lift = (x) => { const u = Math.min(1, Math.abs(x) / (0.24 * F.mw)), q = u * u; return mouth === 'smile' ? 0.052 * q : mouth === 'frown' ? -0.04 * q : mouth === 'smug' ? (x > 0 ? 0.058 * q : 0.004 * q) : 0.002 * q; };
  const N0 = g.attributes.normal, P = g.attributes.position;
  for (let k = 0; k < P.count; k++) {
    const X = P.getX(k), Y = P.getY(k), Z = P.getZ(k), ax = Math.abs(X);
    const fm = dr3Ss(0.02, 0.32, Z); if (fm <= 0) continue;
    let a = 0;
    const nl = 0.04 * F.nl;
    a += 0.05 * dr3G2(ax, Y, 0.3, 0.3 + F.bh, 0.28, 0.075) * (old ? 1.25 : 1);             // brow ridge
    a -= (0.052 + (old ? 0.015 : 0)) * dr3G2(ax, Y, F.ex, 0.035, 0.17 * F.es, 0.115 * F.es);  // eye sockets
    a += (yng ? 0.055 : old ? 0.03 : 0.045) * (1 + 0.9 * F.cb) * dr3G2(ax, Y, 0.57 * (1 + 0.06 * F.fw), -0.2, 0.22, 0.17); // cheekbones
    a += ((yng ? 0.018 : old ? -0.03 : 0) + 0.032 * F.cf) * dr3G2(ax, Y, 0.5, -0.4, 0.17, 0.14); // cheek fullness / hollow
    const br = Math.max(0, Math.min(1, (0.12 - Y) / (0.42 + nl))); a += (0.014 + 0.095 * (1 + 0.55 * F.nb) * Math.pow(br, 1.2)) * Math.exp(-((ax / (0.062 * F.nw)) ** 2)) * dr3Ss(0.16, 0.06, Y) * dr3Ss(-0.4 - nl, -0.3 - nl, Y); // nose bridge
    a += 0.135 * (1 + 0.25 * F.nb) * dr3G2(ax, Y, 0, -0.365 - nl, 0.1 * F.nw, 0.085);       // tip
    a += 0.07 * dr3G2(ax, Y, 0.15 * F.nw, -0.39 - nl, 0.07 * F.nw, 0.06);                    // wings
    a -= 0.04 * dr3G2(ax, Y, 0.075 * F.nw, -0.445 - nl, 0.03, 0.02);                         // nostrils
    a -= 0.03 * dr3G2(ax, Y, 0, -0.47 - nl * 0.75, 0.12, 0.025);                             // under the nose
    a += 0.05 * F.lp * dr3G2(ax, Y, 0, -0.598, 0.2 * F.mw, 0.034);                          // upper lip
    a += 0.058 * F.lp * dr3G2(ax, Y, 0, -0.69, 0.17 * F.mw, 0.045);                         // lower lip
    a -= 0.04 * dr3G2(ax, Y, 0, -0.648, 0.21 * F.mw, 0.011);                                // the mouth line
    a -= 0.016 * dr3G2(ax, Y, 0, -0.53, 0.04, 0.07);                                           // philtrum
    a -= 0.03 * dr3G2(ax, Y, 0, -0.775, 0.14, 0.028);                                          // under the lip
    a += 0.065 * (1 + 0.6 * F.ch) * dr3G2(ax, Y, 0, -0.9 - 0.02 * F.ch, 0.19, 0.09);       // chin
    a -= (old ? 0.022 : 0.01) * dr3G2(ax, Y, 0.3, -0.52, 0.07, 0.2);                           // smile lines
    const nx = N0.getX(k), ny = N0.getY(k), nz = N0.getZ(k);
    let dy = lift(X) * dr3G2(ax, Y, 0, -0.645, 0.4, 0.075) * fm;
    P.setXYZ(k, X + nx * a * fm, Y + ny * a * fm + dy, Z + nz * a * fm);
  }
  weld();
  const tex = (x, y, W, Hh) => { const r = dr3Ring(S, y), s = Math.min(1, Math.abs(x) / Math.max(0.01, r.wx)) ** (N / 2), ph = Math.asin(Math.sign(x) * s); return [(0.5 + ph / (Math.PI * 2)) * W, (1 - (y + 1) / 2) * Hh]; };
  return { g, S, F, tex, lift, mouth, ring: (y) => dr3Ring(S, y), C, R };
}

// ---------------------------------------------------------------- painted skin
let DR3_GRAIN = null;
function dr3Grain() {
  if (DR3_GRAIN) return DR3_GRAIN;
  const c = document.createElement('canvas'); c.width = c.height = 256; const g = c.getContext('2d'), id = g.createImageData(256, 256), d = id.data, r = dr3Rng(99);
  for (let k = 0; k < d.length; k += 4) { const v = r() < 0.5 ? 0 : 255; d[k] = d[k + 1] = d[k + 2] = v; d[k + 3] = Math.round(r() * 7); }
  g.putImageData(id, 0, 0); return (DR3_GRAIN = c);
}
function dr3SkinTex(look, skinHex, H, scalp, beard) {
  const W = 2048, Hh = 1024, c = document.createElement('canvas'); c.width = W; c.height = Hh; const g = c.getContext('2d');
  const sk = new THREE.Color(skinHex), deep = look.skin >= 5, light = look.skin <= 1, age = look.age || 'young', old = age === 'old', yng = age === 'young';
  const rng = dr3Rng(dr3Hash(JSON.stringify(look)) ^ 7), P = (x, y) => H.tex(x, y, W, Hh), UX = (W / (Math.PI * 2)) / 0.9, UY = Hh / 2, F = H.F || dr3Feat(look), nl = 0.04 * F.nl, lpP = 1 + (F.lp - 1) * 0.8;
  g.fillStyle = skinHex; g.fillRect(0, 0, W, Hh);
  const blot = (x, y, rx, ry, col, a) => { const [px, py] = P(x, y); g.save(); g.translate(px, py); g.scale(1, ry / rx); const gr = g.createRadialGradient(0, 0, 0, 0, 0, rx * UX); gr.addColorStop(0, col.replace('A', a)); gr.addColorStop(1, col.replace('A', 0)); g.fillStyle = gr; g.fillRect(-rx * UX, -rx * UX, rx * UX * 2, rx * UX * 2); g.restore(); };
  const warm = deep ? 'rgba(80,28,20,A)' : light ? 'rgba(232,120,110,A)' : 'rgba(214,100,84,A)';
  // large soft variation so skin is never one flat colour
  for (let k = 0; k < 70; k++) { const x = (rng() - 0.5) * 1.5, y = -0.9 + rng() * 1.5, r = 0.12 + rng() * 0.2; blot(x, y, r, r * 1.2, rng() < 0.5 ? 'rgba(255,230,210,A)' : 'rgba(120,60,50,A)', 0.04 + rng() * 0.04); }
  blot(0, 0.72, 0.5, 0.45, 'rgba(255,245,235,A)', deep ? 0.07 : 0.12);          // forehead light
  blot(0.58, -0.22, 0.3, 0.26, warm, yng ? 0.34 : 0.2); blot(-0.58, -0.22, 0.3, 0.26, warm, yng ? 0.34 : 0.2); // cheeks
  blot(0, -0.38 - nl, 0.17 * F.nw, 0.14, warm, 0.26);                                // nose tip
  blot(0, -0.1, 0.2, 0.3, 'rgba(255,240,230,A)', deep ? 0.04 : 0.1);                // nose bridge sheen
  for (const s of [-1, 1]) { blot(s * F.ex, 0.03, 0.27 * F.es, 0.19 * F.es, deep ? 'rgba(40,16,12,A)' : 'rgba(150,80,90,A)', old ? 0.3 : 0.2); blot(s * F.ex, -0.1 * F.es, 0.22 * F.es, 0.08, 'rgba(110,60,70,A)', old ? 0.24 : yng ? 0.05 : 0.14); } // eye sockets and bags
  blot(0, -0.8, 0.3, 0.12, 'rgba(60,24,24,A)', 0.14); blot(0, -0.98, 0.5, 0.2, 'rgba(50,22,20,A)', 0.22); // under the lip, under the jaw
  // lips
  const my = (x) => -0.648 + H.lift(x), lipC = deep ? [112, 52, 50] : light ? [206, 112, 112] : [186, 100, 96], lc = (f, a) => `rgba(${lipC.map((v) => Math.round(Math.min(255, v * f))).join(',')},${a})`;
  const poly = (pts, fill) => { g.beginPath(); pts.forEach(([x, y], i) => { const [px, py] = P(x, y); i ? g.lineTo(px, py) : g.moveTo(px, py); }); g.closePath(); g.fillStyle = fill; g.fill(); };
  const up = [], lo = [];
  const LW = 0.225 * F.mw;
  for (let t = -1; t <= 1.001; t += 0.05) { const x = t * LW, bow = Math.abs(t) < 0.45 ? 0.012 * (1 - Math.abs(t) / 0.45) * (Math.abs(t) < 0.12 ? -1 : 1) : 0; up.push([x, my(x) + 0.05 * lpP * Math.sqrt(Math.max(0, 1 - t * t)) + bow]); lo.push([x, my(x) - 0.068 * lpP * Math.pow(Math.max(0, 1 - t * t), 0.7)]); }
  const mid = []; for (let t = -1; t <= 1.001; t += 0.05) mid.push([t * LW, my(t * LW) + 0.003]);
  poly([...up, ...mid.slice().reverse()], lc(1.05, 0.95)); poly([...mid, ...lo.slice().reverse()], lc(1.12, 0.95));
  const open = H.mouth === 'smile';
  if (open) { const top = [], bot = []; for (let t = -0.8; t <= 0.801; t += 0.05) { const x = t * LW; top.push([x, my(x) + 0.0]); bot.push([x, my(x) - 0.03 * Math.sqrt(1 - t * t / 0.7)]); } poly([...top, ...bot.slice().reverse()], '#f6f2ea'); const g1 = g.createLinearGradient(0, P(0, my(0))[1], 0, P(0, my(0) - 0.03)[1]); }
  g.lineCap = 'round'; g.strokeStyle = lc(0.38, 0.9); g.lineWidth = 5; g.beginPath(); mid.forEach(([x, y], i) => { const [px, py] = P(x, y); i ? g.lineTo(px, py) : g.moveTo(px, py); }); g.stroke();
  const [hx, hy] = P(-0.04, my(0) - 0.04), hg = g.createRadialGradient(hx, hy, 0, hx, hy, 40); hg.addColorStop(0, 'rgba(255,230,230,.28)'); hg.addColorStop(1, 'rgba(255,230,230,0)'); g.fillStyle = hg; g.fillRect(hx - 60, hy - 40, 120, 80);
  // nostrils and the shadow under the nose
  for (const s of [-1, 1]) { const [px, py] = P(s * 0.078 * F.nw, -0.443 - nl); g.fillStyle = 'rgba(40,16,14,.55)'; g.beginPath(); g.ellipse(px, py, 11, 6.5, s * 0.3, 0, 7); g.fill(); }
  blot(0, -0.47 - nl * 0.75, 0.13 * F.nw, 0.04, 'rgba(60,24,22,A)', 0.3);
  if (look.freck) for (let k = 0; k < 220; k++) { const lat = k % 3; let x, y; if (lat) { x = (rng() < 0.5 ? -1 : 1) * (0.14 + rng() * 0.42); y = -0.38 + rng() * 0.28; if (Math.abs(x) < 0.15) x *= 2; } else { x = (rng() - 0.5) * 0.2; y = -0.32 + rng() * 0.38; } const [px, py] = P(x, y); g.fillStyle = `rgba(${deep ? '60,30,16' : '152,86,48'},${0.25 + rng() * 0.35})`; g.beginPath(); g.arc(px, py, 2 + rng() * 3.2, 0, 7); g.fill(); }
  g.strokeStyle = 'rgba(80,44,34,.16)'; g.lineCap = 'round';
  if (!yng) { g.lineWidth = old ? 5 : 3; for (let k = 0; k < (old ? 4 : 2); k++) { g.beginPath(); for (let t = -1; t <= 1.001; t += 0.1) { const x = t * 0.5, y = 0.38 + k * 0.07 + (old ? 0.012 : 0) * Math.sin(t * 5 + k); const [px, py] = P(x, y); t < -0.99 ? g.moveTo(px, py) : g.lineTo(px, py); } g.stroke(); }
    for (const s of [-1, 1]) { for (let k = 0; k < 3; k++) { const [x0, y0] = P(s * 0.6, 0.04 + k * 0.04), [x1, y1] = P(s * (0.74 + k * 0.01), 0.1 - k * 0.1); g.lineWidth = 3; g.beginPath(); g.moveTo(x0, y0); g.lineTo(x1, y1); g.stroke(); } g.lineWidth = old ? 6 : 4; g.beginPath(); for (let t = 0; t <= 1.001; t += 0.1) { const [px, py] = P(s * (0.2 + t * 0.12), -0.42 - t * 0.2); t ? g.lineTo(px, py) : g.moveTo(px, py); } g.stroke(); } }
  // the scalp under thin hair (buzz, fades, braids) and the beard shadow take the hair colour.
  // Masks are evaluated on a quarter-size grid and scaled up (soft edges, ~16x less work).
  const tint = (o, y0f, y1f, cmin) => { const w = W / 4, h = Math.max(1, Math.round((Hh * (y1f - y0f)) / 4)), lc = document.createElement('canvas'); lc.width = w; lc.height = h; const lg = lc.getContext('2d'), id = lg.createImageData(w, h), d = id.data, hc = o.c, rr = Math.round(hc.r * 255), gg = Math.round(hc.g * 255), bb = Math.round(hc.b * 255);
    const SX = new Float32Array(w), CX = new Float32Array(w); for (let px = 0; px < w; px++) { const ph = ((px + 0.5) / w - 0.5) * Math.PI * 2, s = Math.sin(ph); SX[px] = Math.sign(s) * Math.pow(Math.abs(s), 2 / 2.3); CX[px] = Math.cos(ph); }
    for (let py = 0; py < h; py++) { const y = 1 - (y0f + ((py + 0.5) / h) * (y1f - y0f)) * 2, r = H.ring(y); for (let px = 0; px < w; px++) { const c2 = CX[px]; if (c2 < cmin) continue; const m = o.mask(r.wx * SX[px], y, cmin > -1 ? c2 : c2 > 0 ? 1 : -1) * o.a; if (m < 0.01) continue; const k = (py * w + px) * 4; d[k] = rr; d[k + 1] = gg; d[k + 2] = bb; d[k + 3] = Math.min(255, m * 255); } }
    lg.putImageData(id, 0, 0); g.imageSmoothingEnabled = true; g.drawImage(lc, 0, Math.round(Hh * y0f), W, Math.round(Hh * (y1f - y0f))); };
  if (scalp) tint(scalp, 0, 0.82, -2);
  if (beard) tint(beard, 0.05, 0.6, -0.2);
  // skin grain: one shared noise tile
  g.fillStyle = g.createPattern(dr3Grain(), 'repeat'); g.fillRect(0, 0, W, Hh);
  const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 4; t.wrapS = THREE.RepeatWrapping; return t;
}

// ---------------------------------------------------------------- eyes
function dr3EyeTex(ec) {
  const c = document.createElement('canvas'); c.width = 512; c.height = 256; const g = c.getContext('2d');
  g.fillStyle = '#f4efe8'; g.fillRect(0, 0, 512, 256);
  const gs = g.createLinearGradient(0, 0, 0, 256); gs.addColorStop(0, 'rgba(120,90,90,.42)'); gs.addColorStop(0.32, 'rgba(120,90,90,0)'); gs.addColorStop(0.8, 'rgba(120,90,90,0)'); gs.addColorStop(1, 'rgba(120,90,90,.18)'); g.fillStyle = gs; g.fillRect(0, 0, 512, 256);
  const col = new THREE.Color(ec), cx = 128, cy = 128, R = 52;
  const gr = g.createRadialGradient(cx, cy, R * 0.15, cx, cy, R); gr.addColorStop(0, '#' + col.clone().multiplyScalar(1.7).getHexString()); gr.addColorStop(0.55, ec); gr.addColorStop(1, '#' + col.clone().multiplyScalar(0.3).getHexString());
  g.fillStyle = gr; g.beginPath(); g.arc(cx, cy, R, 0, 7); g.fill();
  g.lineWidth = 1.3; for (let k = 0; k < 44; k++) { const a = k / 44 * 6.283, o = (k % 3) * 0.04; g.strokeStyle = k % 2 ? 'rgba(255,255,255,.2)' : 'rgba(0,0,0,.22)'; g.beginPath(); g.moveTo(cx + Math.cos(a) * R * (0.3 + o), cy + Math.sin(a) * R * (0.3 + o)); g.lineTo(cx + Math.cos(a) * R * 0.95, cy + Math.sin(a) * R * 0.95); g.stroke(); }
  g.strokeStyle = 'rgba(8,5,3,.9)'; g.lineWidth = 6; g.beginPath(); g.arc(cx, cy, R - 1.5, 0, 7); g.stroke();
  g.fillStyle = '#040302'; g.beginPath(); g.arc(cx, cy, R * 0.38, 0, 7); g.fill();
  const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; return t;
}

// ---------------------------------------------------------------- the face: eyes behind the skin, brows, ears
function dr3Face(H, mesh, look, S, opt) {
  const grp = new THREE.Group(), ray = new THREE.Raycaster(), age = look.age || 'young';
  const hit = (x, y) => { ray.set(new THREE.Vector3(x, y, 6), new THREE.Vector3(0, 0, -1)); const h = ray.intersectObject(mesh, false)[0]; return h ? { p: h.point.clone(), n: h.face.normal.clone() } : { p: new THREE.Vector3(x, y, 0.8), n: new THREE.Vector3(0, 0, 1) }; };
  const ec = DR_EYEC[look.eyeC || 0] || DR_EYEC[0], eyeMat = new THREE.MeshStandardMaterial({ map: dr3EyeTex(ec), roughness: 0.16, metalness: 0, envMapIntensity: 1.1 });
  const F = H.F || dr3Feat(look), R = 0.172 * F.es, EX = F.ex, EY = 0.035;
  for (const s of [-1, 1]) {
    const h = hit(s * EX, EY), e = new THREE.Group(); e.position.set(s * EX, EY, h.p.z - R * 0.93);
    const ball = new THREE.Mesh(new THREE.SphereGeometry(R, 48, 32), eyeMat); ball.rotation.y = -s * 0.1 + 0.0; e.add(ball);
    const cornea = new THREE.Mesh(new THREE.SphereGeometry(R * 1.012, 36, 24), new THREE.MeshPhysicalMaterial({ color: 0xffffff, transparent: true, opacity: 0.08, roughness: 0.02, metalness: 0, clearcoat: 1, envMapIntensity: 2.4 })); e.add(cornea);
    const cl = new THREE.Mesh(new THREE.CircleGeometry(0.02, 14), new THREE.MeshBasicMaterial({ color: 0xffffff })); cl.position.set(-0.05, 0.05, R * 1.02); cl.lookAt(-0.15, 0.15, 1); e.add(cl);
    const cl2 = new THREE.Mesh(new THREE.CircleGeometry(0.009, 10), new THREE.MeshBasicMaterial({ color: 0xffffff })); cl2.position.set(0.048, -0.04, R * 1.02); cl2.lookAt(0.15, -0.1, 1); e.add(cl2);
    grp.add(e);
    // brows: tapered ribbons that follow the skull
    const bt = look.brows || 'natural', by = (bt === 'arched' ? [0.205, 0.255, 0.3, 0.285, 0.225] : bt === 'thick' ? [0.21, 0.24, 0.255, 0.24, 0.205] : [0.21, 0.24, 0.26, 0.245, 0.205]).map((v) => v + F.bh), bx = [0.1, 0.19, 0.29, 0.4, 0.5].map((v) => v + (F.ex - 0.34));
    const hcol = new THREE.Color(DR_HAIRC[look.hairC] || DR_HAIRC[0]).multiplyScalar(look.hairC >= 7 && look.hairC <= 8 ? 0.55 : 0.78);
    const pts2 = bx.map((x, i) => { const hh = hit(s * x, by[i]); return hh.p.addScaledVector(hh.n, 0.012); });
    const bb = new Dr3Bld(), thick = bt === 'thick' ? 1.4 : bt === 'slit' ? 1.05 : 1.0, prof = (f) => (0.018 + 0.034 * Math.sin(Math.PI * Math.min(1, f * 0.9 + 0.1))) * thick;
    if (bt === 'slit') { bb.tube(pts2.slice(0, 3), prof, hcol, 6); bb.tube(pts2.slice(3), prof, hcol, 6); } else bb.tube(pts2, prof, hcol, 6);
    grp.add(new THREE.Mesh(bb.geo(false), new THREE.MeshStandardMaterial({ color: hcol, roughness: 0.72 })));
    // ears
    const rr = new THREE.Raycaster(new THREE.Vector3(s * 6, -0.12, -0.05), new THREE.Vector3(-s, 0, 0)), eh = rr.intersectObject(mesh, false)[0], ex2 = eh ? eh.point.x : s * 0.88;
    const ear = new THREE.Mesh(new THREE.SphereGeometry(1, 24, 18), S.flat); ear.scale.set(0.055 * F.er, 0.215 * F.er, 0.135 * F.er); ear.position.set(ex2 + s * 0.025, -0.1, -0.06); ear.rotation.set(0, s * 0.4, s * -0.12); ear.castShadow = true; grp.add(ear);
    const inner = new THREE.Mesh(new THREE.SphereGeometry(1, 18, 12), new THREE.MeshStandardMaterial({ color: S.color.clone().multiplyScalar(0.76), roughness: 0.7 })); inner.scale.set(0.028 * F.er, 0.135 * F.er, 0.075 * F.er); inner.position.set(ex2 + s * 0.065, -0.1, -0.035); inner.rotation.copy(ear.rotation); grp.add(inner);
    grp.userData['ear' + s] = new THREE.Vector3(ex2 + s * 0.04, -0.24, -0.06);
  }
  grp.userData.hit = hit; return grp;
}
// the skin shader: opens an almond over each eyeball, with a lash line around the edge
function dr3HeadMat(map, S, look, opt) {
  const age = look.age || 'young', blink = opt && opt.blink;
  const mat = new THREE.MeshPhysicalMaterial({ map, roughness: 0.56, metalness: 0, sheen: 0.45, sheenColor: S.color.clone().lerp(new THREE.Color(0xff9a80), 0.25), sheenRoughness: 0.55, envMapIntensity: 0.45, emissive: S.color.clone().multiplyScalar(0.018) });
  const F = dr3Feat(look);
  const U = { uEx: { value: F.ex }, uEy: { value: 0.035 }, uW: { value: 0.215 * F.es }, uHu: { value: blink ? 0.0 : (age === 'young' ? 0.105 : age === 'old' ? 0.075 : 0.09) * F.es }, uHl: { value: blink ? 0.0 : (age === 'old' ? 0.05 : 0.066) * F.es }, uTilt: { value: (age === 'old' ? -0.03 : 0.07) + F.tilt }, uSss: { value: new THREE.Color(0xff6a4a).multiplyScalar(look.skin >= 5 ? 0.1 : 0.14) } };
  mat.onBeforeCompile = (sh) => {
    Object.assign(sh.uniforms, U);
    sh.vertexShader = sh.vertexShader.replace('#include <common>', '#include <common>\nvarying vec3 vObjP;').replace('#include <begin_vertex>', '#include <begin_vertex>\nvObjP = position;');
    sh.fragmentShader = sh.fragmentShader.replace('#include <common>', '#include <common>\nvarying vec3 vObjP; uniform float uEx, uEy, uW, uHu, uHl, uTilt; uniform vec3 uSss;')
      .replace('#include <map_fragment>', `#include <map_fragment>
      if (vObjP.z > 0.3) {
        vec2 e = vec2(abs(vObjP.x) - uEx, vObjP.y - uEy); float t = e.x / uW;
        if (abs(t) < 1.25) {
          float tc = clamp(t, -1.0, 1.0), sh = pow(max(1.0 - tc * tc, 0.0), 0.75), tilt = uTilt * tc * uW * 2.0 - (t < 0.0 ? 0.01 : 0.0);
          float yT = uHu * sh * (uHu > 0.001 ? 1.0 : 0.0) + tilt, yB = -uHl * sh * (uHl > 0.001 ? 1.0 : 0.0) + tilt;
          float d = min(e.y - yB, yT - e.y);
          if (uHu > 0.001 && abs(t) <= 1.0 && d > 0.0) discard;
          float edge = -d; float wTop = 0.05, wBot = 0.016; float w = e.y > (yT + yB) * 0.5 ? wTop : wBot;
          float lash = (1.0 - smoothstep(0.0, w, edge)) * smoothstep(1.28, 0.9, abs(t)) * (e.y > (yT + yB) * 0.5 ? 0.92 : 0.5);
          if (uHu <= 0.001) lash = (1.0 - smoothstep(0.0, 0.03, abs(e.y - tilt))) * smoothstep(1.1, 0.8, abs(t));
          diffuseColor.rgb = mix(diffuseColor.rgb, vec3(0.045, 0.028, 0.022), clamp(lash, 0.0, 1.0));
        }
      }`)
      .replace('#include <output_fragment>', `float rimF = pow(1.0 - max(dot(normalize(normal), normalize(vViewPosition)), 0.0), 2.6); outgoingLight += uSss * rimF * 0.8;\n#include <output_fragment>`);
  };
  mat.customProgramCacheKey = () => 'drskin';
  return mat;
}
