$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$root=Join-Path $projectRoot 'dist\Preparador-inFAMOUS'
[IO.Directory]::CreateDirectory($root)|Out-Null
Copy-Item -LiteralPath (Join-Path $projectRoot 'assets') -Destination $root -Recurse -Force
[IO.Directory]::CreateDirectory((Join-Path $root 'tools'))|Out-Null
Get-ChildItem -LiteralPath (Join-Path $projectRoot 'tools\7zip') -File | ForEach-Object { Copy-Item -LiteralPath $_.FullName -Destination (Join-Path $root 'tools') -Force }
[IO.Directory]::CreateDirectory($root)|Out-Null
$fx=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$refs=@('mscorlib.dll','System.dll','System.Core.dll','System.Numerics.dll','System.Security.dll','System.Xml.dll','System.Xml.Linq.dll','System.Drawing.dll','System.Windows.Forms.dll','System.IO.Compression.dll','System.IO.Compression.FileSystem.dll') | ForEach-Object { [Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile((Join-Path $fx $_)) }
function Compile([string]$name,[string[]]$files,[Microsoft.CodeAnalysis.MetadataReference[]]$references,[Microsoft.CodeAnalysis.OutputKind]$kind){
    $parse=[Microsoft.CodeAnalysis.CSharp.CSharpParseOptions]::Default.WithPreprocessorSymbols([string[]]@('CORE'))
    $trees=[Microsoft.CodeAnalysis.SyntaxTree[]]@($files | ForEach-Object { [Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree]::ParseText([IO.File]::ReadAllText($_),$parse) })
    $options=[Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions]::new($kind).WithAllowUnsafe($true).WithPlatform([Microsoft.CodeAnalysis.Platform]::X64).WithOptimizationLevel([Microsoft.CodeAnalysis.OptimizationLevel]::Release)
    $compilation=[Microsoft.CodeAnalysis.CSharp.CSharpCompilation]::Create([IO.Path]::GetFileNameWithoutExtension($name),$trees,$references,$options)
    $output=[IO.File]::Create((Join-Path $root $name))
    $iconStream=$null;$resources=$null;$manifestStream=$null
    try{
        $iconPath=Join-Path $root 'assets\infamous.ico'
        if($kind -eq [Microsoft.CodeAnalysis.OutputKind]::WindowsApplication -and (Test-Path -LiteralPath $iconPath)){
            $iconStream=[IO.File]::OpenRead($iconPath)
            $manifestStream=[IO.File]::OpenRead((Join-Path $projectRoot 'src\app.manifest'))
            $resources=$compilation.CreateDefaultWin32Resources($true,$false,$manifestStream,$iconStream)
        }
        $result=$compilation.Emit($output,$null,$null,$resources)
        if(-not $result.Success){throw (($result.Diagnostics | Where-Object Severity -eq Error | ForEach-Object ToString) -join "`n")}
    }finally{if($resources){$resources.Dispose()};if($iconStream){$iconStream.Dispose()};if($manifestStream){$manifestStream.Dispose()};$output.Dispose()}
}
$project=(Resolve-Path (Join-Path $projectRoot 'vendor\LibOrbisPkg\LibOrbisPkg.Core\LibOrbisPkg.Core.csproj')).Path
[xml]$xml=Get-Content -LiteralPath $project -Raw
$sources=@($xml.Project.ItemGroup.Compile | Where-Object { $_.Include -and $_.Include -notlike '*AssemblyInfo.cs' } | ForEach-Object {[IO.Path]::GetFullPath((Join-Path ([IO.Path]::GetDirectoryName($project)) $_.Include))})
Compile 'LibOrbisPkg.dll' $sources $refs ([Microsoft.CodeAnalysis.OutputKind]::DynamicallyLinkedLibrary)
$appRefs=@($refs)+@([Microsoft.CodeAnalysis.MetadataReference]::CreateFromFile((Join-Path $root 'LibOrbisPkg.dll')))
Compile 'Preparador-inFAMOUS.exe' @((Join-Path $projectRoot 'src\App.cs'),(Join-Path $projectRoot 'src\Engine.cs'),(Join-Path $projectRoot 'src\Theme.cs'),(Join-Path $projectRoot 'src\StoredRar5.cs')) $appRefs ([Microsoft.CodeAnalysis.OutputKind]::WindowsApplication)
Copy-Item -LiteralPath (Join-Path $projectRoot 'src\App.config') -Destination (Join-Path $root 'Preparador-inFAMOUS.exe.config') -Force
Copy-Item -LiteralPath (Join-Path $projectRoot 'licencas') -Destination $root -Recurse -Force
foreach($file in @('LICENSE','THIRD_PARTY_NOTICES.md','README.md')){Copy-Item -LiteralPath (Join-Path $projectRoot $file) -Destination $root -Force}
Write-Output 'BUILD_OK'
