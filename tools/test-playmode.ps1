<#
.SYNOPSIS
  Runs the PlayMode playthrough (all six cases, title to credits) in batchmode with the GPU enabled.
  Results: TestResults/PlayMode.xml   Log: Logs/test-PlayMode.log   Screenshots + report: Logs/playtest/
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\test-playmode.ps1
#>
param([string]$Filter, [switch]$Subst, [string]$Drive = 'G:')
$args2 = @{ Platform = 'PlayMode' }
if ($Filter) { $args2.Filter = $Filter }
if ($Subst) { $args2.Subst = $true; $args2.Drive = $Drive }
& "$PSScriptRoot\test.ps1" @args2
exit $LASTEXITCODE