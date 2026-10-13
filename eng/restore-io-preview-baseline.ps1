param([Parameter(Mandatory)][string] $OutputDirectory, [switch] $IncludeReleaseCandidate)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $root) { throw 'Select a fresh baseline restoration directory.' }
New-Item -ItemType Directory -Path $root | Out-Null
$packages = Join-Path $root 'packages'
$refs = @('Penghou.IO.Abstractions', 'Penghou.IO.Protocols', 'Penghou.IO.Local', 'Penghou.Luban') | ForEach-Object { "<PackageReference Include=`"$_`" Version=`"[0.1.0-preview.1]`"/>" }
"<Project Sdk=`"Microsoft.NET.Sdk`"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup>$($refs -join '')</ItemGroup></Project>" | Set-Content -LiteralPath (Join-Path $root 'Baseline.csproj') -Encoding utf8
'<configuration><packageSources><clear/><add key="public-nuget-only" value="https://api.nuget.org/v3/index.json"/></packageSources></configuration>' | Set-Content -LiteralPath (Join-Path $root 'NuGet.Config') -Encoding utf8
dotnet restore (Join-Path $root 'Baseline.csproj') --configfile (Join-Path $root 'NuGet.Config') --packages $packages
if ($LASTEXITCODE -ne 0) { throw 'Public preview baseline restore failed.' }
Write-Output "Restored exact public preview baseline to $packages"
if ($IncludeReleaseCandidate) {
    '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include="Penghou.IO.Identity.Abstractions" Version="[1.0.0-rc.1]"/><PackageReference Include="Penghou.IO.Protocols" Version="[1.0.0-rc.1]"/><PackageReference Include="Penghou.IO.Abstractions" Version="[0.1.0-preview.2]"/></ItemGroup></Project>' | Set-Content -LiteralPath (Join-Path $root 'ReleaseCandidate.csproj') -Encoding utf8
    dotnet restore (Join-Path $root 'ReleaseCandidate.csproj') --configfile (Join-Path $root 'NuGet.Config') --packages $packages
    if ($LASTEXITCODE -ne 0) { throw 'Public release-candidate binary baseline restore failed.' }
}
