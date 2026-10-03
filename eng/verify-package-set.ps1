param(
    [ValidateSet('IO', 'Workflow')]
    [string] $Profile = 'IO',

    [Parameter(Mandatory = $true)]
    [string] $PackageDirectory,

    [Parameter(Mandatory = $true)]
    [string] $PackageVersion
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$packageDirectory = [System.IO.Path]::GetFullPath($PackageDirectory)
[string[]] $expectedIds = if ($Profile -eq 'Workflow') {
    @('Penghou.Workflow.Abstractions')
}
else {
    @('Penghou.IO.Protocols', 'Penghou.IO.Abstractions', 'Penghou.IO.Local')
}
$packages = @(Get-ChildItem -LiteralPath $packageDirectory -File -Filter '*.nupkg' |
    Where-Object { $_.Name -notlike '*.snupkg' })
$symbols = @(Get-ChildItem -LiteralPath $packageDirectory -File -Filter '*.snupkg')

if ($packages.Count -ne $expectedIds.Count) {
    throw "Expected exactly $($expectedIds.Count) NuGet packages in '$packageDirectory'; found $($packages.Count)."
}
if ($symbols.Count -ne $expectedIds.Count) {
    throw "Expected exactly $($expectedIds.Count) symbol packages in '$packageDirectory'; found $($symbols.Count)."
}

$seenIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($package in $packages) {
    $archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $nuspecEntries = @($archive.Entries | Where-Object { $_.FullName -match '(^|/)[^/]+\.nuspec$' })
        if ($nuspecEntries.Count -ne 1) {
            throw "Package '$($package.Name)' must contain exactly one nuspec."
        }
        $entryStream = $nuspecEntries[0].Open()
        try {
            $document = [System.Xml.Linq.XDocument]::Load($entryStream)
        }
        finally {
            $entryStream.Dispose()
        }
        $metadata = $document.Root.Element([System.Xml.Linq.XName]::Get('metadata', $document.Root.Name.NamespaceName))
        $id = $metadata.Element([System.Xml.Linq.XName]::Get('id', $metadata.Name.NamespaceName)).Value
        $version = $metadata.Element([System.Xml.Linq.XName]::Get('version', $metadata.Name.NamespaceName)).Value
        if (-not $seenIds.Add($id)) {
            throw "Duplicate package ID '$id'."
        }
        if ($id -notin $expectedIds) {
            throw "Unexpected package ID '$id' in '$($package.Name)'."
        }
        if ($version -ne $PackageVersion) {
            throw "Package '$id' has version '$version'; expected '$PackageVersion'."
        }
        if (-not $archive.GetEntry('README.md')) {
            throw "Package '$id' is missing the declared README.md."
        }
        if (-not $archive.GetEntry('lib/net8.0/_._') -and
            -not ($archive.Entries | Where-Object { $_.FullName -match '^lib/net8\.0/[^/]+\.dll$' })) {
            throw "Package '$id' does not contain a .NET 8 asset."
        }
        if (-not $archive.GetEntry('lib/net10.0/_._') -and
            -not ($archive.Entries | Where-Object { $_.FullName -match '^lib/net10\.0/[^/]+\.dll$' })) {
            throw "Package '$id' does not contain a .NET 10 asset."
        }
        if ($Profile -eq 'Workflow') {
            $expectedLibraryEntries = @(
                'lib/net10.0/Penghou.Workflow.Abstractions.dll',
                'lib/net10.0/Penghou.Workflow.Abstractions.xml',
                'lib/net8.0/Penghou.Workflow.Abstractions.dll',
                'lib/net8.0/Penghou.Workflow.Abstractions.xml'
            )
            $actualLibraryEntries = @($archive.Entries |
                Where-Object { $_.FullName.StartsWith('lib/', [StringComparison]::Ordinal) } |
                ForEach-Object FullName |
                Sort-Object -CaseSensitive)
            $sortedExpectedLibraryEntries = @($expectedLibraryEntries | Sort-Object -CaseSensitive)
            if ($actualLibraryEntries.Count -ne $sortedExpectedLibraryEntries.Count -or
                [string]::Join("`n", $actualLibraryEntries) -cne [string]::Join("`n", $sortedExpectedLibraryEntries)) {
                throw "Workflow package '$id' must contain only its .NET 8/.NET 10 DLL and XML documentation assets; found: $($actualLibraryEntries -join ', ')."
            }
        }
        if ($metadata.Element([System.Xml.Linq.XName]::Get('repository', $metadata.Name.NamespaceName)).Attribute('url').Value -ne 'https://github.com/jenolaszlo-sketch/penghou') {
            throw "Package '$id' is missing the expected repository URL."
        }
        $dependencies = @($metadata.Descendants() |
            Where-Object { $_.Name.LocalName -eq 'dependency' } |
            ForEach-Object { [pscustomobject]@{ Id = $_.Attribute('id').Value; Version = $_.Attribute('version').Value } })
        if ($Profile -eq 'Workflow' -and $dependencies.Count -ne 0) {
            throw "Workflow package '$id' must have no dependencies; found $($dependencies.Id -join ', ')."
        }
        $internalDependencies = @($dependencies | Where-Object { $_.Id -in $expectedIds })
        $internalDependencyIds = @($internalDependencies | ForEach-Object Id | Select-Object -Unique)
        $requiredInternalDependencies = if ($Profile -eq 'Workflow') { @() } else { switch ($id) {
            'Penghou.IO.Abstractions' { @() }
            'Penghou.IO.Protocols' { @('Penghou.IO.Abstractions') }
            'Penghou.IO.Local' { @('Penghou.IO.Abstractions', 'Penghou.IO.Protocols') }
        } }
        foreach ($requiredDependency in $requiredInternalDependencies) {
            if ($requiredDependency -notin $internalDependencyIds) {
                throw "Package '$id' is missing its '$requiredDependency' package dependency."
            }
        }
        if ($internalDependencyIds.Count -ne $requiredInternalDependencies.Count) {
            throw "Package '$id' has an unexpected internal package dependency set: $($internalDependencyIds -join ', ')."
        }
        foreach ($dependency in $internalDependencies) {
            if ($dependency.Version -ne $PackageVersion) {
                throw "Package '$id' dependency '$($dependency.Id)' uses version '$($dependency.Version)' instead of coordinated version '$PackageVersion'."
            }
        }
        Write-Output "Verified $id $version"
    }
    finally {
        $archive.Dispose()
    }
}

foreach ($id in $expectedIds) {
    if (-not $seenIds.Contains($id)) {
        throw "Package set is missing '$id'."
    }
    if (-not (Test-Path -LiteralPath (Join-Path $packageDirectory "$id.$PackageVersion.snupkg"))) {
        throw "Package set is missing symbols for '$id'."
    }
}
