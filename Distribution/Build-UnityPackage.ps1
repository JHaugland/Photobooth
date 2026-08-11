[CmdletBinding()]
param(
    [string]$OutputDirectory = "Dist"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$packageRoot = Join-Path $repositoryRoot "Packages/com.jhaugland.photobooth"
$packageManifestPath = Join-Path $packageRoot "package.json"

if (-not (Test-Path $packageManifestPath -PathType Leaf)) {
    throw "Package manifest not found at '$packageManifestPath'."
}

$packageManifest = Get-Content $packageManifestPath -Raw | ConvertFrom-Json
$version = $packageManifest.version
if ($version -notmatch '^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$') {
    throw "Package version '$version' is not valid semantic versioning."
}

$outputRoot = if ([IO.Path]::IsPathRooted($OutputDirectory)) {
    [IO.Path]::GetFullPath($OutputDirectory)
} else {
    [IO.Path]::GetFullPath((Join-Path $repositoryRoot $OutputDirectory))
}
$artifactPath = Join-Path $outputRoot "Photobooth-$version.unitypackage"
$stagingRoot = Join-Path ([IO.Path]::GetTempPath()) (
    "PhotoboothUnityPackage-" + [Guid]::NewGuid().ToString("N"))

$contentRoots = @("Editor", "Prefabs", "Profiles", "Scenes")
$rootFiles = @("README.md", "LICENSE.md", "CHANGELOG.md")
$assets = [Collections.Generic.List[object]]::new()

foreach ($contentRoot in $contentRoots) {
    $sourceRoot = Join-Path $packageRoot $contentRoot
    if (-not (Test-Path $sourceRoot -PathType Container)) {
        throw "Required package folder '$contentRoot' is missing."
    }

    Get-ChildItem $sourceRoot -Recurse -File |
        Where-Object { $_.Extension -ne ".meta" } |
        ForEach-Object {
            $relative = [IO.Path]::GetRelativePath($packageRoot, $_.FullName)
            $assets.Add([pscustomobject]@{
                Source = $_.FullName
                Destination = "Assets/Photobooth/" + $relative.Replace('\', '/')
            })
        }
}

foreach ($rootFile in $rootFiles) {
    $source = Join-Path $packageRoot $rootFile
    if (-not (Test-Path $source -PathType Leaf)) {
        throw "Required package file '$rootFile' is missing."
    }
    $assets.Add([pscustomobject]@{
        Source = $source
        Destination = "Assets/Photobooth/$rootFile"
    })
}

$documentationPath = Join-Path $packageRoot "Documentation~/index.md"
if (-not (Test-Path $documentationPath -PathType Leaf)) {
    throw "Package documentation is missing."
}
$assets.Add([pscustomobject]@{
    Source = $documentationPath
    Destination = "Assets/Photobooth/Documentation/README.md"
})

$forbidden = $assets | Where-Object {
    $_.Destination -match '(^|/)(Tests?|Samples?|Synty)(/|$)' -or
    [IO.Path]::GetExtension($_.Source) -in @(".dll", ".so", ".dylib")
}
if ($forbidden) {
    throw "Forbidden distribution content: $($forbidden.Destination -join ', ')"
}

try {
    New-Item $stagingRoot -ItemType Directory -Force | Out-Null
    New-Item $outputRoot -ItemType Directory -Force | Out-Null

    foreach ($asset in $assets) {
        $metaPath = $asset.Source + ".meta"
        if (-not (Test-Path $metaPath -PathType Leaf)) {
            throw "Missing meta file for '$($asset.Source)'. Import the package in Unity first."
        }

        $guidLine = Get-Content $metaPath -TotalCount 4 |
            Where-Object { $_ -match '^guid:\s*([0-9a-f]{32})$' } |
            Select-Object -First 1
        if (-not $guidLine) {
            throw "Could not read a Unity GUID from '$metaPath'."
        }

        $guid = ($guidLine -replace '^guid:\s*', '')
        $guidDirectory = Join-Path $stagingRoot $guid
        New-Item $guidDirectory -ItemType Directory -Force | Out-Null
        Copy-Item $asset.Source (Join-Path $guidDirectory "asset")
        Copy-Item $metaPath (Join-Path $guidDirectory "asset.meta")
        [IO.File]::WriteAllText(
            (Join-Path $guidDirectory "pathname"),
            $asset.Destination,
            [Text.UTF8Encoding]::new($false))
    }

    Remove-Item $artifactPath -ErrorAction SilentlyContinue
    & tar -czf $artifactPath -C $stagingRoot .
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path $artifactPath -PathType Leaf)) {
        throw "tar failed to create '$artifactPath'."
    }

    $archiveEntries = & tar -tzf $artifactPath
    if ($LASTEXITCODE -ne 0 -or -not ($archiveEntries -match '/pathname$')) {
        throw "The generated Unity package could not be inspected."
    }

    $hash = Get-FileHash $artifactPath -Algorithm SHA256
    $checksumPath = Join-Path $outputRoot "SHA256SUMS.txt"
    [IO.File]::WriteAllText(
        $checksumPath,
        "$($hash.Hash.ToLowerInvariant())  $([IO.Path]::GetFileName($artifactPath))`n",
        [Text.UTF8Encoding]::new($false))

    Write-Output "Created $artifactPath"
    Write-Output "Packaged $($assets.Count) assets"
    Write-Output "SHA256 $($hash.Hash.ToLowerInvariant())"
}
finally {
    if (Test-Path $stagingRoot) {
        Remove-Item $stagingRoot -Recurse -Force
    }
}
