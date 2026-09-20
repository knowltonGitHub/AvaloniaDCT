# Builds a downloadable Windows Setup.exe (Velopack, free).
# From the repo root:  powershell -File installer\build.ps1
#
# Output: artifacts\releases\DesktopClipboardToolkit-win-Setup.exe
# Installs per-user under %LocalAppData%\DesktopClipboardToolkit
# Encrypted data stays in %LocalAppData%\AvaloniaDCT\ (created on first login)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$publishDir = Join-Path $root "artifacts\publish"
$releaseDir = Join-Path $root "artifacts\releases"
$csproj = Join-Path $root "AvaloniaDCT.csproj"

if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
if (Test-Path $releaseDir) { Remove-Item $releaseDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishDir | Out-Null
New-Item -ItemType Directory -Path $releaseDir | Out-Null

Write-Host "Publishing self-contained win-x64 Release..."
dotnet publish $csproj -c Release -r win-x64 --self-contained true -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Get-ChildItem $publishDir -Filter "CBDB*" -ErrorAction SilentlyContinue | Remove-Item -Force

$vpk = Get-Command vpk -ErrorAction SilentlyContinue
if (-not $vpk) {
    Write-Host "Installing Velopack CLI (dotnet tool vpk)..."
    dotnet tool install -g vpk
    $tools = Join-Path $env:USERPROFILE ".dotnet\tools"
    $env:PATH = "$tools;$env:PATH"
}

Write-Host "Packing Setup.exe..."
vpk pack `
    --packId DesktopClipboardToolkit `
    --packVersion 1.0.0 `
    --packDir $publishDir `
    --mainExe AvaloniaDCT.exe `
    --packTitle "Desktop Clipboard Toolkit" `
    --outputDir $releaseDir `
    --skipVeloAppCheck

Write-Host ""
Write-Host "Installer:"
Get-ChildItem $releaseDir -Filter "*Setup.exe" | ForEach-Object { $_.FullName }
