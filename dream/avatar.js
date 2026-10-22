// ================================================================ THE DREAM: your player, drawn
// A vector bust at the table: face and hair from the creator; outfit, eyewear, headwear, chain,
// watch, sponsor patch, bracelets and rings from what you've earned; chips from your bankroll.

const DR_SKIN = ['#ffe0c2', '#f2c6a0', '#e2a676', '#c98852', '#a96a3c', '#87502c', '#663a1f', '#452514'];
const DR_HAIRC = ['#17130f', '#3e2717', '#6e4526', '#a8743a', '#e6c46e', '#b8432a', '#8e8e8e', '#ececec'];
const DR_HAIR = [['wavy', 'Wavy'], ['short', 'Textured crop'], ['fade', 'Fade'], ['slick', 'Slicked back'], ['curly', 'Curls'], ['locs', 'Locs'], ['long', 'Long'], ['buzz', 'Buzz'], ['bun', 'Man bun'], ['bald', 'Bald']];
const DR_EYEC = ['#4a2f1c', '#2c1d12', '#7a5a2a', '#3f6e8c', '#4f7a48', '#6e7680'];
const DR_FACIAL = [['none', 'Clean'], ['stubble', 'Stubble'], ['mustache', 'Mustache'], ['goatee', 'Goatee'], ['beard', 'Full beard']];
const DR_LOOK_DEFAULT = { skin: 2, hair: 'wavy', hairC: 0, facial: 'none', eyeC: 0 };

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
  else if (id === 'vampire') {
    s = `<path d="M30 222 C34 186 50 166 70 158 L60 132 C76 146 88 152 100 154 C112 152 124 146 140 132 L130 158 C150 166 166 186 170 222 Z" fill="#8a0f1e" stroke="#4a0610" stroke-width="1.5"/>`
      + torso('#141216', '#000') + `<path d="M86 152 L100 168 L114 152 L114 222 L86 222 Z" fill="#f4efe6"/><path d="M92 158 C96 166 104 166 108 158 L104 172 L96 172 Z" fill="#f4efe6" stroke="#cfc6b6"/>`
      + `<path d="M84 152 L98 204 L88 222 L58 222 C62 196 68 172 84 152 Z M116 152 L102 204 L112 222 L142 222 C138 196 132 172 116 152 Z" fill="#1f1a22" stroke="#000"/><circle cx="100" cy="176" r="4" fill="#b8142a" stroke="#e3b94f"/>`;
  } else if (id === 'flannel') {
    s = torso('#a3262a') + `<g opacity=".55" stroke="#1b0d0d">${[40, 64, 88, 112, 136, 160].map((x) => `<path d="M${x} 160 V222" stroke-width="5"/>`).join('')}${[172, 192, 212].map((y) => `<path d="M20 ${y} H180" stroke-width="5"/>`).join('')}</g>`
      + `<path d="M82 152 L100 168 L118 152 L120 162 L100 178 L80 162 Z" fill="#7e1b1e" stroke="#3a0c0c"/><path d="M100 170 L100 222" stroke="#3a0c0c" stroke-width="2"/>${[184, 198, 212].map((y) => `<circle cx="104" cy="${y}" r="1.6" fill="#e9e5dc"/>`).join('')}`;
  } else if (id === 'ugly') {
    s = torso('#1d6b3a') + fold('#175a30') + `<path d="M24 196 H176" stroke="#c2362a" stroke-width="9"/><g fill="#f3ecdc">${[34, 54, 74, 94, 114, 134, 154].map((x) => `<path d="M${x} 196 l4 -4 l4 4 l-4 4 Z"/>`).join('')}</g>`
      + `<path d="M78 152 C88 162 112 162 122 152 L124 160 C112 170 88 170 76 160 Z" fill="#c2362a"/><g fill="#f3ecdc">${[[60, 176], [100, 182], [140, 176], [80, 212], [120, 212]].map(([x, y]) => `<path d="M${x} ${y - 5} L${x + 1.4} ${y - 1.4} L${x + 5} ${y} L${x + 1.4} ${y + 1.4} L${x} ${y + 5} L${x - 1.4} ${y + 1.4} L${x - 5} ${y} L${x - 1.4} ${y - 1.4} Z"/>`).join('')}</g>`;
  }
  const p = DR_PATCH[patch];
  if (p) s += `<g transform="translate(44 172) rotate(6)"><rect width="30" height="13" rx="2" fill="#0c0c0c" stroke="${p.c}" stroke-width="1.4"/><text x="15" y="9.6" text-anchor="middle" font-family="Arial Black,Arial,sans-serif" font-weight="900" font-size="6.6" fill="${p.c}">${p.t}</text></g>`;
  return s;
}

// hair: drawn as shaded masses (dark underside, base, highlight strands), light from the upper left
function drHairSVG(style, c, back, id) {
  const d = drShade(c, -0.38), dd = drShade(c, -0.6), l = drShade(c, 0.28), g = `url(#${id}h)`;
  if (back) {
    if (style === 'long') return `<path d="M64 92 C58 128 62 158 74 170 L126 170 C138 158 142 128 136 92 Z" fill="${d}"/><path d="M70 120 C68 140 72 156 80 166 M130 120 C132 140 128 156 120 166" stroke="${dd}" stroke-width="2" fill="none" opacity=".6"/>`;
    if (style === 'curly') return `<path d="M58 98 C50 124 60 140 72 146 L128 146 C140 140 150 124 142 98 Z" fill="${d}"/>`;
    if (style === 'locs') return `<g stroke="${d}" stroke-width="6.5" stroke-linecap="round" fill="none">${[66, 72, 78, 122, 128, 134].map((x, i) => `<path d="M${x} 96 C${x - 2 + i} 120 ${x + 2} 140 ${x + (i < 3 ? -2 : 2)} ${152 + (i % 3) * 6}"/>`).join('')}</g>`;
    if (style === 'bun') return `<circle cx="100" cy="46" r="12" fill="${g}" stroke="${dd}" stroke-width="1"/><path d="M92 42 C96 38 104 38 108 42 M90 49 C96 53 104 53 110 49" stroke="${l}" stroke-width="1.4" fill="none" opacity=".7"/><path d="M90 54 C96 58 104 58 110 54" stroke="${dd}" stroke-width="3" fill="none"/>`;
    return '';
  }
  const side = (op) => `<path d="M68 92 C67 100 68 106 69 112 L72 112 C71 104 71 98 72 92 Z M132 92 C133 100 132 106 131 112 L128 112 C129 104 129 98 128 92 Z" fill="${c}" opacity="${op}"/>`;
  switch (style) {
    case 'short': return `${side(0.9)}<path d="M67 96 C64 70 79 53 100 52 C122 52 136 68 133 96 C131 86 128 80 124 76 C119 80 112 81 106 78 C99 82 90 82 83 78 C77 81 71 87 67 96 Z" fill="${g}"/>
      <path d="M76 70 l5 -6 M84 66 l6 -7 M93 63 l6 -7 M103 62 l6 -6 M113 64 l5 -6 M121 68 l4 -5" stroke="${l}" stroke-width="2" stroke-linecap="round" opacity=".55"/>
      <path d="M83 78 C90 82 99 82 106 78 C112 81 119 80 124 76" stroke="${dd}" stroke-width="1.6" fill="none" opacity=".6"/>`;
    case 'wavy': return `${side(0.85)}<path d="M66 100 C58 66 80 46 104 48 C130 50 144 70 135 100 C132 86 127 78 119 74 C112 84 97 88 84 83 C78 89 71 94 66 100 Z" fill="${g}"/>
      <path d="M74 74 C82 60 98 56 108 62 M88 80 C98 72 112 70 122 76 M104 52 C118 52 130 60 133 74" stroke="${l}" stroke-width="2.4" fill="none" stroke-linecap="round" opacity=".55"/>
      <path d="M84 83 C97 88 112 84 119 74" stroke="${dd}" stroke-width="2" fill="none" opacity=".7"/>`;
    case 'fade': return `<path d="M68 90 C68 100 69 108 71 114 L73 114 C72 106 72 98 73 90 Z M132 90 C132 100 131 108 129 114 L127 114 C128 106 128 98 127 90 Z" fill="${c}" opacity=".35"/>
      <path d="M70 90 C68 66 84 55 100 55 C117 55 133 66 130 90 C126 82 120 76 112 75 L88 75 C80 76 74 82 70 90 Z" fill="${g}"/>
      <path d="M70 90 C74 84 80 80 88 79 L112 79 C120 80 126 84 130 90" stroke="${c}" stroke-width="3" fill="none" opacity=".35"/>
      <path d="M82 66 C92 60 108 60 118 66" stroke="${l}" stroke-width="2" fill="none" opacity=".5"/>`;
    case 'slick': return `${side(0.9)}<path d="M67 94 C64 64 84 51 102 51 C123 51 137 64 133 94 C131 82 126 74 119 71 C107 67 93 67 82 71 C75 75 70 84 67 94 Z" fill="${g}"/>
      <path d="M80 64 C94 57 112 57 126 66 M78 71 C94 63 114 63 129 74 M86 59 C98 54 110 54 120 58" stroke="${l}" stroke-width="1.8" fill="none" opacity=".75"/>
      <path d="M82 71 C93 67 107 67 119 71" stroke="${dd}" stroke-width="1.4" fill="none" opacity=".6"/>`;
    case 'curly': {
      // a curly silhouette, then rows of small tight coils over it
      let coils = '', r = 0;
      const rnd = () => { r = (r * 9301 + 49297) % 233280; return r / 233280; };
      for (let row = 0; row < 5; row++) {
        const y = 54 + row * 8.5, half = [22, 32, 36, 37, 37][row];
        for (let x = 100 - half; x <= 100 + half; x += 6.4) {
          if (row >= 3 && x > 76 && x < 124) continue; // keep the forehead clear
          const xx = x + (row % 2) * 3.2 + (rnd() - 0.5) * 1.6, yy = y + (rnd() - 0.5) * 2;
          coils += `<circle cx="${xx.toFixed(1)}" cy="${yy.toFixed(1)}" r="4.3" fill="${g}"/><path d="M${(xx - 2.6).toFixed(1)} ${(yy + 0.6).toFixed(1)} a2.8 2.8 0 1 1 4.6 1.4" stroke="${dd}" stroke-width=".9" fill="none" opacity=".75"/><path d="M${(xx - 1.8).toFixed(1)} ${(yy - 1.6).toFixed(1)} a2 2 0 0 1 2.6 -.6" stroke="${l}" stroke-width=".9" fill="none" opacity=".75"/>`;
        }
      }
      return `<path d="M62 104 C56 70 78 48 100 48 C122 48 144 70 138 104 C134 92 128 84 122 80 C112 76 88 76 78 80 C72 84 66 92 62 104 Z" fill="${d}"/>${coils}`;
    }
    case 'long': return `<path d="M66 106 C58 66 80 50 100 51 C120 50 142 66 134 106 L133 134 C128 120 126 102 122 88 C114 78 106 72 100 66 C94 72 86 78 78 88 C74 102 72 120 67 134 Z" fill="${g}"/>
      <path d="M100 56 L100 68 M90 60 C84 72 78 88 74 110 M110 60 C116 72 122 88 126 110" stroke="${l}" stroke-width="1.8" fill="none" opacity=".55"/>
      <path d="M78 88 C86 78 94 72 100 66 C106 72 114 78 122 88" stroke="${dd}" stroke-width="1.6" fill="none" opacity=".6"/>`;
    case 'buzz': return `<path d="M70 94 C68 70 84 59 100 59 C117 59 133 70 130 94 C127 85 121 79 114 77 L86 77 C79 79 73 85 70 94 Z" fill="${c}" opacity=".55"/><path d="M70 94 C68 70 84 59 100 59 C117 59 133 70 130 94 C127 85 121 79 114 77 L86 77 C79 79 73 85 70 94 Z" fill="url(#${id}st)"/>`;
    case 'bun': return `${side(0.9)}<path d="M67 96 C64 66 82 54 100 54 C120 54 137 66 133 96 C129 80 117 71 100 71 C83 71 71 80 67 96 Z" fill="${g}"/><path d="M80 64 C92 58 108 58 120 64 M76 72 C90 64 110 64 124 72" stroke="${l}" stroke-width="1.6" fill="none" opacity=".7"/>`;
    case 'locs': return `<path d="M67 96 C64 66 82 53 100 53 C120 53 137 66 133 96 C129 82 118 74 100 73 C82 74 71 82 67 96 Z" fill="${g}"/>${[74, 82, 90, 98, 106, 114, 122].map((x) => `<path d="M${x} 60 C${x + 1} 66 ${x - 1} 72 ${x} 78" stroke="${dd}" stroke-width="1.2" fill="none" opacity=".7"/>`).join('')}`;
    case 'bald': return `<ellipse cx="88" cy="70" rx="13" ry="7" fill="#fff" opacity=".18" transform="rotate(-18 88 70)"/>`;
    default: return '';
  }
}

// facial hair: shaped masses in the hair colour, with grain; stubble is a stipple
function drFacialSVG(style, c, id) {
  const d = drShade(c, -0.35), l = drShade(c, 0.25);
  const stache = `<path d="M87 126 C91 121.5 97 121.5 100 124 C103 121.5 109 121.5 113 126 C109 128.4 104 127.6 100 126.6 C96 127.6 91 128.4 87 126 Z" fill="url(#${id}h)" stroke="${d}" stroke-width=".7"/><path d="M90 125 C93 123.6 96 123.6 98.4 124.6 M101.6 124.6 C104 123.6 107 123.6 110 125" stroke="${l}" stroke-width=".8" fill="none" opacity=".7"/>`;
  if (style === 'stubble') return `<path d="M72 112 C73 132 86 147 100 148 C114 147 127 132 128 112 C124 124 118 134 108 136 C104 133 96 133 92 136 C82 134 76 124 72 112 Z M88 125 C93 122 107 122 112 125 C108 127 92 127 88 125 Z" fill="${c}" opacity=".2"/><path d="M72 112 C73 132 86 147 100 148 C114 147 127 132 128 112 C124 124 118 134 108 136 C104 133 96 133 92 136 C82 134 76 124 72 112 Z M88 125 C93 122 107 122 112 125 C108 127 92 127 88 125 Z" fill="url(#${id}st)" opacity=".85"/>`;
  if (style === 'mustache') return stache;
  if (style === 'goatee') return `<path d="M93 133.5 C96 136.5 104 136.5 107 133.5 C107 141 104 146 100 147 C96 146 93 141 93 133.5 Z" fill="url(#${id}h)" stroke="${d}" stroke-width=".7"/><path d="M89 127 C89.5 131 91 133 93 133.5 M111 127 C110.5 131 109 133 107 133.5" stroke="${c}" stroke-width="2.2" fill="none" stroke-linecap="round"/>${stache}<path d="M98 137 L98 144 M102 137 L102 144" stroke="${l}" stroke-width=".8" opacity=".6"/>`;
  if (style === 'beard-top') return stache;
  if (style === 'beard' || style === 'beard-base') return `<path d="M69 104 C68 126 80 152 100 155 C120 152 132 126 131 104 C128 116 124 126 117 131 C111 134 106 131.5 100 131.5 C94 131.5 89 134 83 131 C76 126 72 116 69 104 Z" fill="url(#${id}h)" stroke="${d}" stroke-width=".8"/>
    <path d="M78 128 C82 138 88 144 94 148 M122 128 C118 138 112 144 106 148 M100 138 L100 152 M86 136 C88 142 92 146 96 150 M114 136 C112 142 108 146 104 150" stroke="${l}" stroke-width="1" fill="none" opacity=".5"/>
    <path d="M72 116 C76 128 82 133 86 134 M128 116 C124 128 118 133 114 134" stroke="${d}" stroke-width="1" fill="none" opacity=".7"/>${style === 'beard' ? stache : ''}`;
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
  if (id === 'masq') return `<path d="M72 100 C78 92 92 94 98 101 C100 103 100 103 102 101 C108 94 122 92 128 100 C126 110 116 114 108 110 C104 108 96 108 92 110 C84 114 74 110 72 100 Z" fill="#2a0b3a" stroke="#e3b94f" stroke-width="1.4"/><ellipse cx="87" cy="104" rx="5" ry="3" fill="#0b0609"/><ellipse cx="113" cy="104" rx="5" ry="3" fill="#0b0609"/><path d="M128 98 C136 88 140 80 138 72" stroke="#ff7a1a" stroke-width="2.4" fill="none"/>`;
  if (id === 'amber') return `<g><rect x="77" y="97" width="20" height="14" rx="6" fill="#e08a1e" opacity=".7" stroke="#6a3a10" stroke-width="1.8"/><rect x="103" y="97" width="20" height="14" rx="6" fill="#e08a1e" opacity=".7" stroke="#6a3a10" stroke-width="1.8"/><path d="M97 103 L103 103" stroke="#6a3a10" stroke-width="1.8"/></g>`;
  if (id === 'snow') return `<g><rect x="72" y="95" width="56" height="18" rx="9" fill="#cfefff" stroke="#f3f6fa" stroke-width="2.4"/><rect x="74" y="97" width="52" height="14" rx="7" fill="url(#snowg)"/><defs><linearGradient id="snowg" x1="0" x2="1"><stop offset="0" stop-color="#7fd0ff"/><stop offset=".5" stop-color="#c79bff"/><stop offset="1" stop-color="#ffb37f"/></linearGradient></defs><path d="M72 104 L66 102 M128 104 L134 102" stroke="#f3f6fa" stroke-width="3"/></g>`;
  return '';
}

function drHeadwearSVG(id, hairStyle) {
  if (id === 'cap') return `<path d="M66 86 C66 58 86 50 100 50 C116 50 134 58 134 86 Z" fill="#1d3b2c"/><path d="M64 86 C80 80 120 80 136 86 C136 92 64 92 64 86 Z" fill="#14281e"/><text x="100" y="76" text-anchor="middle" font-family="Arial Black,Arial,sans-serif" font-size="9" fill="#d9b36c">R</text>`;
  if (id === 'visor') return `<path d="M64 84 C80 74 120 74 136 84 L132 92 C118 86 82 86 68 92 Z" fill="#1e8f4a" opacity=".78" stroke="#0d4f28" stroke-width="1.2"/><path d="M68 84 C84 80 116 80 132 84" stroke="#dff5e6" stroke-width="1" fill="none" opacity=".5"/>`;
  if (id === 'pumpkin') return `<path d="M64 88 C60 54 82 44 100 44 C118 44 140 54 136 88 Z" fill="#ff7a1a" stroke="#b44c0a" stroke-width="1.5"/><path d="M84 50 C80 64 80 76 82 88 M116 50 C120 64 120 76 118 88 M100 44 V88" stroke="#d9600f" stroke-width="2" fill="none"/><path d="M98 44 C98 36 102 32 108 30" stroke="#3f6b2a" stroke-width="4" fill="none" stroke-linecap="round"/><path d="M82 66 l6 -6 l6 6 Z M106 66 l6 -6 l6 6 Z M84 76 Q100 86 116 76 l-4 4 l-4 -3 l-4 3 l-4 -3 l-4 3 l-4 -3 Z" fill="#2a1406"/>`;
  if (id === 'pilgrim') return `<rect x="74" y="30" width="52" height="52" rx="3" fill="#1a1a1c"/><path d="M58 86 C70 78 130 78 142 86 C142 92 58 92 58 86 Z" fill="#1a1a1c"/><rect x="74" y="66" width="52" height="9" fill="#5a3a20"/><rect x="92" y="64" width="16" height="13" rx="1" fill="none" stroke="#e3b94f" stroke-width="2.6"/>`;
  if (id === 'santa') return `<path d="M68 86 C70 56 92 44 112 48 C130 52 144 70 150 98 L138 100 C132 82 124 70 114 66 C106 70 92 78 68 86 Z" fill="#c2141e"/><circle cx="146" cy="102" r="8" fill="#f7f6f2"/><rect x="62" y="80" width="76" height="13" rx="6" fill="#f7f6f2"/>`;
  if (id === 'beanie') return `<path d="M66 88 C64 58 84 48 100 48 C118 48 136 58 134 88 Z" fill="#b8432a"/><rect x="64" y="80" width="72" height="12" rx="5" fill="#8a2f1d"/><circle cx="100" cy="46" r="7" fill="#e9e5dc"/>`;
  return '';
}

function drEyeSVG(cx, cy, flip, ec, id, k, blink) {
  // almond eye, coloured iris, upper lid and crease; flip mirrors for the right eye
  const m = (x) => (flip ? 2 * cx - x : x), P = (pts) => pts.map(([x, y]) => `${m(x).toFixed(1)} ${y}`);
  const [a, b, c2, d] = [P([[cx - 8, cy]]), P([[cx - 5, cy - 4.6]]), P([[cx + 5, cy - 4.2]]), P([[cx + 8, cy - 0.6]])];
  const shape = `M${a} C${b} ${c2} ${d} C${P([[cx + 4.6, cy + 3.4]])} ${P([[cx - 4, cy + 3.6]])} ${a} Z`;
  const ix = m(cx + 0.4);
  return `<defs><clipPath id="${id}e${k}"><path d="${shape}"/></clipPath><radialGradient id="${id}i${k}"><stop offset=".25" stop-color="${drShade(ec, 0.25)}"/><stop offset="1" stop-color="${drShade(ec, -0.45)}"/></radialGradient></defs>
    <g${blink}><path d="${shape}" fill="#f6f1ea"/><g clip-path="url(#${id}e${k})"><circle cx="${ix}" cy="${cy - 0.2}" r="3.9" fill="url(#${id}i${k})"/><circle cx="${ix}" cy="${cy - 0.2}" r="1.7" fill="#0d0907"/><circle cx="${(ix - 1.3).toFixed(1)}" cy="${cy - 1.5}" r="1" fill="#fff"/><circle cx="${(ix + 1.4).toFixed(1)}" cy="${cy + 1}" r=".45" fill="#fff" opacity=".8"/><path d="M${P([[cx - 9, cy - 5]])} L${P([[cx + 9, cy - 5]])} L${P([[cx + 9, cy - 1.6]])} C${P([[cx + 3, cy - 3.2]])} ${P([[cx - 3, cy - 3.4]])} ${P([[cx - 9, cy - 1.4]])} Z" fill="#000" opacity=".16"/></g>
    <path d="M${P([[cx - 8.6, cy + 0.4]])} C${P([[cx - 5, cy - 5.2]])} ${P([[cx + 5, cy - 4.8]])} ${P([[cx + 8.8, cy - 0.4]])}" stroke="#1a110b" stroke-width="1.7" fill="none" stroke-linecap="round"/>
    <path d="M${P([[cx - 6.5, cy - 6.6]])} C${P([[cx - 2, cy - 8.4]])} ${P([[cx + 3, cy - 8.2]])} ${P([[cx + 7, cy - 5.6]])}" stroke="#000" stroke-width=".8" fill="none" opacity=".18"/>
    <path d="M${P([[cx - 5, cy + 4]])} C${P([[cx - 1, cy + 5.2]])} ${P([[cx + 3, cy + 5]])} ${P([[cx + 6, cy + 3.4]])}" stroke="#000" stroke-width=".7" fill="none" opacity=".14"/></g>`;
}

function drAvatarSVG(st, opt = {}) {
  const look = { ...DR_LOOK_DEFAULT, ...(st.look || {}) }, eq = st.eq || {};
  const skin = DR_SKIN[look.skin] || DR_SKIN[2], sk2 = drShade(skin, -0.18), sk3 = drShade(skin, -0.32), skL = drShade(skin, 0.16);
  const hc = DR_HAIRC[look.hairC] || DR_HAIRC[0], ec = DR_EYEC[look.eyeC || 0] || DR_EYEC[0];
  const lip = drShade(look.skin >= 5 ? '#6a3428' : '#b8645a', look.skin >= 5 ? 0 : -0.1 * (look.skin / 4));
  const titles = st.titles || {}, br = st.br || 0, id = 'a' + Math.random().toString(36).slice(2, 7);
  const face = opt.face;
  const bracelets = Math.min(4, titles.bracelet || 0), rings = Math.min(4, (titles.ring || 0) + (titles.me || 0));
  const HEAD = 'M100 56 C120 56 132 70 132 92 C132 103 131 111 128 119 C125 131 116 143 104 147.5 Q100 149 96 147.5 C84 143 75 131 72 119 C69 111 68 103 68 92 C68 70 80 56 100 56 Z';
  let s = `<svg class="dravsvg" viewBox="${face ? '50 46 100 100' : '0 30 200 192'}" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="${esc(st.name || 'Your player')}">`;
  s += `<defs>
    <radialGradient id="${id}f" cx="38%" cy="34%" r="78%"><stop offset="0" stop-color="${skL}"/><stop offset=".55" stop-color="${skin}"/><stop offset="1" stop-color="${sk2}"/></radialGradient>
    <linearGradient id="${id}h" x1=".2" y1="0" x2=".8" y2="1"><stop offset="0" stop-color="${drShade(hc, 0.18)}"/><stop offset=".55" stop-color="${hc}"/><stop offset="1" stop-color="${drShade(hc, -0.4)}"/></linearGradient>
    <linearGradient id="${id}t" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#fff" stop-opacity=".14"/><stop offset=".45" stop-color="#fff" stop-opacity="0"/><stop offset=".7" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity=".32"/></linearGradient>
    <linearGradient id="${id}n" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="${sk3}"/><stop offset=".5" stop-color="${sk2}"/></linearGradient>
    <pattern id="${id}st" width="7" height="7" patternUnits="userSpaceOnUse">${[[0.6, 0.9], [2.9, 0.4], [5.2, 1.3], [1.7, 2.6], [4.1, 3.1], [6.3, 3.6], [0.4, 4.4], [2.6, 5.2], [4.9, 5.6], [6.4, 6.4], [1.4, 6.6], [3.6, 2], [5.8, 4.8]].map(([x, y], i) => `<circle cx="${x}" cy="${y}" r="${0.34 + (i % 3) * 0.06}" fill="${drShade(hc, -0.15)}" opacity="${0.65 + (i % 4) * 0.1}"/>`).join('')}</pattern>
    <clipPath id="${id}hd"><path d="${HEAD}"/></clipPath></defs>`;
  s += drHairSVG(look.hair, hc, true, id);
  // neck: shadowed under the jaw
  s += `<path d="M87 128 L113 128 L115 160 C107 166 93 166 85 160 Z" fill="url(#${id}n)"/><path d="M88 150 C94 154 106 154 112 150" stroke="${sk3}" stroke-width="1" fill="none" opacity=".5"/>`;
  s += drOutfitSVG(eq.outfit || 'office', eq.patch);
  s += `<path d="M14 222 C18 178 46 160 76 154 L124 154 C154 160 182 178 186 222 Z" fill="url(#${id}t)" pointer-events="none"/>`;
  if (eq.head === 'phones') s += `<path d="M72 160 C70 144 130 144 128 160" stroke="#222" stroke-width="5" fill="none"/><rect x="64" y="150" width="14" height="18" rx="6" fill="#2b2b2e"/><rect x="122" y="150" width="14" height="18" rx="6" fill="#2b2b2e"/>`;
  if (eq.chain && eq.chain !== 'none') {
    const c = eq.chain === 'silver' ? '#c9ccd4' : eq.chain === 'bones' ? 'none' : eq.chain === 'lights' ? '#1d4a22' : '#e3b94f', w = eq.chain === 'silver' || eq.chain === 'lights' ? 1.6 : eq.chain === 'wishbone' ? 1.4 : 3.4;
    s += `<path d="M84 156 C88 176 112 176 116 156" stroke="${c}" stroke-width="${w}" fill="none" ${['iced', 'cuban'].includes(eq.chain) ? 'stroke-dasharray="3 1.4"' : ''}/>`;
    if (['cuban', 'iced'].includes(eq.chain)) s += `<path d="M84 156 C88 176 112 176 116 156" stroke="#fff6cf" stroke-width=".8" fill="none" opacity=".6" stroke-dasharray="1.5 2.9"/>`;
    if (eq.chain === 'bones') s += [[86, 162], [91, 168], [97, 171], [103, 171], [109, 168], [114, 162]].map(([x, y]) => `<g transform="translate(${x} ${y})"><rect x="-2.6" y="-1" width="5.2" height="2" fill="#efe8d6"/><circle cx="-2.6" cy="-1" r="1.2" fill="#efe8d6"/><circle cx="-2.6" cy="1" r="1.2" fill="#efe8d6"/><circle cx="2.6" cy="-1" r="1.2" fill="#efe8d6"/><circle cx="2.6" cy="1" r="1.2" fill="#efe8d6"/></g>`).join('') + `<circle cx="100" cy="178" r="4.4" fill="#efe8d6"/><circle cx="98.4" cy="177" r="1.1" fill="#111"/><circle cx="101.6" cy="177" r="1.1" fill="#111"/>`;
    if (eq.chain === 'wishbone') s += `<path d="M100 186 L94 174 M100 186 L106 174 M100 186 L100 190" stroke="#e3b94f" stroke-width="2.4" stroke-linecap="round" fill="none"/>`;
    if (eq.chain === 'lights') s += [[86, 162], [90, 168], [95, 171], [100, 172], [105, 171], [110, 168], [114, 162]].map(([x, y], i) => `<ellipse cx="${x}" cy="${y + 2}" rx="1.6" ry="2.4" fill="${['#ff4f4f', '#5cbf64', '#ffd23a', '#5aa9de'][i % 4]}"/>`).join('');
    if (eq.chain === 'iced') s += [[90, 168], [100, 172], [110, 168], [95, 171], [105, 171]].map(([x, y]) => `<circle cx="${x}" cy="${y}" r="1.2" fill="#fff"/>`).join('');
  }
  // ears
  s += `<g fill="${sk2}"><path d="M69 98 C62 95 60 104 62 110 C63 116 67 120 71 118 Z"/><path d="M131 98 C138 95 140 104 138 110 C137 116 133 120 129 118 Z"/></g>
    <path d="M67 102 C64 104 64 110 67 114 M133 102 C136 104 136 110 133 114" stroke="${sk3}" stroke-width="1.2" fill="none" opacity=".7"/>`;
  // head with form shading: light from the upper left
  s += `<path d="${HEAD}" fill="url(#${id}f)"/>
    <g clip-path="url(#${id}hd)">
      <path d="M113 56 C127 62 133 78 133 94 C133 116 124 140 104 149 C117 134 122 116 121 98 C120 80 118 66 113 56 Z" fill="${sk3}" opacity=".32"/>
      <path d="M74 118 C80 134 90 144 100 146 C110 144 120 134 126 118 C120 136 110 150 100 150 C90 150 80 136 74 118 Z" fill="${sk3}" opacity=".35"/>
      <ellipse cx="88" cy="73" rx="14" ry="6.5" fill="#fff" opacity=".13" transform="rotate(-12 88 73)"/>
      <ellipse cx="81" cy="115" rx="7" ry="4" fill="#e0705a" opacity=".13"/><ellipse cx="119" cy="115" rx="7" ry="4" fill="#e0705a" opacity=".1"/>
      <path d="M76 110 C78 118 82 122 86 123" stroke="${sk3}" stroke-width="1.2" fill="none" opacity=".25"/><path d="M124 110 C122 118 118 122 114 123" stroke="${sk3}" stroke-width="1.2" fill="none" opacity=".3"/>
    </g>`;
  if (look.facial === 'beard') s += drFacialSVG('beard-base', hc, id);
  if (look.facial === 'stubble') s += drFacialSVG('stubble', hc, id);
  // eyes and brows: a little confident, one brow a touch higher
  const blink = opt.blink ? ' class="drblink"' : '';
  s += drEyeSVG(87, 103, false, ec, id, 0, blink) + drEyeSVG(113, 103, true, ec, id, 1, blink);
  const bc = drShade(hc, look.hairC >= 6 ? -0.25 : 0.04);
  s += `<path d="M77 94.5 C82 90.6 90 90 96.5 92.6 C96.8 93.6 96.4 94.4 95.6 94.4 C89.5 93 83 93.8 78 96.6 C77 96.4 76.6 95.4 77 94.5 Z" fill="${bc}"/>
    <path d="M123 93.6 C118 89.4 110 89 103.5 91.8 C103.2 92.8 103.6 93.6 104.4 93.6 C110.5 92 117 92.8 122 95.6 C123 95.4 123.4 94.4 123 93.6 Z" fill="${bc}"/>`;
  // nose: bridge shadow on the far side, soft tip, nostrils
  s += `<path d="M103.5 99 C104.6 106 105.6 112 107 116.5" stroke="${sk3}" stroke-width="1.5" fill="none" opacity=".45" stroke-linecap="round"/>
    <path d="M94.5 116.5 C95.5 120.8 104.5 120.8 105.8 116.5" stroke="${sk3}" stroke-width="1.5" fill="none" opacity=".7" stroke-linecap="round"/>
    <ellipse cx="96.8" cy="118.6" rx="1.6" ry=".9" fill="${drShade(skin, -0.5)}" opacity=".55"/><ellipse cx="103.4" cy="118.6" rx="1.6" ry=".9" fill="${drShade(skin, -0.5)}" opacity=".55"/>
    <ellipse cx="99" cy="116" rx="2.2" ry="1.4" fill="#fff" opacity=".2"/>`;
  // mouth: a poker smirk by default
  const mouth = opt.mouth || (opt.smile ? 'smile' : 'smug');
  if (mouth === 'smile') s += `<path d="M89 127.5 C95 135.5 106 135.5 112 127 C106 130 95 130.4 89 127.5 Z" fill="#fff"/><path d="M89 127.5 C95 135.5 106 135.5 112 127" stroke="${drShade(lip, -0.35)}" stroke-width="1.6" fill="none" stroke-linecap="round"/><path d="M93 132.6 C97 134.6 104 134.6 108 132" stroke="${lip}" stroke-width="1.4" fill="none" opacity=".6"/>`;
  else s += `<path d="M91.5 131 C95 134.4 103 134.6 107.5 130.6 C103 132 96 132.4 91.5 131 Z" fill="${lip}" opacity=".55"/>
    <path d="${mouth === 'flat' ? 'M90.5 129.4 C95 130.6 103 130.6 109.5 129' : 'M90.5 129.6 C95.5 130.8 103 130.2 110.5 126.8'}" stroke="${drShade(lip, -0.42)}" stroke-width="1.6" fill="none" stroke-linecap="round"/>
    <path d="M96 126.4 C98 127.3 102 127.3 104 126.4" stroke="${sk3}" stroke-width=".9" fill="none" opacity=".35"/><ellipse cx="100" cy="136.8" rx="5" ry="1.4" fill="${sk3}" opacity=".18"/>`;
  if (look.facial !== 'stubble' && look.facial !== 'none') s += drFacialSVG(look.facial === 'beard' ? 'beard-top' : look.facial, hc, id);
  s += drHairSVG(look.hair, hc, false, id);
  s += drEyewearSVG(eq.eyes);
  s += drHeadwearSVG(eq.head, look.hair);
  if (!face) {
    // the table, your arms, cards, watch, bracelets, rings, chips
    s += `<path d="M0 194 L200 194 L200 222 L0 222 Z" fill="#0e4d39"/><path d="M0 188 C60 182 140 182 200 188 L200 197 C140 191 60 191 0 197 Z" fill="#3a2414"/><path d="M0 189 C60 183 140 183 200 189" stroke="#6a4626" stroke-width="1.2" fill="none"/>`;
    const sleeve = { office: '#cdd8e6', hoodie: '#6b6f76', polo: '#1f2a44', bomber: '#1b1b1e', blazer: '#3b3f47', suit: '#1b2442', designer: '#4a1426', tux: '#121214', vampire: '#141216', flannel: '#a3262a', ugly: '#1d6b3a' }[eq.outfit || 'office'] || '#3b3f47';
    s += `<path d="M10 222 C20 204 46 192 74 190 L80 202 C58 206 40 212 30 222 Z" fill="${sleeve}" stroke="${drShade(sleeve, -0.35)}"/><path d="M14 218 C24 206 44 198 70 194" stroke="#fff" stroke-width="1.4" fill="none" opacity=".12"/>`;
    s += `<path d="M190 222 C180 204 154 192 126 190 L120 202 C142 206 160 212 170 222 Z" fill="${drShade(sleeve, -0.12)}" stroke="${drShade(sleeve, -0.35)}"/>`;
    s += `<g><rect x="86" y="196" width="15" height="21" rx="2" fill="#f7f3e8" stroke="#bbb" transform="rotate(-8 93 206)"/><rect x="98" y="196" width="15" height="21" rx="2" fill="#f7f3e8" stroke="#bbb" transform="rotate(7 105 206)"/><path d="M92 200 l3 4 l-3 4 l-3 -4 Z" fill="#c2362a" transform="rotate(-8 93 206)"/><text x="101.5" y="205" font-family="Georgia,serif" font-size="7" font-weight="700" fill="#141414" transform="rotate(7 105 206)">A</text></g>`;
    // hands resting on the cards: knuckles, thumb, shading
    const hand = (cx, flip) => { const f = flip ? -1 : 1, X = (x) => cx + f * x;
      return `<path d="M${X(-13)} 199 C${X(-13)} 192 ${X(-6)} 190 ${X(2)} 191 C${X(9)} 192 ${X(13)} 196 ${X(12)} 201 C${X(11)} 206 ${X(4)} 208 ${X(-3)} 207.5 C${X(-10)} 207 ${X(-13)} 204 ${X(-13)} 199 Z" fill="${skin}" stroke="${sk3}" stroke-width=".9"/>
        <path d="M${X(-6)} 192.5 L${X(-6.6)} 199 M${X(-1)} 191.6 L${X(-1.4)} 199.6 M${X(4)} 191.8 L${X(4.2)} 199.6" stroke="${sk3}" stroke-width=".8" opacity=".55"/>
        <path d="M${X(-12)} 201 C${X(-6)} 205 ${X(4)} 205 ${X(11)} 201" stroke="${sk3}" stroke-width=".8" fill="none" opacity=".35"/>
        <path d="M${X(10)} 196 C${X(14)} 196 ${X(16)} 199 ${X(14)} 202" stroke="${sk3}" stroke-width="1" fill="${sk2}"/>`; };
    s += hand(86, false) + hand(114, true);
    // watch (left wrist)
    const wt = eq.watch || 'digital', band = { digital: '#222', steel: '#b9bec7', gold: '#d9b36c', iced: '#e9edf2', hyper: '#1a1a1a' }[wt], face2 = { digital: '#3f5f3a', steel: '#1d3b6e', gold: '#f3e2b0', iced: '#f8fbff', hyper: '#2c2c2c' }[wt];
    s += `<g transform="rotate(-18 72 198)"><rect x="66" y="190" width="10" height="17" rx="2" fill="${band}"/><circle cx="71" cy="198.5" r="5.6" fill="${face2}" stroke="${band}" stroke-width="2"/><path d="M71 198.5 L71 195.4 M71 198.5 L73.2 199.6" stroke="#222" stroke-width=".7"/>${wt === 'iced' ? '<circle cx="71" cy="198.5" r="6.6" fill="none" stroke="#fff" stroke-width="1" stroke-dasharray="1 1.2"/>' : ''}${wt === 'hyper' ? '<circle cx="71" cy="198.5" r="2.4" fill="none" stroke="#d9b36c" stroke-width="1"/>' : ''}</g>`;
    for (let k = 0; k < bracelets; k++) s += `<rect x="${124 + k * 3.4}" y="${190 - k * 0.6}" width="2.6" height="16" rx="1.2" fill="#e8c35a" stroke="#9a7a2a" stroke-width=".5" transform="rotate(18 128 198)"/>`;
    for (let k = 0; k < rings; k++) s += `<rect x="${92 + k * 5}" y="196.4" width="3.2" height="2.2" rx=".8" fill="#e8c35a" stroke="#fff" stroke-width=".4"/>`;
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
