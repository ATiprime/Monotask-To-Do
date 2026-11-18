Add-Type -AssemblyName System.Drawing

function New-IconBitmap([int]$size, [System.Drawing.Color]$accent) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $margin = [Math]::Max(1, [int]($size * 0.055))
    $rect = New-Object System.Drawing.RectangleF $margin, $margin, ($size - 2 * $margin), ($size - 2 * $margin)
    $radius = $size * 0.22
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $path.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $path.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $path.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()

    $bg = [System.Drawing.Color]::FromArgb(24, 26, 31)
    $border = [System.Drawing.Color]::FromArgb(55, 59, 69)
    $bgBrush = New-Object System.Drawing.SolidBrush -ArgumentList $bg
    $borderPen = New-Object System.Drawing.Pen -ArgumentList $border, ([Math]::Max(1, $size * 0.018))
    $g.FillPath($bgBrush, $path)
    $g.DrawPath($borderPen, $path)

    $pen = New-Object System.Drawing.Pen -ArgumentList $accent, ([Math]::Max(2, $size * 0.095))
    $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
    $points = [System.Drawing.PointF[]]@(
        (New-Object System.Drawing.PointF -ArgumentList ($size * 0.26), ($size * 0.53)),
        (New-Object System.Drawing.PointF -ArgumentList ($size * 0.43), ($size * 0.69)),
        (New-Object System.Drawing.PointF -ArgumentList ($size * 0.75), ($size * 0.33))
    )
    $g.DrawLines($pen, $points)

    $pen.Dispose()
    $borderPen.Dispose()
    $bgBrush.Dispose()
    $path.Dispose()
    $g.Dispose()
    return $bmp
}

function Save-Icon([string]$file, [System.Drawing.Color]$accent) {
    $sizes = @(256, 64, 32, 16)
    $images = New-Object System.Collections.ArrayList
    foreach ($size in $sizes) {
        $bmp = New-IconBitmap $size $accent
        $ms = New-Object System.IO.MemoryStream
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        [void]$images.Add($ms.ToArray())
        $ms.Dispose()
        $bmp.Dispose()
    }

    $stream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter $stream
    $writer.Write([UInt16]0)
    $writer.Write([UInt16]1)
    $writer.Write([UInt16]$sizes.Count)

    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $size = $sizes[$i]
        $dim = if ($size -ge 256) { 0 } else { $size }
        $writer.Write([Byte]$dim)
        $writer.Write([Byte]$dim)
        $writer.Write([Byte]0)
        $writer.Write([Byte]0)
        $writer.Write([UInt16]1)
        $writer.Write([UInt16]32)
        $writer.Write([UInt32]$images[$i].Length)
        $writer.Write([UInt32]$offset)
        $offset += $images[$i].Length
    }
    foreach ($image in $images) {
        $writer.Write($image)
    }
    $writer.Flush()
    [System.IO.File]::WriteAllBytes($file, $stream.ToArray())
    $writer.Dispose()
    $stream.Dispose()
}

$root = $PSScriptRoot
Save-Icon (Join-Path $root "quick-icon.ico") ([System.Drawing.Color]::FromArgb(231, 168, 92))
Save-Icon (Join-Path $root "weeder-icon.ico") ([System.Drawing.Color]::FromArgb(116, 192, 142))
Write-Host "Icons generated"
