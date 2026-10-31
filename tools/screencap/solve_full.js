// Screenshot of the PC's desktop taken by Windows itself (PowerShell + .NET CopyFromScreen), for when the remote
// screen view comes back blank. Worker runs "node solve_full.js <cfg> <secs> <evs> <out>"; the PNG goes to shot.png
// next to this file and <out> gets the screen geometry (logical pixels, the same frame mouse clicks use).
const fs = require('fs'), path = require('path'), { spawnSync } = require('child_process');
const OUT = process.argv[5], png = path.join(__dirname, 'shot.png');
const ps = `
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
$b = [System.Windows.Forms.SystemInformation]::VirtualScreen
$bmp = New-Object System.Drawing.Bitmap $b.Width, $b.Height
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($b.Left, $b.Top, 0, 0, $bmp.Size)
$bmp.Save('${png.replace(/\\/g, '\\\\')}', [System.Drawing.Imaging.ImageFormat]::Png)
$mons = [System.Windows.Forms.Screen]::AllScreens | ForEach-Object { "$($_.DeviceName) primary=$($_.Primary) bounds=$($_.Bounds)" }
"virtual=$($b.Left),$($b.Top),$($b.Width)x$($b.Height)"
$mons
`;
const r = spawnSync('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-Command', ps], { encoding: 'utf8', timeout: 60000, windowsHide: true });
fs.writeFileSync(OUT, JSON.stringify({ code: r.status, out: r.stdout, err: (r.stderr || '').slice(-2000), png: fs.existsSync(png), bytes: fs.existsSync(png) ? fs.statSync(png).size : 0 }, null, 1));
