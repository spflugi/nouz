# Build script for Nouz
# Usage: .\build.ps1 [release|debug]
# Default: debug

param(
    [ValidateSet("release", "debug")]
    [string]$Mode = "debug"
)

$ErrorActionPreference = "Stop"

# --- Prerequisite checks ---

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error "Node.js is not installed or not in PATH."
    exit 1
}

if (-not (Get-Command cargo -ErrorAction SilentlyContinue)) {
    Write-Error "Rust/Cargo is not installed or not in PATH. Install from https://rustup.rs"
    exit 1
}

# --- Install frontend dependencies if needed ---

if (-not (Test-Path "node_modules")) {
    Write-Host "Installing npm dependencies..." -ForegroundColor Cyan
    npm ci
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

# --- Build ---

if ($Mode -eq "release") {
    Write-Host "Building release installer..." -ForegroundColor Cyan
    npm run tauri build
} else {
    Write-Host "Building debug installer..." -ForegroundColor Cyan
    npm run tauri build -- --debug
}

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# --- Report output location ---

$targetDir = "src-tauri\target"
if ($Mode -eq "release") {
    $bundleDir = "$targetDir\release\bundle"
} else {
    $bundleDir = "$targetDir\debug\bundle"
}

if (Test-Path $bundleDir) {
    Write-Host ""
    Write-Host "Build complete. Installer(s) located in:" -ForegroundColor Green
    Write-Host "  $((Resolve-Path $bundleDir).Path)" -ForegroundColor Green
    Get-ChildItem -Recurse -File $bundleDir | ForEach-Object {
        Write-Host "  $_" -ForegroundColor Gray
    }
} else {
    Write-Host "Build complete." -ForegroundColor Green
}
