$root = $PSScriptRoot
$quick = Join-Path $root "QuickList.exe"
$weeder = Join-Path $root "WeederList.exe"
$zip = Join-Path $root "FloatingWidgets.zip"

& (Join-Path $root "build-widgets.ps1")

if (-not (Test-Path -LiteralPath $quick) -or -not (Test-Path -LiteralPath $weeder)) {
    throw "Missing EXE files. Build failed."
}

$temp = Join-Path $env:TEMP ("FloatingWidgets_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    Copy-Item -LiteralPath $quick -Destination $temp
    Copy-Item -LiteralPath $weeder -Destination $temp
    Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue
    Compress-Archive -Path (Join-Path $temp "*") -DestinationPath $zip -CompressionLevel Optimal
    Write-Host "Created: $zip"
}
finally {
    Remove-Item -LiteralPath $temp -Recurse -Force -ErrorAction SilentlyContinue
}
