param(
    [Parameter(Mandatory)][string] $PackageDirectory,
    [string] $StableVersion = '1.0.0-rc.1',
    [string] $FacadeVersion = '0.1.0-preview.2',
    [string] $BaselinePackageDirectory = (Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages'),
    [switch] $PublicPackage,
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '../artifacts/io-qualification')
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$packageDirectory = [IO.Path]::GetFullPath($PackageDirectory)
$outputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $outputDirectory) { throw "Qualification output already exists; select a fresh evidence directory: $outputDirectory" }
New-Item -ItemType Directory -Path $outputDirectory | Out-Null
$feed = Join-Path $outputDirectory 'feed'
New-Item -ItemType Directory -Path $feed | Out-Null
$baseline = [IO.Path]::GetFullPath($BaselinePackageDirectory)
foreach ($id in @('Penghou.IO.Abstractions', 'Penghou.IO.Protocols', 'Penghou.IO.Local', 'Penghou.Luban')) {
    $archive = Join-Path $baseline "$($id.ToLowerInvariant())/0.1.0-preview.1/$($id.ToLowerInvariant()).0.1.0-preview.1.nupkg"
    if (-not (Test-Path -LiteralPath $archive)) { throw "Missing exact public preview baseline: $archive" }
    Copy-Item -LiteralPath $archive -Destination $feed
}
$expected = @{
    'Penghou.IO.Identity.Abstractions' = $StableVersion
    'Penghou.IO.Protocols' = $StableVersion
    'Penghou.IO.Abstractions' = $FacadeVersion
}
$artifacts = @()
foreach ($id in $expected.Keys) {
    $version = $expected[$id]
    $archivePath = Join-Path $packageDirectory "$id.$version.nupkg"
    if (-not (Test-Path -LiteralPath $archivePath)) { throw "Missing exact candidate artifact: $archivePath" }
    Copy-Item -LiteralPath $archivePath -Destination $feed
    $archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        $spec = @($archive.Entries | Where-Object FullName -Like '*.nuspec')
        if ($spec.Count -ne 1) { throw 'Invalid package specification count.' }
        $stream = $spec[0].Open()
        try { $xml = [Xml.Linq.XDocument]::Load($stream) } finally { $stream.Dispose() }
        $metadata = @($xml.Descendants() | Where-Object { $_.Name.LocalName -eq 'metadata' })[0]
        $actualId = @($metadata.Elements() | Where-Object { $_.Name.LocalName -eq 'id' })[0].Value
        $actualVersion = @($metadata.Elements() | Where-Object { $_.Name.LocalName -eq 'version' })[0].Value
        if ($actualId -cne $id -or $actualVersion -cne $version) { throw "Wrong package identity in $archivePath" }
        foreach ($tfm in @('net8.0', 'net10.0')) {
            if (-not $archive.GetEntry("lib/$tfm/$id.dll")) { throw "Missing $tfm assembly for $id" }
        }
        $dependencies = @($metadata.Descendants() | Where-Object { $_.Name.LocalName -eq 'dependency' })
        if ($id -eq 'Penghou.IO.Identity.Abstractions' -and $dependencies.Count -ne 0) { throw 'Identity must remain dependency-free.' }
        if ($id -ne 'Penghou.IO.Identity.Abstractions') {
            if ($dependencies.Count -ne 2) { throw "$id must have exactly one stable Identity dependency per TFM." }
            foreach ($dependency in $dependencies) {
                if ($dependency.Attribute('id').Value -cne 'Penghou.IO.Identity.Abstractions' -or $dependency.Attribute('version').Value -cne $StableVersion) { throw "Unexpected dependency: $dependency" }
            }
        }
    }
    finally { $archive.Dispose() }
    $artifacts += [ordered]@{ package = $id; version = $version; sha256 = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant() }
}
$previousCache = $env:NUGET_PACKAGES
try {
    $env:NUGET_PACKAGES = Join-Path $outputDirectory 'fresh-packages'
    $protocolConsumer = Join-Path $outputDirectory 'stable-protocol-only'
    New-Item -ItemType Directory -Path $protocolConsumer | Out-Null
    @"
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFrameworks>net8.0;net10.0</TargetFrameworks><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup><ItemGroup><PackageReference Include="Penghou.IO.Identity.Abstractions" Version="[$StableVersion]"/><PackageReference Include="Penghou.IO.Protocols" Version="[$StableVersion]"/><None Update="protocol-v1-vectors.json" CopyToOutputDirectory="PreserveNewest"/></ItemGroup></Project>
"@ | Set-Content -LiteralPath (Join-Path $protocolConsumer 'Consumer.csproj') -Encoding utf8
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'io-protocol-only-consumer.cs') -Destination (Join-Path $protocolConsumer 'Program.cs')
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot '../tests/Penghou.IO.Protocol.Tests/protocol-v1-vectors.json') -Destination $protocolConsumer
    $restoreSource = if ($PublicPackage) { 'https://api.nuget.org/v3/index.json' } else { $feed }
    $feedXml = [Security.SecurityElement]::Escape($restoreSource)
    "<configuration><packageSources><clear/><add key=`"exact-artifacts-only`" value=`"$feedXml`"/></packageSources></configuration>" | Set-Content -LiteralPath (Join-Path $protocolConsumer 'NuGet.Config') -Encoding utf8
    dotnet restore (Join-Path $protocolConsumer 'Consumer.csproj') --configfile (Join-Path $protocolConsumer 'NuGet.Config')
    if ($LASTEXITCODE -ne 0) { throw 'Stable protocol-only package restore failed.' }
    $assets = Get-Content -LiteralPath (Join-Path $protocolConsumer 'obj/project.assets.json') -Raw | ConvertFrom-Json
    foreach ($tfm in @('net8.0', 'net10.0')) {
        $actualIds = @($assets.targets.$tfm.PSObject.Properties.Name | Sort-Object)
        $expectedIds = @("Penghou.IO.Identity.Abstractions/$StableVersion", "Penghou.IO.Protocols/$StableVersion")
        if (($actualIds -join '|') -cne ($expectedIds -join '|')) { throw "Stable package target contains unexpected dependencies: $($actualIds -join ', ')" }
        dotnet run --project (Join-Path $protocolConsumer 'Consumer.csproj') -c Release -f $tfm --no-restore
        if ($LASTEXITCODE -ne 0) { throw "$tfm stable package-only vectors failed." }
    }
    foreach ($stage in @('baseline', 'candidate')) {
        $consumer = Join-Path $outputDirectory $stage
        New-Item -ItemType Directory -Path $consumer | Out-Null
        $references = if ($stage -eq 'baseline') {
            @('Penghou.IO.Abstractions', 'Penghou.IO.Protocols', 'Penghou.IO.Local', 'Penghou.Luban') | ForEach-Object { "<PackageReference Include=`"$_`" Version=`"[0.1.0-preview.1]`" />" }
        } else {
            @("<PackageReference Include=`"Penghou.IO.Identity.Abstractions`" Version=`"[$StableVersion]`" />", "<PackageReference Include=`"Penghou.IO.Protocols`" Version=`"[$StableVersion]`" />", "<PackageReference Include=`"Penghou.IO.Abstractions`" Version=`"[$FacadeVersion]`" />")
        }
        # Candidate consumer references already-published, unchanged Local/Luban DLLs,
        # rather than overriding Luban's exact preview PackageReference constraints.
        if ($stage -eq 'candidate') {
            foreach ($id in @('Penghou.IO.Local', 'Penghou.Luban')) {
                $dll = Join-Path $env:NUGET_PACKAGES "$($id.ToLowerInvariant())/0.1.0-preview.1/lib/`$(TargetFramework)/$id.dll"
                $references += "<Reference Include=`"$id`"><HintPath>$([Security.SecurityElement]::Escape($dll))</HintPath></Reference>"
            }
        }
        @"
<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFrameworks>net8.0;net10.0</TargetFrameworks><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors><AssemblyName>IOGraduationConsumer</AssemblyName></PropertyGroup><ItemGroup>
$($references -join "`n")
</ItemGroup></Project>
"@ | Set-Content -LiteralPath (Join-Path $consumer 'Consumer.csproj') -Encoding utf8
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'io-graduation-consumer.cs') -Destination (Join-Path $consumer 'Program.cs')
        $restoreSource = if ($PublicPackage) { 'https://api.nuget.org/v3/index.json' } else { $feed }
    $feedXml = [Security.SecurityElement]::Escape($restoreSource)
        "<configuration><packageSources><clear/><add key=`"exact-artifacts-only`" value=`"$feedXml`"/></packageSources></configuration>" | Set-Content -LiteralPath (Join-Path $consumer 'NuGet.Config') -Encoding utf8
        dotnet restore (Join-Path $consumer 'Consumer.csproj') --configfile (Join-Path $consumer 'NuGet.Config')
        if ($LASTEXITCODE -ne 0) { throw "$stage isolated restore failed." }
        foreach ($tfm in @('net8.0', 'net10.0')) {
            dotnet build (Join-Path $consumer 'Consumer.csproj') -c Release -f $tfm --no-restore
            if ($LASTEXITCODE -ne 0) { throw "$stage $tfm compilation failed." }
            $program = Join-Path $consumer "bin/Release/$tfm/IOGraduationConsumer.dll"
            $json = Join-Path $outputDirectory "$tfm-preview-data.json"
            $arguments = @($program, "--json=$json")
            if ($stage -eq 'candidate') { $arguments += '--expect-forwarded' }
            & dotnet @arguments
            if ($LASTEXITCODE -ne 0) { throw "$stage $tfm behavior failed." }
        }
    }
    foreach ($tfm in @('net8.0', 'net10.0')) {
        $oldOutput = Join-Path $outputDirectory "baseline/bin/Release/$tfm"
        $newOutput = Join-Path $outputDirectory "candidate/bin/Release/$tfm"
        $compiled = Join-Path $oldOutput 'IOGraduationConsumer.dll'
        $before = (Get-FileHash -LiteralPath $compiled -Algorithm SHA256).Hash
        foreach ($name in @('Penghou.IO.Identity.Abstractions.dll', 'Penghou.IO.Abstractions.dll', 'Penghou.IO.Protocols.dll', 'IOGraduationConsumer.deps.json')) {
            Copy-Item -LiteralPath (Join-Path $newOutput $name) -Destination (Join-Path $oldOutput $name) -Force
        }
        if ((Get-FileHash -LiteralPath $compiled -Algorithm SHA256).Hash -cne $before) { throw 'Previously compiled consumer changed.' }
        dotnet $compiled '--expect-forwarded' "--json=$(Join-Path $outputDirectory "$tfm-preview-data.json")"
        if ($LASTEXITCODE -ne 0) { throw "$tfm unchanged binary behavior failed." }
        $artifacts += [ordered]@{ consumer = 'unchanged-preview-compiled-consumer'; framework = $tfm; sha256 = $before.ToLowerInvariant(); result = 'SUPPORTED BY THIS PROBE' }
    }
    foreach ($id in @('Penghou.IO.Abstractions', 'Penghou.IO.Protocols', 'Penghou.IO.Local', 'Penghou.Luban')) {
        $archive = Join-Path $baseline "$($id.ToLowerInvariant())/0.1.0-preview.1/$($id.ToLowerInvariant()).0.1.0-preview.1.nupkg"
        $artifacts += [ordered]@{ consumerBaseline = $id; version = '0.1.0-preview.1'; sha256 = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
    [ordered]@{ dateUtc = [DateTime]::UtcNow.ToString('o'); repository = 'https://github.com/jenolaszlo-sketch/penghou'; branch = (& git -C (Join-Path $PSScriptRoot '..') branch --show-current); sourceCommit = (& git -C (Join-Path $PSScriptRoot '..') rev-parse HEAD); command = 'eng/qualify-io-graduation.ps1'; os = [Environment]::OSVersion.ToString(); frameworks = @('net8.0', 'net10.0'); stableVersion = $StableVersion; facadeVersion = $FacadeVersion; publicPackageRestore = $(if ($PublicPackage) { 'SUPPORTED BY THIS PROBE: fresh public NuGet restore' } else { 'NOT PROVEN: local exact artifact feed' }); ordinaryLubanPackageAdoption = 'NOT PROVEN: published Luban pins IO preview.1 exactly; unchanged DLL deployment qualified only'; evidence = $artifacts } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $outputDirectory 'qualification.json') -Encoding utf8
}
finally {
    if ($null -eq $previousCache) { Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue } else { $env:NUGET_PACKAGES = $previousCache }
}
