param([Parameter(Mandatory)][string] $PackageVersion, [Parameter(Mandatory)][string] $PackageDirectory, [Parameter(Mandatory)][string] $OutputDirectory, [string] $BaselineVersion = '0.1.0-preview.2')
$ErrorActionPreference = 'Stop'
$feed = [IO.Path]::GetFullPath($PackageDirectory)
$evidence = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $evidence -Force | Out-Null
& "$PSScriptRoot/verify-package-set.ps1" -Profile Workflow -PackageVersion $PackageVersion -PackageDirectory $feed
if (-not $?) { throw 'Package verification failed' }
$cache = Join-Path $evidence ('contract-cache-' + [Guid]::NewGuid().ToString('N'))
$config = Join-Path $evidence 'NuGet.Config'
$escaped = [Security.SecurityElement]::Escape($feed)
"<configuration><packageSources><clear /><add key=`"artifact`" value=`"$escaped`" /><add key=`"nuget`" value=`"https://api.nuget.org/v3/index.json`" /></packageSources><packageSourceMapping><packageSource key=`"artifact`"><package pattern=`"Penghou.Workflow.Abstractions`" /></packageSource><packageSource key=`"nuget`"><package pattern=`"*`" /></packageSource></packageSourceMapping><fallbackPackageFolders><clear /></fallbackPackageFolders></configuration>" | Set-Content $config
$project = Join-Path $PSScriptRoot '../tests/Penghou.Workflow.Tests/Penghou.Workflow.Tests.csproj'
dotnet restore $project --configfile $config "-p:WorkflowPackageVersion=$PackageVersion" "-p:RestorePackagesPath=$cache"
if ($LASTEXITCODE -ne 0) { throw 'Packed contract test restore failed' }
dotnet test $project -c Release --no-restore "-p:WorkflowPackageVersion=$PackageVersion" "-p:RestorePackagesPath=$cache" --logger trx --results-directory $evidence
if ($LASTEXITCODE -ne 0) { throw 'Packed contract tests failed' }
$assets = Get-Content (Join-Path $PSScriptRoot '../tests/Penghou.Workflow.Tests/obj/project.assets.json') -Raw | ConvertFrom-Json
if ("Penghou.Workflow.Abstractions/$PackageVersion" -notin $assets.libraries.PSObject.Properties.Name) { throw 'Contract tests did not resolve exact package' }
if (@($assets.libraries.PSObject.Properties | Where-Object { $_.Value.type -ne 'package' }).Count) { throw 'Contract tests resolved a source project' }
foreach ($tfm in @('net8.0','net10.0')) {
    $used = Join-Path $PSScriptRoot "../tests/Penghou.Workflow.Tests/bin/Release/$tfm/Penghou.Workflow.Abstractions.dll"
    $artifactDll = Join-Path $cache "penghou.workflow.abstractions/$PackageVersion/lib/$tfm/Penghou.Workflow.Abstractions.dll"
    if ((Get-FileHash $used).Hash -ne (Get-FileHash $artifactDll).Hash) { throw 'Tests loaded a different DLL from artifact' }
}
& "$PSScriptRoot/qualify-workflow-package.ps1" -PackageVersion $PackageVersion -PackageDirectory $feed -OutputDirectory $evidence -BaselineVersion $BaselineVersion
if (-not $?) { throw 'Consumer proof failed' }
