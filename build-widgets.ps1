$root = $PSScriptRoot
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $csc)) {
    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path -LiteralPath $csc)) {
    throw "csc.exe not found"
}

Push-Location $root
try {
    & $csc /nologo /target:winexe /codepage:65001 /win32icon:quick-icon.ico /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /out:QuickList.exe WidgetCore.cs QuickList.cs
    if ($LASTEXITCODE -ne 0) { throw "Quick List build failed" }
    Write-Host "Built QuickList.exe"

    & $csc /nologo /target:winexe /codepage:65001 /win32icon:weeder-icon.ico /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /out:WeederList.exe WidgetCore.cs WeederList.cs
    if ($LASTEXITCODE -ne 0) { throw "Weeder List build failed" }
    Write-Host "Built WeederList.exe"
}
finally {
    Pop-Location
}
