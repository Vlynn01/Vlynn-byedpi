$ErrorActionPreference = "Stop"

Write-Host "Preparing certificate for signing..."
$certName = "VlynnByeDPI"
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq "CN=$certName" } | Select-Object -First 1

if (-not $cert) {
    Write-Host "Generating new self-signed certificate..."
    $cert = New-SelfSignedCertificate -DnsName $certName -CertStoreLocation 'Cert:\CurrentUser\My' -Type CodeSigningCert
    
    try {
        $rootStore = New-Object System.Security.Cryptography.X509Certificates.X509Store('Root', 'CurrentUser')
        $rootStore.Open('ReadWrite')
        $rootStore.Add($cert)
        $rootStore.Close()
        
        $pubStore = New-Object System.Security.Cryptography.X509Certificates.X509Store('TrustedPublisher', 'CurrentUser')
        $pubStore.Open('ReadWrite')
        $pubStore.Add($cert)
        $pubStore.Close()
    } catch {
        Write-Warning "Could not add certificate to Root/TrustedPublisher store automatically: $_"
    }
}

Write-Host "Unblocking and signing x86_64 binaries..."
Get-ChildItem "x86_64\*" | Unblock-File -ErrorAction SilentlyContinue
if ($cert) {
    Set-AuthenticodeSignature -FilePath "x86_64\vlynnengine.exe" -Certificate $cert -ErrorAction SilentlyContinue | Out-Null
    Set-AuthenticodeSignature -FilePath "x86_64\WinDivert.dll" -Certificate $cert -ErrorAction SilentlyContinue | Out-Null
}

Write-Host "Compiling VlynnByeDPI_GUI.exe (with embedded x86_64 resources)..."
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
}
if (-not (Test-Path $csc)) {
    Write-Error "C# Compiler not found! Please ensure .NET Framework 4.0 or higher is installed."
    exit 1
}

if (Test-Path "VlynnByeDPI_GUI.exe") {
    Remove-Item "VlynnByeDPI_GUI.exe" -Force
}

$compileArgs = @(
    "/nologo",
    "/target:winexe",
    "/optimize+",
    "/out:VlynnByeDPI_GUI.exe",
    "/win32icon:x86_64\icon.ico",
    "/win32manifest:app.manifest",
    "/resource:x86_64\icon.ico,VlynnByeDPI.icon.ico",
    "/resource:x86_64\vlynnengine.exe,VlynnByeDPI.vlynnengine.exe",
    "/resource:x86_64\WinDivert.dll,VlynnByeDPI.WinDivert.dll",
    "/resource:x86_64\WinDivert64.sys,VlynnByeDPI.WinDivert64.sys",
    "gui_source.cs"
)

& $csc $compileArgs
if ($LASTEXITCODE -ne 0 -or -not (Test-Path "VlynnByeDPI_GUI.exe")) {
    Write-Error "Compilation failed."
    exit 1
}
Write-Host "Compiled successfully!"

if ($cert) {
    Write-Host "Signing VlynnByeDPI_GUI.exe..."
    Set-AuthenticodeSignature -FilePath "VlynnByeDPI_GUI.exe" -Certificate $cert -ErrorAction SilentlyContinue | Out-Null
}

Write-Host "Build and Sign complete! You can now run VlynnByeDPI_GUI.exe"
