# Sanity-checks app.ico rendering: transparency at corners, white digit pixels,
# emerald arrow pixels, and the indigo-to-cyan gradient.
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$icoPath = Join-Path $root 'src\NoMoreThaiNums\app.ico'
$bytes = [System.IO.File]::ReadAllBytes($icoPath)
$count = [BitConverter]::ToUInt16($bytes, 4)

# find the 256 entry (or largest)
$best = 0; $bestSize = 0
for ($i = 0; $i -lt $count; $i++) {
    $entry = 6 + 16 * $i
    $size = [BitConverter]::ToUInt32($bytes, $entry + 8)
    if ($size -gt $bestSize) { $bestSize = $size; $bestOffset = [BitConverter]::ToUInt32($bytes, $entry + 12); $bestLen = $size }
}
$png = New-Object byte[] $bestLen
[Array]::Copy($bytes, $bestOffset, $png, 0, $bestLen)
$ms = New-Object System.IO.MemoryStream(,$png)
$bmp = New-Object System.Drawing.Bitmap($ms)

$w = $bmp.Width; $h = $bmp.Height
$cornerTransparent = $true
foreach ($pt in @(@(2,2), @(($w-3),2), @(2,($h-3)), @(($w-3),($h-3)))) {
    if ($bmp.GetPixel($pt[0], $pt[1]).A -gt 10) { $cornerTransparent = $false }
}

$white = 0; $emerald = 0; $indigo = 0; $cyan = 0
for ($y = 0; $y -lt $h; $y += 2) {
    for ($x = 0; $x -lt $w; $x += 2) {
        $p = $bmp.GetPixel($x, $y)
        if ($p.A -lt 128) { continue }
        if ($p.R -gt 230 -and $p.G -gt 230 -and $p.B -gt 230) { $white++ }
        elseif ($p.G -gt 180 -and $p.B -gt 120 -and $p.R -lt 120) { $emerald++ }
        elseif ($p.R -gt 40 -and $p.R -lt 130 -and $p.B -gt 180) { $indigo++ }
        elseif ($p.G -gt 150 -and $p.B -gt 190 -and $p.R -lt 110) { $cyan++ }
    }
}

Write-Host "size: $w x $h"
Write-Host "corners transparent: $cornerTransparent"
Write-Host "white digit px: $white"
Write-Host "emerald arrow px: $emerald"
Write-Host "indigo px: $indigo"
Write-Host "cyan px: $cyan"
$bmp.Dispose()
