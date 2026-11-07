Add-Type -AssemblyName System.Drawing

$width = 432
$height = 432
$bmp = New-Object System.Drawing.Bitmap($width, $height)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
$g.Clear([System.Drawing.Color]::Transparent)

# Outer Glow & Shadow
$center = New-Object System.Drawing.PointF(216, 216)
$path = New-Object System.Drawing.Drawing2D.GraphicsPath

# Shield outline coordinates
# Top-left curve to top center, top center to top right, right straight down, curves down to bottom point
$pts = @(
    New-Object System.Drawing.PointF(216, 38),   # top center peak
    New-Object System.Drawing.PointF(372, 70),   # top right corner
    New-Object System.Drawing.PointF(372, 230),  # right edge
    New-Object System.Drawing.PointF(216, 394),  # bottom sharp point
    New-Object System.Drawing.PointF(60, 230),   # left edge
    New-Object System.Drawing.PointF(60, 70)     # top left corner
)

$path.AddPolygon($pts)

# Draw Dark Glass Shield Body with linear gradient (top to bottom)
$brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.PointF(0, 38)),
    (New-Object System.Drawing.PointF(0, 394)),
    [System.Drawing.Color]::FromArgb(240, 28, 28, 34),
    [System.Drawing.Color]::FromArgb(250, 10, 10, 12)
)
$g.FillPath($brush, $path)

# Subtle Inner Glass Highlight
$innerPath = New-Object System.Drawing.Drawing2D.GraphicsPath
$innerPts = @(
    New-Object System.Drawing.PointF(216, 52),
    New-Object System.Drawing.PointF(358, 82),
    New-Object System.Drawing.PointF(358, 224),
    New-Object System.Drawing.PointF(216, 376),
    New-Object System.Drawing.PointF(74, 224),
    New-Object System.Drawing.PointF(74, 82)
)
$innerPath.AddPolygon($innerPts)

$innerBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.PointF(0, 52)),
    (New-Object System.Drawing.PointF(0, 376)),
    [System.Drawing.Color]::FromArgb(50, 255, 255, 255),
    [System.Drawing.Color]::FromArgb(5, 255, 255, 255)
)
$g.FillPath($innerBrush, $innerPath)

# Crisp White / Silver Shield Border
$borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(230, 255, 255, 255), 8)
$borderPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$g.DrawPath($borderPen, $path)

# Inner Fine Border
$innerPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(70, 255, 255, 255), 2)
$innerPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$g.DrawPath($innerPen, $innerPath)

# Draw Modern Tech Nodes / Grid Lines
$gridPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(35, 255, 255, 255), 1.5)
$g.DrawLine($gridPen, 216, 52, 216, 376)
$g.DrawLine($gridPen, 74, 150, 358, 150)
$g.DrawLine($gridPen, 100, 270, 332, 270)

# Central Emblem: "STX" in Bold Clean Modern Font
$fontFamilies = @("Segoe UI Variable Display", "Segoe UI", "Arial Black", "Arial")
$chosenFont = "Arial"
foreach ($f in $fontFamilies) {
    try {
        $test = New-Object System.Drawing.Font($f, 10)
        $chosenFont = $f
        $test.Dispose()
        break
    } catch {}
}

$font = New-Object System.Drawing.Font($chosenFont, 62, [System.Drawing.FontStyle]::Bold)
$format = New-Object System.Drawing.StringFormat
$format.Alignment = [System.Drawing.StringAlignment]::Center
$format.LineAlignment = [System.Drawing.StringAlignment]::Center

# Text Glow / Shadow
$shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(120, 0, 0, 0))
$g.DrawString("STX", $font, $shadowBrush, 216, 206, $format)

# Text Pure Crisp White
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
$g.DrawString("STX", $font, $textBrush, 216, 202, $format)

# Subtitle: "V2RAY"
$subFont = New-Object System.Drawing.Font($chosenFont, 16, [System.Drawing.FontStyle]::Bold)
$subBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(200, 255, 255, 255))
$g.DrawString("V 2 R A Y", $subFont, $subBrush, 216, 268, $format)

# Save to logo path
$outPng = "wwwroot/assets/pxn-shield-logo-nHC0FFCc.png"
$bmp.Save($outPng, [System.Drawing.Imaging.ImageFormat]::Png)
Write-Output "Generated: $outPng"

# Also generate icon
$iconBmp = New-Object System.Drawing.Bitmap($bmp, 128, 128)
$iconHandle = $iconBmp.GetHicon()
$ico = [System.Drawing.Icon]::FromHandle($iconHandle)
$icoStream = [System.IO.File]::OpenWrite("src/PxnShield/Assets/pxnshield.ico")
$ico.Save($icoStream)
$icoStream.Close()
$ico.Dispose()
$iconBmp.Dispose()
Write-Output "Generated: src/PxnShield/Assets/pxnshield.ico"

$g.Dispose()
$bmp.Dispose()

