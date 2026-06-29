$ErrorActionPreference = "Stop"

Write-Host "Compiling VlynnByeDPI_GUI.exe..."
$csc = "C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    Write-Error "C# Compiler not found! Please ensure .NET Framework 4.0 or higher is installed."
    exit 1
}

$compileArgs = "/target:winexe /out:VlynnByeDPI_GUI.exe /win32icon:x86_64\icon.ico /win32manifest:app.manifest gui_source.cs"
Start-Process -FilePath $csc -ArgumentList $compileArgs -Wait -NoNewWindow

if (-not (Test-Path "VlynnByeDPI_GUI.exe")) {
    Write-Error "Compilation failed."
    exit 1
}
Write-Host "Compiled successfully!"

Write-Host "Signing executable to prevent Windows Smart App Control issues..."
$certName = "VlynnByeDPI"
$cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq "CN=$certName" } | Select-Object -First 1

if (-not $cert) {
    Write-Host "Generating new self-signed certificate..."
    $cert = New-SelfSignedCertificate -DnsName $certName -CertStoreLocation 'Cert:\CurrentUser\My' -Type CodeSigningCert
    
    $rootStore = New-Object System.Security.Cryptography.X509Certificates.X509Store('Root', 'CurrentUser')
    $rootStore.Open('ReadWrite')
    $rootStore.Add($cert)
    $rootStore.Close()
    
    $pubStore = New-Object System.Security.Cryptography.X509Certificates.X509Store('TrustedPublisher', 'CurrentUser')
    $pubStore.Open('ReadWrite')
    $pubStore.Add($cert)
    $pubStore.Close()
}

Set-AuthenticodeSignature -FilePath "VlynnByeDPI_GUI.exe" -Certificate $cert | Out-Null
Write-Host "Build and Sign complete! You can now run VlynnByeDPI_GUI.exe"
