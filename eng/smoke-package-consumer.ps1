param(
    [Parameter(Mandatory = $true)]
    [string] $PackageDirectory,

    [Parameter(Mandatory = $true)]
    [string] $PackageVersion
)

$ErrorActionPreference = 'Stop'
$packageDirectory = [System.IO.Path]::GetFullPath($PackageDirectory)
$tempRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
$workDirectory = Join-Path $tempRoot "penghou-package-consumer-$([guid]::NewGuid().ToString('N'))"
$feedDirectory = Join-Path $workDirectory 'feed'
$consumerDirectory = Join-Path $workDirectory 'consumer'
$packageIds = @('Penghou.IO.Protocols', 'Penghou.IO.Abstractions', 'Penghou.IO.Local')
$previousNugetPackages = $env:NUGET_PACKAGES

try {
    New-Item -ItemType Directory -Path $feedDirectory, $consumerDirectory | Out-Null
    $env:NUGET_PACKAGES = Join-Path $workDirectory '.nuget-packages'
    Get-ChildItem -LiteralPath $packageDirectory -File -Filter '*.nupkg' |
        Where-Object { $_.Name -notlike '*.snupkg' } |
        Copy-Item -Destination $feedDirectory

    $references = ($packageIds | ForEach-Object {
        "    <PackageReference Include=`"$_`" Version=`"$PackageVersion`" />"
    }) -join "`n"
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
$references
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath (Join-Path $consumerDirectory 'PackageOnlyConsumer.csproj') -Encoding utf8

    $escapedFeed = [System.Security.SecurityElement]::Escape($feedDirectory)
    @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="package-artifacts-only" value="$escapedFeed" />
  </packageSources>
</configuration>
"@ | Set-Content -LiteralPath (Join-Path $consumerDirectory 'NuGet.Config') -Encoding utf8

    @'
using Penghou.IO.Abstractions;
using Penghou.IO.Local;

var workspace = new WorkspaceId("package-consumer");
var path = WindowsWorkspacePath.Normalize(new WorkspacePath("src/example.txt"), allowRoot: false);
var invocation = new HostInvocation("invocation", "subject", "effect", "attempt", "scope", null,
    new RequestIdentity("package-consumer-request"));
var request = new FileReadRequest(invocation, workspace, path, new IoLimits(1024));
var identity = ResourceRequestIdentity.Compute(request);
if (string.IsNullOrWhiteSpace(identity.Value))
    throw new InvalidOperationException("The packaged identity protocol returned no value.");

var provider = new LocalWorkspaceProvider(workspace, Path.GetTempPath());
if (provider.Workspace != workspace)
    throw new InvalidOperationException("The packaged Local provider did not preserve its workspace identity.");

Console.WriteLine($"Loaded package APIs for {workspace.Value}; schema {ResourceRequestIdentity.SchemaVersion}.");
'@ | Set-Content -LiteralPath (Join-Path $consumerDirectory 'Program.cs') -Encoding utf8

    $projectPath = Join-Path $consumerDirectory 'PackageOnlyConsumer.csproj'
    dotnet restore $projectPath --configfile (Join-Path $consumerDirectory 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw 'Package-only restore failed.' }
    foreach ($framework in @('net8.0', 'net10.0')) {
        dotnet run --project $projectPath --configuration Release --framework $framework --no-restore
        if ($LASTEXITCODE -ne 0) { throw "Package-only runtime proof failed for $framework." }
    }
}
finally {
    if ($null -eq $previousNugetPackages) {
        Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
    }
    else {
        $env:NUGET_PACKAGES = $previousNugetPackages
    }
    $resolvedWork = [System.IO.Path]::GetFullPath($workDirectory)
    if (-not $resolvedWork.StartsWith($tempRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove package smoke path outside the temp directory: '$resolvedWork'."
    }
    if (Test-Path -LiteralPath $resolvedWork) {
        Remove-Item -LiteralPath $resolvedWork -Recurse -Force
    }
}
