param(
    [Parameter(Mandatory = $true)]
    [string] $PublishDirectory,

    [string] $RepositoryRoot = "."
)

$ErrorActionPreference = "Stop"
$publishPath = [IO.Path]::GetFullPath($PublishDirectory)
$repositoryPath = [IO.Path]::GetFullPath($RepositoryRoot)
$contentPath = Join-Path $publishPath "Content"

$noticeCopies = @(
    @("docs/content-license.md", "Content/content-license.md"),
    @("docs/third-party-notices.md", "Content/third-party-notices.md")
)
foreach ($pair in $noticeCopies) {
    $source = Join-Path $repositoryPath $pair[0]
    $published = Join-Path $publishPath $pair[1]
    if (-not (Test-Path -LiteralPath $published -PathType Leaf)) {
        throw "Required published notice is missing: $($pair[1])"
    }

    if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne
        (Get-FileHash -LiteralPath $published -Algorithm SHA256).Hash) {
        throw "Published notice differs from its reviewed source: $($pair[1])"
    }
}

$expectedNativeHashes = @{
    "SkiaSharp-HarfBuzzSharp-THIRD-PARTY-NOTICES.txt" = "21504c46c4c58aa64c1055bd2dcbc5f9a136b4b8c412ed3cc6740e22c5b127f5"
    "ANGLE-LICENSE.txt" = "54aff7276217df9f6b5181613999d208c9e40d2b1d51bf55217837e6871a4a63"
}
foreach ($item in $expectedNativeHashes.GetEnumerator()) {
    $path = Join-Path $contentPath $item.Key
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required native notice is missing: $($item.Key)"
    }

    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -ne $item.Value) {
        throw "Published native notice hash changed: $($item.Key)"
    }
}

$manifestPath = Join-Path $contentPath "languages/de/assets.json"
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.assets.Count -ne 12) {
    throw "Expected 12 audited bundled assets, found $($manifest.assets.Count)."
}

foreach ($asset in $manifest.assets) {
    $assetPath = Join-Path (Split-Path -Parent $manifestPath) $asset.file
    if (-not (Test-Path -LiteralPath $assetPath -PathType Leaf)) {
        throw "Bundled asset is missing: $($asset.id)"
    }

    $file = Get-Item -LiteralPath $assetPath
    if ($file.Length -ne $asset.byteSize) {
        throw "Bundled asset size changed: $($asset.id)"
    }

    if ((Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $asset.sha256) {
        throw "Bundled asset hash changed: $($asset.id)"
    }

    if ($asset.license.identifier.StartsWith("CC-BY-SA-") -and
        $asset.transformation.isDerivative -and
        -not $asset.transformation.shareAlikeObligationsRetained) {
        throw "CC BY-SA derivative obligations are not retained: $($asset.id)"
    }
}
