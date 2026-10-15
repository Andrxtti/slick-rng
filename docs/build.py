# Build the installable app from the solver page: python3 build.py
import re, pathlib
root = pathlib.Path(__file__).parent
app = (root / '../app/slick-rng.html').read_text()
ver = re.search(r'ALPHA ([0-9.]+)', app).group(1)
splash = (root / 'splash.html').read_text()
startup = ''.join(
    f'<link rel="apple-touch-startup-image" media="(device-width: {w}px) and (device-height: {h}px) and (-webkit-device-pixel-ratio: {r}) and (orientation: portrait)" href="icons/splash-{w*r}x{h*r}.png">\n'
    for w, h, r in [(430, 932, 3), (393, 852, 3), (390, 844, 3), (428, 926, 3), (375, 812, 3), (414, 896, 2), (375, 667, 2)])
head = f'''<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<meta name="theme-color" content="#090908">
<meta name="description" content="Confirmed Fire Poker · Slick RNG Solver. Free multiway poker solver with strict tournament ICM and a bubble trainer.">
<meta name="apple-mobile-web-app-capable" content="yes">
<meta name="mobile-web-app-capable" content="yes">
<meta name="apple-mobile-web-app-status-bar-style" content="black-translucent">
<meta name="apple-mobile-web-app-title" content="CF Poker">
<link rel="manifest" href="manifest.webmanifest">
<link rel="icon" type="image/png" sizes="32x32" href="icons/favicon-32.png">
<link rel="apple-touch-icon" href="icons/apple-touch-icon.png">
{startup}<style>html,body{{background:#090908;color-scheme:dark}}</style>
</head>
<body>
'''
(root / 'index.html').write_text(head + splash + '\n' + app + '\n</body>\n</html>\n')
sw = (root / 'sw.src.js').read_text().replace('__VERSION__', ver)
(root / 'sw.js').write_text(sw)
print('built', ver)
