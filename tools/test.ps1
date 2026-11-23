<#
.SYNOPSIS
  Runs Unity Test Framework tests in batchmode (EditMode by default; -Platform PlayMode for the
  automated playthrough, which keeps the GPU so it can write screenshots to Logs/playtest/).
  Results: TestResults/<platform>.xml   Log: Logs/test-<platform>.log
  Exit code: 0 = all passed, 2 = some tests failed, other = run error (Unity's own codes).
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\test.ps1
  powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Filter "DeathNotice.Tests.EditMode.CoreInfoTests"
  powershell -ExecutionPolicy Bypass -File tools\test.ps1 -Platform PlayMode
#>
param([string]$Platform = 'EditMode', [string]$Filter, [switch]$Subst, [string]$Drive = 'G:', [switch]$NoGraphics)
. "$PSScriptRoot\_unity.ps1"

Assert-UnityReady
$log = Join-Path $RepoRoot "Logs\test-$Platform.log"
$xml = Join-Path $RepoRoot "TestResults\$Platform.xml"
New-Item -ItemType Directory -Force (Split-Path $xml) | Out-Null
if (Test-Path $xml) { Remove-Item $xml }
$proj = Enter-ProjectPath -Subst:$Subst -Drive $Drive
try {
    # NOTE: -runTests must NOT be combined with -quit.
    $extra = @('-runTests', '-testPlatform', $Platform, '-testResults', "`"$xml`"")
    if ($Filter) { $extra += @('-testFilter', "`"$Filter`"") }
    $gfx = ($Platform -eq 'PlayMode') -and -not $NoGraphics
    $code = Invoke-Unity $proj $log $extra -Graphics:$gfx
    if (Test-Path $xml) {
        [xml]$r = Get-Content $xml -Raw
        $run = $r.'test-run'
        Write-Host "Tests: total=$($run.total) passed=$($run.passed) failed=$($run.failed) skipped=$($run.skipped) result=$($run.result)"
    } else {
        Write-Host "No results file produced ($xml)."
        if ($code -eq 0) { $code = 3 }
    }
} finally { Exit-ProjectPath }
exit $code
