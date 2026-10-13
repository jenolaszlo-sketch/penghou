param(
    [Parameter(Mandatory)][string] $PackageVersion,
    [Parameter(Mandatory)][string] $OutputDirectory,
    [string] $PackageDirectory,
    [string] $BaselineVersion = '0.1.0-preview.2',
    [string] $ZhinuVersion = '0.2.0-preview.3',
    [switch] $PublicPackage
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $root -Force | Out-Null
$run = Join-Path $root ([Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run | Out-Null
$cache = Join-Path $run 'packages'
$repository = Split-Path $PSScriptRoot -Parent
function Checked([string[]] $Arguments, [string] $Log) {
    $output = & dotnet @Arguments 2>&1
    $code = $LASTEXITCODE
    $output | Set-Content -LiteralPath (Join-Path $run $Log)
    if ($code -ne 0) { throw "dotnet failed; $Log`n$($output -join [Environment]::NewLine)" }
}
$feedLine = ''
$mapping = ''
if (-not $PublicPackage) {
    $feed = [Security.SecurityElement]::Escape([IO.Path]::GetFullPath($PackageDirectory))
    $feedLine = "<add key=`"artifact`" value=`"$feed`" />"
    $mapping = '<packageSourceMapping><packageSource key="artifact"><package pattern="Penghou.Workflow.Abstractions" /></packageSource><packageSource key="nuget"><package pattern="*" /></packageSource></packageSourceMapping>'
}
$config = Join-Path $run 'NuGet.Config'
"<configuration><packageSources><clear />$feedLine<add key=`"nuget`" value=`"https://api.nuget.org/v3/index.json`" /></packageSources>$mapping<fallbackPackageFolders><clear /></fallbackPackageFolders></configuration>" | Set-Content $config
$baselineConfig = Join-Path $run 'Baseline.NuGet.Config'
'<configuration><packageSources><clear /><add key="nuget" value="https://api.nuget.org/v3/index.json" /></packageSources><fallbackPackageFolders><clear /></fallbackPackageFolders></configuration>' | Set-Content $baselineConfig
$results = [Collections.Generic.List[object]]::new()
foreach ($kind in @('neutral', 'zhinu')) {
    foreach ($version in @($BaselineVersion, $PackageVersion)) {
        $label = if ($version -eq $BaselineVersion) { 'baseline' } else { 'candidate' }
        $directory = Join-Path $run "$kind-$label"
        New-Item -ItemType Directory -Path $directory | Out-Null
        '<Project />' | Set-Content (Join-Path $directory 'Directory.Build.props')
        '<Project />' | Set-Content (Join-Path $directory 'Directory.Build.targets')
        $extra = if ($kind -eq 'zhinu') { "<PackageReference Include=`"Penghou.Zhinu`" Version=`"[$ZhinuVersion]`" /><PackageReference Include=`"Penghou.Zhinu.Sqlite`" Version=`"[$ZhinuVersion]`" />" } else { '' }
        # Published Zhinu's old exact pin is deliberately overridden only in this compatibility probe.
        "<Project Sdk=`"Microsoft.NET.Sdk`"><PropertyGroup><OutputType>Exe</OutputType><TargetFrameworks>net8.0;net10.0</TargetFrameworks><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><NoWarn>NU1608</NoWarn></PropertyGroup><ItemGroup><PackageReference Include=`"Penghou.Workflow.Abstractions`" Version=`"[$version]`" />$extra</ItemGroup></Project>" | Set-Content (Join-Path $directory 'Consumer.csproj')
        Copy-Item (Join-Path $PSScriptRoot "workflow-$kind-consumer.cs") (Join-Path $directory 'Program.cs')
        $project = Join-Path $directory 'Consumer.csproj'
        $selectedConfig = if ($label -eq 'baseline') { $baselineConfig } else { $config }
        Checked @('restore', $project, '--configfile', $selectedConfig, "-p:RestorePackagesPath=$cache") "$kind-$label-restore.log"
        $assets = Get-Content (Join-Path $directory 'obj/project.assets.json') -Raw | ConvertFrom-Json
        if ($assets.libraries.PSObject.Properties.Name -match 'Penghou.Hufu') { throw 'Forbidden authority dependency' }
        if (@($assets.libraries.PSObject.Properties | Where-Object { $_.Value.type -ne 'package' }).Count) { throw 'Non-package consumer dependency' }
        if ("Penghou.Workflow.Abstractions/$version" -notin $assets.libraries.PSObject.Properties.Name) { throw 'Wrong workflow package resolved' }
        foreach ($framework in @('net8.0', 'net10.0')) {
            Checked @('build', $project, '-c', 'Release', '-f', $framework, '--no-restore') "$kind-$label-$framework-build.log"
            $binary = Join-Path $directory "bin/Release/$framework/Consumer.dll"
            Checked @($binary) "$kind-$label-$framework-run.log"
            $results.Add([ordered]@{ Consumer=$kind; Version=$version; Framework=$framework; Probe='compiled-package-behavior'; Result=$(if ($kind -eq 'zhinu') { 'SUPPORTED WITH QUALIFICATION' } else { 'SUPPORTED BY THIS PROBE' }) })
        }
    }
    foreach ($framework in @('net8.0', 'net10.0')) {
        $baselineOutput = Join-Path $run "$kind-baseline/bin/Release/$framework"
        $candidateOutput = Join-Path $run "$kind-candidate/bin/Release/$framework"
        $binary = Join-Path $baselineOutput 'Consumer.dll'
        $before = (Get-FileHash $binary -Algorithm SHA256).Hash
        Copy-Item (Join-Path $candidateOutput 'Penghou.Workflow.Abstractions.dll') (Join-Path $baselineOutput 'Penghou.Workflow.Abstractions.dll') -Force
        Checked @($binary) "$kind-unchanged-$framework-run.log"
        $after = (Get-FileHash $binary -Algorithm SHA256).Hash
        if ($before -ne $after) { throw 'Previously compiled consumer changed' }
        $results.Add([ordered]@{ Consumer=$kind; CompiledAgainst=$BaselineVersion; RunsAgainst=$PackageVersion; Framework=$framework; ConsumerSHA256=$before; Probe='unchanged-binary-loading-and-behavior'; Result=$(if ($kind -eq 'zhinu') { 'SUPPORTED WITH QUALIFICATION' } else { 'SUPPORTED BY THIS PROBE' }) })
    }
}
$evidence = [ordered]@{ Repository='https://github.com/jenolaszlo-sketch/penghou'; Branch=(git -C $repository branch --show-current); Commit=(git -C $repository rev-parse HEAD); WorkingTreeStatus=@(git -C $repository status --porcelain); QualifiedAt=[DateTimeOffset]::UtcNow.ToString('O'); PackageVersion=$PackageVersion; BaselineVersion=$BaselineVersion; ZhinuVersion=$ZhinuVersion; PublicPackage=[bool]$PublicPackage; OS=[Runtime.InteropServices.RuntimeInformation]::OSDescription; Commands='restore clean cache; build package-only; execute; replace assembly without rebuilding; execute unchanged binaries'; Results=$results.ToArray(); Limits='Only neutral value/interface behavior and published Zhinu direct SQLite/protected builder paths; no Hufu implementation or full runtime certification. Published Zhinu exact old contract pin overridden in isolated compatibility probe.' }
if (-not $PublicPackage) { $evidence.ArtifactSHA256 = (Get-FileHash (Join-Path $PackageDirectory "Penghou.Workflow.Abstractions.$PackageVersion.nupkg") -Algorithm SHA256).Hash }
$evidence | ConvertTo-Json -Depth 12 | Set-Content (Join-Path $run 'qualification.json')
Write-Output (Join-Path $run 'qualification.json')
