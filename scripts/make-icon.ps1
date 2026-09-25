# Generates app.ico for No more Thai Nums: a modern rounded-square tile with a
# vibrant indigo-to-cyan gradient, a faint Thai numeral pattern, a bold white
# "9" (the Thai ๙ becomes 9), and an emerald green arrow sweeping down-right.
# Sizes render at 4x and are downscaled with HighQualityBicubic for crisp
# small icons.
Add-Type -AssemblyName System.Drawing

function New-AppTile {
    param([int]$Size)

    $S = $Size * 4
    $bmp = New-Object System.Drawing.Bitmap($S, $S)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    $g.Clear([System.Drawing.Color]::Transparent)

    # ---- rounded-square tile ----
    $margin = [Math]::Max(1, [int]($S * 0.02))
    $rect = New-Object System.Drawing.Rectangle($margin, $margin, ($S - 2 * $margin), ($S - 2 * $margin))
    $radius = [int]($S * 0.24)

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    # diagonal gradient: indigo -> violet -> cyan
    $gradient = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $rect,
        [System.Drawing.Color]::FromArgb(255, 79, 70, 229),
        [System.Drawing.Color]::FromArgb(255, 34, 211, 238),
        35.0)

    # subtle diagonal highlight on the upper-left half
    $hlPath = New-Object System.Drawing.Drawing2D.GraphicsPath
    $hlPath.AddEllipse(-([int]($S * 0.35)), -([int]($S * 0.45)), [int]($S * 1.35), [int]($S * 1.35))
    $hl = New-Object System.Drawing.Drawing2D.PathGradientBrush($hlPath)
    $hl.CenterColor = [System.Drawing.Color]::FromArgb(70, 255, 255, 255)
    $hl.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 255, 255, 255))
    $g.FillPath($gradient, $path)
    $g.SetClip($path)
    $g.FillPath($hl, $hlPath)
    $g.ResetClip()

    # ---- faint Thai numerals background pattern ----
    $patternFont = New-Object System.Drawing.Font('Leelawadee UI', [float]($S * 0.30), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $ghost = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(30, 255, 255, 255))
    $g.DrawString([string][char]0x0E59, $patternFont, $ghost, [float]($S * -0.08), [float]($S * 0.18), $fmt)
    $g.DrawString([string][char]0x0E50, $patternFont, $ghost, [float]($S * 0.45), [float]($S * 0.55), $fmt)

    # ---- bold white "9" ----
    $digitFont = New-Object System.Drawing.Font('Segoe UI', [float]($S * 0.74), [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
    $white = [System.Drawing.Brushes]::White
    $fmt2 = New-Object System.Drawing.StringFormat
    $fmt2.Alignment = [System.Drawing.StringAlignment]::Center
    $fmt2.LineAlignment = [System.Drawing.StringAlignment]::Center
    $digitRect = New-Object System.Drawing.RectangleF([float]($S * -0.02), [float]($S * -0.10), [float]$S, [float]$S)
    $g.DrawString('9', $digitFont, $white, $digitRect, $fmt2)

    # ---- emerald arrow (conversion direction) ----
    $penW = [float]($S * 0.085)
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 52, 211, 153), $penW)
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawArc($pen, [float]($S * 0.50), [float]($S * 0.48), [float]($S * 0.34), [float]($S * 0.34), 235, 115)
    $arrowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 52, 211, 153))
    $head = @(
        (New-Object System.Drawing.PointF([float]($S * 0.955), [float]($S * 0.800))),
        (New-Object System.Drawing.PointF([float]($S * 0.700), [float]($S * 0.840))),
        (New-Object System.Drawing.PointF([float]($S * 0.880), [float]($S * 1.010)))
    )
    $g.FillPolygon($arrowBrush, $head)

    $g.Dispose()

    # downscale to target size
    $out = New-Object System.Drawing.Bitmap($Size, $Size)
    $og = [System.Drawing.Graphics]::FromImage($out)
    $og.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $og.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $og.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $og.DrawImage($bmp, 0, 0, $Size, $Size)
    $og.Dispose()
    $bmp.Dispose()
    return $out
}

# StringFormat used for the ghost glyphs (declare before first use)
$fmt = New-Object System.Drawing.StringFormat
$fmt.Alignment = [System.Drawing.StringAlignment]::Center
$fmt.LineAlignment = [System.Drawing.StringAlignment]::Center

$sizes = @(16, 24, 32, 48, 64, 128, 256)
$bitmaps = @{}
foreach ($s in $sizes) {
    $bitmaps[$s] = New-AppTile -Size $s
}

$root = Split-Path -Parent $PSScriptRoot
$outPath = Join-Path $root 'src\NoMoreThaiNums\app.ico'
$fs = [System.IO.File]::Create($outPath)

# ICONDIR: reserved, type=1 (icon), count
$fs.Write([BitConverter]::GetBytes([uint16]0), 0, 2)
$fs.Write([BitConverter]::GetBytes([uint16]1), 0, 2)
$fs.Write([BitConverter]::GetBytes([uint16]$sizes.Count), 0, 2)

$offset = 6 + 16 * $sizes.Count
foreach ($s in $sizes) {
    $ms = New-Object System.IO.MemoryStream
    $bitmaps[$s].Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $png = $ms.ToArray()
    $dim = if ($s -ge 256) { 0 } else { $s }
    $fs.Write([BitConverter]::GetBytes([byte]$dim), 0, 1)   # width
    $fs.Write([BitConverter]::GetBytes([byte]$dim), 0, 1)   # height
    $fs.Write([BitConverter]::GetBytes([byte]0), 0, 1)      # palette
    $fs.Write([BitConverter]::GetBytes([byte]0), 0, 1)      # reserved
    $fs.Write([BitConverter]::GetBytes([uint16]1), 0, 2)    # color planes
    $fs.Write([BitConverter]::GetBytes([uint16]32), 0, 2)   # bits per pixel
    $fs.Write([BitConverter]::GetBytes([uint32]$png.Length), 0, 4)
    $fs.Write([BitConverter]::GetBytes([uint32]$offset), 0, 4)
    $offset += $png.Length
    $ms.Dispose()
}

foreach ($s in $sizes) {
    $ms = New-Object System.IO.MemoryStream
    $bitmaps[$s].Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $fs.Write($ms.ToArray(), 0, $ms.ToArray().Length)
    $ms.Dispose()
}

$fs.Dispose()
Write-Host "Icon written to $outPath"
