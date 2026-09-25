$ErrorActionPreference = 'Stop'
$exe = Join-Path $PSScriptRoot '..\publish\NoMoreThaiNums.exe'
$p = Start-Process -FilePath $exe -PassThru
Start-Sleep -Seconds 5
$alive = -not $p.HasExited
$exitCode = if ($p.HasExited) { $p.ExitCode } else { 'n/a' }
$errLog = Test-Path "$env:LOCALAPPDATA\NoMoreThaiNums\error.log"
Write-Output "AliveAfter5s: $alive ExitCode: $exitCode ErrorLogCreated: $errLog"
if ($alive) {
    Stop-Process -Id $p.Id -Force
    Write-Output 'Smoke test process stopped.'
} else {
    Write-Output 'Process exited on its own - check error log.'
}
