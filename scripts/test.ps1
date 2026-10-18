param([string]$RarFixture,[string]$RarLinkFixture)
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$exe=Join-Path $projectRoot 'dist\Preparador-inFAMOUS\Preparador-inFAMOUS.exe'
if(-not (Test-Path -LiteralPath $exe)){ & (Join-Path $PSScriptRoot 'build.ps1') }
$report=Join-Path $projectRoot ('artifacts\tests\'+[Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($report)|Out-Null
if($RarFixture){Copy-Item -LiteralPath $RarFixture -Destination (Join-Path $report 'rar-test.rar')}
if($RarLinkFixture){Copy-Item -LiteralPath $RarLinkFixture -Destination (Join-Path $report 'rar-link-test.rar')}
$process=Start-Process -FilePath $exe -ArgumentList @('--selftest',('"'+$report+'"')) -PassThru -WindowStyle Hidden
if(-not $process.WaitForExit(60000)){$process.Kill();throw 'O autoteste excedeu 60 segundos.'}
if($process.ExitCode -ne 0){Get-Content -LiteralPath (Join-Path $report 'FAILED.txt');throw 'Autoteste falhou.'}
Get-Content -LiteralPath (Join-Path $report 'RESULTADO.txt')
Write-Output "Relatórios: $report"
