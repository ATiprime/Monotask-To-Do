Add-Type -AssemblyName System.Drawing

function New-RoundedIcon([int]$size, [System.Drawing.Color]$accent, [System.Drawing.Color]$background, [bool]$fullBleed) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    if ($fullBleed) {
        $rect = New-Object System.Drawing.RectangleF 0, 0, $size, $size
        $path = New-Object System.Drawing.Drawing2D.GraphicsPath
        $path.AddRectangle($rect)
        $inset = $size * 0.18
    } else {
        $margin = $size * 0.04
        $rect = New-Object System.Drawing.RectangleF $margin, $margin, ($size - 2 * $margin), ($size - 2 * $margin)
        $radius = $size * 0.22
        $d = $radius * 2
        $path = New-Object System.Drawing.Drawing2D.GraphicsPath
        $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
        $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
        $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
        $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
        $path.CloseFigure()
        $inset = $size * 0.24
    }

    $bgBrush = New-Object System.Drawing.SolidBrush -ArgumentList $background
    $g.FillPath($bgBrush, $path)

    if (-not $fullBleed) {
        $border = [System.Drawing.Color]::FromArgb(70, 255, 255, 255)
        $borderPen = New-Object System.Drawing.Pen -ArgumentList $border, ([Math]::Max(1, $size * 0.008))
        $g.DrawPath($borderPen, $path)
        $borderPen.Dispose()
    }

    $pen = New-Object System.Drawing.Pen -ArgumentList $accent, ([Math]::Max(2, $size * 0.085))
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round

    $left = $size * 0.28
    $midX = $size * 0.44
    $right = $size * 0.73
    $top = $size * 0.34
    $midY = $size * 0.66
    $bottom = $size * 0.53

    $points = [System.Drawing.PointF[]]@(
        (New-Object System.Drawing.PointF -ArgumentList $left, $bottom),
        (New-Object System.Drawing.PointF -ArgumentList $midX, $midY),
        (New-Object System.Drawing.PointF -ArgumentList $right, $top)
    )
    $g.DrawLines($pen, $points)

    $pen.Dispose()
    $bgBrush.Dispose()
    $path.Dispose()
    $g.Dispose()
    return $bmp
}

function Save-Icon([string]$path, [int]$size, [System.Drawing.Color]$accent, [System.Drawing.Color]$background, [bool]$fullBleed) {
    $bmp = New-RoundedIcon $size $accent $background $fullBleed
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

function New-AppIcons([string]$folder, [System.Drawing.Color]$accent, [System.Drawing.Color]$background) {
    $dir = Join-Path $folder "icons"
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    Save-Icon (Join-Path $dir "icon-192.png") 192 $accent $background $false
    Save-Icon (Join-Path $dir "icon-512.png") 512 $accent $background $false
    Save-Icon (Join-Path $dir "icon-maskable-512.png") 512 $accent $background $true
    Save-Icon (Join-Path $dir "apple-touch-icon.png") 180 $accent $background $true
}

$root = $PSScriptRoot
New-AppIcons (Join-Path $root "quick") ([System.Drawing.Color]::FromArgb(231, 168, 92)) ([System.Drawing.Color]::FromArgb(24, 26, 31))
New-AppIcons (Join-Path $root "weeder") ([System.Drawing.Color]::FromArgb(116, 192, 142)) ([System.Drawing.Color]::FromArgb(19, 25, 21))
Write-Host "Mobile icons generated"
