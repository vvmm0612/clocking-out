<#
.SYNOPSIS
  Opens the project in Unity batchmode, imports assets and compiles all scripts, then quits.
  Exit code 0 = compiled without errors. Log: Logs/compile.log
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\compile.ps1
#>
param([switch]$Subst, [string]$Drive = 'G:')
. "$PSScriptRoot\_unity.ps1"

Assert-UnityReady
$log = Join-Path $RepoRoot 'Logs\compile.log'
$proj = Enter-ProjectPath -Subst:$Subst -Drive $Drive
try {
    $code = Invoke-Unity $proj $log @('-quit')
    if ($code -eq 0 -and (Select-String -Path $log -Pattern 'error CS\d+' -Quiet)) { $code = 1 }
    if ($code -eq 0) { Write-Host 'COMPILE OK' } else { Write-Host "COMPILE FAILED (see $log)" }
} finally { Exit-ProjectPath }
exit $code
