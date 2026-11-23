# Shared helpers for tools/*.ps1. Dot-source only:  . "$PSScriptRoot\_unity.ps1"
# Override the editor location with $env:UNITY_EXE if your install differs.

$ErrorActionPreference = 'Stop'

$script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$script:UnityExe = if ($env:UNITY_EXE) { $env:UNITY_EXE } else { 'D:\Softwares For Non-Chinese\Unity Editor\2022.3.17f1c1\Editor\Unity.exe' }
$script:SubstDrive = $null

function Assert-UnityReady {
    if (-not (Test-Path $script:UnityExe)) {
        throw "Unity.exe not found at '$script:UnityExe'. Set `$env:UNITY_EXE to your 2022.3.17f1c1 editor."
    }
    $lock = Join-Path $script:RepoRoot 'Temp\UnityLockfile'
    if (Test-Path $lock) {
        try { [IO.File]::Open($lock, 'Open', 'ReadWrite', 'None').Close() }
        catch { throw "Project is already open in another Unity instance (Temp\UnityLockfile is locked). Close it first." }
    }
}

# Maps the repo to a drive letter (session-level `subst`) so IL2CPP/emscripten see an ASCII-only path.
# Returns the project path Unity should use.
function Enter-ProjectPath([switch]$Subst, [string]$Drive = 'G:') {
    if (-not $Subst) { return $script:RepoRoot }
    $Drive = $Drive.TrimEnd('\')
    $existing = (subst) | Where-Object { $_ -like "$Drive\:*" }
    if ($existing) {
        # subst prints the target in the OEM codepage, so compare by content rather than by string.
        if (-not (Test-Path "$Drive\ProjectSettings\ProjectVersion.txt")) { throw "Drive $Drive is already substituted to something else." }
    } else {
        if (Test-Path "$Drive\") { throw "Drive $Drive already exists; pass -Drive with a free letter." }
        subst $Drive $script:RepoRoot | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "subst $Drive failed" }
        $script:SubstDrive = $Drive
    }
    return "$Drive/"  # forward slash: a trailing "\" before the closing quote would escape it
}

function Exit-ProjectPath {
    if ($script:SubstDrive) { subst $script:SubstDrive /D | Out-Null; $script:SubstDrive = $null }
}

# Runs Unity in batchmode, waits, prints elapsed time and returns the exit code.
function Invoke-Unity([string]$ProjectPath, [string]$LogFile, [string[]]$ExtraArgs) {
    New-Item -ItemType Directory -Force (Split-Path $LogFile) | Out-Null
    $argv = @('-batchmode', '-nographics', '-projectPath', "`"$ProjectPath`"", '-buildTarget', 'WebGL', '-logFile', "`"$LogFile`"") + $ExtraArgs
    Write-Host ">> `"$script:UnityExe`" $($argv -join ' ')"
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $p = Start-Process -FilePath $script:UnityExe -ArgumentList $argv -PassThru -Wait -NoNewWindow
    $sw.Stop()
    Write-Host ("<< exit code {0}, elapsed {1:hh\:mm\:ss}" -f $p.ExitCode, $sw.Elapsed)
    if (Test-Path $LogFile) {
        $lic = Select-String -Path $LogFile -Pattern 'No valid Unity Editor license|License is not active' | Select-Object -First 3
        if ($lic) { Write-Warning "Unity license problem (sign in / re-activate via Unity Hub):"; $lic | ForEach-Object { Write-Warning $_.Line } }
        if ($p.ExitCode -ne 0) {
            Write-Host "--- errors from $LogFile ---"
            Select-String -Path $LogFile -Pattern 'error CS\d+|Error|Exception|Failed' | Select-Object -First 40 | ForEach-Object { Write-Host $_.Line }
        }
    }
    return $p.ExitCode
}
