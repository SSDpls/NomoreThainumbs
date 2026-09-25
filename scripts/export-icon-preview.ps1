# Extracts PNG images from src/NoMoreThaiNums/app.ico and writes a small HTML
# preview page (artifacts/icon-preview.html) with the images embedded as base64.
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$icoPath = Join-Path $root 'src\NoMoreThaiNums\app.ico'
$outDir = Join-Path $root 'artifacts'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$bytes = [System.IO.File]::ReadAllBytes($icoPath)
$count = [BitConverter]::ToUInt16($bytes, 4)
$offset = 6 + 16 * $count

$images = @()
for ($i = 0; $i -lt $count; $i++) {
    $entry = 6 + 16 * $i
    $size = [BitConverter]::ToUInt32($bytes, $entry + 8)
    $dataOffset = [BitConverter]::ToUInt32($bytes, $entry + 12)
    $png = New-Object byte[] $size
    [Array]::Copy($bytes, $dataOffset, $png, 0, $size)
    $images += ,@($size, $png)
}

$html = New-Object System.Text.StringBuilder
[void]$html.AppendLine('<!doctype html><html><head><meta charset="utf-8"><title>icon preview</title>')
[void]$html.AppendLine('<style>body{font-family:Segoe UI,sans-serif;background:#1e293b;color:#e2e8f0;padding:24px}')
[void]$html.AppendLine('.row{display:flex;gap:28px;align-items:flex-end;flex-wrap:wrap}.item{text-align:center}')
[void]$html.AppendLine('.dark{background:#0f172a;display:inline-block;padding:10px;border-radius:8px}</style></head><body>')
[void]$html.AppendLine('<h2>No more Thai Nums — icon preview</h2><div class="row">')

foreach ($img in $images) {
    $size = $img[0]; $png = $img[1]
    $b64 = [Convert]::ToBase64String($png)
    [void]$html.AppendLine("<div class='item'><div class='dark'><img src='data:image/png;base64,$b64' width='$size' height='$size'></div><div>$($size)x$($size)</div></div>")
}

[void]$html.AppendLine('</div><p>Light background check:</p><div class="row" style="background:#f1f5f9;padding:12px;border-radius:8px">')
foreach ($img in $images) {
    $size = $img[0]; $png = $img[1]
    if ($size -ge 64) {
        $b64 = [Convert]::ToBase64String($png)
        [void]$html.AppendLine("<div class='item'><img src='data:image/png;base64,$b64' width='$size' height='$size'><div>$($size)x$($size)</div></div>")
    }
}

[void]$html.AppendLine('</div></body></html>')
[System.IO.File]::WriteAllText((Join-Path $outDir 'icon-preview.html'), $html.ToString())
Write-Host "Preview written to artifacts\icon-preview.html"
