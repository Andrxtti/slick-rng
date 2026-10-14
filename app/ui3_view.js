// ---------------------------------------------------------------- solver worker
const worker = new Worker(URL.createObjectURL(new Blob([src], { type: 'text/javascript' })));
let solving = false, t0 = 0, secs = 60, built = null, path = [], selected = null, reqId = 0, report = null, evData = null, timer = null, lastIt = { t: 0, n: 0 }, rate = 0, spotGame = 'cash';
worker.onmessage = (ev) => {
  const m = ev.data;
  if (m.type === 'error') { $('err').textContent = m.message; if (m.reqId === undefined) endSolve(); return; }
  if (m.type === 'built') { built = m; path = []; selected = null; evData = null; renderEVs(); t0 = performance.now(); lastIt = { t: t0, n: 0 }; worker.postMessage({ cmd: 'solve', seconds: secs }); requestReport(); }
  if (m.type === 'progress') {
    const now = performance.now();
    if (now - lastIt.t > 900) { rate = (m.iters - lastIt.n) / ((now - lastIt.t) / 1000); lastIt = { t: now, n: m.iters }; }
    $('meter').style.width = `${Math.min(100, ((now - t0) / 1000 / secs) * 100)}%`;
    $('status').textContent = `${m.iters.toLocaleString()} hands dealt · ${Math.round(rate).toLocaleString()}/s · ${(m.bytes / 1e6).toFixed(m.bytes < 1e7 ? 1 : 0)} MB`;
    if (m.memoryCapped) $('err').textContent = 'Memory limit reached. Try Simple sizes.';
    if (m.done) { endSolve(); requestReport(); worker.postMessage({ cmd: 'ev', samples: 40000 }); }
  }
  if (m.type === 'report' && m.reqId === reqId) { report = m.report; if (view === 'post') renderNode(); }
  if (m.type === 'ev') { evData = m.ev; renderEVs(); }
};
let solvedOnce = false;
function markStale() { if (solvedOnce && !solving) { $('status').textContent = 'Spot changed · press Solve to update'; $('meter').style.width = '0'; } }
function endSolve() { solvedOnce = true; endSolve2(); }
function endSolve2() { solving = false; $('solveBtn').hidden = false; $('stopBtn').hidden = true; $('meter').style.width = '100%'; clearInterval(timer); }
function startSolve(seconds) {
  $('err').textContent = '';
  let spot; try { spot = buildSpot(); Engine.makeSpot(spot); } catch (e) { $('err').textContent = e.message; return; }
  solving = true; secs = seconds || +$('depth').value; spotGame = S.game; setView('post');
  $('solveBtn').hidden = true; $('stopBtn').hidden = false; $('meter').style.width = '0'; $('status').textContent = 'Building the game tree…';
  worker.postMessage({ cmd: 'build', spot });
  clearInterval(timer); timer = setInterval(() => { if (solving) requestReport(); }, 2500);
}
$('solveBtn').onclick = () => startSolve();
$('tabPre').onclick = () => setView('pre');
$('tabPost').onclick = () => setView('post');
$('stopBtn').onclick = () => worker.postMessage({ cmd: 'stop' });
function requestReport() { if (!built) return; reqId++; worker.postMessage({ cmd: 'report', path, reqId }); }

// ---------------------------------------------------------------- strategy view
function colors(types) {
  const bets = types.map((t, i) => (t === 'bet' ? i : -1)).filter((i) => i >= 0), ramp = ['#e3a83f', '#e07a37', '#d24f3c', '#b13447'];
  return types.map((t, i) => t === 'fold' ? 'var(--a-fold)' : t === 'check' || t === 'call' ? 'var(--a-pass)' : t === 'allin' ? 'var(--a-allin)' : ramp[bets.length === 1 ? 1 : Math.round((bets.indexOf(i) / (bets.length - 1)) * 3)]);
}
function cardHTML(c) { const s = SUITS[c & 3]; return `<span class="crumb cardcrumb" style="color:${s === 'h' ? '#ee7a6b' : s === 'd' ? '#69a8e8' : s === 'c' ? '#6fc08a' : 'var(--fg)'}">${RANKS[c >> 2]}${SYM[s]}</span>`; }
function renderEVs() {
  const box = $('evs'); box.innerHTML = '';
  if (!evData || !built) return;
  evData.forEach((e, i) => { const d = document.createElement('div'); d.className = 'ev'; d.title = 'Expected value under the solved strategy';
    d.innerHTML = `${esc(built.names[i])} <b>${spotGame === 'icm' ? `${e.ev.toFixed(2)}%` : `${e.ev >= 0 ? '+' : ''}${e.ev.toFixed(2)} bb`}</b>`; box.appendChild(d); });
}
function renderCrumbs(r) {
  const box = $('crumbs'); box.innerHTML = '';
  if (!built) { box.hidden = true; return; } box.hidden = false;
  const root = document.createElement('button'); root.className = 'crumb'; root.textContent = 'Flop start'; root.onclick = () => { path = []; selected = null; requestReport(); }; box.appendChild(root);
  (r ? r.history : []).forEach((h, i) => {
    box.insertAdjacentHTML('beforeend', '<span class="arrow">›</span>');
    if (h.type === 'card') { box.insertAdjacentHTML('beforeend', cardHTML(h.card)); return; }
    const b = document.createElement('button'); b.className = 'crumb'; b.textContent = `${built.names[h.player]} ${h.label}`;
    b.onclick = () => { path = path.slice(0, i); selected = null; requestReport(); }; box.appendChild(b);
  });
}
function renderNode() {
  const r = view === 'pre' ? preReport : report;
  const v = $('nodeView');
  if (view === 'pre') { $('crumbs').hidden = true; } else renderCrumbs(r);
  if (!r) { v.innerHTML = `<div class="empty-state">${view === 'pre' ? 'Tap a seat to see its preflop chart.' : 'Press Solve to see the postflop strategy.'}</div>`; return; }
  const boardStr = r.board.map(cardHTML).join(' ');
  if (r.kind === 'chance') {
    v.innerHTML = `<div class="node"><div class="nodetitle"><h2>Pick the ${r.street === 1 ? 'turn' : 'river'}</h2><span class="muted mono">pot ${fmt(r.pot)}</span> ${boardStr}</div><div class="picker" id="deal"></div></div>`;
    for (let s = 3; s >= 0; s--) for (let k = 12; k >= 0; k--) {
      const c = k * 4 + s, b = document.createElement('button'); b.className = `card s-${SUITS[s]}`; b.textContent = RANKS[k] + SYM[SUITS[s]]; b.disabled = r.board.includes(c);
      b.onclick = () => { path.push({ card: c }); requestReport(); }; $('deal').appendChild(b);
    }
    return;
  }
  if (r.kind === 'terminal') {
    const left = r.folded.map((f, i) => (f ? null : built.names[i])).filter(Boolean);
    v.innerHTML = `<div class="node"><div class="nodetitle"><h2>${left.length === 1 ? `${esc(left[0])} wins` : 'Showdown'}</h2><span class="muted mono">pot ${fmt(r.pot)}</span> ${boardStr}</div><div class="muted">End of the hand. Tap the trail above to go back.</div></div>`;
    return;
  }
  const col = colors(r.actionTypes), name = r.preflop ? r.name : built.names[r.player];
  v.innerHTML = `<div class="node">
    <div class="nodetitle"><h2>${esc(name)}</h2>${r.preflop ? `<span class="muted">${esc(r.sub)}</span>` : ''}<span class="muted mono">pot ${fmt(r.pot)}</span> ${boardStr}${r.preflop ? '<span class="grow"></span><span class="muted" style="font-size:12px">Preflop: Slick RNG starting ranges (not solved yet)</span>' : ''}</div>
    <div class="actions">${r.actions.map((a, i) => `<button class="act${r.cur && r.preflop && ctxKind(r, i) ? ' cur' : ''}" data-a="${i}"><i style="background:${col[i]}"></i><strong>${esc(a)}</strong><small>${(r.overall[i] * 100).toFixed(1)}%</small></button>`).join('')}</div>
    <div class="freqbar">${r.overall.map((f, i) => `<i style="width:${f * 100}%;background:${col[i]}"></i>`).join('')}</div>
    <div class="split"><div class="gridwrap"><div class="hgrid" id="hgrid"></div></div><div class="detail" id="detail"></div></div>
  </div>`;
  v.querySelectorAll('.act').forEach((b) => (b.onclick = () => { if (r.preflop) { r.pick(+b.dataset.a); return; } path.push({ a: +b.dataset.a }); selected = null; requestReport(); }));
  const g = $('hgrid');
  for (let i = 0; i < 13; i++) for (let j = 0; j < 13; j++) {
    const k = classAt(i, j), e = r.classes[k], cell = document.createElement('button');
    cell.className = 'cell' + (k === selected ? ' sel' : ''); cell.setAttribute('aria-label', k);
    const frac = e ? Math.min(1, e.w / CLASS_N(k)) : 0;
    if (e && e.w > 1e-9) cell.innerHTML = `<div class="mix">${e.s.map((f, a) => `<i style="width:${f * 100}%;background:${col[a]}"></i>`).join('')}</div><div class="fill" style="height:${(1 - frac) * 100}%"></div><span class="lab">${k}</span>`;
    else { cell.classList.add('none'); cell.innerHTML = `<span class="lab">${k}</span>`; }
    cell.onmouseenter = () => detail(k); cell.onfocus = () => detail(k);
    cell.onclick = () => { selected = selected === k ? null : k; g.querySelectorAll('.cell').forEach((c) => c.classList.toggle('sel', c.getAttribute('aria-label') === selected)); detail(k); };
    g.appendChild(cell);
  }
  g.onmouseleave = () => detail(selected);
  detail(selected);
}
function ctxKind(r, i) { return r.cur && r.actions[i] && r.actionTypes && r.curIndex === i; }
function detail(k) {
  const r = view === 'pre' ? preReport : report, d = $('detail'); if (!d || !r || r.kind !== 'action') return;
  const col = colors(r.actionTypes);
  if (!k || !r.classes[k] || r.classes[k].w <= 1e-9) {
    d.innerHTML = `<div class="label">${esc(r.preflop ? r.name : built.names[r.player])}'s range here</div><div class="mono" style="font-size:22px">${r.totalReach.toFixed(1)} combos</div><div class="muted" style="font-size:12px">Hover or tap a hand for every combo.</div>`;
    return;
  }
  const e = r.classes[k];
  d.innerHTML = `<div class="row"><div class="grow"><div class="label">Hand</div><div class="mono" style="font-size:22px">${k}</div></div><span class="mono muted">${e.w.toFixed(2)} combos</span></div>
    <div class="freqbar">${e.s.map((f, a) => `<i style="width:${f * 100}%;background:${col[a]}"></i>`).join('')}</div>
    <div style="overflow-x:auto"><table><thead><tr><th>Combo</th>${r.actions.map((a, i) => `<th style="color:${col[i]}">${esc(a.replace('All-in', 'AI'))}</th>`).join('')}</tr></thead>
    <tbody><tr><td><b>All</b></td>${e.s.map((f) => `<td><b>${(f * 100).toFixed(0)}%</b></td>`).join('')}</tr>
    ${e.combos.filter((c) => c.reach > 1e-9).map((c) => `<tr><td>${c.name}</td>${c.s.map((f) => `<td>${(f * 100).toFixed(0)}%</td>`).join('')}</tr>`).join('')}</tbody></table></div>`;
}

