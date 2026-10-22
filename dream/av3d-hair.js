// ================================================================ 3D hair and facial hair
// Hair is built as real volume: a sculpted cap that follows the scalp (thickness per style), streaked and glossy,
// plus tufts, curls, twists, locs, braids and hanging sheets where a style needs them.
const DR3_Y = new THREE.Vector3(0, 1, 0);
function dr3Vn(x, y, z) { // value noise for hand-built variation
  const h = (a, b, c) => { const s = Math.sin(a * 127.1 + b * 311.7 + c * 74.7) * 43758.5453; return s - Math.floor(s); };
  const xi = Math.floor(x), yi = Math.floor(y), zi = Math.floor(z), xf = x - xi, yf = y - yi, zf = z - zi, u = xf * xf * (3 - 2 * xf), v = yf * yf * (3 - 2 * yf), w = zf * zf * (3 - 2 * zf); let r = 0;
  for (let i = 0; i < 2; i++) for (let j = 0; j < 2; j++) for (let k = 0; k < 2; k++) r += (i ? u : 1 - u) * (j ? v : 1 - v) * (k ? w : 1 - w) * h(xi + i, yi + j, zi + k);
  return r;
}
// hair material: colour streaks along the strand direction, glossy sheen, and an optional stochastic fade (fades and stubble)
function dr3HairMat(col, o = {}) {
  const c = new THREE.Color(col), mat = new THREE.MeshPhysicalMaterial({ color: c, roughness: o.rough == null ? 0.5 : o.rough, metalness: 0, sheen: 0.4, sheenColor: c.clone().lerp(new THREE.Color(0xffffff), 0.25), sheenRoughness: 0.5, envMapIntensity: o.env == null ? 0.9 : o.env, side: o.side || THREE.FrontSide, vertexColors: !!o.vc });
  const U = { uStreak: { value: new THREE.Vector3(...(o.streak || [16, 10, 16])) }, uAmt: { value: o.amt == null ? 0.5 : o.amt }, uRing: { value: o.ring ? new THREE.Vector3(o.ring[0], o.ring[1], o.ring[2]) : new THREE.Vector3(0, 0, 0) } };
  if (o.fade) mat.defines = { USE_FADE: '' };
  mat.onBeforeCompile = (sh) => {
    Object.assign(sh.uniforms, U);
    sh.vertexShader = sh.vertexShader.replace('#include <common>', '#include <common>\nvarying vec3 vObjH;' + (o.fade ? '\nattribute float fadeA; varying float vFade;' : '')).replace('#include <begin_vertex>', '#include <begin_vertex>\nvObjH = position;' + (o.fade ? '\nvFade = fadeA;' : ''));
    sh.fragmentShader = sh.fragmentShader.replace('#include <common>', `#include <common>
      varying vec3 vObjH; uniform vec3 uStreak; uniform float uAmt; uniform vec3 uRing;
      ${o.fade ? 'varying float vFade;' : ''}
      float h31(vec3 p){ p = fract(p * 0.3183099 + 0.1); p *= 17.0; return fract(p.x * p.y * p.z * (p.x + p.y + p.z)); }
      float vn(vec3 x){ vec3 i = floor(x), f = fract(x); f = f * f * (3.0 - 2.0 * f); return mix(mix(mix(h31(i), h31(i + vec3(1,0,0)), f.x), mix(h31(i + vec3(0,1,0)), h31(i + vec3(1,1,0)), f.x), f.y), mix(mix(h31(i + vec3(0,0,1)), h31(i + vec3(1,0,1)), f.x), mix(h31(i + vec3(0,1,1)), h31(i + vec3(1,1,1)), f.x), f.y), f.z); }`)
      .replace('#include <color_fragment>', `#include <color_fragment>
      float st = vn(vObjH * uStreak) * 0.6 + vn(vObjH * uStreak * 2.7) * 0.4;
      if (uRing.z > 0.0) { float rr = length(vObjH.xz - uRing.xy) * uRing.z + vn(vObjH * 5.0) * 1.4; st = mix(st, 0.5 + 0.5 * sin(rr), 0.7); }
      diffuseColor.rgb *= mix(1.0 - uAmt * 0.8, 1.0 + uAmt * 0.5, st);
      ${o.fade ? 'if (h31(vObjH * 211.0 + 3.0) > vFade) discard;' : ''}`)
      .replace('#include <roughnessmap_fragment>', '#include <roughnessmap_fragment>\nroughnessFactor = clamp(roughnessFactor + (0.5 - st) * 0.35, 0.18, 0.95);');
  };
  mat.customProgramCacheKey = () => 'drhair' + (o.fade ? 'f' : '') + (o.ring ? 'r' : '') + (o.vc ? 'v' : '');
  return mat;
}
// a cap over part of the scalp (or lower face): vertices of the head grid pushed out along their normals by a thickness
function dr3Cap(H, spec) {
  const g = H.g, P = g.attributes.position, N = g.attributes.normal, n = P.count, map = new Int32Array(n).fill(-1), pos = [], fa = [], idx = g.index.array;
  for (let k = 0; k < n; k++) {
    const x = P.getX(k), y = P.getY(k), z = P.getZ(k), m = spec.mask(x, y, z); if (m < 0.004) continue;
    const t = spec.th(x, y, z, m) * Math.pow(m, 0.8) + (spec.min == null ? 0.007 : spec.min);
    map[k] = pos.length / 3; pos.push(x + N.getX(k) * t, y + N.getY(k) * t, z + N.getZ(k) * t); fa.push(Math.min(spec.fade ? Math.min(1, Math.max(0, spec.fade(x, y, z, m))) : 1, spec.hard ? 1 : 0.15 + 1.1 * dr3Ss(0.02, 0.7, m)));
  }
  const ix = []; for (let q = 0; q < idx.length; q += 3) { const a = map[idx[q]], b = map[idx[q + 1]], c = map[idx[q + 2]]; if (a >= 0 && b >= 0 && c >= 0) ix.push(a, b, c); }
  const bg = new THREE.BufferGeometry(); bg.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); bg.setAttribute('fadeA', new THREE.Float32BufferAttribute(fa, 1)); bg.setIndex(ix); bg.computeVertexNormals();
  return bg;
}
// random spots on the capped area, for tufts, curls and twists
function dr3Spots(H, spec, count, rng, minM = 0.7) {
  const g = H.g, P = g.attributes.position, N = g.attributes.normal, out = [], tries = count * 12;
  for (let t = 0; t < tries && out.length < count; t++) {
    const k = (rng() * P.count) | 0, x = P.getX(k), y = P.getY(k), z = P.getZ(k), m = spec.mask(x, y, z); if (m < minM) continue;
    const jx = (rng() - 0.5) * 0.05, jz = (rng() - 0.5) * 0.05, nrm = new THREE.Vector3(N.getX(k), N.getY(k), N.getZ(k));
    out.push({ p: new THREE.Vector3(x + jx, y, z + jz), n: nrm, x, y, z, t: spec.th(x, y, z, m), m });
  }
  return out;
}
function dr3Inst(geo, mat, items) {
  const m = new THREE.InstancedMesh(geo, mat, items.length), o = new THREE.Object3D(), c = new THREE.Color();
  items.forEach((it, i) => { o.position.copy(it.p); o.quaternion.setFromUnitVectors(DR3_Y, it.dir.clone().normalize()); if (it.rot) o.rotateY(it.rot); o.scale.set(it.s[0], it.s[1], it.s[2]); o.updateMatrix(); m.setMatrixAt(i, o.matrix); c.setScalar(it.c == null ? 1 : it.c); m.setColorAt(i, c); });
  m.instanceMatrix.needsUpdate = true; if (m.instanceColor) m.instanceColor.needsUpdate = true; m.castShadow = true; m.receiveShadow = true; return m;
}
// a hanging sheet of hair: rows run down, columns run around the head
function dr3Sheet(cols, rows, f, mat) {
  const pos = [], fa = [], ix = [];
  for (let r = 0; r <= rows; r++) for (let c = 0; c <= cols; c++) { const p = f(c / cols, r / rows); pos.push(p.x, p.y, p.z); fa.push(p.a == null ? 1 : p.a); }
  for (let r = 0; r < rows; r++) for (let c = 0; c < cols; c++) { const a = r * (cols + 1) + c, b = a + 1, d = a + cols + 1, e = d + 1; ix.push(a, d, b, b, d, e); }
  const g = new THREE.BufferGeometry(); g.setAttribute('position', new THREE.Float32BufferAttribute(pos, 3)); g.setAttribute('fadeA', new THREE.Float32BufferAttribute(fa, 1)); g.setIndex(ix); g.computeVertexNormals();
  const m = new THREE.Mesh(g, mat); m.castShadow = true; m.receiveShadow = true; return m;
}
const DR3_HAT_CUT = { cap: 0.34, snap: 0.34, beanie: 0.3, bucket: 0.38, cowboy: 0.38, fedora: 0.42, pumpkin: 0.3, pilgrim: 0.38, santa: 0.3, phones: 9, visor: 9 };

// per-style recipe: where hair sits (cut = lowest hair line at the sides/back, sb = sideburn depth), how thick it is, how it fades
const DR3_STYLES = {
  short: { cut: 0.2, sb: 0.46, th: (x, y) => 0.05 + 0.085 * dr3Ss(0.3, 0.85, y), fade: (x, y) => dr3Ss(0.12, 0.5, y), streak: [20, 12, 20], tufts: 'crop' },
  fade: { cut: 0.1, sb: 0.3, th: (x, y, z) => 0.03 + 0.1 * dr3Ss(0.45, 0.9, y) + 0.03 * dr3Ss(0.2, 0.9, z), fade: (x, y) => dr3Ss(0.02, 0.62, y), streak: [22, 14, 22], scalp: 0.7 },
  waves: { cut: 0.16, sb: 0.4, th: (x, y) => 0.034 + 0.03 * dr3Ss(0.3, 0.9, y), fade: (x, y) => dr3Ss(0.05, 0.48, y), ring: true, scalp: 0.6 },
  buzz: { cut: 0.1, sb: 0.4, th: () => 0.016, fade: (x, y) => 0.9 * dr3Ss(0.0, 0.3, y), streak: [60, 60, 60], amt: 0.3, scalp: 0.7 },
  slick: { cut: 0.14, sb: 0.4, th: (x, y, z) => 0.055 + 0.075 * dr3Ss(0.5, 0.95, y) + 0.06 * dr3Ss(0.3, 0.7, y) * dr3Ss(0.1, 0.9, z), fade: (x, y) => dr3Ss(0.02, 0.4, y), streak: [34, 3, 3], rough: 0.32, flow: 'back' },
  quiff: { cut: 0.18, sb: 0.4, th: (x, y, z) => 0.05 + 0.07 * dr3Ss(0.4, 0.9, y) + 0.2 * dr3G2(x, y, 0, 0.8, 0.45, 0.28) * dr3Ss(-0.1, 0.8, z), fade: (x, y) => dr3Ss(0.05, 0.5, y), streak: [34, 3, 3], rough: 0.36, tufts: 'quiff' },
  mohawk: { cut: 0.3, sb: 0.4, th: (x, y) => 0.025 + 0.02 * dr3Ss(0.4, 0.9, y), fade: (x, y, z) => 0.55 * dr3Ss(0.2, 0.7, y) * (Math.abs(x) < 0.3 ? 2 : 0.3), streak: [50, 30, 50], scalp: 0.6, tufts: 'mohawk' },
  ponytail: { cut: 0.14, sb: 0.4, th: (x, y, z) => 0.05 + 0.065 * dr3Ss(0.5, 0.95, y), fade: (x, y) => dr3Ss(0.02, 0.4, y), streak: [34, 3, 3], rough: 0.34, flow: 'back', extra: 'ponytail' },
  bun: { cut: 0.14, sb: 0.4, th: (x, y, z) => 0.05 + 0.06 * dr3Ss(0.5, 0.95, y), fade: (x, y) => dr3Ss(0.02, 0.45, y), streak: [34, 3, 3], rough: 0.34, flow: 'back', extra: 'bun' },
  curly: { cut: 0.22, sb: 0.4, th: (x, y) => 0.06 + 0.04 * dr3Ss(0.3, 0.9, y), fade: (x, y) => dr3Ss(0.12, 0.5, y), tufts: 'curls', scalp: 0.4 },
  afro: { cut: 0.1, sb: 0.46, th: (x, y) => 0.1, fade: (x, y) => dr3Ss(0.0, 0.3, y), tufts: 'afro' },
  twists: { cut: 0.2, sb: 0.46, th: (x, y) => 0.05, fade: (x, y) => dr3Ss(0.1, 0.5, y), tufts: 'twists', scalp: 0.5 },
  cornrows: { cut: 0.12, sb: 0.4, th: () => 0.022, fade: (x, y) => 0.9 * dr3Ss(0.02, 0.28, y), tufts: 'cornrows', scalp: 0.65, streak: [40, 40, 40] },
  locs: { cut: 0.28, sb: 0.46, th: () => 0.05, fade: (x, y) => dr3Ss(0.1, 0.5, y), tufts: 'locs' },
  wavy: { cut: -0.05, sb: 0.7, hlOff: -0.12, th: (x, y, z) => 0.07 + 0.08 * dr3Ss(0.3, 0.95, y), fade: () => 1, streak: [16, 5, 16], tufts: 'shag' },
  curtains: { cut: -0.02, sb: 0.7, hlOff: -0.1, th: (x, y, z) => 0.06 + 0.07 * dr3Ss(0.3, 0.95, y), fade: () => 1, streak: [30, 3, 30], tufts: 'curtains' },
  mullet: { cut: 0.0, sb: 0.55, th: (x, y, z) => 0.05 + 0.08 * dr3Ss(0.3, 0.9, y), fade: () => 1, streak: [26, 4, 26], tufts: 'crop', extra: 'mullet' },
  long: { cut: -0.3, sb: 0.7, th: (x, y, z) => 0.06 + 0.08 * dr3Ss(0.3, 0.95, y), fade: () => 1, streak: [34, 3, 34], rough: 0.4, extra: 'long' },
};
function dr3HairSpec(look, style, hatCut) {
  const S = DR3_STYLES[style]; if (!S) return null;
  const old = look.age === 'old', hl = (old ? 0.66 : look.age === 'adult' ? 0.6 : 0.575) + (S.hlOff || 0);
  const yh = (ax) => hl + 0.08 * dr3Ss(0.2, 0.5, ax) - S.sb * dr3Ss(0.6, 0.84, ax);
  const mask = (x, y, z) => {
    const ax = Math.abs(x), w = dr3Ss(-0.15, 0.35, z), cut = S.cut + (yh(ax) - S.cut) * w;
    let m = dr3Ss(cut - 0.03, cut + 0.05, y); if (hatCut < 5) m *= 1 - dr3Ss(hatCut - 0.02, hatCut + 0.08, y) * (S.extra === 'long' || style === 'locs' ? 0 : 1);
    return m;
  };
  return { ...S, mask, yh, hl };
}
// the scalp tint under short hair, so a fade or buzz reads as hair rather than bare skin
function dr3Scalp(look, style, hatCut) { const sp = dr3HairSpec(look, style, 99); if (!sp || !sp.scalp) return null; const c = new THREE.Color(DR_HAIRC[look.hairC] || DR_HAIRC[0]).multiplyScalar(0.8); return { c, a: sp.scalp, mask: (x, y, z) => sp.mask(x, y, z * 0.9 + 0.001) * (sp.fade ? Math.min(1, 0.35 + sp.fade(x, y, z)) : 1) * 0.7 }; }

function dr3Hair(H, look, hat, parts) {
  const style = DR_HAIR.some((h) => h[0] === look.hair) ? look.hair : 'short', grp = new THREE.Group(), hatCut = hat ? (DR3_HAT_CUT[hat] == null ? 99 : DR3_HAT_CUT[hat]) : 99;
  const hc = DR_HAIRC[look.hairC] || DR_HAIRC[0], rng = dr3Rng(dr3Hash(style + look.hairC + look.skin));
  if (style === 'bald') return { grp, vol: 1 };
  const spec = dr3HairSpec(look, style, hatCut), fadeMat = dr3HairMat(hc, { fade: true, streak: spec.streak, ring: spec.ring ? [0, -0.1, 7] : null, rough: spec.rough, amt: spec.amt });
  const solid = dr3HairMat(hc, { streak: spec.streak || [16, 10, 16], rough: spec.rough });
  const cap = new THREE.Mesh(dr3Cap(H, spec), fadeMat); cap.castShadow = true; cap.receiveShadow = true; grp.add(cap);
  const hatOn = hatCut < 5, vol = 1 + (hatOn ? 0 : { afro: 0.34, curly: 0.14, quiff: 0.12, twists: 0.1, locs: 0.1, mohawk: 0.04 }[style] || 0.04);
  const up = (a) => a.y < hatCut + 0.12;
  const spots = (n, mm) => dr3Spots(H, spec, n, rng, mm).filter((s) => s.y < (hatOn ? hatCut : 99));
  const T = spec.tufts;
  if (T === 'crop' || T === 'quiff') { // a choppy fringe of flat blades, and chunky textured clumps on top
    const geo = new THREE.ConeGeometry(0.11, 0.34, 6); geo.translate(0, 0.17, 0); const items = [], fr = T === 'quiff' ? 20 : 17;
    for (let i = 0; i < fr; i++) { const t = i / (fr - 1), x = (t - 0.5) * 1.12, y = spec.yh(Math.abs(x)) + 0.035 + rng() * 0.05, r = H.ring(y), z = r.zf * Math.pow(Math.max(0, 1 - Math.pow(Math.abs(x) / Math.max(0.1, r.wx), 2.3)), 1 / 2.3) - 0.02; if (hatOn && y > hatCut) continue;
      const th = spec.th(x, y, z, 1) + 0.01, dir = new THREE.Vector3(x * 0.3, T === 'quiff' ? 0.9 : -0.35, 0.8 + rng() * 0.2); items.push({ p: new THREE.Vector3(x, y + th * 0.4, z + th * 0.5), dir, s: [1 + rng() * 0.5, 0.75 + rng() * 0.45, 0.5], rot: rng() * 3, c: 0.8 + rng() * 0.3 }); }
    grp.add(dr3Inst(geo, solid, items));
    const ch = new THREE.IcosahedronGeometry(1, 1), citems = [];
    for (const s of spots(T === 'quiff' ? 56 : 46, 0.9)) { if (s.y < 0.5) continue; const big = T === 'quiff' && s.z > 0.2 && s.y > 0.6; const sc = (big ? 0.2 : 0.15) * (0.8 + rng() * 0.5); citems.push({ p: s.p.clone().addScaledVector(s.n, s.t * 0.55 + (big ? 0.07 : 0.0)), dir: big ? s.n.clone().add(new THREE.Vector3(0, 0.6, 0.5)) : s.n, s: [sc * 1.1, sc * 0.55, sc * 1.3], rot: rng() * 6, c: 0.62 + rng() * 0.55 }); }
    grp.add(dr3Inst(ch, dr3HairMat(hc, { streak: [22, 22, 22], amt: 0.7, rough: 0.5 }), citems));
  } else if (T === 'mohawk') {
    const geo = new THREE.ConeGeometry(0.1, 0.6, 7); geo.translate(0, 0.3, 0); const items = [];
    for (let i = 0; i < 44; i++) { const t = i / 43, ang = Math.PI * (0.18 + t * 0.78), y = Math.sin(ang) * 0.98, z = Math.cos(ang) * 0.98, rr = H.ring(Math.min(0.99, y)); const pz = z > 0 ? Math.min(z, rr.zf) : Math.max(z, -rr.zb); if (hatOn && y > hatCut) continue;
      const h = 0.5 + 0.35 * Math.sin(t * 3.1 + 0.3), n = new THREE.Vector3(0, Math.sin(ang), Math.cos(ang)); items.push({ p: new THREE.Vector3((rng() - 0.5) * 0.04, y * 0.98, pz * 0.98), dir: n.add(new THREE.Vector3(0, 0, -0.2)), s: [2.1, h / 0.6 * (0.9 + rng() * 0.2), 0.9], rot: 0, c: 0.9 + rng() * 0.2 }); }
    for (let i = 0; i < 44; i++) { const t = i / 43, ang = Math.PI * (0.18 + t * 0.78) + 0.03, y = Math.sin(ang) * 0.97, z = Math.cos(ang) * 0.97, rr = H.ring(Math.min(0.99, y)); const pz = z > 0 ? Math.min(z, rr.zf) : Math.max(z, -rr.zb); items.push({ p: new THREE.Vector3(i % 2 ? 0.07 : -0.07, y * 0.98, pz * 0.98), dir: new THREE.Vector3(i % 2 ? 0.2 : -0.2, Math.sin(ang), Math.cos(ang)), s: [1.1, 0.7, 0.55], rot: 0, c: 0.85 }); }
    grp.add(dr3Inst(geo, solid, items));
  } else if (T === 'curls' || T === 'afro') {
    const geo = new THREE.IcosahedronGeometry(1, 2), items = [];
    if (T === 'curls') { for (const s of spots(230, 0.8)) { const r = 0.11 + rng() * 0.07; items.push({ p: s.p.clone().addScaledVector(s.n, s.t + r * 0.55), dir: DR3_Y, s: [r, r * 0.92, r], c: 0.62 + rng() * 0.5 }); } }
    else { // afro: a big halo of coils around the whole head
      const C = new THREE.Vector3(0, 0.22, -0.12), N2 = 520;
      for (let i = 0; i < N2; i++) { const yy = 1 - (i + 0.5) / N2 * 1.55, rr = Math.sqrt(Math.max(0, 1 - yy * yy)), th = i * 2.399963, d = new THREE.Vector3(Math.cos(th) * rr, yy, Math.sin(th) * rr); const front = d.z > 0.2 && d.y < 0.35; if (front || d.y < -0.35) continue; if (hatOn && d.y * 1.2 + 0.22 > hatCut) continue; const R = 1.27 + (rng() - 0.5) * 0.1, r = 0.17 + rng() * 0.06; const p = C.clone().add(new THREE.Vector3(d.x * R * 0.98, d.y * R * 0.92, d.z * R)); items.push({ p, dir: DR3_Y, s: [r, r * 0.9, r], c: 0.6 + rng() * 0.5 }); }
      const core = new THREE.Mesh(new THREE.SphereGeometry(1, 36, 24), dr3HairMat(new THREE.Color(hc).multiplyScalar(0.55), { streak: [8, 8, 8] })); core.scale.set(1.18, 1.05, 0.78); core.position.set(0, 0.22, -0.4); if (!hatOn) grp.add(core); }
    grp.add(dr3Inst(geo, dr3HairMat(hc, { streak: [10, 10, 10], amt: 0.65, rough: 0.6 }), items));
  } else if (T === 'twists') {
    const geo = new THREE.CapsuleGeometry(0.072, 0.17, 4, 10); geo.translate(0, 0.14, 0); const items = [];
    const tw = dr3HairMat(hc, { streak: [10, 40, 10], amt: 0.75, rough: 0.5 });
    for (const s of spots(230, 0.85)) { const d = s.n.clone().multiplyScalar(1.0).add(new THREE.Vector3((rng() - 0.5) * 0.5, (rng() - 0.5) * 0.3, (rng() - 0.5) * 0.5)); items.push({ p: s.p.clone().addScaledVector(s.n, s.t * 0.4), dir: d, s: [1, 0.8 + rng() * 0.8, 1], rot: rng() * 6, c: 0.65 + rng() * 0.45 }); }
    grp.add(dr3Inst(geo, tw, items));
  } else if (T === 'cornrows') {
    const mat = dr3HairMat(hc, { streak: [60, 8, 60], amt: 0.6, rough: 0.4 }), b = new Dr3Bld(), nr = 7;
    for (let k = 0; k < nr; k++) { const x0 = (k - (nr - 1) / 2) * 0.17, pts = []; for (let i = 0; i <= 44; i++) { const t = i / 44, ang = Math.PI * (0.16 + t * 0.76), y = Math.sin(ang), z = Math.cos(ang), r = H.ring(Math.min(0.995, y)), x = x0 * Math.sqrt(Math.max(0.05, r.wx / 0.9)); const sx = Math.min(Math.abs(x), r.wx * 0.97) * Math.sign(x), zz = (z > 0 ? r.zf : r.zb) * Math.pow(Math.max(0, 1 - Math.pow(Math.abs(sx) / Math.max(0.01, r.wx), 2.3)), 1 / 2.3); const zv = z > 0 ? zz : -zz, p = new THREE.Vector3(sx, y, zv); if (y < spec.yh(Math.abs(sx)) + 0.0 && z > 0) continue; pts.push(p.multiplyScalar(1.0)); }
      if (pts.length < 4) continue; const sm = pts.map((p, i) => { const nn = p.clone().normalize(); return p.clone().addScaledVector(new THREE.Vector3(p.x * 0.35, p.y, p.z * 0.95).normalize(), 0.035); });
      b.tube(sm, (f) => 0.045 * (0.5 + 0.8 * Math.sin(Math.PI * Math.min(1, f * 0.94 + 0.03))) , new THREE.Color(0xffffff).multiplyScalar(0.8 + 0.2 * Math.sin(k)), 7); }
    if (b.p.length) { const m = new THREE.Mesh(b.geo(), dr3HairMat(hc, { streak: [50, 50, 50], amt: 0.35, rough: 0.4, vc: true })); m.castShadow = true; grp.add(m); }
  } else if (T === 'locs') {
    const b = new Dr3Bld(), spotsL = spots(110, 0.9).filter((s) => s.z < 0.3 || (s.y > 0.7 && Math.abs(s.x) > 0.5)).slice(0, 56);
    for (const s of spotsL) { if (s.z > 0.55 && s.y > 0.55 && s.y < 0.9 && Math.abs(s.x) < 0.45) { /* hairline clump stays short */ }
      const front = s.z > 0.0, L = front ? 0.9 + rng() * 0.5 : 1.4 + rng() * 0.9, pts = [], root = s.p.clone().addScaledVector(s.n, s.t * 0.6), dir = new THREE.Vector3(s.n.x * 0.9, -0.15 + s.n.y * 0.25, Math.min(0.1, s.n.z) * 0.9 - 0.1).normalize(), pos = root.clone(), ph = rng() * 6;
      pts.push(pos.clone()); const seg = 9; for (let i = 0; i < seg; i++) { const f = (i + 1) / seg; dir.y -= 0.35 + f * 0.3; dir.x += Math.sin(f * 5 + ph) * 0.05; dir.z += Math.cos(f * 4 + ph) * 0.04; dir.normalize(); pos.addScaledVector(dir, L / seg); dr3Collide(pos, 0.1); pts.push(pos.clone()); }
      const col = new THREE.Color(0xffffff).multiplyScalar(0.7 + rng() * 0.5), rad = 0.075 + rng() * 0.02; b.tube(pts, (f) => rad * (1 - Math.pow(f, 7) * 0.9) * (0.9 + 0.1 * Math.sin(f * 40)), col, 7); }
    if (b.p.length) { const m = new THREE.Mesh(b.geo(), dr3HairMat(hc, { streak: [8, 60, 8], amt: 0.7, rough: 0.55, vc: true })); m.castShadow = true; m.receiveShadow = true; grp.add(m); }
  } else if (T === 'shag' || T === 'curtains') {
    const b = new Dr3Bld(), n = T === 'curtains' ? 64 : 84, sp = spots(n * 3, 0.9).filter((s) => (T === 'curtains' ? s.y > 0.55 && Math.abs(s.x) > 0.04 : s.y > 0.3));
    for (let i = 0; i < Math.min(n, sp.length); i++) { const s = sp[i], side = s.x < 0 ? -1 : 1, root = s.p.clone().addScaledVector(s.n, s.t * 0.6), pos = root.clone(), pts = [root.clone()], L = T === 'curtains' ? 0.7 + rng() * 0.5 : 0.5 + rng() * 0.5, seg = 7;
      const dir = new THREE.Vector3(side * (T === 'curtains' ? 0.7 : 0.35) + s.n.x * 0.4, 0.15, s.n.z * 0.4 + (T === 'curtains' ? 0.45 : 0.3)).normalize();
      for (let k = 0; k < seg; k++) { const f = (k + 1) / seg; dir.y -= 0.28 + f * 0.2; dir.x += side * 0.05; dir.normalize(); pos.addScaledVector(dir, L / seg); dr3Collide(pos, 0.07); pts.push(pos.clone()); }
      b.tube(pts, (f) => 0.085 * (1 - Math.pow(f, 3) * 0.85), new THREE.Color(0xffffff).multiplyScalar(0.72 + rng() * 0.45), 5); }
    if (b.p.length) { const m = new THREE.Mesh(b.geo(), dr3HairMat(hc, { streak: [10, 50, 10], amt: 0.55, rough: 0.5, vc: true })); m.castShadow = true; m.receiveShadow = true; grp.add(m); }
  }
  // extras that hang or gather
  const sheetMat = dr3HairMat(hc, { fade: true, streak: [38, 2, 38], amt: 0.6, rough: 0.42, side: THREE.DoubleSide });
  const dropRing = (psi, y0, rad, drop) => { const r = H.ring(y0), s = Math.sin(psi), c = Math.cos(psi); return new THREE.Vector3(r.wx * Math.sign(s) * Math.pow(Math.abs(s), 0.87) * rad, 0, -(c > 0 ? r.zb : r.zf * 0.9) * Math.pow(Math.abs(c), 0.87) * rad * Math.sign(c)); };
  if (spec.extra === 'long' || spec.extra === 'mullet') {
    const long = spec.extra === 'long', span = long ? 2.55 : 2.0, bottom = long ? -2.35 : -1.55;
    grp.add(dr3Sheet(46, 20, (u, v) => { const psi = (u - 0.5) * span * 1.25, y0 = long ? 0.45 - 0.35 * Math.pow(Math.abs(psi) / 1.6, 1.4) : 0.18, rad = 1.07 + 0.32 * Math.pow(v, 0.9) + (long ? 0.18 : 0.06) * v * Math.abs(Math.sin(psi)), base = dropRing(psi, Math.max(-0.5, y0 - 0.3 * v), rad, 0), edge = 1 + 0.14 * dr3Vn(psi * 5, 3.1, 0) - 0.05;
      const L = (y0 - bottom) * (long ? 1 - 0.18 * Math.pow(Math.abs(psi) / 1.6, 2) : 1) * edge, p = new THREE.Vector3(base.x * (1 + 0.1 * v), y0 - v * L, base.z * (1 + 0.1 * v)); dr3Collide(p, 0.09); p.a = v < 0.62 ? 1 : 1 - dr3Ss(0.62, 1, v) * (0.5 + 0.5 * dr3Vn(psi * 18, v * 9, 4)); return p; }, sheetMat));
    if (long) for (const side of [-1, 1]) { const b = new Dr3Bld(); for (let i = 0; i < 10; i++) { const f0 = i / 9, o = (f0 - 0.5) * 0.18, base = [[0.8, 0.3, 0.15], [0.94, -0.35, 0.12], [1.02, -0.95, 0.12], [1.2, -1.55, 0.3], [1.34, -2.05, 0.5], [1.38, -2.4 - f0 * 0.25, 0.6]].map(([x, y, z]) => new THREE.Vector3(side * (x + o * 0.4), y, z + o * 0.6 + 0.02 * Math.sin(i * 2.1 + y * 3)));
      const curve = new THREE.CatmullRomCurve3(base), pts = curve.getPoints(14); for (const p of pts) dr3Collide(p, 0.1); b.tube(pts, (f) => 0.1 * (1 - Math.pow(f, 3) * 0.8), new THREE.Color(0xffffff).multiplyScalar(0.75 + 0.35 * dr3Vn(i, side, 2)), 6); }
      const m = new THREE.Mesh(b.geo(), dr3HairMat(hc, { streak: [10, 40, 10], amt: 0.6, rough: 0.45, vc: true })); m.castShadow = true; grp.add(m); }
  }
  if (spec.extra === 'ponytail') {
    const b = new Dr3Bld(), root = new THREE.Vector3(0, 0.62, -0.92);
    for (let i = 0; i < 9; i++) { const o = (i - 4) * 0.035, pts = []; for (let k = 0; k <= 12; k++) { const f = k / 12; pts.push(new THREE.Vector3(o * (1 + f * 2) + Math.sin(f * 5 + i) * 0.03, root.y + 0.1 - f * 1.2 + 0.18 * Math.sin(f * 2.2), root.z - 0.06 - 0.4 * Math.sin(f * 1.9) - f * 0.12)); } b.tube(pts, (f) => 0.1 * (1 - Math.pow(f, 2.4) * 0.8), new THREE.Color(0xffffff).multiplyScalar(0.75 + 0.3 * dr3Vn(i, 1, 1)), 6); }
    const m = new THREE.Mesh(b.geo(), dr3HairMat(hc, { streak: [10, 50, 10], amt: 0.5, rough: 0.35, vc: true })); m.castShadow = true; grp.add(m);
    const band = new THREE.Mesh(new THREE.TorusGeometry(0.13, 0.03, 8, 20), new THREE.MeshStandardMaterial({ color: 0x1c1c20, roughness: 0.6 })); band.position.set(0, root.y, root.z - 0.02); band.rotation.x = Math.PI / 2.4; grp.add(band);
  }
  if (spec.extra === 'bun') {
    const bun = new THREE.Mesh(new THREE.SphereGeometry(0.4, 40, 28), dr3HairMat(hc, { streak: [14, 14, 14], ring: [0, 0, 0], amt: 0.5, rough: 0.34 })); bun.position.set(0, 1.0, -0.4); bun.scale.set(1, 0.92, 1); bun.castShadow = true; grp.add(bun);
    const tie = new THREE.Mesh(new THREE.TorusGeometry(0.28, 0.03, 8, 24), new THREE.MeshStandardMaterial({ color: 0x1c1c20, roughness: 0.6 })); tie.position.set(0, 0.74, -0.33); tie.rotation.x = Math.PI / 2 - 0.3; grp.add(tie);
  }
  return { grp, vol };
}

// ---------------------------------------------------------------- facial hair: the same cap trick on the lower face
function dr3Beard(H, mesh, look, hit) {
  const grp = new THREE.Group(), f = look.facial, hc = DR_HAIRC[look.hairC] || DR_HAIRC[0]; if (!f || f === 'none') return grp;
  const e = (ax, y) => (ax / 0.27) ** 2 + ((y + 0.645) / 0.075) ** 2, notMouth = (ax, y) => dr3Ss(0.75, 1.25, e(ax, y));
  const stache = (x, y, z) => { const ax = Math.abs(x); return z > 0 ? dr3Ss(0.34, 0.26, ax) * dr3Ss(-0.58, -0.54, y) * dr3Ss(-0.44, -0.49, y) * 0 + dr3Ss(0.34, 0.2, ax) * dr3Ss(-0.445, -0.49, y) * dr3Ss(-0.595, -0.565, y) : 0; };
  const defs = {
    stubble: { mask: (x, y, z) => { const ax = Math.abs(x), r = H.ring(y); return z > -0.1 ? dr3Ss(-0.3, -0.5, y) * dr3Ss(r.wx * 0.98, r.wx * 0.8, ax) * notMouth(ax, y) * 0.9 + 0.1 * dr3Ss(0.78, 0.84, ax) * dr3Ss(-0.4, -0.15, y) * dr3Ss(-0.55, -0.35, y) * 0 : 0; }, th: () => 0.012, fade: () => 0.5, o: { amt: 0.2, streak: [90, 90, 90] } },
    mustache: { mask: stache, th: () => 0.04, o: { streak: [30, 6, 30] } },
    goatee: { mask: (x, y, z) => { const ax = Math.abs(x); return Math.max(stache(x, y, z), z > 0 ? dr3Ss(0.26, 0.14, ax) * dr3Ss(-0.72, -0.78, y) * dr3Ss(-1.0, -0.9, -Math.abs(y + 0.9) + 0.0 - 0.0 + 1.0) : 0); }, th: (x, y) => 0.04 + 0.035 * dr3Ss(-0.75, -0.95, y), o: { streak: [30, 6, 30] } },
    chin: { mask: (x, y, z) => { const ax = Math.abs(x), r = H.ring(y), t = Math.exp(-(((ax - r.wx * 0.9) / 0.06) ** 2)); return z > -0.2 ? t * dr3Ss(-0.98, -0.85, y) * dr3Ss(-0.02, -0.2, y) * 0 + t * dr3Ss(-0.04, -0.3, y) * dr3Ss(-0.98, -0.88, y) + (Math.abs(x) < 0.18 && y < -0.84 ? dr3Ss(0.18, 0.1, ax) * 0.8 : 0) : 0; }, th: () => 0.045, o: { streak: [30, 8, 30] } },
    beard: { mask: (x, y, z) => { const ax = Math.abs(x), r = H.ring(y); if (z < -0.15) return 0; const below = dr3Ss(-0.36, -0.5, y) * dr3Ss(r.wx * 1.02, r.wx * 0.9, ax), side = dr3Ss(0.74, 0.82, ax) * dr3Ss(-0.62, -0.5, y) * dr3Ss(0.0, -0.1, y) * 0 + dr3Ss(0.72, 0.8, ax) * dr3Ss(-0.05, -0.2, y) * dr3Ss(-0.65, -0.5, y); return Math.max(below, side * 0.9) * notMouth(ax, y) * (1 - dr3Ss(-0.46, -0.52, y) * dr3Ss(0.22, 0.12, ax) * 0); }, th: (x, y) => 0.035 + 0.05 * dr3Ss(-0.65, -0.95, y), fade: (x, y, z, m) => dr3Ss(0.0, 0.7, m), o: { streak: [28, 7, 28] } },
  };
  const d = defs[f] || defs.stubble, o = { ...d.o, fade: true, rough: 0.55 }, spec = { mask: d.mask, th: d.th, fade: d.fade || ((x, y, z, m) => dr3Ss(0.05, 0.6, m)), min: 0.006 };
  const m = new THREE.Mesh(dr3Cap(H, spec), dr3HairMat(hc, o)); m.castShadow = true; m.receiveShadow = true; grp.add(m);
  return grp;
}
// a shadow of stubble or beard on the skin under the facial-hair cap
function dr3BeardTint(look, H) {
  const f = look.facial; if (!f || f === 'none' || f === 'mustache' || f === 'goatee' || f === 'chin') return null;
  const e = (ax, y) => (ax / 0.27) ** 2 + ((y + 0.645) / 0.075) ** 2;
  const c = new THREE.Color(DR_HAIRC[look.hairC] || DR_HAIRC[0]).multiplyScalar(0.7);
  return { c, a: f === 'beard' ? 0.7 : 0.38, mask: (x, y, z) => { const ax = Math.abs(x), r = H.ring(y); return dr3Ss(-0.3, -0.5, y) * dr3Ss(r.wx * 1.0, r.wx * 0.82, ax) * dr3Ss(0.75, 1.25, e(ax, y)); } };
}
