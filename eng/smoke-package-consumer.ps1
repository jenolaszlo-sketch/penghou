param(
    [ValidateSet('IO', 'Workflow')]
    [string] $Profile = 'IO',

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
[string[]] $packageIds = if ($Profile -eq 'Workflow') {
    @('Penghou.Workflow.Abstractions')
}
else {
    @('Penghou.IO.Protocols', 'Penghou.IO.Abstractions', 'Penghou.IO.Local')
}
$previousNugetPackages = $env:NUGET_PACKAGES

try {
    New-Item -ItemType Directory -Path $feedDirectory, $consumerDirectory | Out-Null
    $env:NUGET_PACKAGES = Join-Path $workDirectory '.nuget-packages'
    Get-ChildItem -LiteralPath $packageDirectory -File -Filter '*.nupkg' |
        Where-Object { $_.Name -notlike '*.snupkg' } |
        Copy-Item -Destination $feedDirectory

    $references = ($packageIds | ForEach-Object {
        "    <PackageReference Include=`"$_`" Version=`"[$PackageVersion]`" />"
    }) -join "`n"
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFrameworks>net8.0;net10.0</TargetFrameworks>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
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

    $consumerProgram = if ($Profile -eq 'Workflow') {
@'
using Penghou.Workflow.Abstractions;

var requirement = new ExecutionRequirement(
    "penghou.execution", 1, "workflow.operation.run", "urn:demo:operation:read");
var identity = new ExecutionIdentity(
    "exec-1", "op-1", attempt: 1, executionRevision: "epoch-1");
var context = new ExecutionAuthorizationContext(
    identity, authorizationRequestId: "auth-1", requirements: new[] { requirement });
var authorizer = new DemoAuthorizer();
var callbacks = 0;
var result = await authorizer.AuthorizeAsync(context);
if (result.AuthorizationRequestId != context.AuthorizationRequestId ||
    result.ProviderId != "demo-authorizer" || string.IsNullOrWhiteSpace(result.DecisionId))
    throw new InvalidOperationException("The authorization response was not bound to this consumer request.");
if (result.Decision != ExecutionAuthorizationDecision.Denied)
    throw new InvalidOperationException("The denied-only proof expected an explicit denial.");
if (callbacks != 0)
    throw new InvalidOperationException("Denied workflow work reached a protected callback.");

Console.WriteLine($"A neutral consumer received a request-bound {result.Decision} from {result.ProviderId}; the protected callback count was {callbacks}.");

sealed class DemoAuthorizer : IExecutionAuthorizer
{
    public ValueTask<ExecutionAuthorizationResult> AuthorizeAsync(
        ExecutionAuthorizationContext context,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new ExecutionAuthorizationResult(
            ExecutionAuthorizationDecision.Denied,
            context.AuthorizationRequestId,
            "demo-authorizer",
            "decision-1",
            DateTimeOffset.UtcNow,
            reasonCode: "demo-denial"));
}
'@
    }
    else {
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
'@
    }
    $consumerProgram | Set-Content -LiteralPath (Join-Path $consumerDirectory 'Program.cs') -Encoding utf8

    $projectPath = Join-Path $consumerDirectory 'PackageOnlyConsumer.csproj'
    dotnet restore $projectPath --configfile (Join-Path $consumerDirectory 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw 'Package-only restore failed.' }
    if ($Profile -eq 'Workflow') {
        $assetsPath = Join-Path $consumerDirectory 'obj/project.assets.json'
        $assets = Get-Content -LiteralPath $assetsPath -Raw | ConvertFrom-Json
        foreach ($framework in @('net8.0', 'net10.0')) {
            $target = $assets.targets.$framework
            $targetPackageIds = @($target.PSObject.Properties.Name | ForEach-Object { ($_ -split '/', 2)[0] })
            if ($targetPackageIds.Count -ne 1 -or $targetPackageIds[0] -ne $packageIds[0]) {
                throw "The fresh $framework package-only target must contain exactly $($packageIds[0]); found $($targetPackageIds -join ', ')."
            }
            $frameworkDependencies = @($assets.project.frameworks.$framework.dependencies.PSObject.Properties)
            if ($frameworkDependencies.Count -ne 1 -or $frameworkDependencies[0].Name -ne $packageIds[0]) {
                throw "The fresh $framework consumer project must directly reference exactly $($packageIds[0])."
            }
            if ($frameworkDependencies[0].Value.version -ne "[$PackageVersion, $PackageVersion]") {
                throw "The $framework consumer reference is not pinned to exact version $PackageVersion."
            }
        }
    }
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
