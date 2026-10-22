// ================================================================ THE DREAM: your player in 3D
// A real 3D bust (Three.js): sculpted head, painted skin, strand-built hair, 3D hats, glasses, chains, rings.
// One shared WebGL renderer draws every character; results are cached as images so lists and tables stay light.
// If WebGL is missing the old flat renderer (avatar.js) takes over.

const DR3 = { R: null, ok: null, cache: new Map(), env: null, canvasKey: '' };
const dr3Hash = (s) => { let h = 2166136261; for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 16777619); } return h >>> 0; };
const dr3Rng = (seed) => { let a = seed >>> 0; return () => { a = (a + 0x6D2B79F5) >>> 0; let t = a; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; }; };
const dr3Sm = (a, b, x) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };
const dr3Mix = (a, b, t) => a + (b - a) * t;

function dr3Init() {
  if (DR3.ok !== null) return DR3.ok;
  try {
    if (typeof THREE === 'undefined') throw new Error('no three');
    const c = document.createElement('canvas'); c.width = 16; c.height = 16;
    const R = new THREE.WebGLRenderer({ canvas: c, antialias: true, alpha: true, preserveDrawingBuffer: true });
    R.outputColorSpace = THREE.SRGBColorSpace; R.toneMapping = THREE.ACESFilmicToneMapping; R.toneMappingExposure = 0.7;
    R.shadowMap.enabled = true; R.shadowMap.type = THREE.PCFSoftShadowMap; R.setClearColor(0x000000, 0);
    // a little studio: soft boxes prefiltered once, so metal and skin pick up believable highlights
    const es = new THREE.Scene(); es.background = new THREE.Color(0x14161c);
    const box = (w, h, x, y, z, col) => { const m = new THREE.Mesh(new THREE.PlaneGeometry(w, h), new THREE.MeshBasicMaterial({ color: new THREE.Color(col), side: THREE.DoubleSide })); m.position.set(x, y, z); m.lookAt(0, 0, 0); es.add(m); };
    box(8, 5, -6, 5, 6, new THREE.Color(1, .93, .82).multiplyScalar(5)); box(6, 6, 7, 1, 4, new THREE.Color(.62, .78, 1).multiplyScalar(2)); box(10, 3, 0, 7, -3, new THREE.Color(1, 1, 1).multiplyScalar(3));
    box(12, 3, 0, -4, 5, new THREE.Color(.6, .45, .3).multiplyScalar(1.2)); box(4, 8, -8, 0, -2, new THREE.Color(.8, .85, 1).multiplyScalar(1.6));
    const pm = new THREE.PMREMGenerator(R); DR3.env = pm.fromScene(es, 0.03).texture; pm.dispose();
    DR3.R = R; DR3.ok = true;
  } catch (e) { DR3.ok = false; }
  return DR3.ok;
}

// ---------------------------------------------------------------- tubes (hair strands, brows, chains) into one buffer
class Dr3Bld {
  constructor() { this.p = []; this.c = []; this.i = []; this.n = 0; }
  tube(pts, rad, col, sides = 5, helix) {
    const m = pts.length; if (m < 2) return;
    const T = [], N = [], B = [];
    for (let i = 0; i < m; i++) { const a = pts[Math.max(0, i - 1)], b = pts[Math.min(m - 1, i + 1)]; T.push(b.clone().sub(a).normalize()); }
    let n0 = Math.abs(T[0].y) < 0.9 ? new THREE.Vector3(0, 1, 0) : new THREE.Vector3(1, 0, 0);
    n0 = n0.sub(T[0].clone().multiplyScalar(n0.dot(T[0]))).normalize(); N.push(n0); B.push(new THREE.Vector3().crossVectors(T[0], n0));
    for (let i = 1; i < m; i++) { const q = N[i - 1].clone().sub(T[i].clone().multiplyScalar(N[i - 1].dot(T[i]))).normalize(); N.push(q); B.push(new THREE.Vector3().crossVectors(T[i], q)); }
    let len = 0; const v = new THREE.Vector3(), cc = new THREE.Color();
    for (let i = 0; i < m; i++) {
      if (i) len += pts[i].distanceTo(pts[i - 1]);
      const r = typeof rad === 'function' ? rad(i / (m - 1)) : rad;
      const centre = pts[i].clone();
      if (helix) { const a = helix.k * len + (helix.ph || 0), R = helix.r * (helix.taper ? 1 - 0.5 * i / (m - 1) : 1); centre.addScaledVector(N[i], Math.cos(a) * R).addScaledVector(B[i], Math.sin(a) * R); }
      const colr = typeof col === 'function' ? col(i / (m - 1)) : col; cc.set(colr);
      for (let s = 0; s < sides; s++) {
        const a = (s / sides) * Math.PI * 2; v.copy(centre).addScaledVector(N[i], Math.cos(a) * r).addScaledVector(B[i], Math.sin(a) * r);
        this.p.push(v.x, v.y, v.z); this.c.push(cc.r, cc.g, cc.b);
      }
    }
    for (let i = 0; i < m - 1; i++) for (let s = 0; s < sides; s++) {
      const a = this.n + i * sides + s, b = this.n + i * sides + (s + 1) % sides, c2 = a + sides, d = b + sides;
      this.i.push(a, b, c2, b, d, c2);
    }
    this.n += m * sides;
  }
  geo(col = true) {
    const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(this.p, 3));
    if (col) g.setAttribute('color', new THREE.Float32BufferAttribute(this.c, 3)); g.setIndex(this.i); g.computeVertexNormals(); return g;
  }
}

// ---------------------------------------------------------------- the head
const DR3_UP = new THREE.Vector3(0, 1, 0);
function dr3Collide(p, m) {
  let q = (p.x / 0.87) ** 2 + (p.y / 1.02) ** 2 + (p.z / 0.98) ** 2; const lim = (1 + m) ** 2;
  if (q < lim) p.multiplyScalar(Math.sqrt(lim / Math.max(q, 1e-4)));
  const y = p.y + 2.55; q = (p.x / 1.7) ** 2 + (y / 1.45) ** 2 + (p.z / 0.98) ** 2; const l2 = (1 + m * 0.5) ** 2;
  if (q < l2) { const s = Math.sqrt(l2 / Math.max(q, 1e-4)); p.x *= s; p.z *= s; p.y = -2.55 + y * s; }
}
function dr3Skin(look) {
  const hex = DR_SKIN[look.skin] || DR_SKIN[2], c = new THREE.Color(hex);
  return { hex, color: c, flat: new THREE.MeshStandardMaterial({ color: c, roughness: 0.6, envMapIntensity: 0.5, emissive: c.clone().multiplyScalar(0.03) }) };
}
// ---------------------------------------------------------------- body and clothes
const DR3_TORSO = { cx: 0, cy: -2.55, rx: 1.62, ry: 1.4, rz: 0.92 };
// a lofted upper body: neck base, sloped trapezius, deltoids, chest (y, half-width, front depth, back depth)
const DR3_TROWS = [[-1.1, 0.5, 0.45, 0.43], [-1.24, 0.82, 0.52, 0.46], [-1.44, 1.28, 0.6, 0.5], [-1.7, 1.66, 0.7, 0.56], [-2.02, 1.82, 0.76, 0.6], [-2.5, 1.74, 0.8, 0.62], [-3.1, 1.6, 0.8, 0.62], [-4.0, 1.55, 0.78, 0.62]];
function dr3TorsoGeo() {
  const rows = DR3_TROWS, R = 46, C = 72, N = 2.6, pos = [], uvs = [], idx = [];
  const at = (y) => { let i = 0; while (i < rows.length - 2 && y < rows[i + 1][0]) i++; const a = rows[i], b = rows[i + 1], u = Math.min(1, Math.max(0, (y - a[0]) / (b[0] - a[0]))), s = u * u * (3 - 2 * u); return [1, 2, 3].map((k) => a[k] + (b[k] - a[k]) * s); };
  for (let j = 0; j <= R; j++) {
    const y = rows[0][0] + (rows[rows.length - 1][0] - rows[0][0]) * (j / R), [w, df, db] = at(y);
    for (let i = 0; i <= C; i++) { const ph = (i / C) * Math.PI * 2, s = Math.sin(ph), c = Math.cos(ph), X = w * Math.sign(s) * Math.pow(Math.abs(s), 2 / N), Z = -0.05 + (c > 0 ? df : db) * Math.sign(c) * Math.pow(Math.abs(c), 2 / N);
      pos.push(X, y, Z); uvs.push((X / DR3_TORSO.rx) * 0.5 + 0.5, 1 + (1.3875 + 1.25 * y) / 1.9); }
  }
  for (let j = 0; j < R; j++) for (let i = 0; i < C; i++) { const a = j * (C + 1) + i, b = a + 1, c2 = a + C + 1, d = c2 + 1; idx.push(a, b, c2, b, d, c2); }
  const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); g.setAttribute('uv', new THREE.Float32BufferAttribute(uvs, 2)); g.setIndex(idx); g.computeVertexNormals(); return g;
}
function dr3Neck(S) {
  const m = new THREE.Mesh(new THREE.CylinderGeometry(0.43, 0.47, 0.8, 28, 1, true), S.flat); m.position.set(0, -0.88, -0.04); m.castShadow = true; m.receiveShadow = true; return m;
}

// ---------------------------------------------------------------- outfits (the front of the shirt is painted; collars and cuffs are 3D)
function dr3OutTex(id, c, skinHex) {
  const W = 1024, Hh = 800, cv = document.createElement('canvas'); cv.width = W; cv.height = Hh; const g = cv.getContext('2d');
  const X = (x) => (x / 2.1 * 0.5 + 0.5) * W, Y = (y) => (-1.25 - y) / 1.9 * Hh, sh = (f) => '#' + new THREE.Color(c).multiplyScalar(f).getHexString();
  const poly = (pts, fill, stroke, lw = 3) => { g.beginPath(); pts.forEach(([x, y], i) => (i ? g.lineTo(X(x), Y(y)) : g.moveTo(X(x), Y(y)))); g.closePath(); if (fill) { g.fillStyle = fill; g.fill(); } if (stroke) { g.strokeStyle = stroke; g.lineWidth = lw; g.stroke(); } };
  const ln = (a, b, col, lw = 3) => { g.strokeStyle = col; g.lineWidth = lw; g.beginPath(); g.moveTo(X(a[0]), Y(a[1])); g.lineTo(X(b[0]), Y(b[1])); g.stroke(); };
  const txt = (s, x, y, size, col, font = 'Arial Black,Arial,sans-serif') => { g.fillStyle = col; g.font = `900 ${size}px ${font}`; g.textAlign = 'center'; g.fillText(s, X(x), Y(y)); };
  const skinC = skinHex;
  g.fillStyle = c; g.fillRect(0, 0, W, Hh);
  const rr = dr3Rng(dr3Hash(id)); g.globalAlpha = 0.05; for (let i = 0; i < 700; i++) { g.fillStyle = rr() < 0.5 ? '#fff' : '#000'; g.fillRect(rr() * W, rr() * Hh, 1 + rr() * 6, 1); } g.globalAlpha = 1;
  const vneck = (w, d, shirt) => { poly([[-w, -1.25], [w, -1.25], [0, d]], shirt || skinC); };
  let collar = 'crew', sleeve = c, cuff = null;
  switch (id) {
    case 'office': collar = 'shirt'; poly([[-0.32, -1.3], [0.32, -1.3], [0.06, -1.7], [-0.06, -1.7]], '#f4f7fb'); poly([[-0.07, -1.55], [0.07, -1.55], [0.1, -2.5], [0, -2.62], [-0.1, -2.5]], '#7a1f2b'); poly([[-0.05, -1.5], [0.05, -1.5], [0.08, -1.62], [-0.08, -1.62]], '#591520');
      g.strokeStyle = '#a9b8cc'; g.lineWidth = 2; for (let x = -1.6; x < 1.7; x += 0.2) { ln([x, -1.6], [x * 1.05, -3.1], 'rgba(120,140,170,.2)', 2); }
      g.fillStyle = '#f4f7fb'; g.fillRect(X(0.6), Y(-2.15), X(0.95) - X(0.6), Y(-2.65) - Y(-2.15)); ln([0.62, -2.18], [0.93, -2.18], '#2457b8', 5); break;
    case 'hoodie': collar = 'hood'; poly([[-0.5, -2.45], [0.5, -2.45], [0.7, -3.1], [-0.7, -3.1]], sh(0.86), sh(0.6), 4); ln([-0.2, -1.55], [-0.26, -2.35], '#ece8de', 6); ln([0.2, -1.55], [0.26, -2.35], '#ece8de', 6); break;
    case 'tee': collar = 'crew'; g.strokeStyle = '#ece8de'; g.lineWidth = 6; g.beginPath(); g.arc(X(0), Y(-2.3), 70, 0, 7); g.stroke(); g.fillStyle = '#ece8de'; g.beginPath(); g.moveTo(X(0), Y(-2.12)); g.bezierCurveTo(X(0.15), Y(-2.28), X(0.28), Y(-2.34), X(0.22), Y(-2.42)); g.lineTo(X(0.03), Y(-2.4)); g.lineTo(X(0.06), Y(-2.52)); g.lineTo(X(-0.06), Y(-2.52)); g.lineTo(X(-0.03), Y(-2.4)); g.lineTo(X(-0.22), Y(-2.42)); g.bezierCurveTo(X(-0.28), Y(-2.34), X(-0.15), Y(-2.28), X(0), Y(-2.12)); g.fill(); break;
    case 'varsity': collar = 'rib'; sleeve = '#efe6cf'; vneck(0.42, -1.85, '#efeee9'); ln([0, -1.85], [0, -3.2], sh(0.6), 3); for (let y = -2.1; y > -3.1; y -= 0.22) { g.fillStyle = '#efe6cf'; g.beginPath(); g.arc(X(0.06), Y(y), 5, 0, 7); g.fill(); }
      poly([[-0.9, -2.2], [-0.62, -2.2], [-0.62, -2.62], [-0.9, -2.62]], '#efe6cf'); txt('C', -0.76, -2.55, 72, c, 'Georgia,serif'); break;
    case 'bomber': collar = 'rib'; ln([0, -1.5], [0, -3.2], '#c9ccd4', 6); ln([-0.05, -1.5], [-0.05, -3.2], '#d98a2b', 3); poly([[0.55, -2.6], [1.05, -2.6], [1.05, -2.62], [0.55, -2.62]], sh(0.5), sh(0.5), 3); break;
    case 'track': collar = 'turtle'; ln([0, -1.5], [0, -3.2], '#c9ccd4', 5); for (const s of [-1, 1]) { ln([s * 1.0, -1.6], [s * 1.55, -2.0], '#f3f3f3', 12); ln([s * 0.9, -1.75], [s * 1.5, -2.2], '#f3f3f3', 12); ln([s * 0.72, -1.95], [s * 1.62, -2.7], '#f3f3f3', 10); } break;
    case 'cardigan': collar = 'rib'; vneck(0.5, -2.1, '#f1f0ea'); poly([[-0.07, -1.3], [0.07, -1.3], [0.1, -1.6], [-0.1, -1.6]], '#2a4a8a'); for (const y of [-2.3, -2.55, -2.8]) { g.fillStyle = sh(1.5); g.beginPath(); g.arc(X(0.12), Y(y), 7, 0, 7); g.fill(); } ln([0, -2.1], [0, -3.2], sh(0.55), 3); break;
    case 'vest': collar = 'shirt'; g.fillStyle = '#eef0f4'; g.fillRect(0, 0, W, Hh * 0.3); poly([[-0.9, -1.3], [-0.45, -1.3], [0, -2.3], [0.45, -1.3], [0.9, -1.3], [1.5, -2.2], [1.6, -3.3], [-1.6, -3.3], [-1.5, -2.2]], c, sh(0.6), 3);
      g.save(); g.globalAlpha = 0.5; for (let yy = -2.0; yy > -3.3; yy -= 0.32) for (let xx = -1.5; xx < 1.6; xx += 0.32) poly([[xx, yy + 0.15], [xx + 0.15, yy], [xx, yy - 0.15], [xx - 0.15, yy]], sh(0.55)); g.restore(); poly([[-0.1, -1.55], [0.1, -1.55], [0.1, -1.95], [-0.1, -1.95]], '#b3262c'); break;
    case 'denim': collar = 'shirt'; g.fillStyle = '#efeee9'; poly([[-0.4, -1.3], [0.4, -1.3], [0.28, -3.3], [-0.28, -3.3]], '#efeee9'); poly([[-0.4, -1.3], [-0.26, -1.3], [-0.26, -3.3], [-0.5, -3.3]], c, sh(0.6)); poly([[0.4, -1.3], [0.26, -1.3], [0.26, -3.3], [0.5, -3.3]], c, sh(0.6));
      for (const s of [-1, 1]) { g.strokeStyle = '#e8c48a'; g.setLineDash([6, 5]); g.lineWidth = 3; g.strokeRect(X(s * 0.75) - 40, Y(-2.2), 80, 70); g.setLineDash([]); } break;
    case 'leather': collar = 'shirt'; poly([[-0.4, -1.3], [0.4, -1.3], [0.3, -3.3], [-0.3, -3.3]], '#2a2a2e'); poly([[-0.4, -1.3], [-0.12, -2.2], [-0.2, -3.3], [-0.7, -3.3], [-0.9, -1.8]], c, '#000', 3); poly([[0.4, -1.3], [0.12, -2.2], [0.2, -3.3], [0.7, -3.3], [0.9, -1.8]], c, '#000', 3); ln([-0.12, -2.2], [-0.2, -3.3], '#c9ccd4', 5); g.fillStyle = 'rgba(255,255,255,.06)'; g.fillRect(X(-1.5), Y(-2.1), 140, 30); break;
    case 'fleece': collar = 'turtle'; ln([0, -1.5], [0, -3.2], sh(0.55), 6); g.fillStyle = '#d9603a'; g.fillRect(X(0.55), Y(-2.3), 130, 70); ln([0.55, -2.3], [1.0, -2.3], sh(0.55), 4); break;
    case 'jersey': collar = 'rib'; vneck(0.46, -1.9, skinC); g.font = '900 250px Arial Black,Arial'; txt('7', 0, -3.0, 250, '#f3f3f3'); g.strokeStyle = sh(0.5); g.lineWidth = 6; g.strokeText('7', X(0), Y(-3.0)); ln([-1.55, -1.7], [-1.5, -3.2], '#f3f3f3', 12); ln([1.55, -1.7], [1.5, -3.2], '#f3f3f3', 12); break;
    case 'polo': collar = 'polo'; ln([0, -1.4], [0, -2.1], '#c9ccd4', 5); g.fillStyle = '#c9ccd4'; for (const y of [-1.6, -1.9]) { g.beginPath(); g.arc(X(0), Y(y), 7, 0, 7); g.fill(); } break;
    case 'blazer': collar = 'shirt'; poly([[-0.35, -1.3], [0.35, -1.3], [0.25, -3.3], [-0.25, -3.3]], '#17171a'); poly([[-0.35, -1.3], [-0.1, -2.5], [-0.25, -3.3], [-0.95, -3.3], [-0.9, -1.7]], sh(0.88), sh(0.45)); poly([[0.35, -1.3], [0.1, -2.5], [0.25, -3.3], [0.95, -3.3], [0.9, -1.7]], sh(0.88), sh(0.45)); break;
    case 'suit': case 'designer': case 'tux': {
      collar = 'shirt'; const shirt = id === 'designer' ? '#141416' : '#f7f6f2', lap = id === 'tux' ? '#26262a' : sh(0.72);
      poly([[-0.38, -1.3], [0.38, -1.3], [0.28, -3.3], [-0.28, -3.3]], shirt);
      poly([[-0.38, -1.3], [-0.06, -2.6], [-0.2, -3.3], [-0.95, -3.3], [-1.0, -1.7]], c, sh(0.35), 3); poly([[0.38, -1.3], [0.06, -2.6], [0.2, -3.3], [0.95, -3.3], [1.0, -1.7]], c, sh(0.35), 3);
      poly([[-0.38, -1.3], [-0.05, -2.3], [-0.5, -2.45], [-0.78, -1.7]], lap, sh(0.3), 3); poly([[0.38, -1.3], [0.05, -2.3], [0.5, -2.45], [0.78, -1.7]], lap, sh(0.3), 3);
      if (id === 'suit') poly([[-0.07, -1.6], [0.07, -1.6], [0.1, -2.7], [0, -2.85], [-0.1, -2.7]], '#8a1c24'); else if (id === 'tux') { poly([[-0.2, -1.5], [0, -1.58], [0.2, -1.5], [0.2, -1.74], [0, -1.66], [-0.2, -1.74]], '#000'); g.fillStyle = '#222'; g.beginPath(); g.arc(X(0), Y(-1.62), 8, 0, 7); g.fill(); }
      else { ln([-0.3, -1.4], [0, -2.5], '#2a2a2e', 5); ln([0.3, -1.4], [0, -2.5], '#2a2a2e', 5); }
      break; }
    case 'vampire': collar = 'cape'; poly([[-0.38, -1.3], [0.38, -1.3], [0.28, -3.3], [-0.28, -3.3]], '#f4efe6'); poly([[-0.38, -1.3], [-0.06, -2.6], [-0.2, -3.3], [-0.95, -3.3], [-1.0, -1.7]], '#141216', '#000', 3); poly([[0.38, -1.3], [0.06, -2.6], [0.2, -3.3], [0.95, -3.3], [1.0, -1.7]], '#141216', '#000', 3); g.fillStyle = '#b8142a'; g.beginPath(); g.arc(X(0), Y(-2.1), 12, 0, 7); g.fill(); g.strokeStyle = '#e3b94f'; g.lineWidth = 3; g.stroke(); break;
    case 'flannel': collar = 'polo'; g.globalAlpha = 0.5; g.fillStyle = '#1b0d0d'; for (let x = -2; x < 2; x += 0.3) g.fillRect(X(x), 0, 30, Hh); for (let y = -1.2; y > -3.4; y -= 0.3) g.fillRect(0, Y(y), W, 30); g.globalAlpha = 1; ln([0, -1.4], [0, -3.2], '#3a0c0c', 4); g.fillStyle = '#e9e5dc'; for (const y of [-1.7, -2.1, -2.5, -2.9]) { g.beginPath(); g.arc(X(0.03), Y(y), 7, 0, 7); g.fill(); } break;
    case 'ugly': collar = 'rib'; g.fillStyle = '#c2362a'; g.fillRect(0, Y(-2.3), W, 70); g.fillStyle = '#f3ecdc'; for (let x = -1.8; x < 1.9; x += 0.26) poly([[x, -2.32], [x + 0.08, -2.4], [x, -2.48], [x - 0.08, -2.4]], '#f3ecdc'); break;
  }
  const t = new THREE.CanvasTexture(cv); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 4; return { tex: t, collar, sleeve };
}
function dr3Collar(kind, S, colHex, root) {
  const m = new THREE.MeshStandardMaterial({ color: colHex, roughness: 0.85, envMapIntensity: 0.3 }), dk = new THREE.MeshStandardMaterial({ color: new THREE.Color(colHex).multiplyScalar(0.7), roughness: 0.9 });
  const add = (mesh, x, y, z, rx = 0, ry = 0, rz = 0) => { mesh.position.set(x, y, z); mesh.rotation.set(rx, ry, rz); mesh.castShadow = true; mesh.receiveShadow = true; root.add(mesh); return mesh; };
  const ring = (R, r, y, scz = 0.95, mat = m) => { const t = new THREE.Mesh(new THREE.TorusGeometry(R, r, 12, 40), mat); t.scale.set(1, scz, 1); return add(t, 0, y, -0.03, Math.PI / 2); };
  if (kind === 'crew') ring(0.53, 0.075, -1.2);
  else if (kind === 'rib') { ring(0.54, 0.08, -1.2); ring(0.54, 0.05, -1.27, 0.95, dk); }
  else if (kind === 'turtle') { const c = new THREE.Mesh(new THREE.CylinderGeometry(0.47, 0.52, 0.36, 32, 1, true), new THREE.MeshStandardMaterial({ color: colHex, roughness: 0.85, side: THREE.DoubleSide })); add(c, 0, -1.2, -0.04); }
  else if (kind === 'hood') { const h = new THREE.Mesh(new THREE.TorusGeometry(0.62, 0.2, 14, 40, Math.PI * 1.55), dk); h.scale.set(1, 1, 0.9); h.scale.multiplyScalar(0.85); add(h, 0, -1.2, -0.1, Math.PI / 2 - 0.35, 0, Math.PI * 1.22); ring(0.48, 0.065, -1.34); }
  else if (kind === 'polo' || kind === 'shirt') { const col = kind === 'shirt' ? 0xf4f7fb : colHex; const fm = new THREE.MeshStandardMaterial({ color: col, roughness: 0.8, side: THREE.DoubleSide }); for (const s of [-1, 1]) { const f = new THREE.Mesh(new THREE.BoxGeometry(0.34, 0.02, 0.3), fm); add(f, s * 0.25, -1.28, 0.33, -0.5, s * -0.55, s * -0.25); } ring(0.42, 0.05, -1.24, 0.9, new THREE.MeshStandardMaterial({ color: col, roughness: 0.8 })); }
  else if (kind === 'cape') { const cp = new THREE.Mesh(new THREE.CylinderGeometry(1.05, 0.62, 1.25, 28, 1, true, Math.PI * 0.55, Math.PI * 0.9 + 1.35), new THREE.MeshStandardMaterial({ color: 0x8a0f1e, roughness: 0.6, side: THREE.DoubleSide })); add(cp, 0, -0.9, -0.25, 0, 3.14, 0); ring(0.46, 0.05, -1.26); }
}

// ---------------------------------------------------------------- hats and eyewear
const DR3_HAT_COL = { cap: '#17171a', snap: '#24427e', beanie: '#b3262c', bucket: '#d8cfb4', cowboy: '#8a5a32', fedora: '#2a2522', visor: '#1e8f4a' };
function dr3Cloth(col, rough = 0.85) { return new THREE.MeshStandardMaterial({ color: col, roughness: rough, envMapIntensity: 0.35, side: THREE.DoubleSide }); }
function dr3Panel(col, seams = 6) {
  const c = document.createElement('canvas'); c.width = 512; c.height = 256; const g = c.getContext('2d'); g.fillStyle = col; g.fillRect(0, 0, 512, 256);
  g.strokeStyle = 'rgba(0,0,0,.4)'; g.lineWidth = 3; for (let i = 0; i < seams; i++) { g.beginPath(); g.moveTo(512 * i / seams, 0); g.lineTo(512 * i / seams, 256); g.stroke(); g.strokeStyle = 'rgba(255,255,255,.12)'; g.beginPath(); g.moveTo(512 * i / seams + 4, 0); g.lineTo(512 * i / seams + 4, 256); g.stroke(); g.strokeStyle = 'rgba(0,0,0,.4)'; }
  const r = dr3Rng(5); g.globalAlpha = 0.06; for (let i = 0; i < 1400; i++) { g.fillStyle = r() < 0.5 ? '#fff' : '#000'; g.fillRect(r() * 512, r() * 256, 2, 2); } const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; return t;
}
function dr3Brim(innerR, outerR, arc, centerAng, depth, droop, col) {
  const sh = new THREE.Shape(); sh.absarc(0, 0, outerR, centerAng - arc / 2, centerAng + arc / 2, false); sh.absarc(0, 0, innerR, centerAng + arc / 2, centerAng - arc / 2, true);
  const g = new THREE.ExtrudeGeometry(sh, { depth, bevelEnabled: false, curveSegments: 40 }); g.rotateX(-Math.PI / 2);
  const p = g.attributes.position; for (let i = 0; i < p.count; i++) { const r = Math.hypot(p.getX(i), p.getZ(i)); p.setY(i, p.getY(i) - droop * Math.pow(Math.max(0, (r - innerR) / (outerR - innerR)), 2)); }
  g.computeVertexNormals(); const m = new THREE.Mesh(g, dr3Cloth(col, 0.7)); m.castShadow = true; m.receiveShadow = true; return m;
}
function dr3Hat(id, colHex, vol, look) {
  const grp = new THREE.Group(), col = new THREE.Color(colHex), dk = col.clone().multiplyScalar(0.7), V = vol, C = (g, mat) => { const m = new THREE.Mesh(g, mat); m.castShadow = true; m.receiveShadow = true; return m; };
  const crown = (r, h0, th) => { const g = new THREE.SphereGeometry(1, 48, 32, 0, Math.PI * 2, 0, th); g.scale(r, r * 0.98, r * 1.02); return g; };
  switch (id) {
    case 'cap': case 'snap': {
      const back = id === 'cap', c = C(crown(1.07 * V, 0, 1.25), new THREE.MeshStandardMaterial({ map: dr3Panel(colHex), roughness: 0.82, envMapIntensity: 0.35, side: THREE.DoubleSide })); c.position.set(0, 0.02, -0.03); c.rotation.x = back ? -0.1 : -0.06; grp.add(c);
      const bt = C(new THREE.SphereGeometry(0.05, 12, 10), dr3Cloth(dk)); bt.position.set(0, 0.02 + 1.07 * V * 0.98, -0.03); grp.add(bt);
      const brim = dr3Brim(1.0 * V, back ? 1.46 * V : 1.72 * V, back ? 1.8 : 2.0, back ? Math.PI / 2 : -Math.PI / 2, 0.04, back ? 0.1 : 0.03, colHex); brim.position.set(0, 0.36, 0); brim.rotation.x = back ? -0.08 : 0.04; grp.add(brim);
      if (back) { const strap = C(new THREE.BoxGeometry(0.5, 0.07, 0.04), dr3Cloth(dk)); strap.position.set(0, 0.42, 1.0 * V * 0.95); strap.rotation.x = -0.3; grp.add(strap); }
      const lg = document.createElement('canvas'); lg.width = 128; lg.height = 128; const lc = lg.getContext('2d'); lc.fillStyle = 'rgba(0,0,0,0)'; lc.clearRect(0, 0, 128, 128); lc.fillStyle = '#e3b94f'; lc.font = '900 78px Georgia,serif'; lc.textAlign = 'center'; lc.textBaseline = 'middle'; lc.fillText('CF', 64, 68);
      const logo = C(new THREE.PlaneGeometry(0.42, 0.42), new THREE.MeshStandardMaterial({ map: new THREE.CanvasTexture(lg), transparent: true, roughness: 0.6 })); logo.castShadow = false;
      if (back) { logo.position.set(0, 0.95 * V, -1.05 * V * 0.86); logo.rotation.set(-0.5, Math.PI, 0); } else { logo.position.set(0, 0.66, 1.06 * V * 0.97); logo.rotation.x = -0.28; }
      grp.add(logo);
      break; }
    case 'beanie': {
      const c = C(crown(1.1 * V, 0, 1.52), dr3Cloth(colHex)); c.position.set(0, 0.04, -0.02); grp.add(c);
      const ribs = new THREE.Mesh(new THREE.SphereGeometry(1.115 * V, 64, 24, 0, Math.PI * 2, 0.12, 1.4), new THREE.MeshStandardMaterial({ color: dk, roughness: 1, transparent: true, opacity: 0.0 })); // (kept for layout; cuff below)
      const cuff = C(new THREE.TorusGeometry(1.06 * V, 0.17, 14, 56), dr3Cloth(dk, 0.95)); cuff.rotation.x = Math.PI / 2; cuff.position.set(0, 0.32, -0.02); cuff.scale.set(1, 1.02, 0.55); grp.add(cuff);
      for (let i = 0; i < 28; i++) { const a = i / 28 * 6.283, r = C(new THREE.CylinderGeometry(0.012, 0.012, 0.28, 5), dr3Cloth(col.clone().multiplyScalar(0.62))); r.position.set(Math.sin(a) * 1.22 * V, 0.32, Math.cos(a) * 1.22 * V - 0.02); r.scale.z = 1; grp.add(r); }
      const pom = C(new THREE.SphereGeometry(0.16, 16, 12), dr3Cloth(0xefeee9)); pom.position.set(0, 1.1 * V + 0.08, -0.02); grp.add(pom); break; }
    case 'bucket': {
      const pts = [[0, 1.0], [0.6, 0.97], [0.98, 0.7], [1.1, 0.42], [1.55, 0.34], [1.82, 0.2], [1.82, 0.16], [1.55, 0.3], [1.08, 0.38]].map(([r, y]) => new THREE.Vector2(r * V, y));
      const l = C(new THREE.LatheGeometry(pts, 56), dr3Cloth(colHex, 0.9)); l.position.set(0, 0, -0.02); grp.add(l);
      const band = C(new THREE.TorusGeometry(1.06 * V, 0.03, 6, 48), dr3Cloth(dk)); band.rotation.x = Math.PI / 2; band.position.set(0, 0.58, -0.02); grp.add(band); break; }
    case 'cowboy': {
      const pts = []; for (let i = 0; i <= 10; i++) { const f = i / 10, r = 1.05 + f * 1.0; pts.push(new THREE.Vector2(r * V, 0.42 + 0.34 * Math.pow(f, 2.2) - 0.1 * Math.sin(f * 3))); } for (let i = 10; i >= 0; i--) { const f = i / 10, r = 1.05 + f * 1.0; pts.push(new THREE.Vector2(r * V, 0.38 + 0.34 * Math.pow(f, 2.2) - 0.1 * Math.sin(f * 3))); }
      const b = C(new THREE.LatheGeometry(pts, 56), dr3Cloth(colHex, 0.7)); b.scale.set(1, 1, 0.92); b.position.set(0, 0, -0.02); grp.add(b);
      const cr = [[0.0, 1.12], [0.35, 1.2], [0.62, 1.2], [0.9, 1.0], [1.04, 0.45]].map(([r, y]) => new THREE.Vector2(r * V, y)); const c = C(new THREE.LatheGeometry(cr, 48), dr3Cloth(colHex, 0.7)); c.position.set(0, 0, -0.02); grp.add(c);
      const band = C(new THREE.TorusGeometry(1.02 * V, 0.045, 6, 48), dr3Cloth(0x2a1a10)); band.rotation.x = Math.PI / 2; band.position.set(0, 0.56, -0.02); grp.add(band);
      const bk = C(new THREE.BoxGeometry(0.16, 0.12, 0.04), new THREE.MeshStandardMaterial({ color: 0xc9ccd4, metalness: 1, roughness: 0.25 })); bk.position.set(0, 0.56, 1.03 * V); grp.add(bk); break; }
    case 'fedora': {
      const cr = [[0.0, 1.0], [0.3, 1.04], [0.55, 1.0], [0.92, 0.82], [1.06, 0.46]].map(([r, y]) => new THREE.Vector2(r * V, y)); const c = C(new THREE.LatheGeometry(cr, 48), dr3Cloth(colHex, 0.75)); c.scale.set(1, 1, 1.02); c.position.set(0, 0, -0.02); grp.add(c);
      const pinch = C(new THREE.BoxGeometry(0.1, 0.18, 1.4 * V), dr3Cloth(dk)); pinch.position.set(0, 0.96, -0.02); grp.add(pinch);
      const pts = [[1.05, 0.46], [1.5, 0.4], [1.8, 0.3], [1.8, 0.26], [1.5, 0.34], [1.04, 0.4]].map(([r, y]) => new THREE.Vector2(r * V, y)); const b = C(new THREE.LatheGeometry(pts, 56), dr3Cloth(colHex, 0.7)); b.scale.set(1, 1, 0.95); b.position.set(0, 0, -0.02); grp.add(b);
      const band = C(new THREE.TorusGeometry(1.02 * V, 0.08, 6, 48), dr3Cloth(0x141210)); band.rotation.x = Math.PI / 2; band.position.set(0, 0.62, -0.02); grp.add(band); break; }
    case 'visor': {
      const brim = dr3Brim(1.0 * V, 1.6 * V, 2.4, Math.PI / 2, 0.035, 0.08, colHex); brim.position.set(0, 0.5, 0); grp.add(brim);
      const band = C(new THREE.TorusGeometry(1.05 * V, 0.08, 8, 56), dr3Cloth(dk)); band.rotation.x = Math.PI / 2; band.position.set(0, 0.52, -0.02); band.scale.set(1, 1.03, 0.45); grp.add(band); break; }
    case 'pumpkin': {
      const p = []; for (let i = 0; i <= 24; i++) { const t = i / 24 * Math.PI * 0.5; p.push(new THREE.Vector2(Math.sin(t) * 1.14 * V * (1 + 0.05 * Math.cos(i * 4)), Math.cos(t) * 1.0 * V)); }
      const l = C(new THREE.LatheGeometry(p, 10), new THREE.MeshStandardMaterial({ color: 0xff7a1a, roughness: 0.55, flatShading: false })); l.position.set(0, 0.2, 0); grp.add(l);
      const st = C(new THREE.CylinderGeometry(0.07, 0.1, 0.3, 8), dr3Cloth(0x3f6b2a)); st.position.set(0.04, 1.2 * V + 0.2, 0); st.rotation.z = -0.3; grp.add(st); break; }
    case 'pilgrim': {
      const cyl = C(new THREE.CylinderGeometry(0.78, 0.9, 1.1, 32), dr3Cloth(0x1a1a1c, 0.7)); cyl.position.set(0, 0.95, 0); grp.add(cyl);
      const b = C(new THREE.CylinderGeometry(1.7, 1.7, 0.05, 40), dr3Cloth(0x1a1a1c, 0.7)); b.position.set(0, 0.4, 0); grp.add(b);
      const band = C(new THREE.CylinderGeometry(0.91, 0.92, 0.2, 32), dr3Cloth(0x5a3a20)); band.position.set(0, 0.55, 0); grp.add(band);
      const bk = C(new THREE.BoxGeometry(0.3, 0.26, 0.04), new THREE.MeshStandardMaterial({ color: 0xe3b94f, metalness: 1, roughness: 0.25 })); bk.position.set(0, 0.55, 0.93); grp.add(bk); break; }
    case 'santa': {
      const c = C(crown(1.1 * V, 0, 1.45), dr3Cloth(0xc2141e)); c.position.set(0, 0.06, -0.02); grp.add(c);
      const f = C(new THREE.TorusGeometry(1.04 * V, 0.17, 12, 48), dr3Cloth(0xf7f6f2, 1)); f.rotation.x = Math.PI / 2; f.position.set(0, 0.34, -0.02); f.scale.set(1, 1, 0.6); grp.add(f);
      const cone = C(new THREE.ConeGeometry(0.55, 1.1, 24), dr3Cloth(0xc2141e)); cone.position.set(0.55, 1.15, -0.45); cone.rotation.set(0.25, 0, -0.95); grp.add(cone);
      const pom = C(new THREE.SphereGeometry(0.17, 14, 10), dr3Cloth(0xf7f6f2)); pom.position.set(1.0, 1.0, -0.55); grp.add(pom); break; }
    case 'phones': {
      const mat = new THREE.MeshStandardMaterial({ color: 0x2b2b2e, roughness: 0.45, metalness: 0.2 }), band = C(new THREE.TorusGeometry(1.06, 0.045, 10, 40, Math.PI), mat); band.rotation.z = 0; band.position.set(0, 0.0, 0); grp.add(band);
      for (const s of [-1, 1]) { const cup = C(new THREE.CylinderGeometry(0.3, 0.3, 0.2, 24), mat); cup.rotation.z = Math.PI / 2; cup.position.set(s * 1.0, -0.08, -0.04); grp.add(cup); const ring = C(new THREE.TorusGeometry(0.22, 0.025, 6, 24), new THREE.MeshStandardMaterial({ color: 0x3cbcd0, emissive: 0x1a6a78, roughness: 0.4 })); ring.rotation.y = Math.PI / 2; ring.position.set(s * 1.11, -0.08, -0.04); grp.add(ring); } break; }
  }
  return grp;
}
function dr3Eyewear(id, hit) {
  const grp = new THREE.Group(), z0 = 1.0, ex = 0.345, ey = 0.035;
  const frame = (c, rough = 0.35, met = 0) => new THREE.MeshStandardMaterial({ color: c, roughness: rough, metalness: met, envMapIntensity: 1 });
  const lens = (c, op, ci = 0) => new THREE.MeshPhysicalMaterial({ color: c, transparent: true, opacity: op, roughness: 0.05, metalness: 0, clearcoat: 1, envMapIntensity: 1.8, side: THREE.DoubleSide });
  const add = (m, x, y, z) => { m.position.set(x, y, z); m.castShadow = true; grp.add(m); return m; };
  const arms = (c, y = 0.03, rad = 0.016, zf = 0.92) => { for (const s of [-1, 1]) { const b = new Dr3Bld(), pts = [new THREE.Vector3(s * 0.66, y, zf), new THREE.Vector3(s * 0.8, y, 0.55), new THREE.Vector3(s * 0.88, y - 0.01, 0.0), new THREE.Vector3(s * 0.9, y - 0.03, -0.12)]; b.tube(pts, rad, c, 6); const m = new THREE.Mesh(b.geo(false), frame(c)); grp.add(m); } };
  const rect = (w, h, r) => { const s = new THREE.Shape(); s.moveTo(-w / 2 + r, -h / 2); s.lineTo(w / 2 - r, -h / 2); s.quadraticCurveTo(w / 2, -h / 2, w / 2, -h / 2 + r); s.lineTo(w / 2, h / 2 - r); s.quadraticCurveTo(w / 2, h / 2, w / 2 - r, h / 2); s.lineTo(-w / 2 + r, h / 2); s.quadraticCurveTo(-w / 2, h / 2, -w / 2, h / 2 - r); s.lineTo(-w / 2, -h / 2 + r); s.quadraticCurveTo(-w / 2, -h / 2, -w / 2 + r, -h / 2); return s; };
  const framed = (w, h, r, thick, c, lc, lo, depth = 0.05, met = 0) => { const outer = rect(w, h, r); const hole = rect(w - thick * 2, h - thick * 2, Math.max(0.01, r - thick * 0.6)); outer.holes.push(new THREE.Path(hole.getPoints(14).reverse())); const f = new THREE.Mesh(new THREE.ExtrudeGeometry(outer, { depth, bevelEnabled: true, bevelThickness: 0.01, bevelSize: 0.01, bevelSegments: 2, curveSegments: 8 }), frame(c, 0.3, met)); f.castShadow = true; const l = new THREE.Mesh(new THREE.ShapeGeometry(rect(w - thick, h - thick, r)), lens(lc, lo)); l.position.z = depth / 2; const g = new THREE.Group(); g.add(f, l); return g; };
  switch (id) {
    case 'readers': for (const s of [-1, 1]) add(framed(0.5, 0.34, 0.1, 0.035, 0x2b2b2e, 0xcfe8ff, 0.12, 0.04), s * ex, ey, z0); add(new THREE.Mesh(new THREE.BoxGeometry(0.2, 0.03, 0.03), frame(0x2b2b2e)), 0, ey + 0.07, z0 + 0.02); arms(0x2b2b2e); break;
    case 'thick': for (const s of [-1, 1]) add(framed(0.54, 0.4, 0.09, 0.07, 0x121214, 0xcfe8ff, 0.08, 0.07), s * ex, ey, z0); add(new THREE.Mesh(new THREE.BoxGeometry(0.2, 0.06, 0.05), frame(0x121214)), 0, ey + 0.08, z0 + 0.03); arms(0x121214, 0.05, 0.03); break;
    case 'round': for (const s of [-1, 1]) { const f = new THREE.Mesh(new THREE.TorusGeometry(0.235, 0.022, 10, 40), frame(0x1d1d22, 0.3, 0.3)); f.castShadow = true; add(f, s * ex, ey, z0); add(new THREE.Mesh(new THREE.CircleGeometry(0.235, 32), lens(0xcfe8ff, 0.1)), s * ex, ey, z0); } add(new THREE.Mesh(new THREE.TorusGeometry(0.1, 0.014, 6, 16, Math.PI), frame(0x1d1d22, 0.3, 0.3)), 0, ey + 0.1, z0); arms(0x1d1d22); break;
    case 'aviator': for (const s of [-1, 1]) { const sh = new THREE.Shape(); sh.moveTo(-0.27, 0.15); sh.lineTo(0.27, 0.15); sh.bezierCurveTo(0.29, -0.02, 0.2, -0.25, 0.0, -0.24); sh.bezierCurveTo(-0.2, -0.25, -0.29, -0.02, -0.27, 0.15); const g = new THREE.Group(); const f = new THREE.Mesh(new THREE.ExtrudeGeometry(sh, { depth: 0.03, bevelEnabled: false }), frame(0xd9b36c, 0.2, 1)); const l = new THREE.Mesh(new THREE.ShapeGeometry(sh), lens(0x3a2e22, 0.82)); l.position.z = 0.032; l.scale.set(0.93, 0.93, 1); g.add(l); const ring = new THREE.Mesh(new THREE.TubeGeometry(new THREE.CatmullRomCurve3(sh.getPoints(30).map((p) => new THREE.Vector3(p.x, p.y, 0.015)), true), 60, 0.014, 6, true), frame(0xd9b36c, 0.2, 1)); g.add(ring); add(g, s * ex, ey - 0.01, z0 + 0.0); }
      add(new THREE.Mesh(new THREE.BoxGeometry(0.16, 0.02, 0.02), frame(0xd9b36c, 0.2, 1)), 0, ey + 0.09, z0 + 0.02); arms(0xd9b36c, 0.07, 0.012); break;
    case 'tinted': case 'amber': { const c = id === 'amber' ? 0xe08a1e : 0xc9a27a, fc = id === 'amber' ? 0x6a3a10 : 0xc9b9a0; for (const s of [-1, 1]) add(framed(0.56, 0.38, 0.14, 0.03, fc, c, 0.6, 0.04), s * ex, ey, z0); add(new THREE.Mesh(new THREE.BoxGeometry(0.2, 0.025, 0.03), frame(fc)), 0, ey + 0.07, z0 + 0.02); arms(fc); break; }
    case 'sport': for (const s of [-1, 1]) { const f = framed(0.58, 0.34, 0.15, 0.02, 0x111111, 0xff8a3a, 0.55, 0.05, 0.2); f.rotation.z = s * 0.06; add(f, s * 0.35, ey, z0 + 0.01); } add(new THREE.Mesh(new THREE.BoxGeometry(0.16, 0.05, 0.04), frame(0x111111)), 0, ey + 0.08, z0 + 0.03); arms(0x111111, 0.06, 0.02); break;
    case 'shades': { const rows = ['KKKKKKKKKKKKKKKK', '.KwwKKK..KwwKKK.', '..KwwKK...KwwKK.', '...KKK.....KKK..'], P = 0.085, mat = new THREE.MeshStandardMaterial({ color: 0x050505, roughness: 0.25, envMapIntensity: 1 }), wm = new THREE.MeshStandardMaterial({ color: 0xffffff, roughness: 0.2, emissive: 0x777777 });
      rows.forEach((row, y) => [...row].forEach((ch, x) => { if (ch === '.') return; const b = new THREE.Mesh(new THREE.BoxGeometry(P, P, 0.09), ch === 'K' ? mat : wm); b.position.set((x - 7.5) * P, ey + 0.09 - y * P, z0 - 0.02 - 0.0); b.castShadow = true; grp.add(b); })); arms(0x050505, 0.12); break; }
    case 'masq': { const sh = new THREE.Shape(); sh.moveTo(-0.7, 0.12); sh.bezierCurveTo(-0.5, 0.3, -0.15, 0.26, 0, 0.1); sh.bezierCurveTo(0.15, 0.26, 0.5, 0.3, 0.7, 0.12); sh.bezierCurveTo(0.7, -0.15, 0.4, -0.25, 0.2, -0.12); sh.lineTo(-0.2, -0.12); sh.bezierCurveTo(-0.4, -0.25, -0.7, -0.15, -0.7, 0.12);
      for (const s of [-1, 1]) { const h = new THREE.Path(); h.absellipse(s * ex, -0.0, 0.17, 0.09, 0, 7, true); sh.holes.push(h); }
      const m = new THREE.Mesh(new THREE.ExtrudeGeometry(sh, { depth: 0.05, bevelEnabled: true, bevelSize: 0.01, bevelThickness: 0.01 }), new THREE.MeshStandardMaterial({ color: 0x2a0b3a, roughness: 0.35, metalness: 0.3 })); add(m, 0, ey, z0 - 0.02); const tr = new THREE.Mesh(new THREE.TorusGeometry(0.9, 0.012, 6, 60, 0.0), frame(0xe3b94f)); arms(0x2a0b3a, 0.05, 0.02);
      const feather = new THREE.Mesh(new THREE.ConeGeometry(0.06, 0.6, 8), new THREE.MeshStandardMaterial({ color: 0xff7a1a, roughness: 0.6 })); feather.position.set(0.8, 0.3, 0.6); feather.rotation.z = -0.5; grp.add(feather); break; }
    case 'snow': { const sh = rect(1.5, 0.52, 0.22), g = new THREE.Group(); const f = new THREE.Mesh(new THREE.ExtrudeGeometry(sh, { depth: 0.08, bevelEnabled: true, bevelSize: 0.02, bevelThickness: 0.02 }), frame(0xf3f6fa, 0.4)); f.castShadow = true; const l = new THREE.Mesh(new THREE.ShapeGeometry(rect(1.36, 0.4, 0.17)), new THREE.MeshPhysicalMaterial({ color: 0xb9a0ff, transparent: true, opacity: 0.75, roughness: 0.05, clearcoat: 1, envMapIntensity: 1.8 })); l.position.z = 0.1; g.add(f, l); add(g, 0, ey + 0.01, z0 - 0.04); const strap = new THREE.Mesh(new THREE.TorusGeometry(1.0, 0.07, 8, 56), frame(0xf3f6fa, 0.6)); strap.rotation.x = Math.PI / 2; strap.position.set(0, ey, 0); strap.scale.set(1, 1.0, 1.02); grp.add(strap); break; }
  }
  return grp;
}

// ---------------------------------------------------------------- jewelry
const DR3_GOLD = () => new THREE.MeshStandardMaterial({ color: 0xe3b94f, metalness: 1, roughness: 0.2, envMapIntensity: 1.7 });
const DR3_SILVER = () => new THREE.MeshStandardMaterial({ color: 0xd2d6de, metalness: 1, roughness: 0.18, envMapIntensity: 1.7 });
const DR3_GEM = () => new THREE.MeshStandardMaterial({ color: 0xf4fbff, metalness: 0.1, roughness: 0.02, envMapIntensity: 3.2, emissive: 0x555a60 });
function dr3ChainPath(drop, y0 = -1.27, rx = 0.46, rz = 0.46) {
  const pts = []; for (let i = 0; i < 28; i++) { const t = i / 28 * Math.PI * 2, fr = Math.max(0, Math.cos(t)), sag = drop * Math.pow(fr, 1.4); const p = new THREE.Vector3(Math.sin(t) * (rx + sag * 0.6), y0 - sag, Math.cos(t) * (rz + sag * 0.3 * (fr > 0 ? 1 : 0)) - 0.05); dr3Collide(p, 0.035); pts.push(p); }
  return new THREE.CatmullRomCurve3(pts, true);
}
function dr3Links(curve, count, mat, size = 0.05, elong = 1.5, gems) {
  const geo = new THREE.TorusGeometry(size, size * 0.34, 8, 18), im = new THREE.InstancedMesh(geo, mat, count), mtx = new THREE.Matrix4(), cs = curve.getSpacedPoints(count), grp = new THREE.Group();
  const gm = gems ? new THREE.InstancedMesh(new THREE.SphereGeometry(size * 0.42, 10, 8), DR3_GEM(), count) : null;
  for (let i = 0; i < count; i++) {
    const p = cs[i], q = cs[(i + 1) % count], tg = q.clone().sub(p).normalize(), out = new THREE.Vector3(p.x, 0, p.z).normalize(), nrm = i % 2 ? out : new THREE.Vector3().crossVectors(tg, out).normalize();
    const x = tg, z = nrm.clone().sub(tg.clone().multiplyScalar(nrm.dot(tg))).normalize(), y = new THREE.Vector3().crossVectors(z, x);
    mtx.makeBasis(x.clone().multiplyScalar(elong), y, z); mtx.setPosition(p); im.setMatrixAt(i, mtx);
    if (gm) { const m2 = new THREE.Matrix4().makeTranslation(p.x, p.y, p.z).multiply(new THREE.Matrix4().makeScale(i % 2 ? 0.01 : 1, i % 2 ? 0.01 : 1, i % 2 ? 0.01 : 1)); const o = out.clone().multiplyScalar(size * 0.4); m2.setPosition(p.x + o.x, p.y, p.z + o.z); gm.setMatrixAt(i, m2); }
  }
  im.castShadow = true; grp.add(im); if (gm) grp.add(gm); return grp;
}
function dr3Pendant(txt, mat) {
  const c = document.createElement('canvas'); c.width = c.height = 128; const g = c.getContext('2d'); g.fillStyle = '#e3b94f'; g.fillRect(0, 0, 128, 128); g.strokeStyle = '#8f6f33'; g.lineWidth = 8; g.beginPath(); g.arc(64, 64, 56, 0, 7); g.stroke(); g.fillStyle = '#5a3f10'; g.font = '900 54px Georgia,serif'; g.textAlign = 'center'; g.fillText(txt, 64, 82);
  const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; const m = new THREE.Mesh(new THREE.CylinderGeometry(0.13, 0.13, 0.035, 28), [mat, new THREE.MeshStandardMaterial({ map: t, metalness: 0.85, roughness: 0.28, envMapIntensity: 1.5 }), mat]); m.rotation.x = Math.PI / 2; return m;
}
function dr3Chain(id) {
  const grp = new THREE.Group(); if (!id || id === 'none') return grp;
  const gold = DR3_GOLD();
  const onChest = (m, curve, fr) => { const p = curve.getPoint(fr); m.position.copy(p).add(new THREE.Vector3(0, -0.12, 0.05)); dr3Collide(m.position, 0.07); grp.add(m); };
  const bottom = (curve) => { let best = curve.getPoint(0), bi = 0; for (let i = 0; i < 40; i++) { const p = curve.getPoint(i / 40); if (p.y < best.y) { best = p; bi = i / 40; } } return bi; };
  if (id === 'silver') { const c = dr3ChainPath(0.38), t = new THREE.Mesh(new THREE.TubeGeometry(c, 80, 0.016, 6, true), DR3_SILVER()); t.castShadow = true; grp.add(t); }
  else if (id === 'cuban' || id === 'iced') { const c = dr3ChainPath(0.62); grp.add(dr3Links(c, 76, gold, 0.056, 1.5, id === 'iced')); }
  else if (id === 'rope') { const c = dr3ChainPath(0.85), b = new Dr3Bld(); const pts = c.getSpacedPoints(120); b.tube(pts.concat([pts[0]]), 0.03, 0xe3b94f, 6, { r: 0.016, k: 150 }); const m = new THREE.Mesh(b.geo(false), gold); m.castShadow = true; grp.add(m); const pend = dr3Pendant('CF', gold); onChest(pend, c, bottom(c)); }
  else if (id === 'layered') { const c1 = dr3ChainPath(0.36, -1.24), c2 = dr3ChainPath(0.8, -1.3, 0.5, 0.5); const t = new THREE.Mesh(new THREE.TubeGeometry(c1, 80, 0.014, 6, true), gold); grp.add(t); grp.add(dr3Links(c2, 84, gold, 0.052, 1.5)); const pend = dr3Pendant('CF', gold); onChest(pend, c2, bottom(c2)); }
  else if (id === 'bones') { const c = dr3ChainPath(0.55), bm = new THREE.MeshStandardMaterial({ color: 0xefe8d6, roughness: 0.6 }), n = 22, im = new THREE.InstancedMesh(new THREE.CapsuleGeometry(0.03, 0.09, 4, 8), bm, n), mt = new THREE.Matrix4(), q = new THREE.Quaternion(); const ps = c.getSpacedPoints(n); for (let i = 0; i < n; i++) { const tg = ps[(i + 1) % n].clone().sub(ps[i]).normalize(); q.setFromUnitVectors(new THREE.Vector3(0, 1, 0), tg); mt.compose(ps[i], q, new THREE.Vector3(1, 1, 1)); im.setMatrixAt(i, mt); } grp.add(im);
    const sk = new THREE.Mesh(new THREE.SphereGeometry(0.1, 16, 12), bm); onChest(sk, c, bottom(c)); }
  else if (id === 'wishbone') { const c = dr3ChainPath(0.6), t = new THREE.Mesh(new THREE.TubeGeometry(c, 80, 0.015, 6, true), gold); grp.add(t); const w = new THREE.Mesh(new THREE.TorusGeometry(0.12, 0.022, 8, 20, Math.PI), gold); w.rotation.z = Math.PI; onChest(w, c, bottom(c)); }
  else if (id === 'lights') { const c = dr3ChainPath(0.55), t = new THREE.Mesh(new THREE.TubeGeometry(c, 80, 0.012, 6, true), new THREE.MeshStandardMaterial({ color: 0x1d4a22 })); grp.add(t); const cols = [0xff4f4f, 0x5cbf64, 0xffd23a, 0x5aa9de], n = 20; c.getSpacedPoints(n).forEach((p, i) => { const b = new THREE.Mesh(new THREE.SphereGeometry(0.05, 10, 8), new THREE.MeshStandardMaterial({ color: cols[i % 4], emissive: cols[i % 4], emissiveIntensity: 0.9, roughness: 0.3 })); b.position.copy(p).add(new THREE.Vector3(0, -0.05, 0)); grp.add(b); }); }
  return grp;
}
function dr3Ear(id, earPos) {
  const grp = new THREE.Group(); if (!id || id === 'none') return grp;
  for (const s of [-1, 1]) { const p = earPos[s]; if (!p) continue;
    if (id === 'stud') { const m = new THREE.Mesh(new THREE.SphereGeometry(0.048, 12, 10), DR3_SILVER()); m.position.copy(p); grp.add(m); }
    else if (id === 'diamond') { const m = new THREE.Mesh(new THREE.OctahedronGeometry(0.06), DR3_GEM()); m.position.copy(p); m.rotation.y = 0.6; grp.add(m); const b = new THREE.Mesh(new THREE.SphereGeometry(0.03, 8, 6), DR3_SILVER()); b.position.copy(p).add(new THREE.Vector3(s * -0.03, 0, 0)); grp.add(b); }
    else if (id === 'hoop') { const m = new THREE.Mesh(new THREE.TorusGeometry(0.15, 0.022, 10, 28), DR3_GOLD()); m.position.copy(p).add(new THREE.Vector3(s * 0.02, -0.13, 0)); m.rotation.y = Math.PI / 2; m.castShadow = true; grp.add(m); } }
  return grp;
}

// ---------------------------------------------------------------- arms, hands, table
function dr3Limb(a, b, r, mat) { const d = b.clone().sub(a), len = d.length(), m = new THREE.Mesh(new THREE.CapsuleGeometry(r, Math.max(0.001, len - 2 * r), 8, 16), mat); m.position.copy(a).add(b).multiplyScalar(0.5); m.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), d.normalize()); m.castShadow = true; m.receiveShadow = true; return m; }
function dr3Hand(S, s) {
  const g = new THREE.Group(), mat = S.flat;
  const palm = new THREE.Mesh(new THREE.SphereGeometry(1, 24, 18), mat); palm.scale.set(0.27, 0.15, 0.3); palm.position.set(0, 0, 0.28); palm.castShadow = true; g.add(palm);
  const knuck = new THREE.Mesh(new THREE.SphereGeometry(1, 20, 14), mat); knuck.scale.set(0.27, 0.12, 0.1); knuck.position.set(0, -0.015, 0.52); g.add(knuck);
  const fp = [];
  for (let i = 0; i < 4; i++) { const x = -0.15 + i * 0.1, L = 0.17 + (i === 1 || i === 2 ? 0.03 : 0); const a = new THREE.Vector3(x, -0.02, 0.52), m1 = new THREE.Vector3(x, -0.05, 0.52 + L * 0.65), b2 = new THREE.Vector3(x, -0.15, 0.55 + L * 0.8); g.add(dr3Limb(a, m1, 0.043, mat)); g.add(dr3Limb(m1, b2, 0.04, mat)); fp.push(new THREE.Vector3(x, -0.02, 0.56)); }
  const th = dr3Limb(new THREE.Vector3(-s * 0.22, 0, 0.26), new THREE.Vector3(-s * 0.3, -0.06, 0.5), 0.052, mat); g.add(th);
  g.userData.fp = fp; return g;
}
function dr3Chip(col) {
  const c = document.createElement('canvas'); c.width = 128; c.height = 16; const g = c.getContext('2d'); g.fillStyle = col; g.fillRect(0, 0, 128, 16); g.fillStyle = '#f3ecdc'; for (let i = 0; i < 8; i++) g.fillRect(i * 16 + 3, 0, 10, 16);
  const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace;
  return [new THREE.MeshStandardMaterial({ map: t, roughness: 0.4 }), new THREE.MeshStandardMaterial({ color: col, roughness: 0.4 }), new THREE.MeshStandardMaterial({ color: col, roughness: 0.4 })];
}
const DR3_SLEEVE_SHORT = { tee: 1, jersey: 1, polo: 1 };
function dr3Patch(pid) {
  const p = DR_PATCH[pid]; if (!p) return null;
  const w = Math.max(0.46, p.t.length * 0.09), c = document.createElement('canvas'); c.width = 256; c.height = Math.round(256 * 0.26 / w * (w / 0.46 > 1 ? 1 : 1)); c.height = 96; const g = c.getContext('2d');
  g.fillStyle = p.bg || '#0c0c0c'; g.beginPath(); g.roundRect ? g.roundRect(4, 4, 248, 88, 14) : g.rect(4, 4, 248, 88); g.fill(); g.strokeStyle = p.c; g.lineWidth = 6; g.stroke(); g.setLineDash([4, 5]); g.lineWidth = 2; g.strokeRect(14, 14, 228, 68); g.setLineDash([]);
  g.fillStyle = p.c; g.font = `900 ${p.t.length > 6 ? 44 : 54}px Arial Black,Arial,sans-serif`; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(p.t, 128, 50);
  const t = new THREE.CanvasTexture(c); t.colorSpace = THREE.SRGBColorSpace; t.anisotropy = 4;
  const m = new THREE.Mesh(new THREE.PlaneGeometry(w, w * 96 / 256), new THREE.MeshStandardMaterial({ map: t, roughness: 0.7, transparent: true })); m.castShadow = false; return m;
}

// ---------------------------------------------------------------- the whole player
function dr3Build(st, opt) {
  const look = { ...DR_LOOK_DEFAULT, ...(st.look || {}) }, eq = st.eq || {}, dye = st.dye || {}, titles = st.titles || {}, face = !!opt.face;
  const S = dr3Skin(look), scene = new THREE.Scene(); scene.environment = DR3.env;
  // lights: warm key with soft shadows, cool fill, hot rim from behind
  const key = new THREE.DirectionalLight(0xfff0dc, 1.7); key.position.set(-3.2, 4.4, 6.5); key.castShadow = true; key.shadow.mapSize.set(1024, 1024); key.shadow.bias = -0.0005; key.shadow.normalBias = 0.025;
  const sc = key.shadow.camera; sc.left = -3.5; sc.right = 3.5; sc.top = 3.5; sc.bottom = -4; sc.near = 1; sc.far = 18; scene.add(key);
  const fill = new THREE.DirectionalLight(0xbcd2ff, 0.45); fill.position.set(5, 1, 4); scene.add(fill);
  const rim = new THREE.DirectionalLight(0xfff4ea, 1.2); rim.position.set(3, 3.5, -5); scene.add(rim);
  scene.add(new THREE.HemisphereLight(0xdfe8ff, 0x3a2a22, 0.28));
  const chr = new THREE.Group(); scene.add(chr);
  // head
  const H = dr3Head(look, opt), skinTex = dr3SkinTex(look, S.hex, H, dr3Scalp(look, look.hair), dr3BeardTint(look, H)), headMat = dr3HeadMat(skinTex, S, look, opt);
  const head = new THREE.Mesh(H.g, headMat); head.castShadow = true; head.receiveShadow = true; chr.add(head); head.updateMatrixWorld(true);
  const fc = dr3Face(H, head, look, S, opt); chr.add(fc);
  const hat = eq.head && eq.head !== 'none' ? eq.head : null, hair = dr3Hair(H, look, hat); chr.add(hair.grp);
  // facial hair shell on top of the skin
  if (look.facial && look.facial !== 'none') chr.add(dr3Beard(H, head, look, fc.userData.hit));
  if (hat && hat !== 'phones') { const col = DR_DYES[dye[hat]] || DR3_HAT_COL[hat] || '#222'; const h3 = dr3Hat(hat, col, Math.min(1.5, hair.vol), look); chr.add(h3); } else if (hat === 'phones') chr.add(dr3Hat('phones', '#222', 1, look));
  if (eq.eyes && eq.eyes !== 'none') chr.add(dr3Eyewear(eq.eyes, fc.userData.hit));
  chr.add(dr3Ear(eq.ear, { [-1]: fc.userData['ear-1'], 1: fc.userData['ear1'] }));
  chr.add(dr3Neck(S));
  // torso and clothes
  const outfit = eq.outfit || 'office', oc = DR_DYES[dye[outfit]] || DR_OUTFIT_BASE[outfit] || '#3b3f47', ot = dr3OutTex(outfit, oc, S.hex);
  const tm = new THREE.MeshStandardMaterial({ map: ot.tex, roughness: outfit === 'leather' ? 0.38 : 0.85, metalness: outfit === 'leather' ? 0.12 : 0, envMapIntensity: outfit === 'leather' ? 1 : 0.35 });
  const torso = new THREE.Mesh(dr3TorsoGeo(), tm); torso.castShadow = true; torso.receiveShadow = true; chr.add(torso); torso.updateMatrixWorld(true);
  dr3Collar(ot.collar, S, new THREE.Color(oc).multiplyScalar(outfit === 'varsity' ? 0 : 1).add(outfit === 'varsity' ? new THREE.Color('#efe6cf') : new THREE.Color(0)).getHex(), chr);
  chr.add(dr3Chain(eq.chain));
  const sleeveHex = ot.sleeve === oc ? oc : ot.sleeve, slm = new THREE.MeshStandardMaterial({ color: sleeveHex, roughness: outfit === 'leather' ? 0.4 : 0.85, metalness: outfit === 'leather' ? 0.12 : 0, envMapIntensity: 0.4 });
  const arms = new THREE.Group(); chr.add(arms);
  const ray = new THREE.Raycaster(), chestHit = (x, y) => { ray.set(new THREE.Vector3(x, y, 6), new THREE.Vector3(0, 0, -1)); const h = ray.intersectObject(torso, false)[0]; return h ? h.point : new THREE.Vector3(x, y, 0.9); };
  if (eq.patch && eq.patch !== 'none') { const pm = dr3Patch(eq.patch); if (pm) { const p = chestHit(0.78, -2.15); pm.position.set(p.x, p.y, p.z + 0.012); pm.rotation.y = 0.22; pm.rotation.z = 0.04; chr.add(pm); } }
  if (!face) {
    const bank = st.br || 0, wrists = {};
    for (const s of [-1, 1]) {
      const sh = new THREE.Vector3(s * 1.5, -1.95, -0.05), el = new THREE.Vector3(s * 1.82, -2.7, 0.14), wr = new THREE.Vector3(s * 0.78, -2.4, 1.2);
      const shorts = !!DR3_SLEEVE_SHORT[outfit], skm = S.flat;
      arms.add(dr3Limb(sh, el, 0.33, slm)); arms.add(dr3Limb(el, wr, 0.255, shorts ? skm : slm));
      if (shorts) { arms.add(dr3Limb(sh, sh.clone().lerp(el, 0.7), 0.345, slm)); }
      const hd = dr3Hand(S, s); hd.position.copy(wr).add(new THREE.Vector3(0, -0.02, 0.1)); hd.scale.setScalar(1.25); hd.lookAt(new THREE.Vector3(s * 0.2, -2.4, 2.4)); arms.add(hd); wrists[s] = hd;
      if (eq.sleeve && eq.sleeve !== 'none' && s === 1) { const pm = dr3Patch(eq.sleeve); if (pm) { pm.scale.setScalar(0.8); const mid = sh.clone().lerp(el, 0.42); pm.position.set(mid.x + 0.38, mid.y, mid.z + 0.08); pm.rotation.y = Math.PI / 2 - 0.15; arms.add(pm); } }
    }
    // watch (left hand), bracelets (right), rings
    const wt = eq.watch || 'digital', PAL = { digital: ['#222', '#3f5f3a'], steel: ['#b9bec7', '#1d3b6e'], gold: ['#d9b36c', '#f3e2b0'], iced: ['#e9edf2', '#f8fbff'], hyper: ['#1a1a1a', '#2c2c2c'] }[wt] || ['#222', '#3f5f3a'];
    const hl = wrists[-1], hr = wrists[1], bm = new THREE.MeshStandardMaterial({ color: PAL[0], metalness: wt === 'digital' ? 0.1 : 0.9, roughness: wt === 'digital' ? 0.6 : 0.25, envMapIntensity: 1.2 });
    const strap = new THREE.Mesh(new THREE.TorusGeometry(0.215, 0.05, 10, 32), bm); strap.position.set(0, 0, -0.1); strap.castShadow = true; hl.add(strap);
    const wc = document.createElement('canvas'); wc.width = wc.height = 128; const wg = wc.getContext('2d'); wg.fillStyle = PAL[1]; wg.fillRect(0, 0, 128, 128); wg.strokeStyle = wt === 'digital' ? '#9fe69a' : '#e8e8e8'; wg.lineWidth = 5; if (wt === 'digital') { wg.font = '900 40px monospace'; wg.fillStyle = '#9fe69a'; wg.textAlign = 'center'; wg.fillText('8:00', 64, 76); } else { for (let i = 0; i < 12; i++) { const a = i / 12 * 6.283; wg.beginPath(); wg.moveTo(64 + Math.cos(a) * 46, 64 + Math.sin(a) * 46); wg.lineTo(64 + Math.cos(a) * 56, 64 + Math.sin(a) * 56); wg.stroke(); } wg.lineWidth = 6; wg.beginPath(); wg.moveTo(64, 64); wg.lineTo(64, 28); wg.moveTo(64, 64); wg.lineTo(86, 74); wg.stroke(); }
    const wtex = new THREE.CanvasTexture(wc); wtex.colorSpace = THREE.SRGBColorSpace;
    const wf = new THREE.Mesh(new THREE.CylinderGeometry(0.17, 0.17, 0.07, 32), [bm, new THREE.MeshStandardMaterial({ map: wtex, roughness: 0.2, metalness: 0.2, envMapIntensity: 1.4 }), bm]); wf.position.set(0, 0.255, -0.1); wf.castShadow = true; hl.add(wf);
    if (wt === 'iced') { const ring = new THREE.Mesh(new THREE.TorusGeometry(0.2, 0.025, 6, 32), DR3_GEM()); ring.rotation.x = Math.PI / 2; ring.position.set(0, 0.29, -0.1); ring.scale.setScalar(0.85); hl.add(ring); }
    const wr = eq.wrist && eq.wrist !== 'none' ? eq.wrist : null;
    if (wr === 'gold') { const t = new THREE.Mesh(new THREE.TorusGeometry(0.22, 0.05, 10, 32), DR3_GOLD()); t.position.z = -0.12; t.castShadow = true; hr.add(t); }
    if (wr === 'beads') { for (let i = 0; i < 22; i++) { const a = i / 22 * 6.283, b = new THREE.Mesh(new THREE.SphereGeometry(0.052, 10, 8), new THREE.MeshStandardMaterial({ color: [0x2a2a2e, 0x7a5a3a, 0x2a2a2e, 0xc9a03e][i % 4], roughness: 0.4 })); b.position.set(Math.cos(a) * 0.215, Math.sin(a) * 0.215, -0.1); hr.add(b); } }
    if (wr === 'tennis') { const t = new THREE.Mesh(new THREE.TorusGeometry(0.215, 0.02, 8, 40), DR3_SILVER()); t.position.z = -0.1; hr.add(t); for (let i = 0; i < 18; i++) { const a = i / 18 * 6.283, b = new THREE.Mesh(new THREE.SphereGeometry(0.03, 8, 6), DR3_GEM()); b.position.set(Math.cos(a) * 0.225, Math.sin(a) * 0.225, -0.1); hr.add(b); } }
    for (let k = 0; k < Math.min(4, titles.bracelet || 0); k++) { const t = new THREE.Mesh(new THREE.TorusGeometry(0.225, 0.028, 8, 32), DR3_GOLD()); t.position.z = -0.2 - k * 0.07; t.castShadow = true; hr.add(t); }
    const rings = Math.min(3, (titles.ring || 0) + (titles.me || 0)); for (let k = 0; k < rings; k++) { const fp = hl.userData.fp[k]; const t = new THREE.Mesh(new THREE.TorusGeometry(0.049, 0.014, 8, 18), DR3_GOLD()); t.position.copy(fp).add(new THREE.Vector3(0, 0, 0.02 * k)); hl.add(t); const g = new THREE.Mesh(new THREE.OctahedronGeometry(0.022), DR3_GEM()); g.position.copy(fp).add(new THREE.Vector3(0, 0.05, 0.02 * k)); hl.add(g); }
    const ring = eq.ring && eq.ring !== 'none' ? eq.ring : null;
    if (ring) { const fp = hr.userData.fp[ring === 'pinky' ? 0 : 2], mat = ring === 'signet' || ring === 'pinky' ? DR3_SILVER() : DR3_GOLD(); const t = new THREE.Mesh(new THREE.TorusGeometry(0.05, ring === 'champ' ? 0.02 : 0.015, 8, 18), mat); t.position.copy(fp); hr.add(t);
      if (ring === 'signet') { const sq = new THREE.Mesh(new THREE.BoxGeometry(0.06, 0.04, 0.06), mat); sq.position.copy(fp).add(new THREE.Vector3(0, 0.055, 0)); hr.add(sq); } if (ring === 'pinky') { const gm = new THREE.Mesh(new THREE.OctahedronGeometry(0.035), DR3_GEM()); gm.position.copy(fp).add(new THREE.Vector3(0, 0.06, 0)); hr.add(gm); } if (ring === 'champ') { const gm = new THREE.Mesh(new THREE.BoxGeometry(0.1, 0.05, 0.1), new THREE.MeshStandardMaterial({ color: 0xc2362a, roughness: 0.1, metalness: 0.2, envMapIntensity: 2 })); gm.position.copy(fp).add(new THREE.Vector3(0, 0.07, 0)); hr.add(gm); } }
    // the table, cards and chips stay put while you turn
    const table = new THREE.Group(); scene.add(table);
    const felt = new THREE.Mesh(new THREE.PlaneGeometry(16, 9), new THREE.MeshStandardMaterial({ color: 0x0e4d39, roughness: 0.95, envMapIntensity: 0.2 })); felt.rotation.x = -Math.PI / 2; felt.position.set(0, -2.55, 3); felt.receiveShadow = true; table.add(felt);
    const rail = new THREE.Mesh(new THREE.CylinderGeometry(0.3, 0.3, 16, 24), new THREE.MeshStandardMaterial({ color: 0x3a2414, roughness: 0.35, envMapIntensity: 0.9 })); rail.rotation.z = Math.PI / 2; rail.position.set(0, -2.35, 4.3); rail.castShadow = true; table.add(rail);
    const cc = document.createElement('canvas'); cc.width = 64; cc.height = 90; const cg = cc.getContext('2d'); cg.fillStyle = '#b3262c'; cg.fillRect(0, 0, 64, 90); cg.strokeStyle = '#f3ecdc'; cg.lineWidth = 3; cg.strokeRect(5, 5, 54, 80); cg.beginPath(); cg.moveTo(32, 20); cg.lineTo(48, 45); cg.lineTo(32, 70); cg.lineTo(16, 45); cg.closePath(); cg.stroke(); const ctex = new THREE.CanvasTexture(cc); ctex.colorSpace = THREE.SRGBColorSpace;
    for (const [dx, rot] of [[-0.18, 0.18], [0.2, -0.12]]) { const cd = new THREE.Mesh(new THREE.BoxGeometry(0.5, 0.02, 0.7), [new THREE.MeshStandardMaterial({ color: 0xf7f3e8 }), new THREE.MeshStandardMaterial({ color: 0xf7f3e8 }), new THREE.MeshStandardMaterial({ color: 0xf7f3e8 }), new THREE.MeshStandardMaterial({ map: ctex, roughness: 0.4 }), new THREE.MeshStandardMaterial({ color: 0xf7f3e8 }), new THREE.MeshStandardMaterial({ color: 0xf7f3e8 })]); cd.position.set(dx, -2.52, 2.2); cd.rotation.y = rot; cd.rotation.x = 0; cd.castShadow = true; cd.receiveShadow = true; table.add(cd); }
    const tiers = [0, 1000, 10000, 50000, 250000, 1e6, 5e6], lv = tiers.filter((t) => bank >= t).length, cols = ['#f3ecdc', '#c2362a', '#2a6fdb', '#1e8f4a', '#141414', '#7a3fc0', '#e3b94f'], mats = cols.map(dr3Chip);
    for (let c = 0; c < Math.min(3, Math.ceil(lv / 2)); c++) { const h = Math.max(2, lv * 2 - c * 3); for (let k = 0; k < h; k++) { const ch = new THREE.Mesh(new THREE.CylinderGeometry(0.22, 0.22, 0.065, 28), mats[(k + c) % Math.min(cols.length, lv + 1)]); ch.position.set(1.2 + c * 0.47, -2.55 + 0.034 + k * 0.067, 2.4 - c * 0.14); ch.rotation.y = k * 0.7; ch.castShadow = true; ch.receiveShadow = true; table.add(ch); } }
  }
  return { scene, chr, look };
}
// ---------------------------------------------------------------- render, snapshot, mount
function dr3Cam(opt) {
  const face = !!opt.face, w = opt.w, h = opt.h, cam = new THREE.PerspectiveCamera(opt.fov || (face ? 26 : 31), w / h, 0.5, 60);
  if (face) { cam.position.set(0, 0.1, 6.6); cam.lookAt(0, opt.cy == null ? -0.03 : opt.cy, 0); } else { cam.position.set(0, 0.5, 7.9); cam.lookAt(0, -0.95, 0); }
  return cam;
}
function dr3Dispose(scene) { scene.traverse((o) => { if (o.geometry) o.geometry.dispose(); const ms = o.material ? (Array.isArray(o.material) ? o.material : [o.material]) : []; for (const m of ms) { for (const k of ['map']) if (m[k]) m[k].dispose(); m.dispose(); } }); }
function dr3Draw(st, opt) {
  const B = dr3Build(st, opt), R = DR3.R, cam = dr3Cam(opt); R.setSize(opt.w, opt.h, false); R.setPixelRatio(1);
  B.chr.rotation.y = opt.ang || 0; B.chr.rotation.x = 0;
  R.render(B.scene, cam); dr3Dispose(B.scene); return R.domElement;
}
function dr3Key(st, opt) { return JSON.stringify([opt.fov, opt.cy, st.look, st.eq, st.dye, st.titles, Math.min(7, [0, 1000, 10000, 50000, 250000, 1e6, 5e6].filter((t) => (st.br || 0) >= t).length), opt.face, opt.mouth, opt.blink, opt.ang, opt.w, opt.h]); }
function dr3Snap(st, opt) {
  const k = dr3Key(st, opt); let u = DR3.cache.get(k); if (u) return u;
  const cv = dr3Draw(st, opt); u = cv.toDataURL('image/png'); if (DR3.cache.size > 60) DR3.cache.delete(DR3.cache.keys().next().value); DR3.cache.set(k, u); return u;
}
function drAvatarSVG(st, opt = {}) {
  if (!dr3Init() || window.__dr2d) return drAvatarSVG2D(st, opt);
  try {
    const face = !!opt.face, w = face ? 240 : 480, h = face ? 240 : 520, url = dr3Snap(st, { ...opt, w, h });
    return `<img class="dravsvg dr3img" src="${url}" width="${w}" height="${h}" alt="${(typeof esc === 'function' ? esc : (x) => x)(st.name || 'Your player')}" draggable="false">`;
  } catch (e) { DR3.ok = false; return drAvatarSVG2D(st, opt); }
}
function dr3LiveOK() { return !window.__dr2d && dr3Init(); }
function dr3LiveHTML(st, alt) { return `<canvas id="dr3live" class="dravsvg dr3live" width="440" height="480" role="img" aria-label="${alt || 'Your player'}"></canvas>`; }
// the live builder view: drag to turn the character around
function dr3Mount() {
  const cv = document.getElementById('dr3live'); if (!cv || !dr3Init()) return;
  const st = cv._st = cv._st || null; const get = () => (typeof DR3.liveState === 'function' ? DR3.liveState() : null); const S0 = get(); if (!S0) return;
  const w = 440, h = 480; cv.width = w; cv.height = h; const ctx = cv.getContext('2d');
  let ang = DR3.ang || 0, vel = 0, drag = false, lx = 0, raf = 0, idle = 0, t0 = performance.now();
  const o = { w, h, mouth: 'smug' }, B = dr3Build(S0, o), camL = dr3Cam(o);
  const paint = (a) => { if (!document.body.contains(cv)) { if (B) { dr3Dispose(B.scene); } return; } const R = DR3.R; R.setSize(w, h, false); R.setPixelRatio(1); B.chr.rotation.y = a; R.render(B.scene, camL); ctx.clearRect(0, 0, w, h); ctx.drawImage(R.domElement, 0, 0); };
  const tick = (now) => { if (!document.body.contains(cv)) return; if (!drag) { idle = now - t0; const swing = Math.sin(idle / 1400) * 0.28; if (idle < 4000) { paint(ang + swing * Math.max(0, 1 - idle / 4000)); raf = requestAnimationFrame(tick); return; } return; } };
  paint(ang); raf = requestAnimationFrame(tick);
  const down = (e) => { drag = true; lx = e.clientX; cv.setPointerCapture && cv.setPointerCapture(e.pointerId); };
  const move = (e) => { if (!drag) return; ang += (e.clientX - lx) * 0.011; lx = e.clientX; DR3.ang = ang; if (!cv._busy) { cv._busy = true; requestAnimationFrame(() => { paint(ang); cv._busy = false; }); } };
  const up = () => { drag = false; };
  cv.onpointerdown = down; cv.onpointermove = move; cv.onpointerup = up; cv.onpointercancel = up; cv.style.touchAction = 'pan-y';
}
