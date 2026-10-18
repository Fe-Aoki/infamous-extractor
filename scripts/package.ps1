$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
& (Join-Path $PSScriptRoot 'build.ps1')
$version='0.7'
$releaseRoot=Join-Path $projectRoot 'artifacts\releases'
[IO.Directory]::CreateDirectory($releaseRoot)|Out-Null
$staging=Join-Path $releaseRoot ([Guid]::NewGuid().ToString('N'))
$package=Join-Path $staging "Preparador-inFAMOUS-v$version"
Copy-Item -LiteralPath (Join-Path $projectRoot 'dist\Preparador-inFAMOUS') -Destination $package -Recurse
$source=Join-Path $package 'fontes'
[IO.Directory]::CreateDirectory($source)|Out-Null
foreach($folder in @('src','scripts','assets','vendor','tools','licencas','docs','.github')){Copy-Item -LiteralPath (Join-Path $projectRoot $folder) -Destination $source -Recurse}
foreach($file in @('LICENSE','README.md','README.en.md','THIRD_PARTY_NOTICES.md','CONTRIBUTING.md','CHANGELOG.md','.gitignore','.gitattributes')){Copy-Item -LiteralPath (Join-Path $projectRoot $file) -Destination $source}
Get-ChildItem -LiteralPath $package -Recurse -File | Sort-Object FullName | ForEach-Object { $hash=Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256; '{0}  {1}' -f $hash.Hash.ToLowerInvariant(),[IO.Path]::GetRelativePath($package,$_.FullName).Replace('\','/') } | Set-Content -LiteralPath (Join-Path $package 'SHA256SUMS.txt') -Encoding utf8
$zip=Join-Path $releaseRoot "Preparador-inFAMOUS-v$version.zip"
Compress-Archive -LiteralPath $package -DestinationPath $zip -CompressionLevel Optimal -Force
Write-Output "RELEASE_ZIP:$zip"
