$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot
$androidDir = Join-Path $projectRoot "android"
$appMain = Join-Path $androidDir "app\src\main"
$buildDir = Join-Path $androidDir "build\direct"
$outApk = Join-Path $projectRoot "VlynnByeDPI_Android16.apk"

# Locate Android SDK
$sdkDir = $env:ANDROID_HOME
if (-not $sdkDir -or -not (Test-Path $sdkDir)) {
    $sdkDir = $env:ANDROID_SDK_ROOT
}
if (-not $sdkDir -or -not (Test-Path $sdkDir)) {
    $sdkDir = Join-Path $env:LOCALAPPDATA "Android\Sdk"
}

if (-not (Test-Path $sdkDir)) {
    Write-Error "Android SDK not found at $sdkDir. Please install Android SDK (or run 'android sdk install platforms/android-36 build-tools')."
    exit 1
}

Write-Host "Using Android SDK: $sdkDir"

# Find latest build-tools and platform (prefer android-36 for Android 16)
$buildToolsDir = Get-ChildItem (Join-Path $sdkDir "build-tools") -Directory | Sort-Object Name -Descending | Select-Object -First 1
$platformDir = Get-ChildItem (Join-Path $sdkDir "platforms") -Directory | Sort-Object Name -Descending | Select-Object -First 1

if (-not $buildToolsDir -or -not $platformDir) {
    Write-Error "Android build-tools or platforms not found in $sdkDir."
    exit 1
}

Write-Host "Using Build-Tools: $($buildToolsDir.Name) | Platform: $($platformDir.Name)"

$aapt2 = Join-Path $buildToolsDir.FullName "aapt2.exe"
$d8 = Join-Path $buildToolsDir.FullName "d8.bat"
$zipalign = Join-Path $buildToolsDir.FullName "zipalign.exe"
$apksigner = Join-Path $buildToolsDir.FullName "apksigner.bat"
$androidJar = Join-Path $platformDir.FullName "android.jar"

# Clean & prepare build directories
if (Test-Path $buildDir) {
    Remove-Item $buildDir -Recurse -Force
}
$compiledResDir = Join-Path $buildDir "compiled_res"
$genJavaDir = Join-Path $buildDir "gen"
$classesDir = Join-Path $buildDir "classes"
$dexDir = Join-Path $buildDir "dex"
New-Item -ItemType Directory -Path $compiledResDir, $genJavaDir, $classesDir, $dexDir -Force | Out-Null

Write-Host "1/5 Compiling Android resources with aapt2..."
$resFiles = Get-ChildItem (Join-Path $appMain "res") -Recurse -File
foreach ($rf in $resFiles) {
    & $aapt2 compile $rf.FullName -o $compiledResDir
    if ($LASTEXITCODE -ne 0) { throw "aapt2 compile failed on $($rf.FullName)" }
}

$unalignedApk = Join-Path $buildDir "unaligned.apk"
$flatFiles = Get-ChildItem $compiledResDir -Filter "*.flat" | ForEach-Object { $_.FullName }
$manifestPath = Join-Path $appMain "AndroidManifest.xml"

& $aapt2 link -o $unalignedApk -I $androidJar --manifest $manifestPath --java $genJavaDir --min-sdk-version 26 --target-sdk-version 36 --version-code 11 --version-name "1.1.0" $flatFiles
if ($LASTEXITCODE -ne 0) { throw "aapt2 link failed" }

Write-Host "2/5 Compiling Java sources (Java 17 bytecode)..."
$javaFiles = @()
$javaFiles += Get-ChildItem (Join-Path $appMain "java") -Recurse -Filter "*.java" | ForEach-Object { $_.FullName }
$javaFiles += Get-ChildItem $genJavaDir -Recurse -Filter "*.java" | ForEach-Object { $_.FullName }

& javac -source 17 -target 17 -encoding UTF-8 -cp $androidJar -d $classesDir $javaFiles
if ($LASTEXITCODE -ne 0) { throw "javac compilation failed" }

Write-Host "3/5 Converting bytecode to DEX with d8..."
$classFiles = Get-ChildItem $classesDir -Recurse -Filter "*.class" | ForEach-Object { $_.FullName }
& $d8 --min-api 26 --lib $androidJar --output $dexDir $classFiles
if ($LASTEXITCODE -ne 0) { throw "d8 failed" }

# Add classes.dex into the APK archive
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::Open($unalignedApk, [System.IO.Compression.ZipArchiveMode]::Update)
[System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, (Join-Path $dexDir "classes.dex"), "classes.dex", [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
$zip.Dispose()

Write-Host "4/5 Aligning APK with zipalign..."
$alignedApk = Join-Path $buildDir "aligned.apk"
& $zipalign -f -p 4 $unalignedApk $alignedApk
if ($LASTEXITCODE -ne 0) { throw "zipalign failed" }

Write-Host "5/5 Signing APK (v1 + v2 + v3 + v4 scheme for Android 16)..."
$keystorePath = Join-Path $buildDir "vlynn_release.jks"
$prevErrPref = $ErrorActionPreference
$ErrorActionPreference = "Continue"
& keytool -genkeypair -keystore $keystorePath -alias vlynnkey -keyalg RSA -keysize 2048 -validity 10000 -storepass vlynn123 -keypass vlynn123 -dname "CN=Vlynn, OU=VlynnByeDPI, O=Vlynn, L=VN, S=VN, C=VN" *> $null
$ErrorActionPreference = $prevErrPref

& $apksigner sign --ks $keystorePath --ks-key-alias vlynnkey --ks-pass pass:vlynn123 --key-pass pass:vlynn123 --out $outApk $alignedApk
if ($LASTEXITCODE -ne 0) { throw "apksigner failed" }

Write-Host "SUCCESS! Built Android 16 APK at: $outApk"
Get-Item $outApk | Select-Object FullName, Length, LastWriteTime
