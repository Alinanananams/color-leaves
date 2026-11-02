# Одноразовая генерация DOCS/screenshots/ribbon.png: вкладка плагина на ленте Revit.
# Рисуется из реальных иконок плагина + подписи кнопок (Segoe UI), как в ленте.
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$res = Join-Path $root "src\ColorLeaves.Revit\Resources"
$out = Join-Path $root "DOCS\screenshots\ribbon.png"

$buttonW = 80
$height = 80
$bg = [System.Drawing.Color]::FromArgb(255, 240, 240, 240)
$sep = [System.Drawing.Color]::FromArgb(255, 213, 213, 213)
$textColor = [System.Drawing.Color]::FromArgb(255, 59, 59, 59)

$bmp = New-Object System.Drawing.Bitmap((3 * $buttonW), $height)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
$g.Clear($bg)

$buttons = @(
    @{ Icon = "sheet.png";     Text = "Показать`nлисты" },
    @{ Icon = "parameter.png"; Text = "Создать`nпараметр" },
    @{ Icon = "settings.png";  Text = "Настройки" }
)

$iconBox = 28
$font = New-Object System.Drawing.Font("Segoe UI", 9)
$brush = New-Object System.Drawing.SolidBrush($textColor)
$fmt = New-Object System.Drawing.StringFormat
$fmt.Alignment = [System.Drawing.StringAlignment]::Center
$fmt.LineAlignment = [System.Drawing.StringAlignment]::Center

for ($i = 0; $i -lt $buttons.Count; $i++) {
    $x0 = $i * $buttonW
    $icon = [System.Drawing.Image]::FromFile((Join-Path $res $buttons[$i].Icon))
    $g.DrawImage($icon, ($x0 + [int](($buttonW - $iconBox) / 2)), 6, $iconBox, $iconBox)
    $icon.Dispose()
    $textRect = New-Object System.Drawing.RectangleF($x0, 36, $buttonW, ($height - 38))
    $g.DrawString($buttons[$i].Text, $font, $brush, $textRect, $fmt)
    if ($i -gt 0) {
        $pen = New-Object System.Drawing.Pen($sep)
        $g.DrawLine($pen, $x0, 2, $x0, ($height - 2))
        $pen.Dispose()
    }
}
$g.Dispose()
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
Write-Host "saved: $out"
