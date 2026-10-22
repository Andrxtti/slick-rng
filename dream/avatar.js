// ================================================================ THE DREAM: your player, drawn
// A stylized vector bust at the table. The creator sets the body (skin, face, age, hair, eyes, brows,
// facial hair, freckles); gear sets the look (outfit, headwear, eyewear, chains, earrings, watch,
// bracelets, rings, chest and sleeve sponsor patches), and most clothes and hats can be re-dyed.
// Title bracelets and rings show up on your wrists and fingers as you win them.

const DR_SKIN = ['#ffe3c8', '#f6cfab', '#e9b48a', '#d49a6a', '#b97c4c', '#9a6038', '#784626', '#55301a', '#3c2010'];
const DR_HAIRC = ['#141010', '#3a2416', '#64402a', '#9a6a3a', '#d6b06a', '#f1e2b6', '#b4472a', '#8e8e8e', '#efefef', '#3a6fd8', '#e86fae', '#d4433a', '#2ab3a6', '#8d4fd6'];
const DR_EYEC = ['#4a2f1c', '#2c1d12', '#7a5a2a', '#3f6e8c', '#4f7a48', '#6e7680', '#5b8fb9'];
const DR_HAIR = [['short', 'Textured crop'], ['fade', 'High fade'], ['waves', '360 waves'], ['buzz', 'Buzz'], ['quiff', 'Quiff'], ['slick', 'Slicked back'], ['curly', 'Curly top'], ['afro', 'Afro'], ['twists', 'Twists'], ['cornrows', 'Cornrows'], ['locs', 'Locs'], ['mohawk', 'Mohawk'], ['wavy', 'Shaggy'], ['curtains', 'Curtains'], ['mullet', 'Modern mullet'], ['long', 'Long'], ['ponytail', 'Ponytail'], ['bun', 'Man bun'], ['bald', 'Bald']];
const DR_FACIAL = [['none', 'Clean'], ['stubble', 'Stubble'], ['mustache', 'Thin mustache'], ['goatee', 'Goatee'], ['chin', 'Chin strap'], ['beard', 'Short beard']];
const DR_BROWS = [['natural', 'Natural'], ['thick', 'Thick'], ['slit', 'Slit'], ['arched', 'Arched']];
const DR_FACES = [['oval', 'Oval'], ['round', 'Round'], ['angular', 'Sharp jaw']];
const DR_AGES = [['young', '20s'], ['adult', '30s'], ['old', '50s+']];
const DR_LOOK_DEFAULT = { skin: 2, hair: 'short', hairC: 1, facial: 'none', eyeC: 0, brows: 'natural', face: 'oval', age: 'young', freck: false };
// clothes and hats you can re-dye once you own them (index 0 = the item's own colour)
const DR_DYES = [null, '#17171a', '#efeee9', '#24427e', '#b3262c', '#1e6b45', '#6b3fa0', '#d98a2b', '#8a8f98', '#e8a6c2', '#c9a03e', '#3cbcd0', '#5a3a22'];

function drShade(hex, f) {
  const n = parseInt(hex.slice(1), 16), c = [(n >> 16) & 255, (n >> 8) & 255, n & 255];
  return '#' + c.map((v) => Math.round(f < 0 ? v * (1 + f) : v + (255 - v) * f).toString(16).padStart(2, '0')).join('');
}

// ---------------------------------------------------------------- outfits
const DR_OUTFIT_BASE = { office: '#cdd8e6', hoodie: '#5f636b', tee: '#1c1c20', varsity: '#1f3566', bomber: '#2a2d22', track: '#16161a', cardigan: '#6a2232', vest: '#c9b98a', denim: '#4a6fa0', leather: '#1a1716', fleece: '#7d8590', jersey: '#b3262c', polo: '#1f2a44', blazer: '#3b3f47', suit: '#1b2442', designer: '#4a1426', tux: '#121214', vampire: '#141216', flannel: '#a3262a', ugly: '#1d6b3a' };
const DR_DYEABLE = { hoodie: 1, tee: 1, varsity: 1, bomber: 1, track: 1, cardigan: 1, vest: 1, fleece: 1, jersey: 1, polo: 1, cap: 1, snap: 1, beanie: 1, bucket: 1 };
function drOutfitSVG(id, c) {
  const torso = (fill, stroke) => `<path d="M12 222 C16 180 44 162 74 156 L126 156 C156 162 184 180 188 222 Z" fill="${fill}" stroke="${stroke || drShade(fill, -0.38)}" stroke-width="1.4"/>`;
  const folds = (col, o = 0.5) => `<path d="M48 178 C56 192 58 208 56 222 M152 178 C144 192 142 208 144 222 M78 196 C84 204 86 214 86 222" stroke="${col}" stroke-width="1.8" fill="none" opacity="${o}"/>`;
  const tee = (col) => `<path d="M86 155 C90 163 110 163 114 155 L114 222 L86 222 Z" fill="${col}"/><path d="M86 155 C90 163 110 163 114 155" stroke="${drShade(col, -0.25)}" stroke-width="2.2" fill="none"/>`;
  const dk = drShade(c, -0.3), lt = drShade(c, 0.18);
  let s = '';
  switch (id) {
    case 'office':
      s = torso(c, '#93a3b8') + folds('#a9b8cc') + `<path d="M84 154 L100 172 L116 154 L111 150 L100 162 L89 150 Z" fill="#f4f7fb" stroke="#93a3b8"/>`
        + `<path d="M97 164 L103 164 L106 200 L100 210 L94 200 Z" fill="#7a1f2b" transform="rotate(6 100 182)"/>`
        + `<path d="M82 156 L96 196 M118 156 L104 196" stroke="#2457b8" stroke-width="3" fill="none"/><rect x="92" y="194" width="16" height="20" rx="2" fill="#f4f7fb" stroke="#2457b8"/><rect x="95" y="198" width="10" height="3" fill="#2457b8"/>`; break;
    case 'hoodie':
      s = `<path d="M64 166 C62 146 80 138 100 138 C120 138 138 146 136 166 C126 156 74 156 64 166 Z" fill="${dk}"/>` + torso(c) + folds(dk)
        + `<path d="M72 160 C84 176 116 176 128 160" stroke="${dk}" stroke-width="6" fill="none"/><path d="M76 160 C86 172 114 172 124 160" stroke="${lt}" stroke-width="1.2" fill="none" opacity=".6"/>`
        + `<path d="M92 170 C91 182 90 190 89 198 M108 170 C109 182 110 190 111 198" stroke="#ece8de" stroke-width="2.2" stroke-linecap="round"/><rect x="87.6" y="197" width="3" height="5" rx="1" fill="#d6d1c4"/><rect x="109.6" y="197" width="3" height="5" rx="1" fill="#d6d1c4"/>`
        + `<path d="M64 212 C80 206 120 206 136 212" stroke="${dk}" stroke-width="2" fill="none"/>`; break;
    case 'tee':
      s = torso(c) + folds(lt, 0.25) + `<path d="M84 155 C88 166 112 166 116 155" stroke="${drShade(c, 0.12)}" stroke-width="3.4" fill="none"/>`
        + `<g transform="translate(100 196)"><circle r="13" fill="none" stroke="${drShade(c, 0.75)}" stroke-width="1.6"/><path d="M0 -8 C3 -4 9 -1 7 4 C6 7 2 7 0.8 4.6 L2.4 9 L-2.4 9 L-0.8 4.6 C-2 7 -6 7 -7 4 C-9 -1 -3 -4 0 -8 Z" fill="${drShade(c, 0.75)}"/></g>`; break;
    case 'varsity':
      s = torso(c) + folds(dk) + `<path d="M62 170 C60 186 60 200 62 222 M138 170 C140 186 140 200 138 222" stroke="#efe6cf" stroke-width="10" fill="none" opacity=".95"/>`
        + tee('#efeee9') + `<path d="M80 154 C88 168 112 168 120 154" stroke="#efe6cf" stroke-width="6" fill="none"/><path d="M80 154 C88 168 112 168 120 154" stroke="${c}" stroke-width="2" fill="none"/>`
        + `<path d="M100 166 V222" stroke="${dk}" stroke-width="1.5"/>` + [176, 190, 204, 218].map((y) => `<circle cx="104" cy="${y}" r="1.8" fill="#efe6cf"/>`).join('')
        + `<g transform="translate(76 186)"><rect x="-9" y="-11" width="18" height="22" rx="3" fill="#efe6cf"/><text x="0" y="6" text-anchor="middle" font-family="Georgia,serif" font-weight="900" font-size="17" fill="${c}">C</text></g>`; break;
    case 'bomber':
      s = torso(c, '#000') + folds(lt, 0.35) + `<path d="M78 152 C88 162 112 162 122 152 L124 160 C112 170 88 170 76 160 Z" fill="${dk}"/><path d="M78 155 L122 155" stroke="${lt}" stroke-dasharray="2 2"/>`
        + `<path d="M100 162 L100 222" stroke="#c9ccd4" stroke-width="2.2"/><path d="M96 162 L96 222" stroke="#d98a2b" stroke-width="2" opacity=".85"/><rect x="98.4" y="168" width="3.2" height="7" rx="1" fill="#c9ccd4"/>`; break;
    case 'track':
      s = torso(c) + folds(lt, 0.3) + `<path d="M86 150 L100 162 L114 150 L116 160 L100 170 L84 160 Z" fill="${dk}" stroke="${drShade(c, 0.4)}" stroke-width=".8"/><path d="M100 162 V222" stroke="#c9ccd4" stroke-width="2"/>`
        + `<path d="M40 172 C50 166 60 162 72 158 M44 178 C54 172 64 168 76 164 M160 172 C150 166 140 162 128 158 M156 178 C146 172 136 168 124 164" stroke="#f3f3f3" stroke-width="2.6" fill="none"/>`; break;
    case 'cardigan':
      s = torso(c) + `<path d="M84 152 L100 168 L116 152 L116 222 L84 222 Z" fill="#f1f0ea"/><path d="M84 152 L92 160 L100 166 L108 160 L116 152" stroke="#c9c6bb" fill="none"/>`
        + `<path d="M98 166 L102 166 L104 176 L100 180 L96 176 Z" fill="#2a4a8a"/>`
        + `<path d="M84 152 C90 170 92 196 92 222 L76 222 C76 200 78 176 84 152 Z M116 152 C110 170 108 196 108 222 L124 222 C124 200 122 176 116 152 Z" fill="${dk}" opacity=".55"/>` + folds(dk)
        + [180, 194, 208].map((y) => `<circle cx="91" cy="${y}" r="2" fill="${lt}" stroke="${dk}" stroke-width=".6"/>`).join('')
        + `<path d="M52 196 l9 -2 l-1 9 Z" fill="${dk}"/>`; break;
    case 'vest':
      s = torso('#eef0f4', '#b8bfca') + `<path d="M84 154 L100 172 L116 154 L111 150 L100 162 L89 150 Z" fill="#fbfcfe" stroke="#b8bfca"/>`
        + `<path d="M72 160 C78 160 84 164 100 196 C116 164 122 160 128 160 C136 180 136 204 132 222 L68 222 C64 204 64 180 72 160 Z" fill="${c}" stroke="${dk}" stroke-width="1.2"/>`
        + `<g opacity=".55">${[[84, 200], [100, 212], [116, 200], [92, 216], [108, 216]].map(([x, y]) => `<path d="M${x} ${y - 7} L${x + 6} ${y} L${x} ${y + 7} L${x - 6} ${y} Z" fill="${dk}"/>`).join('')}<path d="M70 184 L130 222 M130 184 L70 222" stroke="${lt}" stroke-width=".8"/></g>`
        + `<path d="M92 164 L100 168 L108 164 L108 172 L100 168 L92 172 Z" fill="#b3262c"/><circle cx="100" cy="168" r="1.8" fill="#7a1a1f"/>`; break;
    case 'denim':
      s = torso(c) + tee('#efeee9') + `<path d="M84 152 L98 176 L90 222 L62 222 C64 196 70 172 84 152 Z M116 152 L102 176 L110 222 L138 222 C136 196 130 172 116 152 Z" fill="${c}" stroke="${dk}"/>`
        + `<path d="M84 152 L94 168 L86 172 Z M116 152 L106 168 L114 172 Z" fill="${dk}"/>` + folds(dk)
        + `<path d="M66 190 H86 V200 H66 Z M114 190 H134 V200 H114 Z" fill="none" stroke="#e8c48a" stroke-width=".9" stroke-dasharray="1.6 1.4"/><circle cx="76" cy="195" r="1.4" fill="#c9a03e"/><circle cx="124" cy="195" r="1.4" fill="#c9a03e"/>`; break;
    case 'leather':
      s = torso(c, '#000') + tee('#2a2a2e') + `<path d="M84 152 L104 182 L96 222 L60 222 C62 196 68 172 84 152 Z M116 152 L108 172 L116 222 L140 222 C138 196 132 172 116 152 Z" fill="${c}" stroke="#000"/>`
        + `<path d="M86 156 L98 176 L88 180 Z M116 156 L110 170 L118 174 Z" fill="#2a2523"/><path d="M104 182 L96 222" stroke="#c9ccd4" stroke-width="1.6"/>` + folds('#3a3432', 0.7)
        + `<path d="M60 184 C68 180 76 180 82 184 M140 184 C132 180 124 180 118 184" stroke="#c9ccd4" stroke-width="1.2" fill="none"/><circle cx="70" cy="168" r="1.6" fill="#c9ccd4"/><circle cx="130" cy="168" r="1.6" fill="#c9ccd4"/>`; break;
    case 'fleece':
      s = torso('#2a2c31') + `<path d="M86 155 C90 163 110 163 114 155" stroke="#3a3c42" stroke-width="2.4" fill="none"/>`
        + `<path d="M70 160 C80 154 88 150 92 150 L100 168 L108 150 C112 150 120 154 130 160 C136 182 136 204 134 222 L66 222 C64 204 64 182 70 160 Z" fill="${c}" stroke="${dk}"/>`
        + `<path d="M92 150 L100 168 L108 150 L106 146 L100 156 L94 146 Z" fill="${dk}"/><path d="M100 168 V222" stroke="${dk}" stroke-width="2"/>` + folds(dk, 0.35)
        + `<path d="M74 176 H90 M110 176 H126" stroke="${dk}" stroke-width="1.4"/><rect x="72" y="182" width="12" height="7" rx="1.5" fill="#d9603a"/>`; break;
    case 'jersey':
      s = torso(c) + `<path d="M84 152 L100 172 L116 152" fill="#f3f3f3" stroke="#f3f3f3" stroke-width="3"/><path d="M84 152 L100 172 L116 152" fill="none" stroke="${dk}" stroke-width="1"/>`
        + `<path d="M58 166 C56 186 58 206 60 222 M142 166 C144 186 142 206 140 222" stroke="#f3f3f3" stroke-width="4" fill="none"/>`
        + `<text x="100" y="206" text-anchor="middle" font-family="Arial Black,Arial,sans-serif" font-weight="900" font-size="26" fill="#f3f3f3" stroke="${dk}" stroke-width="1.2">7</text>`; break;
    case 'polo':
      s = torso(c) + folds(lt, 0.3) + `<path d="M82 152 L100 168 L118 152 L120 162 L100 178 L80 162 Z" fill="${lt}" stroke="${dk}"/><path d="M100 168 L100 222" stroke="#c9ccd4" stroke-width="2"/><rect x="97" y="176" width="6" height="8" rx="1" fill="#c9ccd4"/>`; break;
    case 'blazer':
      s = torso(c) + tee('#17171a') + `<path d="M84 152 L98 200 L86 222 L60 222 C64 196 70 172 84 152 Z M116 152 L102 200 L114 222 L140 222 C136 196 130 172 116 152 Z" fill="${drShade(c, -0.12)}" stroke="${dk}"/>` + folds(dk); break;
    case 'suit': case 'designer': case 'tux': {
      const lap = id === 'tux' ? '#26262a' : drShade(c, -0.25), shirt = id === 'designer' ? '#141416' : '#f7f6f2';
      s = torso(c) + `<path d="M86 152 L100 168 L114 152 L114 222 L86 222 Z" fill="${shirt}"/>`
        + (id === 'suit' ? `<path d="M97 166 L103 166 L105 204 L100 212 L95 204 Z" fill="#8a1c24"/>` : id === 'tux' ? `<path d="M90 162 L100 166 L110 162 L110 172 L100 168 L90 172 Z" fill="#000"/><circle cx="100" cy="167" r="2" fill="#222"/>` : `<path d="M90 152 L100 172 L110 152" stroke="#2a2a2e" stroke-width="2" fill="none"/>`)
        + `<path d="M84 152 L98 204 L88 222 L58 222 C62 196 68 172 84 152 Z M116 152 L102 204 L112 222 L142 222 C138 196 132 172 116 152 Z" fill="${lap}" stroke="${drShade(c, -0.5)}"/>` + folds(drShade(c, 0.12))
        + (id !== 'tux' ? `<path d="M64 196 l10 -3 l0 5 Z" fill="#f7f6f2" opacity=".85"/>` : ''); break;
    }
    case 'vampire':
      s = `<path d="M30 222 C34 186 50 166 70 158 L60 132 C76 146 88 152 100 154 C112 152 124 146 140 132 L130 158 C150 166 166 186 170 222 Z" fill="#8a0f1e" stroke="#4a0610" stroke-width="1.5"/>`
        + torso('#141216', '#000') + `<path d="M86 152 L100 168 L114 152 L114 222 L86 222 Z" fill="#f4efe6"/><path d="M92 158 C96 166 104 166 108 158 L104 172 L96 172 Z" fill="#f4efe6" stroke="#cfc6b6"/>`
        + `<path d="M84 152 L98 204 L88 222 L58 222 C62 196 68 172 84 152 Z M116 152 L102 204 L112 222 L142 222 C138 196 132 172 116 152 Z" fill="#1f1a22" stroke="#000"/><circle cx="100" cy="176" r="4" fill="#b8142a" stroke="#e3b94f"/>`; break;
    case 'flannel':
      s = torso('#a3262a') + `<g opacity=".55" stroke="#1b0d0d">${[40, 64, 88, 112, 136, 160].map((x) => `<path d="M${x} 160 V222" stroke-width="5"/>`).join('')}${[172, 192, 212].map((y) => `<path d="M20 ${y} H180" stroke-width="5"/>`).join('')}</g>`
        + `<path d="M82 152 L100 168 L118 152 L120 162 L100 178 L80 162 Z" fill="#7e1b1e" stroke="#3a0c0c"/><path d="M100 170 L100 222" stroke="#3a0c0c" stroke-width="2"/>${[184, 198, 212].map((y) => `<circle cx="104" cy="${y}" r="1.6" fill="#e9e5dc"/>`).join('')}`; break;
    case 'ugly':
      s = torso('#1d6b3a') + folds('#175a30') + `<path d="M24 196 H176" stroke="#c2362a" stroke-width="9"/><g fill="#f3ecdc">${[34, 54, 74, 94, 114, 134, 154].map((x) => `<path d="M${x} 196 l4 -4 l4 4 l-4 4 Z"/>`).join('')}</g>`
        + `<path d="M78 152 C88 162 112 162 122 152 L124 160 C112 170 88 170 76 160 Z" fill="#c2362a"/>`; break;
    default: s = torso(c);
  }
  return s;
}
// sponsor patches: chest and sleeve
function drPatchSVG(pid, x, y, rot, scale = 1) {
  const p = DR_PATCH[pid]; if (!p) return '';
  const w = Math.max(26, p.t.length * 4.3 + 8);
  return `<g transform="translate(${x} ${y}) rotate(${rot}) scale(${scale})"><rect x="${-w / 2}" y="-7" width="${w}" height="14" rx="3.5" fill="${p.bg || '#0c0c0c'}" stroke="${p.c}" stroke-width="1.4"/><rect x="${-w / 2 + 1.6}" y="-5.4" width="${w - 3.2}" height="10.8" rx="2.4" fill="none" stroke="${p.c}" stroke-width=".5" stroke-dasharray="1 1" opacity=".7"/><text x="0" y="2.6" text-anchor="middle" font-family="Arial Black,Arial,sans-serif" font-weight="900" font-size="6.8" fill="${p.c}">${p.t}</text></g>`;
}

// ---------------------------------------------------------------- hair
function drHairSVG(style, c, back, id) {
  const d = drShade(c, -0.38), dd = drShade(c, -0.6), l = drShade(c, 0.3), g = `url(#${id}h)`;
  if (back) {
    if (style === 'long') return `<path d="M64 90 C58 126 62 158 74 172 L126 172 C138 158 142 126 136 90 Z" fill="${d}"/><path d="M70 120 C68 140 72 158 80 168 M130 120 C132 140 128 158 120 168" stroke="${dd}" stroke-width="2" fill="none" opacity=".6"/>`;
    if (style === 'mullet') return `<path d="M66 100 C62 124 66 142 78 150 L122 150 C134 142 138 124 134 100 Z" fill="${d}"/><path d="M72 124 C72 136 76 144 82 148 M128 124 C128 136 124 144 118 148" stroke="${dd}" stroke-width="1.6" fill="none" opacity=".6"/>`;
    if (style === 'curly') return `<path d="M60 96 C54 118 62 132 70 136 L130 136 C138 132 146 118 140 96 Z" fill="${d}" opacity=".9"/>`;
    if (style === 'locs') return `<g stroke="${d}" stroke-width="6.5" stroke-linecap="round" fill="none">${[66, 72, 78, 122, 128, 134].map((x, i) => `<path d="M${x} 96 C${x - 2 + i} 120 ${x + 2} 140 ${x + (i < 3 ? -2 : 2)} ${152 + (i % 3) * 6}"/>`).join('')}</g>`;
    if (style === 'bun') return `<circle cx="100" cy="45" r="12.5" fill="${g}" stroke="${dd}" stroke-width="1"/><path d="M91 41 C96 37 104 37 109 41 M89 48 C96 52 104 52 111 48" stroke="${l}" stroke-width="1.4" fill="none" opacity=".7"/><path d="M90 54 C96 58 104 58 110 54" stroke="${dd}" stroke-width="3" fill="none"/>`;
    return '';
  }
  const side = (op, w = 4) => `<path d="M67 90 C66 100 67 106 68 113 L${68 + w} 113 C${67 + w} 104 ${67 + w} 98 ${68 + w} 90 Z M133 90 C134 100 133 106 132 113 L${132 - w} 113 C${133 - w} 104 ${133 - w} 98 ${132 - w} 90 Z" fill="${c}" opacity="${op}"/>`;
  const fadeSides = `<path d="M67 88 C66 98 67 106 69 114 L74 114 C72 104 72 96 73 88 Z M133 88 C134 98 133 106 131 114 L126 114 C128 104 128 96 127 88 Z" fill="${c}" opacity=".28"/>`;
  switch (style) {
    case 'short': {
      // textured crop: a choppy fringe over the forehead
      const fringe = [[74, 82], [80, 86], [86, 81], [92, 86], [98, 82], [104, 87], [110, 82], [116, 86], [122, 81], [127, 85]];
      return `${fadeSides}<path d="M68 92 C64 66 80 52 100 52 C121 52 136 66 132 92 C130 86 128 82 127 85 L${fringe.map(([x, y]) => `${x} ${y}`).reverse().join(' L ')} C72 84 70 88 68 92 Z" fill="${g}"/>
        <path d="M78 64 l6 -7 M88 60 l6 -7 M98 58 l6 -6 M108 59 l6 -6 M118 63 l5 -6 M84 72 l6 -6 M96 70 l6 -6 M108 71 l6 -5" stroke="${l}" stroke-width="2" stroke-linecap="round" opacity=".5"/>
        <path d="M${fringe.map(([x, y]) => `${x} ${y}`).join(' L ')}" stroke="${dd}" stroke-width="1.2" fill="none" opacity=".55"/>`;
    }
    case 'wavy': return `${side(0.85)}<path d="M66 100 C58 66 80 46 104 48 C130 50 144 70 135 100 C132 86 127 78 119 74 C112 84 97 88 84 83 C78 89 71 94 66 100 Z" fill="${g}"/>
      <path d="M74 74 C82 60 98 56 108 62 M88 80 C98 72 112 70 122 76 M104 52 C118 52 130 60 133 74" stroke="${l}" stroke-width="2.4" fill="none" stroke-linecap="round" opacity=".55"/>
      <path d="M84 83 C97 88 112 84 119 74" stroke="${dd}" stroke-width="2" fill="none" opacity=".7"/>`;
    case 'curtains': return `${side(0.9)}<path d="M67 100 C62 64 82 50 100 50 C118 50 138 64 133 100 C131 90 127 84 121 80 C114 78 106 74 101 62 C96 74 86 78 79 80 C73 84 69 90 67 100 Z" fill="${g}"/>
      <path d="M101 62 C99 72 92 80 82 86 M101 62 C104 72 110 80 120 86" stroke="${dd}" stroke-width="1.6" fill="none" opacity=".75"/>
      <path d="M100 52 L101 62 M86 58 C80 64 76 72 74 80 M114 58 C120 64 124 72 126 80" stroke="${l}" stroke-width="1.8" fill="none" opacity=".55"/>`;
    case 'fade': return `<path d="M68 90 C68 100 69 108 71 114 L74 114 C73 106 73 98 74 90 Z M132 90 C132 100 131 108 129 114 L126 114 C127 106 127 98 126 90 Z" fill="${c}" opacity=".3"/>
      <path d="M70 86 C68 62 84 51 100 51 C117 51 133 62 130 86 C126 79 120 75 112 74 L88 74 C80 75 74 79 70 86 Z" fill="${g}"/>
      <path d="M70 86 C68 92 68 98 69 104 M130 86 C132 92 132 98 131 104" stroke="${c}" stroke-width="3" fill="none" opacity=".3"/>
      <path d="M82 62 C92 56 108 56 118 62" stroke="${l}" stroke-width="2" fill="none" opacity=".5"/>`;
    case 'waves': {
      let w = ''; for (let i = 0; i < 6; i++) w += `<path d="M${74 + i * 2} ${80 - i * 4} C${86 + i} ${70 - i * 4} ${114 - i} ${70 - i * 4} ${126 - i * 2} ${80 - i * 4}" stroke="${l}" stroke-width="1.3" fill="none" opacity="${(0.55 - i * 0.05).toFixed(2)}"/>`;
      return `${side(0.75, 3)}<path d="M69 90 C67 66 83 55 100 55 C117 55 133 66 131 90 C127 82 120 78 112 77 L88 77 C80 78 73 82 69 90 Z" fill="${c}"/>${w}`;
    }
    case 'twists': {
      let t = ''; const pts = [[74, 76], [82, 68], [90, 63], [100, 61], [110, 63], [118, 68], [126, 76], [80, 78], [92, 71], [104, 70], [116, 72], [88, 80], [100, 78], [112, 79]];
      for (const [x, y] of pts) t += `<path d="M${x - 2.6} ${y + 8} C${x - 3} ${y} ${x - 2} ${y - 7} ${x} ${y - 9} C${x + 2} ${y - 7} ${x + 3} ${y} ${x + 2.6} ${y + 8} Z" fill="${g}" stroke="${dd}" stroke-width=".7"/><path d="M${x - 1.6} ${y + 4} L${x + 1.6} ${y + 1} M${x - 1.6} ${y - 1} L${x + 1.6} ${y - 4}" stroke="${l}" stroke-width=".8" opacity=".7"/>`;
      return `${fadeSides}<path d="M70 90 C68 70 84 58 100 58 C117 58 132 70 130 90 C126 84 120 80 112 79 L88 79 C80 80 74 84 70 90 Z" fill="${d}"/>${t}`;
    }
    case 'curly': {
      let coils = '', r = 3;
      const rnd = () => { r = (r * 9301 + 49297) % 233280; return r / 233280; };
      for (let row = 0; row < 4; row++) {
        const y = 52 + row * 8, half = [20, 30, 34, 35][row];
        for (let x = 100 - half; x <= 100 + half; x += 6.4) {
          if (row >= 3 && x > 78 && x < 122) continue;
          const xx = x + (row % 2) * 3.2 + (rnd() - 0.5) * 1.6, yy = y + (rnd() - 0.5) * 2;
          coils += `<circle cx="${xx.toFixed(1)}" cy="${yy.toFixed(1)}" r="4.3" fill="${g}"/><path d="M${(xx - 2.6).toFixed(1)} ${(yy + 0.6).toFixed(1)} a2.8 2.8 0 1 1 4.6 1.4" stroke="${dd}" stroke-width=".9" fill="none" opacity=".75"/><path d="M${(xx - 1.8).toFixed(1)} ${(yy - 1.6).toFixed(1)} a2 2 0 0 1 2.6 -.6" stroke="${l}" stroke-width=".9" fill="none" opacity=".75"/>`;
        }
      }
      return `${fadeSides}<path d="M64 96 C58 66 80 46 100 46 C120 46 142 66 136 96 C132 86 126 80 120 78 C110 74 90 74 80 78 C74 80 68 86 64 96 Z" fill="${d}"/>${coils}`;
    }
    case 'locs': return `<path d="M67 96 C64 66 82 53 100 53 C120 53 137 66 133 96 C129 82 118 74 100 73 C82 74 71 82 67 96 Z" fill="${g}"/>${[74, 82, 90, 98, 106, 114, 122].map((x) => `<path d="M${x} 60 C${x + 1} 66 ${x - 1} 72 ${x} 78" stroke="${dd}" stroke-width="1.2" fill="none" opacity=".7"/>`).join('')}`;
    case 'slick': return `${side(0.9)}<path d="M67 94 C64 64 84 51 102 51 C123 51 137 64 133 94 C131 82 126 74 119 71 C107 67 93 67 82 71 C75 75 70 84 67 94 Z" fill="${g}"/>
      <path d="M80 64 C94 57 112 57 126 66 M78 71 C94 63 114 63 129 74 M86 59 C98 54 110 54 120 58" stroke="${l}" stroke-width="1.8" fill="none" opacity=".75"/>`;
    case 'mullet': {
      const fringe = [[74, 84], [82, 80], [90, 85], [98, 80], [106, 85], [114, 80], [122, 84], [128, 82]];
      return `${side(0.95, 5)}<path d="M67 96 C62 66 80 52 100 52 C121 52 137 66 133 96 C131 88 129 84 128 82 L${fringe.map(([x, y]) => `${x} ${y}`).reverse().join(' L ')} C70 86 68 90 67 96 Z" fill="${g}"/>
        <path d="M80 62 l6 -6 M92 58 l6 -6 M104 58 l6 -6 M116 61 l6 -6" stroke="${l}" stroke-width="2" stroke-linecap="round" opacity=".5"/>`;
    }
    case 'long': return `<path d="M66 106 C58 66 80 50 100 51 C120 50 142 66 134 106 L133 134 C128 120 126 102 122 88 C114 78 106 72 100 66 C94 72 86 78 78 88 C74 102 72 120 67 134 Z" fill="${g}"/>
      <path d="M100 56 L100 68 M90 60 C84 72 78 88 74 110 M110 60 C116 72 122 88 126 110" stroke="${l}" stroke-width="1.8" fill="none" opacity=".55"/>`;
    case 'buzz': return `<path d="M70 92 C68 68 84 58 100 58 C117 58 133 68 130 92 C127 83 121 77 114 76 L86 76 C79 77 73 83 70 92 Z" fill="${c}" opacity=".5"/><path d="M70 92 C68 68 84 58 100 58 C117 58 133 68 130 92 C127 83 121 77 114 76 L86 76 C79 77 73 83 70 92 Z" fill="url(#${id}st)"/>`;
    case 'bun': return `${side(0.9)}<path d="M67 96 C64 66 82 54 100 54 C120 54 137 66 133 96 C129 80 117 71 100 71 C83 71 71 80 67 96 Z" fill="${g}"/><path d="M80 64 C92 58 108 58 120 64 M76 72 C90 64 110 64 124 72" stroke="${l}" stroke-width="1.6" fill="none" opacity=".7"/>`;
    case 'bald': return `<ellipse cx="88" cy="68" rx="13" ry="7" fill="#fff" opacity=".18" transform="rotate(-18 88 68)"/>`;
    default: return '';
  }
}

// ---------------------------------------------------------------- facial hair
function drFacialSVG(style, c, id) {
  const d = drShade(c, -0.35), l = drShade(c, 0.25);
  const stache = (thin) => thin ? `<path d="M89 123.6 C93 121.6 98 122 100 123.2 C102 122 107 121.6 111 123.6 C107 124.6 103 124.4 100 124.2 C97 124.4 93 124.6 89 123.6 Z" fill="${c}" opacity=".9"/>`
    : `<path d="M87 124 C91 119.5 97 119.5 100 122 C103 119.5 109 119.5 113 124 C109 126.4 104 125.6 100 124.6 C96 125.6 91 126.4 87 124 Z" fill="url(#${id}h)" stroke="${d}" stroke-width=".7"/>`;
  const stub = 'M72 110 C73 130 86 144 100 145 C114 144 127 130 128 110 C124 122 118 131 108 133 C104 130 96 130 92 133 C82 131 76 122 72 110 Z M88 123 C93 120 107 120 112 123 C108 125 92 125 88 123 Z';
  if (style === 'stubble') return `<path d="${stub}" fill="${c}" opacity=".18"/><path d="${stub}" fill="url(#${id}st)" opacity=".85"/>`;
  if (style === 'mustache') return stache(true);
  if (style === 'goatee') return `<path d="M93 131 C96 134 104 134 107 131 C107 138 104 143 100 144 C96 143 93 138 93 131 Z" fill="url(#${id}h)" stroke="${d}" stroke-width=".7"/><path d="M89 125 C89.5 129 91 131 93 131.5 M111 125 C110.5 129 109 131 107 131.5" stroke="${c}" stroke-width="2" fill="none" stroke-linecap="round"/>${stache(false)}`;
  if (style === 'chin') return `<path d="M70 106 C70 128 84 145 100 146 C116 145 130 128 130 106 L127 106 C126 124 114 140 100 141 C86 140 74 124 73 106 Z" fill="${c}"/><path d="M96 134 C98 137 102 137 104 134" stroke="${c}" stroke-width="2.4" fill="none" stroke-linecap="round"/>`;
  if (style === 'beard-top') return stache(false);
  if (style === 'beard-base') return `<path d="M69 102 C68 124 80 148 100 151 C120 148 132 124 131 102 C128 114 124 124 117 128 C111 131 106 129 100 129 C94 129 89 131 83 128 C76 124 72 114 69 102 Z" fill="url(#${id}h)" stroke="${d}" stroke-width=".8"/>
    <path d="M78 126 C82 136 88 142 94 146 M122 126 C118 136 112 142 106 146 M100 136 L100 148" stroke="${l}" stroke-width="1" fill="none" opacity=".5"/>`;
  return '';
}

// ---------------------------------------------------------------- eyewear
function drEyewearSVG(id) {
  if (id === 'readers') return `<g fill="none" stroke="#2b2b2b" stroke-width="1.6"><rect x="76" y="96" width="20" height="13" rx="4"/><rect x="104" y="96" width="20" height="13" rx="4"/><path d="M96 101 L104 101 M76 100 L68 98 M124 100 L132 98"/></g>`;
  if (id === 'round') return `<g fill="rgba(200,230,255,.08)" stroke="#1d1d22" stroke-width="2"><circle cx="86" cy="102" r="9.6"/><circle cx="114" cy="102" r="9.6"/></g><path d="M95.6 100 C98 98.4 102 98.4 104.4 100 M76.4 100 L68 98 M123.6 100 L132 98" stroke="#1d1d22" stroke-width="2" fill="none"/><path d="M80 97 C82 95 85 94.4 87 94.6 M108 97 C110 95 113 94.4 115 94.6" stroke="#fff" stroke-width="1" opacity=".5" fill="none"/>`;
  if (id === 'thick') return `<g fill="rgba(200,230,255,.06)" stroke="#121214" stroke-width="3.4"><rect x="74.5" y="94.5" width="22" height="15" rx="3.5"/><rect x="103.5" y="94.5" width="22" height="15" rx="3.5"/></g><path d="M96.5 100 L103.5 100" stroke="#121214" stroke-width="3"/><path d="M78 98 L84 97" stroke="#fff" stroke-width="1.2" opacity=".45"/>`;
  if (id === 'tinted') return `<g><rect x="75" y="95" width="22" height="15" rx="5" fill="#c9a27a" opacity=".55" stroke="#c9b9a0" stroke-width="1.4"/><rect x="103" y="95" width="22" height="15" rx="5" fill="#c9a27a" opacity=".55" stroke="#c9b9a0" stroke-width="1.4"/><path d="M97 101 L103 101" stroke="#c9b9a0" stroke-width="1.4"/></g>`;
  if (id === 'aviator') return `<defs><linearGradient id="avl" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#2a2a2a"/><stop offset="1" stop-color="#6e5a3a"/></linearGradient></defs><g stroke="#d9b36c" stroke-width="1.6"><path d="M74 96 L97 96 L95 108 C92 115 79 115 76 108 Z" fill="url(#avl)"/><path d="M103 96 L126 96 L124 108 C121 115 108 115 105 108 Z" fill="url(#avl)"/><path d="M97 97 L103 97" fill="none"/></g>`;
  if (id === 'sport') return `<defs><linearGradient id="spg" x1="0" x2="1"><stop offset="0" stop-color="#ff5e3a"/><stop offset=".5" stop-color="#ffd23a"/><stop offset="1" stop-color="#3cbcd0"/></linearGradient></defs><path d="M66 98 C76 92 124 92 134 98 L132 106 C122 112 108 110 100 104 C92 110 78 112 68 106 Z" fill="url(#spg)" opacity=".9" stroke="#111" stroke-width="1.6"/><path d="M72 98 C84 95 116 95 128 98" stroke="#fff" stroke-width="1" opacity=".6" fill="none"/>`;
  if (id === 'shades') {
    // the deal-with-it 8-bit shades, built from pixels
    const P = 3, rows = ['KKKKKKKKKKKKKKKK', '.KwwKKK..KwwKKK.', '..KwwKK...KwwKK.', '...KKK.....KKK..'];
    let r = '';
    rows.forEach((row, y) => [...row].forEach((ch, x) => { if (ch !== '.') r += `<rect x="${76 + x * P}" y="${95 + y * P}" width="${P}" height="${P}" fill="${ch === 'K' ? '#050505' : '#fff'}"/>`; }));
    return `<g shape-rendering="crispEdges">${r}</g>`;
  }
  if (id === 'masq') return `<path d="M72 98 C78 90 92 92 98 99 C100 101 100 101 102 99 C108 92 122 90 128 98 C126 108 116 112 108 108 C104 106 96 106 92 108 C84 112 74 108 72 98 Z" fill="#2a0b3a" stroke="#e3b94f" stroke-width="1.4"/><ellipse cx="86" cy="102" rx="5" ry="3" fill="#0b0609"/><ellipse cx="114" cy="102" rx="5" ry="3" fill="#0b0609"/><path d="M128 96 C136 86 140 78 138 70" stroke="#ff7a1a" stroke-width="2.4" fill="none"/>`;
  if (id === 'amber') return `<g><rect x="75" y="95" width="22" height="15" rx="6" fill="#e08a1e" opacity=".7" stroke="#6a3a10" stroke-width="1.8"/><rect x="103" y="95" width="22" height="15" rx="6" fill="#e08a1e" opacity=".7" stroke="#6a3a10" stroke-width="1.8"/><path d="M97 101 L103 101" stroke="#6a3a10" stroke-width="1.8"/></g>`;
  if (id === 'snow') return `<g><rect x="70" y="93" width="60" height="18" rx="9" fill="#cfefff" stroke="#f3f6fa" stroke-width="2.4"/><rect x="72" y="95" width="56" height="14" rx="7" fill="url(#snowg)"/><defs><linearGradient id="snowg" x1="0" x2="1"><stop offset="0" stop-color="#7fd0ff"/><stop offset=".5" stop-color="#c79bff"/><stop offset="1" stop-color="#ffb37f"/></linearGradient></defs><path d="M70 102 L64 100 M130 102 L136 100" stroke="#f3f6fa" stroke-width="3"/></g>`;
  return '';
}

// ---------------------------------------------------------------- headwear (a back layer, then the front)
const DR_HAT_BASE = { cap: '#17171a', snap: '#24427e', beanie: '#b3262c', bucket: '#d8cfb4', cowboy: '#8a5a32', fedora: '#2a2522', visor: '#1e8f4a' };
function drHeadwearSVG(id, c, back) {
  const dk = drShade(c, -0.32), lt = drShade(c, 0.22);
  if (back) {
    if (id === 'cap') return `<path d="M80 50 C90 40 110 40 120 50 L118 55 C108 47 92 47 82 55 Z" fill="${drShade(c, -0.45)}"/>`;
    return '';
  }
  switch (id) {
    case 'cap': // worn backwards: the snap strap opening sits on your forehead
      return `<path d="M64 86 C63 58 82 46 100 46 C118 46 137 58 136 86 C128 82 114 80 100 80 C86 80 72 82 64 86 Z" fill="${c}" stroke="${dk}" stroke-width="1"/>
        <path d="M100 46 V80 M82 50 C80 60 79 70 80 81 M118 50 C120 60 121 70 120 81" stroke="${dk}" stroke-width="1" opacity=".7" fill="none"/>
        <path d="M86 81 C86 72 91 67 100 67 C109 67 114 72 114 81 Z" fill="#0b0a09" opacity=".55"/><path d="M84 79 H116" stroke="${dk}" stroke-width="3"/><path d="M88 79 H112" stroke="${lt}" stroke-width="1.2" stroke-dasharray="2 1.6"/>
        <circle cx="100" cy="46.5" r="2.4" fill="${dk}"/><path d="M72 62 C80 54 92 51 100 51" stroke="${lt}" stroke-width="2.2" fill="none" opacity=".45"/>
        <path d="M63 85 C74 80 126 80 137 85 L137 88 C126 84 74 84 63 88 Z" fill="${dk}"/>`;
    case 'snap': // flat brim, forward, with the sticker still on
      return `<path d="M65 84 C64 56 83 45 100 45 C117 45 136 56 135 84 Z" fill="${c}" stroke="${dk}" stroke-width="1"/>
        <path d="M100 45 V84 M82 49 C80 60 79 72 80 84 M118 49 C120 60 121 72 120 84" stroke="${dk}" stroke-width="1" opacity=".7" fill="none"/>
        <rect x="88" y="60" width="24" height="14" rx="2" fill="${lt}" opacity=".9"/><text x="100" y="71" text-anchor="middle" font-family="Arial Black,Arial,sans-serif" font-size="9" font-weight="900" fill="${dk}">CF</text>
        <path d="M58 84 C74 78 126 78 142 84 L144 91 C126 88 74 88 56 91 Z" fill="${dk}"/><path d="M58 84 C74 79 126 79 142 84" stroke="${lt}" stroke-width="1.2" fill="none" opacity=".7"/>
        <circle cx="128" cy="85.5" r="3" fill="#d9b36c" stroke="#8f6f33" stroke-width=".6"/><circle cx="100" cy="45.5" r="2.4" fill="${dk}"/>`;
    case 'beanie':
      return `<path d="M65 88 C62 56 82 44 100 44 C118 44 138 56 135 88 Z" fill="${c}"/>${[74, 81, 88, 95, 102, 109, 116, 123].map((x) => `<path d="M${x + 2} 50 C${x} 62 ${x} 74 ${x + 1} 84" stroke="${dk}" stroke-width="1.6" fill="none" opacity=".5"/>`).join('')}
        <rect x="63" y="78" width="74" height="13" rx="5" fill="${dk}"/>${[68, 74, 80, 86, 92, 98, 104, 110, 116, 122, 128].map((x) => `<path d="M${x} 79 V90" stroke="${drShade(c, -0.5)}" stroke-width="1.2"/>`).join('')}
        <rect x="92" y="81" width="16" height="7" rx="1.5" fill="#efeee9"/><text x="100" y="86.8" text-anchor="middle" font-family="Arial Black,Arial,sans-serif" font-size="5" fill="${dk}">CF</text>`;
    case 'bucket':
      return `<path d="M70 82 C70 56 84 46 100 46 C116 46 130 56 130 82 Z" fill="${c}" stroke="${dk}"/><path d="M58 92 C66 80 134 80 142 92 C138 96 62 96 58 92 Z" fill="${dk}"/><path d="M60 91 C70 84 130 84 140 91" stroke="${lt}" stroke-width="1" fill="none" stroke-dasharray="2 2"/><path d="M71 76 H129" stroke="${dk}" stroke-width="1.2" stroke-dasharray="2 2"/>`;
    case 'cowboy':
      return `<path d="M72 80 C70 58 82 42 92 46 C96 50 104 50 108 46 C118 42 130 58 128 80 Z" fill="${c}" stroke="${dk}"/><path d="M92 46 C96 54 104 54 108 46" stroke="${dk}" stroke-width="1.6" fill="none"/><rect x="71" y="72" width="58" height="7" fill="${dk}"/>
        <path d="M44 74 C52 90 70 88 100 88 C130 88 148 90 156 74 C150 92 130 96 100 96 C70 96 50 92 44 74 Z" fill="${c}" stroke="${dk}"/><circle cx="100" cy="75.5" r="2" fill="#c9ccd4"/>`;
    case 'fedora':
      return `<path d="M72 82 C70 58 84 46 100 46 C116 46 130 58 128 82 Z" fill="${c}"/><path d="M90 50 C96 56 104 56 110 50" stroke="${dk}" stroke-width="2" fill="none"/><rect x="71" y="72" width="58" height="8" fill="#141210"/>
        <path d="M54 88 C64 78 136 78 146 88 C140 92 60 92 54 88 Z" fill="${dk}"/>`;
    case 'visor': return `<path d="M64 82 C80 72 120 72 136 82 L132 90 C118 84 82 84 68 90 Z" fill="${c}" opacity=".85" stroke="${dk}" stroke-width="1.2"/><path d="M68 82 C84 78 116 78 132 82" stroke="#dff5e6" stroke-width="1" fill="none" opacity=".5"/>`;
    case 'pumpkin': return `<path d="M64 88 C60 54 82 44 100 44 C118 44 140 54 136 88 Z" fill="#ff7a1a" stroke="#b44c0a" stroke-width="1.5"/><path d="M84 50 C80 64 80 76 82 88 M116 50 C120 64 120 76 118 88 M100 44 V88" stroke="#d9600f" stroke-width="2" fill="none"/><path d="M98 44 C98 36 102 32 108 30" stroke="#3f6b2a" stroke-width="4" fill="none" stroke-linecap="round"/><path d="M82 66 l6 -6 l6 6 Z M106 66 l6 -6 l6 6 Z M84 76 Q100 86 116 76 l-4 4 l-4 -3 l-4 3 l-4 -3 l-4 3 l-4 -3 Z" fill="#2a1406"/>`;
    case 'pilgrim': return `<rect x="74" y="30" width="52" height="52" rx="3" fill="#1a1a1c"/><path d="M58 86 C70 78 130 78 142 86 C142 92 58 92 58 86 Z" fill="#1a1a1c"/><rect x="74" y="66" width="52" height="9" fill="#5a3a20"/><rect x="92" y="64" width="16" height="13" rx="1" fill="none" stroke="#e3b94f" stroke-width="2.6"/>`;
    case 'santa': return `<path d="M68 86 C70 56 92 44 112 48 C130 52 144 70 150 98 L138 100 C132 82 124 70 114 66 C106 70 92 78 68 86 Z" fill="#c2141e"/><circle cx="146" cy="102" r="8" fill="#f7f6f2"/><rect x="62" y="80" width="76" height="13" rx="6" fill="#f7f6f2"/>`;
    default: return '';
  }
}

// ---------------------------------------------------------------- chains, earrings, eyes
function drChainSVG(id) {
  if (!id || id === 'none') return '';
  const gold = '#e3b94f', hi = '#fff3c4';
  const curve = (dy = 0) => `M84 ${156 + dy} C88 ${176 + dy} 112 ${176 + dy} 116 ${156 + dy}`;
  if (id === 'silver') return `<path d="${curve()}" stroke="#c9ccd4" stroke-width="1.6" fill="none"/>`;
  if (id === 'cuban' || id === 'iced') return `<path d="${curve()}" stroke="${gold}" stroke-width="3.6" fill="none" stroke-dasharray="3 1.2"/><path d="${curve()}" stroke="${hi}" stroke-width=".8" fill="none" opacity=".7" stroke-dasharray="1.5 2.9"/>${id === 'iced' ? [[89, 167], [94, 171], [100, 172.5], [106, 171], [111, 167]].map(([x, y]) => `<circle cx="${x}" cy="${y}" r="1.3" fill="#fff"/>`).join('') : ''}`;
  if (id === 'rope') return `<path d="${curve(2)}" stroke="${gold}" stroke-width="2.4" fill="none"/><path d="${curve(2)}" stroke="#a37a1f" stroke-width="2.4" fill="none" stroke-dasharray="1.2 1.6"/>
    <g transform="translate(100 177)"><path d="M0 -1 C2 1 5 2.6 4 6 C3.4 8 1 8 .4 6.6 L1.4 9.6 L-1.4 9.6 L-.4 6.6 C-1 8 -3.4 8 -4 6 C-5 2.6 -2 1 0 -1 Z" fill="#141414" stroke="${gold}" stroke-width=".9"/></g>`;
  if (id === 'layered') return `<path d="${curve(-2)}" stroke="${gold}" stroke-width="1.2" fill="none"/><path d="${curve(5)}" stroke="${gold}" stroke-width="3.4" fill="none" stroke-dasharray="3 1.2"/><path d="${curve(5)}" stroke="${hi}" stroke-width=".7" fill="none" opacity=".6" stroke-dasharray="1.5 2.9"/>
    <g transform="translate(100 171)"><circle r="5.2" fill="${gold}" stroke="#8f6f33" stroke-width=".8"/><circle r="3.4" fill="none" stroke="#8f6f33" stroke-width=".6"/><text y="1.8" text-anchor="middle" font-family="Georgia,serif" font-weight="900" font-size="5" fill="#5a3f10">CF</text></g>`;
  if (id === 'bones') return [[86, 162], [91, 168], [97, 171], [103, 171], [109, 168], [114, 162]].map(([x, y]) => `<g transform="translate(${x} ${y})"><rect x="-2.6" y="-1" width="5.2" height="2" fill="#efe8d6"/><circle cx="-2.6" cy="-1" r="1.2" fill="#efe8d6"/><circle cx="-2.6" cy="1" r="1.2" fill="#efe8d6"/><circle cx="2.6" cy="-1" r="1.2" fill="#efe8d6"/><circle cx="2.6" cy="1" r="1.2" fill="#efe8d6"/></g>`).join('') + `<circle cx="100" cy="178" r="4.4" fill="#efe8d6"/><circle cx="98.4" cy="177" r="1.1" fill="#111"/><circle cx="101.6" cy="177" r="1.1" fill="#111"/>`;
  if (id === 'wishbone') return `<path d="${curve()}" stroke="${gold}" stroke-width="1.4" fill="none"/><path d="M100 186 L94 174 M100 186 L106 174 M100 186 L100 190" stroke="${gold}" stroke-width="2.4" stroke-linecap="round" fill="none"/>`;
  if (id === 'lights') return `<path d="${curve()}" stroke="#1d4a22" stroke-width="1.6" fill="none"/>` + [[86, 162], [90, 168], [95, 171], [100, 172], [105, 171], [110, 168], [114, 162]].map(([x, y], i) => `<ellipse cx="${x}" cy="${y + 2}" rx="1.6" ry="2.4" fill="${['#ff4f4f', '#5cbf64', '#ffd23a', '#5aa9de'][i % 4]}"/>`).join('');
  return '';
}
function drEarSVG(id) {
  if (!id || id === 'none') return '';
  const at = [[63.5, 113], [136.5, 113]];
  if (id === 'stud') return at.map(([x, y]) => `<circle cx="${x}" cy="${y}" r="1.6" fill="#d8dbe2" stroke="#8a8f98" stroke-width=".5"/>`).join('');
  if (id === 'diamond') return at.map(([x, y]) => `<path d="M${x} ${y - 2.4} L${x + 2.2} ${y} L${x} ${y + 2.4} L${x - 2.2} ${y} Z" fill="#f4fbff" stroke="#9fd0ff" stroke-width=".6"/><circle cx="${x - 0.6}" cy="${y - 0.6}" r=".5" fill="#fff"/>`).join('');
  if (id === 'hoop') return at.map(([x, y]) => `<circle cx="${x}" cy="${y + 3}" r="3.6" fill="none" stroke="#e3b94f" stroke-width="1.3"/>`).join('');
  return '';
}
function drEyeSVG(cx, cy, flip, ec, id, k, blink, sc) {
  // almond eye, coloured iris, upper lid, a little wing; flip mirrors for the right eye; sc scales for age
  const m = (x) => (flip ? 2 * cx - x : x), P = (pts) => pts.map(([x, y]) => `${m(cx + (x - cx) * sc).toFixed(1)} ${(cy + (y - cy) * sc).toFixed(1)}`);
  const [a, b, c2, d] = [P([[cx - 8.6, cy]]), P([[cx - 5.4, cy - 5]]), P([[cx + 5.4, cy - 4.6]]), P([[cx + 8.6, cy - 0.6]])];
  const shape = `M${a} C${b} ${c2} ${d} C${P([[cx + 5, cy + 3.8]])} ${P([[cx - 4.4, cy + 4]])} ${a} Z`;
  const ix = +m(cx + 0.4).toFixed(1), ir = 4.4 * sc;
  return `<defs><clipPath id="${id}e${k}"><path d="${shape}"/></clipPath><radialGradient id="${id}i${k}"><stop offset=".2" stop-color="${drShade(ec, 0.3)}"/><stop offset="1" stop-color="${drShade(ec, -0.45)}"/></radialGradient></defs>
    <g${blink}><path d="${shape}" fill="#f7f3ec"/><g clip-path="url(#${id}e${k})"><circle cx="${ix}" cy="${cy - 0.2}" r="${ir.toFixed(2)}" fill="url(#${id}i${k})"/><circle cx="${ix}" cy="${cy - 0.2}" r="${(ir * 0.43).toFixed(2)}" fill="#0d0907"/><circle cx="${(ix - 1.5).toFixed(1)}" cy="${(cy - 1.7).toFixed(1)}" r="${(1.15 * sc).toFixed(2)}" fill="#fff"/><circle cx="${(ix + 1.5).toFixed(1)}" cy="${(cy + 1.1).toFixed(1)}" r=".5" fill="#fff" opacity=".8"/><path d="M${P([[cx - 10, cy - 6]])} L${P([[cx + 10, cy - 6]])} L${P([[cx + 10, cy - 1.8]])} C${P([[cx + 3, cy - 3.6]])} ${P([[cx - 3, cy - 3.8]])} ${P([[cx - 10, cy - 1.6]])} Z" fill="#000" opacity=".15"/></g>
    <path d="M${P([[cx - 9.2, cy + 0.4]])} C${P([[cx - 5.4, cy - 5.6]])} ${P([[cx + 5.4, cy - 5.2]])} ${P([[cx + 9.4, cy - 0.4]])}" stroke="#1a110b" stroke-width="1.9" fill="none" stroke-linecap="round"/>
    <path d="M${P([[cx + 7.6, cy - 2.4]])} L${P([[cx + 10.4, cy - 3.6]])}" stroke="#1a110b" stroke-width="1.2" stroke-linecap="round"/>
    <path d="M${P([[cx - 6.5, cy - 7.4]])} C${P([[cx - 2, cy - 9.2]])} ${P([[cx + 3, cy - 9]])} ${P([[cx + 7, cy - 6.4]])}" stroke="#000" stroke-width=".8" fill="none" opacity=".16"/></g>`;
}

// ---------------------------------------------------------------- the whole player
const DR_HEADS = {
  oval: 'M100 55 C121 55 133 70 133 91 C133 103 131 113 127 122 C122 134 112 143 100 145 C88 143 78 134 73 122 C69 113 67 103 67 91 C67 70 79 55 100 55 Z',
  round: 'M100 55 C122 55 135 70 135 92 C135 106 132 117 126 126 C119 137 110 143 100 144 C90 143 81 137 74 126 C68 117 65 106 65 92 C65 70 78 55 100 55 Z',
  angular: 'M100 55 C121 55 133 70 133 91 C133 103 132 112 129 120 L118 137 C112 143 106 145 100 145 C94 145 88 143 82 137 L71 120 C68 112 67 103 67 91 C67 70 79 55 100 55 Z',
};
const DR_FULL_HATS = ['beanie', 'cowboy', 'fedora', 'bucket', 'pumpkin', 'pilgrim', 'santa'];
function drAvatarSVG2D(st, opt = {}) {
  const look = { ...DR_LOOK_DEFAULT, ...(st.look || {}) }, eq = st.eq || {}, dye = st.dye || {};
  const hair = DR_HAIR.some((h) => h[0] === look.hair) ? look.hair : 'short';
  const skin = DR_SKIN[look.skin] || DR_SKIN[2], sk2 = drShade(skin, -0.16), sk3 = drShade(skin, -0.3), skL = drShade(skin, 0.18);
  const hc = DR_HAIRC[look.hairC] || DR_HAIRC[0], ec = DR_EYEC[look.eyeC || 0] || DR_EYEC[0];
  const lip = drShade(look.skin >= 5 ? '#6a3428' : '#c0685c', look.skin >= 5 ? 0 : -0.08 * (look.skin / 4));
  const titles = st.titles || {}, br = st.br || 0, id = 'a' + Math.random().toString(36).slice(2, 7);
  const face = opt.face, age = look.age || 'young', esc2 = typeof esc === 'function' ? esc : (x) => x;
  const outfit = eq.outfit || 'office', oc = DR_DYES[dye[outfit]] || DR_OUTFIT_BASE[outfit] || '#3b3f47';
  const hat = eq.head && eq.head !== 'none' && eq.head !== 'phones' ? eq.head : null, hcol = hat ? DR_DYES[dye[hat]] || DR_HAT_BASE[hat] || '#222' : null;
  const bracelets = Math.min(4, titles.bracelet || 0), rings = Math.min(3, (titles.ring || 0) + (titles.me || 0));
  const HEAD = DR_HEADS[look.face] || DR_HEADS.oval, eyeSc = age === 'young' ? 1.04 : age === 'old' ? 0.9 : 0.97;
  let s = `<svg class="dravsvg" viewBox="${face ? '50 44 100 100' : '0 30 200 192'}" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="${esc2(st.name || 'Your player')}">`;
  s += `<defs>
    <radialGradient id="${id}f" cx="38%" cy="32%" r="80%"><stop offset="0" stop-color="${skL}"/><stop offset=".55" stop-color="${skin}"/><stop offset="1" stop-color="${sk2}"/></radialGradient>
    <linearGradient id="${id}h" x1=".2" y1="0" x2=".8" y2="1"><stop offset="0" stop-color="${drShade(hc, 0.2)}"/><stop offset=".55" stop-color="${hc}"/><stop offset="1" stop-color="${drShade(hc, -0.42)}"/></linearGradient>
    <linearGradient id="${id}t" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#fff" stop-opacity=".16"/><stop offset=".42" stop-color="#fff" stop-opacity="0"/><stop offset=".68" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity=".34"/></linearGradient>
    <linearGradient id="${id}n" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${sk3}"/><stop offset=".55" stop-color="${sk2}"/></linearGradient>
    <pattern id="${id}st" width="7" height="7" patternUnits="userSpaceOnUse">${[[0.6, 0.9], [2.9, 0.4], [5.2, 1.3], [1.7, 2.6], [4.1, 3.1], [6.3, 3.6], [0.4, 4.4], [2.6, 5.2], [4.9, 5.6], [6.4, 6.4], [1.4, 6.6], [3.6, 2], [5.8, 4.8]].map(([x, y], i) => `<circle cx="${x}" cy="${y}" r="${(0.34 + (i % 3) * 0.06).toFixed(2)}" fill="${drShade(hc, -0.15)}" opacity="${(0.65 + (i % 4) * 0.1).toFixed(2)}"/>`).join('')}</pattern>
    <clipPath id="${id}hd"><path d="${HEAD}"/></clipPath><clipPath id="${id}hb"><rect x="0" y="88" width="200" height="80"/></clipPath></defs>`;
  if (hat) s += drHeadwearSVG(hat, hcol, true);
  if (!hat || !DR_FULL_HATS.includes(hat) || hair === 'long' || hair === 'locs' || hair === 'mullet') s += drHairSVG(hair, hc, true, id);
  // neck: slimmer when young, shadowed under the jaw
  s += `<path d="M${age === 'young' ? 89 : 87} 128 L${age === 'young' ? 111 : 113} 128 L114 160 C106 166 94 166 86 160 Z" fill="url(#${id}n)"/>`;
  s += drOutfitSVG(outfit, oc);
  s += `<path d="M12 222 C16 180 44 162 74 156 L126 156 C156 162 184 180 188 222 Z" fill="url(#${id}t)" pointer-events="none"/>`;
  if (eq.patch && eq.patch !== 'none') s += drPatchSVG(eq.patch, 128, 182, -4);
  if (eq.head === 'phones') s += `<path d="M72 160 C70 144 130 144 128 160" stroke="#222" stroke-width="5" fill="none"/><rect x="64" y="150" width="14" height="18" rx="6" fill="#2b2b2e"/><rect x="122" y="150" width="14" height="18" rx="6" fill="#2b2b2e"/><rect x="66" y="153" width="3" height="11" rx="1.5" fill="#3cbcd0"/>`;
  s += drChainSVG(eq.chain);
  // ears and earrings
  s += `<g fill="${sk2}"><path d="M68.5 96 C61 93 59 103 61 110 C62 116 66 120 70.5 117 Z"/><path d="M131.5 96 C139 93 141 103 139 110 C138 116 134 120 129.5 117 Z"/></g>
    <path d="M66 101 C63 103 63 109 66 113 M134 101 C137 103 137 109 134 113" stroke="${sk3}" stroke-width="1.2" fill="none" opacity=".7"/>`;
  s += drEarSVG(eq.ear);
  // head with form shading: light from the upper left
  s += `<path d="${HEAD}" fill="url(#${id}f)"/>
    <g clip-path="url(#${id}hd)">
      <path d="M113 55 C127 61 134 77 134 93 C134 115 125 139 104 148 C117 133 122 115 121 97 C120 79 118 65 113 55 Z" fill="${sk3}" opacity=".3"/>
      <path d="M74 118 C80 133 90 142 100 144 C110 142 120 133 126 118 C120 136 110 149 100 149 C90 149 80 136 74 118 Z" fill="${sk3}" opacity=".32"/>
      <ellipse cx="88" cy="72" rx="14" ry="6.5" fill="#fff" opacity=".13" transform="rotate(-12 88 72)"/>
      <ellipse cx="80" cy="114" rx="7.5" ry="4.2" fill="#e8705a" opacity="${age === 'young' ? 0.16 : 0.1}"/><ellipse cx="120" cy="114" rx="7.5" ry="4.2" fill="#e8705a" opacity="${age === 'young' ? 0.13 : 0.08}"/>
      ${age === 'old' ? `<path d="M80 70 C90 67 110 67 120 70 M84 75 C94 72.5 106 72.5 116 75" stroke="${sk3}" stroke-width=".9" fill="none" opacity=".5"/><path d="M90 118 C88 124 88 128 90 132 M110 118 C112 124 112 128 110 132" stroke="${sk3}" stroke-width="1" fill="none" opacity=".45"/>` : age === 'adult' ? `<path d="M76 108 C78 116 82 120 86 121 M124 108 C122 116 118 120 114 121" stroke="${sk3}" stroke-width="1.1" fill="none" opacity=".22"/>` : ''}
    </g>`;
  if (look.freck) s += `<g fill="${drShade(skin, -0.42)}" opacity=".5">${[[78, 110], [82, 112], [80, 115], [85, 114], [76, 113], [122, 110], [118, 112], [120, 115], [115, 114], [124, 113], [97, 109], [103, 109]].map(([x, y]) => `<circle cx="${x}" cy="${y}" r=".75"/>`).join('')}</g>`;
  if (look.facial === 'beard') s += drFacialSVG('beard-base', hc, id);
  if (look.facial === 'stubble') s += drFacialSVG('stubble', hc, id);
  // eyes and brows
  const blink = opt.blink ? ' class="drblink"' : '';
  s += drEyeSVG(86, 102, false, ec, id, 0, blink, eyeSc) + drEyeSVG(114, 102, true, ec, id, 1, blink, eyeSc);
  if (age === 'old') s += `<path d="M78 108 C82 110 88 110 92 108.6 M122 108 C118 110 112 110 108 108.6" stroke="${sk3}" stroke-width=".9" fill="none" opacity=".55"/>`;
  const bc = drShade(hc, look.hairC >= 7 && look.hairC <= 8 ? -0.3 : 0.02), bw = look.brows === 'thick' ? 1.5 : 1;
  const browL = look.brows === 'arched' ? 'M76 92 C80 86.5 89 86 96.5 89.4 C96.8 90.4 96.4 91.2 95.6 91.2 C89.5 89.6 82 90 77 93.8 C76 93.6 75.6 92.8 76 92 Z' : `M76 92.4 C81 ${88.6 - bw} 89.5 ${88 - bw} 96.5 90.4 C96.8 91.4 96.4 92.2 95.6 92.2 C89.5 ${(90.8 - bw * 0.3).toFixed(2)} 82.5 91.6 77 94.4 C76 94.2 75.6 93.2 76 92.4 Z`;
  const browR = look.brows === 'arched' ? 'M124 91 C120 85.5 111 85 103.5 88.4 C103.2 89.4 103.6 90.2 104.4 90.2 C110.5 88.6 118 89 123 92.8 C124 92.6 124.4 91.8 124 91 Z' : `M124 91.4 C119 ${87.4 - bw} 110.5 ${87 - bw} 103.5 89.6 C103.2 90.6 103.6 91.4 104.4 91.4 C110.5 ${(89.8 - bw * 0.3).toFixed(2)} 117.5 90.6 123 93.4 C124 93.2 124.4 92.2 124 91.4 Z`;
  s += `<path d="${browL}" fill="${bc}"/><path d="${browR}" fill="${bc}"/>`;
  if (look.brows === 'slit') s += `<path d="M117 86 L115.4 93.6 M119.6 86.6 L118 94.2" stroke="${skin}" stroke-width="1.3"/>`;
  // nose: small and soft
  const ny = age === 'young' ? 0 : 1;
  s += `<path d="M103.5 ${99 + ny} C104.4 ${105 + ny} 105.2 ${110 + ny} 106.2 ${114 + ny}" stroke="${sk3}" stroke-width="1.4" fill="none" opacity=".4" stroke-linecap="round"/>
    <path d="M95.5 ${114.5 + ny} C96.5 ${118.4 + ny} 104 ${118.4 + ny} 105 ${114.5 + ny}" stroke="${sk3}" stroke-width="1.5" fill="none" opacity=".7" stroke-linecap="round"/>
    <ellipse cx="97.4" cy="${116.6 + ny}" rx="1.4" ry=".8" fill="${drShade(skin, -0.5)}" opacity=".55"/><ellipse cx="102.8" cy="${116.6 + ny}" rx="1.4" ry=".8" fill="${drShade(skin, -0.5)}" opacity=".55"/>
    <ellipse cx="99" cy="${113.6 + ny}" rx="2.2" ry="1.4" fill="#fff" opacity=".22"/>`;
  // mouth: a confident smirk by default
  const mouth = opt.mouth || (opt.smile ? 'smile' : 'smug');
  if (mouth === 'frown') s += `<path d="M92 131 C96 127.6 104.6 127.6 109 131" stroke="${drShade(lip, -0.42)}" stroke-width="1.7" fill="none" stroke-linecap="round"/><path d="M94 132 C98 130.6 103 130.6 107 132" stroke="${lip}" stroke-width="1.2" fill="none" opacity=".5"/>`;
  else if (mouth === 'smile') s += `<path d="M89 125.5 C95 133.5 106 133.5 112 125 C106 128 95 128.4 89 125.5 Z" fill="#fff"/><path d="M89 125.5 C95 133.5 106 133.5 112 125" stroke="${drShade(lip, -0.35)}" stroke-width="1.6" fill="none" stroke-linecap="round"/><path d="M93 130.6 C97 132.6 104 132.6 108 130" stroke="${lip}" stroke-width="1.4" fill="none" opacity=".6"/>`;
  else s += `<path d="M91.5 129 C95 132.4 103 132.6 107.5 128.6 C103 130 96 130.4 91.5 129 Z" fill="${lip}" opacity=".55"/>
    <path d="${mouth === 'flat' ? 'M90.5 127.4 C95 128.6 103 128.6 109.5 127' : 'M90.5 127.6 C95.5 128.8 103 128.2 110.5 124.8'}" stroke="${drShade(lip, -0.42)}" stroke-width="1.6" fill="none" stroke-linecap="round"/>
    <path d="M96 124.4 C98 125.3 102 125.3 104 124.4" stroke="${sk3}" stroke-width=".9" fill="none" opacity=".35"/><ellipse cx="100" cy="134.8" rx="5" ry="1.4" fill="${sk3}" opacity=".18"/>`;
  if (['mustache', 'goatee', 'chin'].includes(look.facial)) s += drFacialSVG(look.facial, hc, id);
  if (look.facial === 'beard') s += drFacialSVG('beard-top', hc, id);
  // hair under the hat: caps sit on top; full hats only let long hair show below the brim
  if (!hat || ((hat === 'cap' || hat === 'snap') && !['curly', 'twists', 'bun'].includes(hair)) || hat === 'visor') s += drHairSVG(hair, hc, false, id);
  else if (DR_FULL_HATS.includes(hat) && ['long', 'locs', 'curtains', 'wavy', 'mullet'].includes(hair)) s += `<g clip-path="url(#${id}hb)">${drHairSVG(hair, hc, false, id)}</g>`;
  if (eq.eyes && eq.eyes !== 'none') s += drEyewearSVG(eq.eyes);
  if (hat) s += drHeadwearSVG(hat, hcol, false);
  if (!face) {
    // the table, your arms, cards, watch, bracelets, rings, chips
    s += `<path d="M0 194 L200 194 L200 222 L0 222 Z" fill="#0e4d39"/><path d="M0 188 C60 182 140 182 200 188 L200 197 C140 191 60 191 0 197 Z" fill="#3a2414"/><path d="M0 189 C60 183 140 183 200 189" stroke="#6a4626" stroke-width="1.2" fill="none"/>`;
    const sleeve = { varsity: '#efe6cf', vest: '#eef0f4', fleece: '#2a2c31', tee: skin, jersey: skin, office: '#cdd8e6' }[outfit] || oc;
    s += `<path d="M10 222 C20 204 46 192 74 190 L80 202 C58 206 40 212 30 222 Z" fill="${sleeve}" stroke="${drShade(sleeve, -0.35)}"/><path d="M14 218 C24 206 44 198 70 194" stroke="#fff" stroke-width="1.4" fill="none" opacity=".12"/>`;
    s += `<path d="M190 222 C180 204 154 192 126 190 L120 202 C142 206 160 212 170 222 Z" fill="${drShade(sleeve, -0.12)}" stroke="${drShade(sleeve, -0.35)}"/>`;
    if (outfit === 'tee' || outfit === 'jersey') s += `<path d="M10 222 C14 214 22 208 30 204 L36 212 C30 214 24 218 20 222 Z M190 222 C186 214 178 208 170 204 L164 212 C170 214 176 218 180 222 Z" fill="${oc}"/>`;
    if (outfit === 'track') s += `<path d="M14 214 C30 202 50 196 74 194 M16 219 C32 207 52 200 76 198" stroke="#f3f3f3" stroke-width="2" fill="none"/>`;
    if (eq.sleeve && eq.sleeve !== 'none') s += drPatchSVG(eq.sleeve, 158, 206, -24, 0.85);
    s += `<g><rect x="86" y="196" width="15" height="21" rx="2" fill="#f7f3e8" stroke="#bbb" transform="rotate(-8 93 206)"/><rect x="98" y="196" width="15" height="21" rx="2" fill="#f7f3e8" stroke="#bbb" transform="rotate(7 105 206)"/><path d="M92 200 l3 4 l-3 4 l-3 -4 Z" fill="#c2362a" transform="rotate(-8 93 206)"/><text x="101.5" y="205" font-family="Georgia,serif" font-size="7" font-weight="700" fill="#141414" transform="rotate(7 105 206)">A</text></g>`;
    const hand = (cx, flip) => { const f = flip ? -1 : 1, X = (x) => cx + f * x;
      return `<path d="M${X(-13)} 199 C${X(-13)} 192 ${X(-6)} 190 ${X(2)} 191 C${X(9)} 192 ${X(13)} 196 ${X(12)} 201 C${X(11)} 206 ${X(4)} 208 ${X(-3)} 207.5 C${X(-10)} 207 ${X(-13)} 204 ${X(-13)} 199 Z" fill="${skin}" stroke="${sk3}" stroke-width=".9"/>
        <path d="M${X(-6)} 192.5 L${X(-6.6)} 199 M${X(-1)} 191.6 L${X(-1.4)} 199.6 M${X(4)} 191.8 L${X(4.2)} 199.6" stroke="${sk3}" stroke-width=".8" opacity=".55"/>
        <path d="M${X(10)} 196 C${X(14)} 196 ${X(16)} 199 ${X(14)} 202" stroke="${sk3}" stroke-width="1" fill="${sk2}"/>`; };
    s += hand(86, false) + hand(114, true);
    // title rings on your left hand, your own ring on the right
    for (let k = 0; k < rings; k++) s += `<rect x="${78 + k * 5}" y="193" width="3.6" height="2.6" rx=".9" fill="#e8c35a" stroke="#fff" stroke-width=".4"/><circle cx="${79.8 + k * 5}" cy="192.6" r="1.1" fill="#b9f2ff"/>`;
    const ring = eq.ring && eq.ring !== 'none' ? eq.ring : null;
    if (ring) { const rc = { signet: '#c9ccd4', gold: '#e3b94f', pinky: '#f4fbff', champ: '#e3b94f' }[ring] || '#e3b94f';
      s += `<rect x="111" y="192.6" width="${ring === 'champ' ? 6 : 3.8}" height="${ring === 'champ' ? 4.4 : 2.6}" rx="1" fill="${rc}" stroke="${ring === 'pinky' ? '#9fd0ff' : '#8f6f33'}" stroke-width=".5"/>${ring === 'champ' ? '<circle cx="114" cy="194.8" r="1.4" fill="#c2362a"/>' : ring === 'pinky' ? '<circle cx="112.9" cy="193.9" r=".8" fill="#fff"/>' : ''}`; }
    // watch (left wrist)
    const wt = eq.watch || 'digital', band = { digital: '#222', steel: '#b9bec7', gold: '#d9b36c', iced: '#e9edf2', hyper: '#1a1a1a' }[wt] || '#222', face2 = { digital: '#3f5f3a', steel: '#1d3b6e', gold: '#f3e2b0', iced: '#f8fbff', hyper: '#2c2c2c' }[wt] || '#3f5f3a';
    s += `<g transform="rotate(-18 72 198)"><rect x="66" y="190" width="10" height="17" rx="2" fill="${band}"/><circle cx="71" cy="198.5" r="5.6" fill="${face2}" stroke="${band}" stroke-width="2"/><path d="M71 198.5 L71 195.4 M71 198.5 L73.2 199.6" stroke="#222" stroke-width=".7"/>${wt === 'iced' ? '<circle cx="71" cy="198.5" r="6.6" fill="none" stroke="#fff" stroke-width="1" stroke-dasharray="1 1.2"/>' : ''}${wt === 'hyper' ? '<circle cx="71" cy="198.5" r="2.4" fill="none" stroke="#d9b36c" stroke-width="1"/>' : ''}</g>`;
    // your bracelet, then a gold band for every title bracelet (right wrist)
    const wr = eq.wrist && eq.wrist !== 'none' ? eq.wrist : null;
    if (wr === 'beads') s += `<g transform="rotate(18 128 198)">${[0, 1, 2, 3, 4, 5].map((k) => `<circle cx="121.6" cy="${190 + k * 3}" r="1.7" fill="${['#2a2a2e', '#7a5a3a', '#2a2a2e', '#c9a03e', '#2a2a2e', '#7a5a3a'][k]}"/>`).join('')}</g>`;
    if (wr === 'gold') s += `<rect x="120" y="189" width="3.4" height="17" rx="1.6" fill="#e3b94f" stroke="#8f6f33" stroke-width=".5" stroke-dasharray="2 .8" transform="rotate(18 128 198)"/>`;
    if (wr === 'tennis') s += `<g transform="rotate(18 128 198)"><rect x="120.2" y="189" width="2.6" height="17" rx="1.2" fill="#d8dbe2"/>${[0, 1, 2, 3, 4].map((k) => `<circle cx="121.5" cy="${191 + k * 3.3}" r="1" fill="#fff"/>`).join('')}</g>`;
    for (let k = 0; k < bracelets; k++) s += `<rect x="${125 + k * 3.4}" y="${190 - k * 0.6}" width="2.6" height="16" rx="1.2" fill="#e8c35a" stroke="#9a7a2a" stroke-width=".5" transform="rotate(18 128 198)"/>`;
    // chips: the stack grows with your bankroll
    const tiers = [0, 1000, 10000, 50000, 250000, 1e6, 5e6], lv = tiers.filter((t) => br >= t).length;
    const cols = ['#f3ecdc', '#c2362a', '#2a6fdb', '#1e8f4a', '#141414', '#7a3fc0', '#e3b94f'];
    for (let c = 0; c < Math.min(3, Math.ceil(lv / 2)); c++) {
      const h = Math.max(2, lv * 2 - c * 3);
      for (let k = 0; k < h; k++) s += `<rect x="${150 + c * 14}" y="${210 - k * 3}" width="12" height="3" rx="1" fill="${cols[(k + c) % Math.min(cols.length, lv + 1)]}" stroke="#000" stroke-width=".4"/><rect x="${153 + c * 14}" y="${210.6 - k * 3}" width="2" height="1.8" fill="#fff" opacity=".5"/>`;
    }
  }
  return s + '</svg>';
}
