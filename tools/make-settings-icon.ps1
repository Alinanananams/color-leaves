# Генератор иконки settings.png: розовая шестерёнка (глиф Segoe MDL2/Fluent Icons).
# Глиф рендерится в большом буфере, автокадрируется по непрозрачным пикселям
# и точно центрируется в итоговом 32x32 — иконка не зависит от метрик шрифта.
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "src\ColorLeaves.Revit\Resources\settings.png"

$finalSize = 32
$margin = 1
$bigSize = 128
$fill = [System.Drawing.Color]::FromArgb(255, 242, 169, 196)
$outline = [System.Drawing.Color]::FromArgb(255, 201, 121, 159)

# --- 1. Большой буфер, рендер глифа ---
$big = New-Object System.Drawing.Bitmap($bigSize, $bigSize)
$g = [System.Drawing.Graphics]::FromImage($big)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$g.Clear([System.Drawing.Color]::Transparent)
$font = $null
foreach ($name in @("Segoe Fluent Icons", "Segoe MDL2 Assets")) {
    $f = New-Object System.Drawing.Font($name, 92)
    if ($f.Name -eq $name) { $font = $f; break }
}
if ($font -eq $null) { throw "Не найден шрифт Segoe Fluent Icons / Segoe MDL2 Assets" }
$brush = New-Object System.Drawing.SolidBrush($fill)
$fmt = New-Object System.Drawing.StringFormat
$fmt.Alignment = [System.Drawing.StringAlignment]::Center
$fmt.LineAlignment = [System.Drawing.StringAlignment]::Center
$g.DrawString([string][char]0xE713, $font, $brush, (New-Object System.Drawing.RectangleF(0, 0, $bigSize, $bigSize)), $fmt)
$g.Dispose()

# --- 2. Границы непрозрачной области ---
$minX = $bigSize; $minY = $bigSize; $maxX = -1; $maxY = -1
for ($y = 0; $y -lt $bigSize; $y++) {
    for ($x = 0; $x -lt $bigSize; $x++) {
        if ($big.GetPixel($x, $y).A -gt 8) {
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }
}
if ($maxX -lt 0) { $big.Dispose(); throw "Глиф отрисовался пустым" }
$contentW = $maxX - $minX + 1
$contentH = $maxY - $minY + 1
Write-Host ("Глиф: {0}x{1} от ({2};{3})" -f $contentW, $contentH, $minX, $minY)

# --- 3. Итоговая иконка: контент по центру с полем ---
$bmp = New-Object System.Drawing.Bitmap($finalSize, $finalSize)
$g2 = [System.Drawing.Graphics]::FromImage($bmp)
$g2.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g2.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$g2.Clear([System.Drawing.Color]::Transparent)
$drawW = $finalSize - 2 * $margin
$drawH = $finalSize - 2 * $margin
$srcRect = New-Object System.Drawing.Rectangle($minX, $minY, $contentW, $contentH)
$dstRect = New-Object System.Drawing.Rectangle($margin, $margin, $drawW, $drawH)
$g2.DrawImage($big, $dstRect, $srcRect, [System.Drawing.GraphicsUnit]::Pixel)
$g2.Dispose()
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
$big.Dispose()
Write-Host "saved: $out"
