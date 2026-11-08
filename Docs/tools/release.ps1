# Builds, (optionally) signs and zips the Windows release. Run from the project root:
#   powershell -File Docs/tools/release.ps1               # unsigned
#   powershell -File Docs/tools/release.ps1 -Sign         # signed (see sign_build.ps1 for the certificate env vars)
param(
	[switch]$Sign,
	[string]$Version = "0.1.0"
)
$ErrorActionPreference = 'Stop'
$godot = "C:\Projects\External\AIVoidLabs\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"

Write-Host "== dotnet build"
dotnet build | Select-String -Pattern 'Hata|error' | Select-Object -First 5

Write-Host "== export"
if (Test-Path build) { Remove-Item build -Recurse -Force }
New-Item -ItemType Directory -Force build | Out-Null
& $godot --headless --path . --export-release "Windows Desktop" "build/PawStorm.exe" | Out-Null
if (-not (Test-Path build/PawStorm.exe)) { throw "Export failed (are the 4.7.2 mono export templates installed?)" }

if ($Sign) {
	Write-Host "== sign"
	& "$PSScriptRoot\sign_build.ps1" -File "build/PawStorm.exe"
}

Write-Host "== zip"
New-Item -ItemType Directory -Force release | Out-Null
$name = if ($Sign) { "PawStorm-$Version-windows-x64-signed.zip" } else { "PawStorm-$Version-windows-x64.zip" }
$zip = "release/$name"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path "build/PawStorm.exe", "build/data_PawStorm_windows_x86_64" -DestinationPath $zip -CompressionLevel Optimal
"{0}  {1:N1} MB" -f (Resolve-Path $zip), ((Get-Item $zip).Length / 1MB)
