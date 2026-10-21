// ================================================================ THE DREAM: your player, drawn
// A vector bust at the table: face and hair from the creator; outfit, eyewear, headwear, chain,
// watch, sponsor patch, bracelets and rings from what you've earned; chips from your bankroll.

const DR_SKIN = ['#ffe0c2', '#f2c6a0', '#e2a676', '#c98852', '#a96a3c', '#87502c', '#663a1f', '#452514'];
const DR_HAIRC = ['#17130f', '#3e2717', '#6e4526', '#a8743a', '#e6c46e', '#b8432a', '#8e8e8e', '#ececec'];
const DR_HAIR = [['wavy', 'Wavy'], ['short', 'Short'], ['fade', 'Fade'], ['slick', 'Slicked back'], ['curly', 'Curly'], ['long', 'Long'], ['buzz', 'Buzz'], ['bun', 'Man bun'], ['bald', 'Bald']];
const DR_FACIAL = [['none', 'Clean'], ['stubble', 'Stubble'], ['mustache', 'Mustache'], ['goatee', 'Goatee'], ['beard', 'Full beard']];
const DR_LOOK_DEFAULT = { skin: 2, hair: 'wavy', hairC: 0, facial: 'none' };

function drShade(hex, f) {
  const n = parseInt(hex.slice(1), 16), c = [(n >> 16) & 255, (n >> 8) & 255, n & 255];
  return '#' + c.map((v) => Math.round(f < 0 ? v * (1 + f) : v + (255 - v) * f).toString(16).padStart(2, '0')).join('');
}

function drOutfitSVG(id, patch) {
  const torso = (fill, stroke) => `<path d="M14 222 C18 178 46 160 76 154 L124 154 C154 160 182 178 186 222 Z" fill="${fill}" stroke="${stroke || drShade(fill, -0.35)}" stroke-width="1.5"/>`;
  const fold = (c) => `<path d="M52 176 C60 190 62 206 60 222 M148 176 C140 190 138 206 140 222" stroke="${c}" stroke-width="2" fill="none" opacity=".55"/>`;
  let s = '';
  if (id === 'office') {
    s = torso('#cdd8e6', '#93a3b8') + fold('#a9b8cc')
      + `<path d="M84 154 L100 172 L116 154 L111 150 L100 162 L89 150 Z" fill="#f4f7fb" stroke="#93a3b8"/>`
      + `<path d="M97 164 L103 164 L106 200 L100 210 L94 200 Z" fill="#7a1f2b" transform="rotate(8 100 180)"/>`
      + `<path d="M82 156 L96 196 M118 156 L104 196" stroke="#2457b8" stroke-width="3" fill="none"/><rect x="92" y="194" width="16" height="20" rx="2" fill="#f4f7fb" stroke="#2457b8"/><rect x="95" y="198" width="10" height="3" fill="#2457b8"/>`;
  } else if (id === 'hoodie') {
    s = `<path d="M66 160 C70 140 130 140 134 160 C120 152 80 152 66 160 Z" fill="#555960"/>` + torso('#6b6f76') + fold('#5a5d63')
      + `<path d="M74 158 C86 172 114 172 126 158" stroke="#4d5056" stroke-width="5" fill="none"/>`
      + `<path d="M92 166 L90 196 M108 166 L110 196" stroke="#e9e5dc" stroke-width="2.2" stroke-linecap="round"/><circle cx="90" cy="197" r="2" fill="#bbb"/><circle cx="110" cy="197" r="2" fill="#bbb"/>`;
  } else if (id === 'polo') {
    s = torso('#1f2a44') + fold('#2c3a5c')
      + `<path d="M82 152 L100 168 L118 152 L120 162 L100 178 L80 162 Z" fill="#26345a" stroke="#141c30"/>`
      + `<path d="M100 168 L100 222" stroke="#c9ccd4" stroke-width="2"/><rect x="97" y="176" width="6" height="8" rx="1" fill="#c9ccd4"/>`;
  } else if (id === 'bomber') {
    s = torso('#1b1b1e', '#000') + fold('#2c2c31')
      + `<path d="M78 152 C88 160 112 160 122 152 L124 160 C112 168 88 168 76 160 Z" fill="#2a2a2e"/><path d="M78 155 L122 155" stroke="#3d3d42" stroke-dasharray="2 2"/>`
      + `<path d="M100 162 L100 222" stroke="#5c5c64" stroke-width="2.5"/>`
      + `<text x="128" y="186" font-family="Georgia,serif" font-style="italic" font-size="7" fill="#d9b36c">Champions</text>`;
  } else if (id === 'blazer') {
    s = torso('#3b3f47') + `<path d="M86 152 L100 168 L114 152 L114 222 L86 222 Z" fill="#ece8df"/>`
      + `<path d="M84 152 L98 200 L86 222 L60 222 C64 196 70 172 84 152 Z M116 152 L102 200 L114 222 L140 222 C136 196 130 172 116 152 Z" fill="#30333a" stroke="#1d1f24"/>` + fold('#2a2d33');
  } else if (id === 'suit' || id === 'designer' || id === 'tux') {
    const c = id === 'suit' ? '#1b2442' : id === 'designer' ? '#4a1426' : '#121214', lap = id === 'tux' ? '#26262a' : drShade(c, -0.25), shirt = id === 'designer' ? '#141416' : '#f7f6f2';
    s = torso(c) + `<path d="M86 152 L100 168 L114 152 L114 222 L86 222 Z" fill="${shirt}"/>`
      + (id === 'suit' ? `<path d="M97 166 L103 166 L105 204 L100 212 L95 204 Z" fill="#8a1c24"/>` : id === 'tux' ? `<path d="M90 162 L100 166 L110 162 L110 172 L100 168 L90 172 Z" fill="#000"/><circle cx="100" cy="167" r="2" fill="#222"/>` : `<path d="M90 152 L100 172 L110 152" stroke="#2a2a2e" stroke-width="2" fill="none"/>`)
      + `<path d="M84 152 L98 204 L88 222 L58 222 C62 196 68 172 84 152 Z M116 152 L102 204 L112 222 L142 222 C138 196 132 172 116 152 Z" fill="${lap}" stroke="${drShade(c, -0.5)}"/>` + fold(drShade(c, 0.12))
      + (id !== 'tux' ? `<path d="M64 196 l10 -3 l0 5 Z" fill="#f7f6f2" opacity=".85"/>` : '');
  }
  const p = DR_PATCH[patch];
  if (p) s += `<g transform="translate(44 172) rotate(6)"><rect width="30" height="13" rx="2" fill="#0c0c0c" stroke="${p.c}" stroke-width="1.4"/><text x="15" y="9.6" text-anchor="middle" font-family="Arial Black,Arial,sans-serif" font-weight="900" font-size="6.6" fill="${p.c}">${p.t}</text></g>`;
  return s;
}

function drHairSVG(style, c, back) {
  const d = drShade(c, -0.3), l = drShade(c, 0.22);
  // back layer: hair that sits behind the head
  if (back) {
    if (style === 'long') return `<path d="M64 92 C60 140 70 156 84 160 L116 160 C130 156 140 140 136 92 Z" fill="${d}"/>`;
    if (style === 'curly') return `<path d="M62 96 C56 122 66 136 74 140 L126 140 C134 136 144 122 138 96 Z" fill="${d}"/>`;
    return '';
  }
  switch (style) {
    case 'wavy': return `<path d="M66 100 C60 66 82 50 104 52 C126 52 144 66 136 100 C134 88 130 80 124 76 C120 86 108 92 96 90 C90 96 80 98 74 92 C70 96 68 98 66 100 Z" fill="${c}"/>
      <path d="M78 66 C88 60 96 62 100 70 M104 60 C116 58 126 64 128 74 M86 80 C94 74 104 78 106 86" stroke="${l}" stroke-width="2.4" fill="none" stroke-linecap="round" opacity=".7"/>
      <path d="M96 88 C104 96 116 94 122 84" stroke="${d}" stroke-width="2" fill="none"/>`;
    case 'short': return `<path d="M68 96 C64 68 82 56 100 56 C120 56 138 68 132 96 C130 84 124 76 116 74 C106 80 90 80 80 76 C74 80 70 86 68 96 Z" fill="${c}"/>`;
    case 'fade': return `<path d="M70 92 C68 68 84 58 100 58 C118 58 134 68 130 92 C128 82 122 76 114 74 L86 74 C78 76 72 82 70 92 Z" fill="${c}"/><path d="M68 94 L68 112 M132 94 L132 112" stroke="${c}" stroke-width="3" opacity=".35"/>`;
    case 'slick': return `<path d="M68 98 C64 66 84 54 102 54 C122 54 138 66 132 98 C130 86 126 78 120 74 C108 70 92 70 80 74 C74 80 70 88 68 98 Z" fill="${c}"/><path d="M80 66 C94 60 110 60 126 70 M78 74 C94 66 112 66 128 78" stroke="${l}" stroke-width="2" fill="none" opacity=".75"/>`;
    case 'curly': return `<g fill="${c}">${[[72, 74], [82, 62], [96, 56], [110, 58], [124, 66], [132, 80], [66, 88], [134, 94], [64, 102], [88, 70], [104, 68], [118, 74]].map(([x, y]) => `<circle cx="${x}" cy="${y}" r="11"/>`).join('')}</g>`;
    case 'long': return `<path d="M66 104 C60 66 82 52 102 52 C124 52 142 66 134 104 C132 90 128 80 120 74 C106 82 90 82 80 76 C72 84 68 92 66 104 Z" fill="${c}"/>`;
    case 'buzz': return `<path d="M70 94 C68 70 84 60 100 60 C118 60 134 70 130 94 C128 84 122 78 116 76 L84 76 C78 78 72 84 70 94 Z" fill="${c}" opacity=".85"/>`;
    case 'bun': return `<circle cx="100" cy="46" r="11" fill="${c}"/><path d="M68 96 C64 68 82 56 100 56 C120 56 138 68 132 96 C128 80 116 72 100 72 C84 72 72 80 68 96 Z" fill="${c}"/>`;
    default: return '';
  }
}

function drFacialSVG(style, c) {
  if (style === 'stubble') return `<path d="M76 116 C80 138 92 146 100 146 C108 146 120 138 124 116 C118 130 110 136 100 136 C90 136 82 130 76 116 Z" fill="${c}" opacity=".28"/>`;
  if (style === 'mustache') return `<path d="M90 126 C94 122 106 122 110 126 C106 128 94 128 90 126 Z" fill="${c}"/>`;
  if (style === 'goatee') return `<path d="M90 126 C94 122 106 122 110 126 C106 128 94 128 90 126 Z M94 134 C96 142 104 142 106 134 C104 138 96 138 94 134 Z" fill="${c}"/>`;
  if (style === 'beard') return `<path d="M74 110 C76 140 90 150 100 150 C110 150 124 140 126 110 C122 124 114 132 100 132 C86 132 78 124 74 110 Z M90 126 C94 122 106 122 110 126 C106 128 94 128 90 126 Z" fill="${c}"/>`;
  return '';
}

function drEyewearSVG(id) {
  if (id === 'readers') return `<g fill="none" stroke="#2b2b2b" stroke-width="1.6"><rect x="78" y="98" width="18" height="12" rx="4"/><rect x="104" y="98" width="18" height="12" rx="4"/><path d="M96 103 L104 103 M78 102 L70 100 M122 102 L130 100"/></g>`;
  if (id === 'tinted') return `<g><rect x="77" y="97" width="20" height="14" rx="5" fill="#c9a27a" opacity=".55" stroke="#c9b9a0" stroke-width="1.4"/><rect x="103" y="97" width="20" height="14" rx="5" fill="#c9a27a" opacity=".55" stroke="#c9b9a0" stroke-width="1.4"/><path d="M97 103 L103 103" stroke="#c9b9a0" stroke-width="1.4"/></g>`;
  if (id === 'aviator') return `<defs><linearGradient id="avl" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#2a2a2a"/><stop offset="1" stop-color="#6e5a3a"/></linearGradient></defs><g stroke="#d9b36c" stroke-width="1.6"><path d="M76 98 L97 98 L95 110 C92 116 80 116 78 110 Z" fill="url(#avl)"/><path d="M103 98 L124 98 L122 110 C120 116 108 116 105 110 Z" fill="url(#avl)"/><path d="M97 99 L103 99" fill="none"/></g>`;
  if (id === 'shades') {
    // the deal-with-it 8-bit shades, built from pixels
    const P = 3, rows = ['KKKKKKKKKKKKKKKK', '.KwwKKK..KwwKKK.', '..KwwKK...KwwKK.', '...KKK.....KKK..'];
    let r = '';
    rows.forEach((row, y) => [...row].forEach((ch, x) => { if (ch !== '.') r += `<rect x="${76 + x * P}" y="${97 + y * P}" width="${P}" height="${P}" fill="${ch === 'K' ? '#050505' : '#fff'}"/>`; }));
    return `<g shape-rendering="crispEdges">${r}</g>`;
  }
  return '';
}

function drHeadwearSVG(id, hairStyle) {
  if (id === 'cap') return `<path d="M66 86 C66 58 86 50 100 50 C116 50 134 58 134 86 Z" fill="#1d3b2c"/><path d="M64 86 C80 80 120 80 136 86 C136 92 64 92 64 86 Z" fill="#14281e"/><text x="100" y="76" text-anchor="middle" font-family="Arial Black,Arial,sans-serif" font-size="9" fill="#d9b36c">R</text>`;
  if (id === 'beanie') return `<path d="M66 88 C64 58 84 48 100 48 C118 48 136 58 134 88 Z" fill="#b8432a"/><rect x="64" y="80" width="72" height="12" rx="5" fill="#8a2f1d"/><circle cx="100" cy="46" r="7" fill="#e9e5dc"/>`;
  return '';
}

function drAvatarSVG(st, opt = {}) {
  const look = { ...DR_LOOK_DEFAULT, ...(st.look || {}) }, eq = st.eq || {};
  const skin = DR_SKIN[look.skin] || DR_SKIN[2], sk2 = drShade(skin, -0.16), hc = DR_HAIRC[look.hairC] || DR_HAIRC[0];
  const titles = st.titles || {}, br = st.br || 0, id = 'a' + Math.random().toString(36).slice(2, 7);
  const face = opt.face;
  const bracelets = Math.min(4, titles.bracelet || 0), rings = Math.min(4, (titles.ring || 0) + (titles.me || 0));
  let s = `<svg class="dravsvg" viewBox="${face ? '50 46 100 100' : '0 30 200 192'}" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="${esc(st.name || 'Your player')}">`;
  s += drHairSVG(look.hair, hc, true);
  // neck and outfit
  s += `<path d="M88 126 L112 126 L114 158 C106 164 94 164 86 158 Z" fill="${sk2}"/>`;
  s += drOutfitSVG(eq.outfit || 'office', eq.patch);
  s += `<path d="M14 222 C18 178 46 160 76 154 L124 154 C154 160 182 178 186 222 Z" fill="url(#${id}t)" pointer-events="none"/>`;
  if (eq.head === 'phones') s += `<path d="M72 160 C70 144 130 144 128 160" stroke="#222" stroke-width="5" fill="none"/><rect x="64" y="150" width="14" height="18" rx="6" fill="#2b2b2e"/><rect x="122" y="150" width="14" height="18" rx="6" fill="#2b2b2e"/>`;
  if (eq.chain && eq.chain !== 'none') {
    const c = eq.chain === 'silver' ? '#c9ccd4' : '#e3b94f', w = eq.chain === 'silver' ? 1.6 : 3.4;
    s += `<path d="M84 156 C88 176 112 176 116 156" stroke="${c}" stroke-width="${w}" fill="none" ${eq.chain !== 'silver' ? 'stroke-dasharray="3 1.4"' : ''}/>`;
    if (eq.chain === 'iced') s += [[90, 168], [100, 172], [110, 168], [95, 171], [105, 171]].map(([x, y]) => `<circle cx="${x}" cy="${y}" r="1.2" fill="#fff"/>`).join('');
  }
  // head
  s += `<ellipse cx="70" cy="106" rx="6" ry="9" fill="${sk2}"/><ellipse cx="130" cy="106" rx="6" ry="9" fill="${sk2}"/>`;
  s += `<defs><radialGradient id="${id}f" cx="42%" cy="38%" r="70%"><stop offset="0" stop-color="${drShade(skin, 0.12)}"/><stop offset=".7" stop-color="${skin}"/><stop offset="1" stop-color="${sk2}"/></radialGradient>
    <linearGradient id="${id}t" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#fff" stop-opacity=".16"/><stop offset=".5" stop-color="#fff" stop-opacity="0"/></linearGradient></defs>`;
  s += `<path d="M70 96 C70 66 84 58 100 58 C116 58 130 66 130 96 C130 124 118 146 100 146 C82 146 70 124 70 96 Z" fill="url(#${id}f)"/>`;
  s += `<ellipse cx="82" cy="117" rx="6" ry="3.4" fill="#e8705a" opacity=".12"/><ellipse cx="118" cy="117" rx="6" ry="3.4" fill="#e8705a" opacity=".12"/>`;
  s += `<path d="M76 120 C82 138 92 145 100 145 C108 145 118 138 124 120 C118 134 110 140 100 140 C90 140 82 134 76 120 Z" fill="${sk2}" opacity=".6"/>`;
  // eyes, brows, nose, mouth
  const blink = opt.blink ? ' class="drblink"' : '';
  s += `<g${blink}><ellipse cx="87" cy="104" rx="5.5" ry="3.6" fill="#fff"/><ellipse cx="113" cy="104" rx="5.5" ry="3.6" fill="#fff"/><circle cx="88" cy="104" r="2.6" fill="#2a1d14"/><circle cx="114" cy="104" r="2.6" fill="#2a1d14"/><circle cx="89" cy="103" r=".8" fill="#fff"/><circle cx="115" cy="103" r=".8" fill="#fff"/></g>`;
  s += `<path d="M79 95 C84 92 90 92 95 94 M105 94 C110 92 116 92 121 95" stroke="${drShade(hc, 0.05)}" stroke-width="2.6" fill="none" stroke-linecap="round"/>`;
  s += `<path d="M100 106 C99 112 97 116 96 118 C99 120 103 120 104 118" stroke="${sk2}" stroke-width="1.8" fill="none" stroke-linecap="round"/>`;
  const mouth = opt.mouth || (opt.smile ? 'smile' : 'flat');
  s += `<path d="${mouth === 'smile' ? 'M90 127 C96 133 104 133 110 127' : mouth === 'smug' ? 'M91 129 C98 130 104 128 110 124' : 'M91 128 C96 130 104 130 109 127'}" stroke="#8a4a3a" stroke-width="2.2" fill="none" stroke-linecap="round"/>`;
  s += drFacialSVG(look.facial, hc);
  s += drHairSVG(look.hair, hc, false);
  s += drEyewearSVG(eq.eyes);
  s += drHeadwearSVG(eq.head, look.hair);
  if (!face) {
    // the table, your arms, cards, watch, bracelets, rings, chips
    s += `<path d="M0 194 L200 194 L200 222 L0 222 Z" fill="#0e4d39"/><path d="M0 188 C60 182 140 182 200 188 L200 197 C140 191 60 191 0 197 Z" fill="#3a2414"/><path d="M0 189 C60 183 140 183 200 189" stroke="#5a3a20" stroke-width="1.2" fill="none"/>`;
    const sleeve = { office: '#cdd8e6', hoodie: '#6b6f76', polo: '#1f2a44', bomber: '#1b1b1e', blazer: '#3b3f47', suit: '#1b2442', designer: '#4a1426', tux: '#121214' }[eq.outfit || 'office'];
    s += `<path d="M10 222 C20 204 46 192 74 190 L80 202 C58 206 40 212 30 222 Z" fill="${sleeve}" stroke="${drShade(sleeve, -0.35)}"/>`;
    s += `<path d="M190 222 C180 204 154 192 126 190 L120 202 C142 206 160 212 170 222 Z" fill="${sleeve}" stroke="${drShade(sleeve, -0.35)}"/>`;
    s += `<g><rect x="86" y="196" width="15" height="21" rx="2" fill="#f7f3e8" stroke="#bbb" transform="rotate(-8 93 206)"/><rect x="98" y="196" width="15" height="21" rx="2" fill="#f7f3e8" stroke="#bbb" transform="rotate(7 105 206)"/><path d="M92 200 l3 4 l-3 4 l-3 -4 Z" fill="#c2362a" transform="rotate(-8 93 206)"/></g>`;
    s += `<ellipse cx="86" cy="200" rx="13" ry="8" fill="${skin}" stroke="${sk2}"/><ellipse cx="114" cy="200" rx="13" ry="8" fill="${skin}" stroke="${sk2}"/>`;
    s += `<path d="M92 196 L92 204 M97 195 L97 204 M103 195 L103 204 M108 196 L108 204" stroke="${sk2}" stroke-width="1"/>`;
    // watch (left wrist)
    const wt = eq.watch || 'digital', band = { digital: '#222', steel: '#b9bec7', gold: '#d9b36c', iced: '#e9edf2', hyper: '#1a1a1a' }[wt], face2 = { digital: '#3f5f3a', steel: '#1d3b6e', gold: '#f3e2b0', iced: '#f8fbff', hyper: '#2c2c2c' }[wt];
    s += `<g transform="rotate(-18 72 198)"><rect x="66" y="190" width="10" height="17" rx="2" fill="${band}"/><circle cx="71" cy="198.5" r="5.6" fill="${face2}" stroke="${band}" stroke-width="2"/>${wt === 'iced' ? '<circle cx="71" cy="198.5" r="6.6" fill="none" stroke="#fff" stroke-width="1" stroke-dasharray="1 1.2"/>' : ''}${wt === 'hyper' ? '<circle cx="71" cy="198.5" r="2.4" fill="none" stroke="#d9b36c" stroke-width="1"/>' : ''}</g>`;
    // bracelets (right wrist), one band per title, plus the count
    for (let k = 0; k < bracelets; k++) s += `<rect x="${124 + k * 3.4}" y="${190 - k * 0.6}" width="2.6" height="16" rx="1.2" fill="#e8c35a" stroke="#9a7a2a" stroke-width=".5" transform="rotate(18 128 198)"/>`;
    // rings on fingers
    for (let k = 0; k < rings; k++) s += `<rect x="${91 + k * 5.4}" y="196" width="3.4" height="2.4" rx=".8" fill="#e8c35a" stroke="#fff" stroke-width=".4"/>`;
    // chips: the stack grows with your bankroll
    const tiers = [0, 1000, 10000, 50000, 250000, 1e6, 5e6], lv = tiers.filter((t) => br >= t).length;
    const cols = ['#f3ecdc', '#c2362a', '#2a6fdb', '#1e8f4a', '#141414', '#7a3fc0', '#e3b94f'];
    for (let c = 0; c < Math.min(3, Math.ceil(lv / 2)); c++) {
      const h = Math.max(2, lv * 2 - c * 3);
      for (let k = 0; k < h; k++) s += `<rect x="${150 + c * 14}" y="${210 - k * 3}" width="12" height="3" rx="1" fill="${cols[(k + c) % Math.min(cols.length, lv + 1)]}" stroke="#000" stroke-width=".4"/>`;
    }
  }
  return s + '</svg>';
}
