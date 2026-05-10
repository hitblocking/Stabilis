# Adds Git for Windows to your *user* PATH if missing, then refreshes this session.
# Run from PowerShell (recommended: Right-click -> Run with PowerShell, or: powershell -ExecutionPolicy Bypass -File .\tools\fix-git-path.ps1)

$ErrorActionPreference = "Stop"

$candidates = @(
    "${env:ProgramFiles}\Git\cmd",
    "${env:ProgramFiles(x86)}\Git\cmd",
    "${env:LOCALAPPDATA}\Programs\Git\cmd",
    "${env:ProgramFiles}\Git\bin",
    "${env:USERPROFILE}\AppData\Local\Programs\Git\cmd"
)

$gitCmd = $null
foreach ($dir in $candidates) {
    if ($dir -and (Test-Path (Join-Path $dir "git.exe"))) {
        $gitCmd = $dir
        break
    }
}

if (-not $gitCmd) {
    Write-Host "git.exe was not found in common locations. Install Git from https://git-scm.com/download/win" -ForegroundColor Red
    Write-Host "If Git is already installed, edit tools\fix-git-path.ps1 and add your install folder to `$candidates." -ForegroundColor Yellow
    exit 1
}

Write-Host "Found Git at: $gitCmd" -ForegroundColor Green

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
$paths = $userPath -split ';' | Where-Object { $_ -and $_.Trim() -ne '' }

if ($paths -contains $gitCmd) {
    Write-Host "Already on user PATH: $gitCmd" -ForegroundColor Cyan
} else {
    $newUserPath = ($userPath.TrimEnd(';') + ';' + $gitCmd).Trim(';')
    [Environment]::SetEnvironmentVariable("Path", $newUserPath, "User")
    Write-Host "Added to user PATH: $gitCmd" -ForegroundColor Green
}

# Refresh current process PATH so git works without restarting this window
$machinePath = [Environment]::GetEnvironmentVariable("Path", "Machine")
$userPathFresh = [Environment]::GetEnvironmentVariable("Path", "User")
$env:Path = "$machinePath;$userPathFresh"

& (Join-Path $gitCmd "git.exe") --version
Write-Host "`nDone. Open a *new* terminal for other apps to see the change." -ForegroundColor Cyan
