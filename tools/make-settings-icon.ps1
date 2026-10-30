# Одноразовый генератор иконки settings.png: розовая шестерёнка (глиф Segoe MDL2/Fluent Icons).
Add-Type -AssemblyName System.Drawing
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root "src\ColorLeaves.Revit\Resources\settings.png"
$size = 32
$bmp = New-Object System.Drawing.Bitmap($size, $size)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$g.Clear([System.Drawing.Color]::Transparent)
$fill = [System.Drawing.Color]::FromArgb(255, 242, 169, 196)
$outline = [System.Drawing.Color]::FromArgb(255, 201, 121, 159)
$font = $null
foreach ($name in @("Segoe Fluent Icons", "Segoe MDL2 Assets")) {
    $f = New-Object System.Drawing.Font($name, 23)
    if ($f.Name -eq $name) { $font = $f; break }
}
if ($font -ne $null) {
    $brush = New-Object System.Drawing.SolidBrush($fill)
    $fmt = New-Object System.Drawing.StringFormat
    $fmt.Alignment = [System.Drawing.StringAlignment]::Center
    $fmt.LineAlignment = [System.Drawing.StringAlignment]::Center
    $g.DrawString([string][char]0xE713, $font, $brush, (New-Object System.Drawing.RectangleF(0, 0, $size, $size)), $fmt)
    Write-Host "icon: glyph"
} else {
    Write-Host "icon: manual"
    $pen = New-Object System.Drawing.Pen($outline, 2)
    $brush = New-Object System.Drawing.SolidBrush($fill)
    $cx = 16.0
    $cy = 16.0
    for ($i = 0; $i -lt 8; $i++) {
        $st = $g.Save()
        $g.TranslateTransform($cx, $cy)
        $g.RotateTransform($i * 45.0)
        $g.FillRectangle($brush, -3.5, -15, 7, 8)
        $g.DrawRectangle($pen, -3.5, -15, 7, 8)
        $g.Restore($st)
    }
    $g.FillEllipse($brush, 5, 5, 22, 22)
    $g.DrawEllipse($pen, 5, 5, 22, 22)
    $white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
    $g.FillEllipse($white, 12, 12, 8, 8)
}
$g.Dispose()
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "saved: $out"
