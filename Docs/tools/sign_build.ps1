# Code-signs an exported Windows build with signtool.exe (Windows SDK). Run from the project root:
#   powershell -File Docs/tools/sign_build.ps1                 # signs build/PawStorm.exe
#
# The certificate is NEVER stored in the repo. Configure it with environment variables in your own shell:
#   A) .pfx file:                 $env:PAWSTORM_PFX = "C:\path\cert.pfx";  $env:PAWSTORM_PFX_PASSWORD = "..."
#   B) cert in the Windows store  $env:PAWSTORM_CERT_SHA1 = "<thumbprint>"   (hardware token / cloud HSM / imported cert)
# Optional: $env:PAWSTORM_TIMESTAMP_URL (default http://timestamp.digicert.com)
param(
	[string]$File = "build/PawStorm.exe",
	[string]$Description = "PawStorm"
)
$ErrorActionPreference = 'Stop'

# newest x64 signtool from any installed Windows SDK
$signtool = Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
	Sort-Object { [version]($_.FullName -replace '^.*\\bin\\([\d\.]+)\\x64.*$', '$1') } -Descending | Select-Object -First 1
if (-not $signtool) { throw "signtool.exe (x64) not found. Install the 'Windows SDK Signing Tools for Desktop Apps'." }
if (-not (Test-Path $File)) { throw "File not found: $File (export the game first)" }

$timestamp = if ($env:PAWSTORM_TIMESTAMP_URL) { $env:PAWSTORM_TIMESTAMP_URL } else { 'http://timestamp.digicert.com' }
$args = @('sign', '/fd', 'SHA256', '/tr', $timestamp, '/td', 'SHA256', '/d', $Description)
if ($env:PAWSTORM_PFX) {
	if (-not (Test-Path $env:PAWSTORM_PFX)) { throw "PAWSTORM_PFX points to a missing file." }
	$args += @('/f', $env:PAWSTORM_PFX)
	if ($env:PAWSTORM_PFX_PASSWORD) { $args += @('/p', $env:PAWSTORM_PFX_PASSWORD) }
} elseif ($env:PAWSTORM_CERT_SHA1) {
	$args += @('/sha1', $env:PAWSTORM_CERT_SHA1)
} else {
	throw "No certificate configured. Set PAWSTORM_PFX (+ PAWSTORM_PFX_PASSWORD) or PAWSTORM_CERT_SHA1."
}
$args += (Resolve-Path $File).Path

Write-Host "Signing $File with $($signtool.FullName)"
& $signtool.FullName @args
if ($LASTEXITCODE -ne 0) { throw "signtool sign failed (exit $LASTEXITCODE)" }

$sig = Get-AuthenticodeSignature $File
Write-Host "Signature status : $($sig.Status)"
Write-Host "Signer           : $($sig.SignerCertificate.Subject)"
Write-Host "Timestamped by   : $($sig.TimeStamperCertificate.Subject)"
# 'Valid' only appears with a certificate chaining to a trusted root (a real CA). A self-signed test cert shows UnknownError/UntrustedRoot.
