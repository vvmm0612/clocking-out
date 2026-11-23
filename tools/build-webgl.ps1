<#
.SYNOPSIS
  Builds the WebGL player to Builds/WebGL via BuildScript.BuildWebGL. Log: Logs/build-webgl.log
  First build can take 15-40 minutes.
.PARAMETER Subst
  Map the repo to an ASCII drive letter with `subst` (session-level, removed afterwards) and build
  from there. Use this if IL2CPP/emscripten fails on the non-ASCII repo path ("02.游戏制作").
.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\build-webgl.ps1
  powershell -ExecutionPolicy Bypass -File tools\build-webgl.ps1 -Subst -Drive G:
#>
param([switch]$Subst, [string]$Drive = 'G:')
. "$PSScriptRoot\_unity.ps1"

Assert-UnityReady
$log = Join-Path $RepoRoot 'Logs\build-webgl.log'
$proj = Enter-ProjectPath -Subst:$Subst -Drive $Drive
try {
    $code = Invoke-Unity $proj $log @('-quit', '-executeMethod', 'BuildScript.BuildWebGL')
} finally { Exit-ProjectPath }

$out = Join-Path $RepoRoot 'Builds\WebGL'
if ($code -eq 0) {
    $required = @('index.html') + (@('*.data*', '*.wasm*', '*.framework.js*', '*.loader.js') | ForEach-Object { "Build\$_" })
    $missing = $required | Where-Object { -not (Get-ChildItem (Join-Path $out $_) -ErrorAction SilentlyContinue) }
    if ($missing) { Write-Host "Build output incomplete, missing: $($missing -join ', ')"; $code = 1 }
    else {
        $bytes = (Get-ChildItem $out -Recurse -File | Measure-Object Length -Sum).Sum
        Get-ChildItem (Join-Path $out 'Build') -File | ForEach-Object { Write-Host ("  {0,-40} {1,10:N0} bytes" -f $_.Name, $_.Length) }
        Write-Host ("WEBGL BUILD OK: {0} ({1:N2} MB total)" -f $out, ($bytes / 1MB))
    }
} else { Write-Host "WEBGL BUILD FAILED (see $log)" }
exit $code
