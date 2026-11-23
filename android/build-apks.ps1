$ErrorActionPreference = "Stop"

$androidRoot = $PSScriptRoot
$repoRoot = Split-Path $androidRoot -Parent
$docsRoot = Join-Path $repoRoot "docs"
$distRoot = Join-Path $androidRoot "dist"
$keyDir = Join-Path $androidRoot "keys"
$keystore = Join-Path $keyDir "monotask-release.keystore"
$passwordFile = Join-Path $keyDir "password.txt"

function Find-Jdk {
    $envPath = $env:JAVA_HOME
    if ($envPath -and (Test-Path -LiteralPath (Join-Path $envPath "bin\javac.exe"))) {
        return $envPath
    }
    $roots = @(
        (Join-Path $env:LOCALAPPDATA "Android\jdk17"),
        (Join-Path $env:ProgramFiles "Microsoft"),
        (Join-Path $env:ProgramFiles "Eclipse Adoptium")
    )
    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root)) { continue }
        $candidates = Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue |
            Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "bin\javac.exe") }
        if ($root.EndsWith("jdk17")) {
            $candidates = Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue |
                Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "bin\javac.exe") }
        }
        $found = $candidates | Select-Object -First 1
        if ($found) { return $found.FullName }
    }
    throw "JDK 17 not found. Set JAVA_HOME to a JDK with javac.exe."
}

function Find-Sdk {
    $candidates = @($env:ANDROID_HOME, $env:ANDROID_SDK_ROOT,
        (Join-Path $env:LOCALAPPDATA "Android\Sdk"), "C:\Android\Sdk")
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath (Join-Path $candidate "platforms"))) {
            return $candidate
        }
    }
    throw "Android SDK not found. Set ANDROID_HOME."
}

function Write-Utf8([string]$path, [string]$content) {
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $encoding)
}

function Escape-Xml([string]$value) {
    return [System.Security.SecurityElement]::Escape($value)
}

function Stage-Assets([string]$source, [string]$key, [string]$destination) {
    $sourceFull = (Resolve-Path -LiteralPath $source).Path
    $files = Get-ChildItem -LiteralPath $sourceFull -Recurse -File
    foreach ($file in $files) {
        $relative = $file.FullName.Substring($sourceFull.Length).TrimStart('\', '/')
        $relative = $relative.Replace('\', '/')
        $flat = "mt__" + $key + "__" + $relative.Replace('/', '__')
        Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $destination $flat)
    }
}

$jdk = Find-Jdk
$sdk = Find-Sdk
$buildTools = Get-ChildItem -LiteralPath (Join-Path $sdk "build-tools") -Directory |
    Sort-Object Name -Descending |
    Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "aapt2.exe") } |
    Select-Object -First 1
if (-not $buildTools) { throw "Android build-tools not found." }
$buildTools = $buildTools.FullName

$platform = Get-ChildItem -LiteralPath (Join-Path $sdk "platforms") -Directory |
    Sort-Object Name -Descending |
    Where-Object { Test-Path -LiteralPath (Join-Path $_.FullName "android.jar") } |
    Select-Object -First 1
if (-not $platform) { throw "Android platform android.jar not found." }
$androidJar = Join-Path $platform.FullName "android.jar"

$javac = Join-Path $jdk "bin\javac.exe"
$jar = Join-Path $jdk "bin\jar.exe"
$java = Join-Path $jdk "bin\java.exe"
$keytool = Join-Path $jdk "bin\keytool.exe"
$aapt2 = Join-Path $buildTools "aapt2.exe"
$zipalign = Join-Path $buildTools "zipalign.exe"
$d8Jar = Join-Path $buildTools "lib\d8.jar"
$apksignerJar = Join-Path $buildTools "lib\apksigner.jar"

$env:JAVA_HOME = $jdk
$env:PATH = (Join-Path $jdk "bin") + ";" + $buildTools + ";" + $env:PATH

if (-not (Test-Path -LiteralPath $keystore)) {
    New-Item -ItemType Directory -Path $keyDir -Force | Out-Null
    $password = [Guid]::NewGuid().ToString("N").Substring(0, 24)
    Write-Utf8 $passwordFile $password
    & $keytool -genkeypair -v -keystore $keystore -alias monotask `
        -keyalg RSA -keysize 2048 -validity 10000 `
        -storepass $password -keypass $password `
        -dname "CN=Monotask, OU=Mobile, O=Monotask, L=Local, ST=Local, C=CN" `
        -noprompt | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Failed to create signing key." }
} elseif (-not (Test-Path -LiteralPath $passwordFile)) {
    throw "Signing key password file is missing: $passwordFile"
}
$storePassword = (Get-Content -LiteralPath $passwordFile -Raw).Trim()

New-Item -ItemType Directory -Path $distRoot -Force | Out-Null

$apps = @(
    @{
        Key = "quick"
        Name = [regex]::Unescape("\u5FEB\u6377\u6E05\u5355")
        Package = "com.monotask.quick"
        Path = "quick"
        Port = 48765
        Background = "#15161A"
        Accent = "#E7A85C"
        Output = "Monotask-QuickList.apk"
    },
    @{
        Key = "weeder"
        Name = [regex]::Unescape("\u9664\u8349\u5668")
        Package = "com.monotask.weeder"
        Path = "weeder"
        Port = 48766
        Background = "#121713"
        Accent = "#74C08E"
        Output = "Monotask-Weeder.apk"
    }
)

$javaSources = @(
    (Join-Path $androidRoot "src\com\monotask\shell\MainActivity.java"),
    (Join-Path $androidRoot "src\com\monotask\shell\LocalServer.java")
)

foreach ($app in $apps) {
    $work = Join-Path $androidRoot ("build\" + $app.Key)
    $res = Join-Path $work "res"
    $assets = Join-Path $work "assets"
    $gen = Join-Path $work "gen"
    $classes = Join-Path $work "classes"
    $dex = Join-Path $work "dex"

    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
    New-Item -ItemType Directory -Path $res, $assets, $gen, $classes, $dex -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $res "values"), (Join-Path $res "mipmap-xxxhdpi") -Force | Out-Null

    $label = Escape-Xml $app.Name
    Write-Utf8 (Join-Path $res "values\strings.xml") @"
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <string name="app_name">$label</string>
</resources>
"@

    Write-Utf8 (Join-Path $res "values\colors.xml") @"
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <color name="app_bg">$($app.Background)</color>
    <color name="app_accent">$($app.Accent)</color>
</resources>
"@

    Write-Utf8 (Join-Path $res "values\styles.xml") @"
<?xml version="1.0" encoding="utf-8"?>
<resources>
    <style name="AppTheme" parent="@android:style/Theme.Material.NoActionBar">
        <item name="android:windowBackground">@color/app_bg</item>
        <item name="android:statusBarColor">@color/app_bg</item>
        <item name="android:navigationBarColor">@color/app_bg</item>
        <item name="android:windowLightStatusBar">false</item>
        <item name="android:windowLightNavigationBar">false</item>
        <item name="android:colorAccent">@color/app_accent</item>
    </style>
</resources>
"@

    $manifest = @"
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android"
    package="$($app.Package)"
    android:versionCode="1"
    android:versionName="1.0">
    <uses-sdk android:minSdkVersion="23" android:targetSdkVersion="34" />
    <uses-permission android:name="android.permission.INTERNET" />
    <application
        android:allowBackup="false"
        android:hardwareAccelerated="true"
        android:icon="@mipmap/ic_launcher"
        android:label="@string/app_name"
        android:roundIcon="@mipmap/ic_launcher"
        android:supportsRtl="true"
        android:theme="@style/AppTheme"
        android:usesCleartextTraffic="true">
        <activity
            android:name="com.monotask.shell.MainActivity"
            android:configChanges="orientation|screenSize|smallestScreenSize|screenLayout|keyboardHidden|uiMode"
            android:exported="true"
            android:launchMode="singleTask"
            android:windowSoftInputMode="adjustResize">
            <intent-filter>
                <action android:name="android.intent.action.MAIN" />
                <category android:name="android.intent.category.LAUNCHER" />
            </intent-filter>
            <meta-data android:name="monotask.path" android:value="$($app.Path)" />
            <meta-data android:name="monotask.port" android:value="$($app.Port)" />
            <meta-data android:name="monotask.bg" android:resource="@color/app_bg" />
        </activity>
    </application>
</manifest>
"@
    Write-Utf8 (Join-Path $work "AndroidManifest.xml") $manifest

    $iconSource = Join-Path $docsRoot ($app.Key + "\icons\icon-192.png")
    if (-not (Test-Path -LiteralPath $iconSource)) { throw "Missing icon: $iconSource" }
    Copy-Item -LiteralPath $iconSource -Destination (Join-Path $res "mipmap-xxxhdpi\ic_launcher.png")

    Stage-Assets (Join-Path $docsRoot "shared") "shared" $assets
    Stage-Assets (Join-Path $docsRoot $app.Key) $app.Key $assets

    $compiled = Join-Path $work "resources.zip"
    & $aapt2 compile --dir $res -o $compiled
    if ($LASTEXITCODE -ne 0) { throw "aapt2 compile failed for $($app.Name)" }

    $unsigned = Join-Path $work "unsigned.apk"
    & $aapt2 link -o $unsigned -I $androidJar --manifest (Join-Path $work "AndroidManifest.xml") `
        -A $assets --java $gen -R $compiled --auto-add-overlay `
        --min-sdk-version 23 --target-sdk-version 34
    if ($LASTEXITCODE -ne 0) { throw "aapt2 link failed for $($app.Name)" }

    $rJava = Get-ChildItem -LiteralPath $gen -Recurse -Filter "R.java" | ForEach-Object { $_.FullName }
    $sourceFiles = @($javaSources) + @($rJava)
    & $javac -encoding UTF-8 -source 8 -target 8 -bootclasspath $androidJar -classpath $androidJar `
        -d $classes $sourceFiles
    if ($LASTEXITCODE -ne 0) { throw "javac failed for $($app.Name)" }

    $classesJar = Join-Path $work "classes.jar"
    & $jar cf $classesJar -C $classes .
    if ($LASTEXITCODE -ne 0) { throw "jar failed for $($app.Name)" }

    & $java -cp $d8Jar com.android.tools.r8.D8 --lib $androidJar --min-api 23 --output $dex $classesJar
    if ($LASTEXITCODE -ne 0) { throw "d8 failed for $($app.Name)" }

    & $jar uf $unsigned -C $dex "classes.dex"
    if ($LASTEXITCODE -ne 0) { throw "Failed to add classes.dex for $($app.Name)" }

    $aligned = Join-Path $work "aligned.apk"
    & $zipalign -f -p 4 $unsigned $aligned
    if ($LASTEXITCODE -ne 0) { throw "zipalign failed for $($app.Name)" }

    $output = Join-Path $distRoot $app.Output
    & $java -jar $apksignerJar sign --ks $keystore --ks-key-alias monotask `
        --ks-pass ("pass:" + $storePassword) --key-pass ("pass:" + $storePassword) `
        --out $output $aligned
    if ($LASTEXITCODE -ne 0) { throw "apksigner failed for $($app.Name)" }

    & $java -jar $apksignerJar verify --verbose --print-certs $output | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "APK verification failed for $($app.Name)" }

    $size = [Math]::Round((Get-Item -LiteralPath $output).Length / 1KB, 1)
    Write-Host ("Built {0} ({1} KB)" -f $app.Output, $size)
}

Write-Host "APK output directory: $distRoot"
