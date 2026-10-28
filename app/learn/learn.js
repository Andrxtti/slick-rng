// ---------------------------------------------------------------- Learn: how the solver works + coaching
(() => {
  const L = document.getElementById('learn'); if (!L) return;
  const secs = [...L.querySelectorAll('.lrn-main article')], navs = [...L.querySelectorAll('.lrn-nav [data-sec]')];
  const show = (sec) => {
    if (!secs.some((a) => a.dataset.sec === sec)) sec = 'start';
    secs.forEach((a) => (a.hidden = a.dataset.sec !== sec));
    navs.forEach((b) => b.setAttribute('aria-current', b.dataset.sec === sec ? 'true' : 'false'));
    L.querySelector('.lrn-main').scrollTop = 0;
    const cur = navs.find((b) => b.dataset.sec === sec); if (cur && cur.scrollIntoView) cur.scrollIntoView({ block: 'nearest', inline: 'nearest' });
  };
  const open = (sec) => {
    L.hidden = false; document.body.classList.add('learning'); show(sec || 'start');
    try { history.replaceState(null, '', '#learn/' + (sec || 'start')); } catch (e) {}
    const b = document.getElementById('modeLearn'); if (b) b.setAttribute('aria-pressed', 'true');
  };
  const close = () => {
    L.hidden = true; document.body.classList.remove('learning');
    try { history.replaceState(null, '', location.pathname + location.search); } catch (e) {}
    const b = document.getElementById('modeLearn'); if (b) b.setAttribute('aria-pressed', 'false');
  };
  window.openLearn = open;
  navs.forEach((b) => (b.onclick = () => { show(b.dataset.sec); try { history.replaceState(null, '', '#learn/' + b.dataset.sec); } catch (e) {} }));
  L.addEventListener('click', (e) => { const a = e.target.closest('[data-go]'); if (a) { e.preventDefault(); show(a.dataset.go); } });
  document.getElementById('learnClose').onclick = close;
  document.addEventListener('keydown', (e) => { if (e.key === 'Escape' && !L.hidden) close(); });
  document.addEventListener('click', (e) => { const a = e.target.closest('[data-learn]'); if (a) { e.preventDefault(); open(a.dataset.learn); } });
  const m = /^#learn(?:\/(\w+))?$/.exec(location.hash); if (m) open(m[1]);
})();
