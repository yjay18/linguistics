param(
    [Parameter(Mandatory = $true)]
    [string] $PublishDirectory,

    [Parameter(Mandatory = $true)]
    [string] $OutputDirectory
)

$ErrorActionPreference = "Stop"
$publishPath = [IO.Path]::GetFullPath($PublishDirectory)
$outputPath = [IO.Path]::GetFullPath($OutputDirectory)
$executableName = if ($IsWindows) { "Linguistics.exe" } else { "Linguistics" }
$executablePath = Join-Path $publishPath $executableName
if (-not (Test-Path -LiteralPath $executablePath -PathType Leaf)) {
    throw "Published app host not found at $executablePath"
}

New-Item -ItemType Directory -Path $outputPath -Force | Out-Null
$dataRoot = Join-Path ([IO.Path]::GetTempPath()) ("linguistics-gallery-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $dataRoot | Out-Null

$profile = @'
{
  "schemaVersion": 7,
  "profile": {
    "id": "00000000-0000-0000-0000-000000000008",
    "targetLanguage": { "value": "de" },
    "knownLanguages": [
      {
        "language": { "value": "en" },
        "proficiency": "advanced",
        "comfortableReading": true,
        "comfortableListening": true,
        "allowExplanations": true
      }
    ],
    "settings": {
      "shortcutMode": "askFirst",
      "preferredExplanationLanguage": null,
      "microphone": "never",
      "retainSpeechRecordings": false,
      "selectedLocalModel": null,
      "reduceMotion": true,
      "appLanguageOverride": null
    }
  },
  "curriculum": {
    "progress": [],
    "attempts": [],
    "progressionConfigurationVersion": { "value": "progression-v1" },
    "selectionConfigurationVersion": { "value": "selection-v1" }
  },
  "tasks": { "attempts": [], "reviewHandoffs": [] },
  "pronunciation": { "attempts": [] },
  "review": { "schedules": [], "attempts": [] },
  "lessons": { "lessons": [] }
}
'@

try {
    foreach ($theme in @("Light", "Dark")) {
        $themeName = $theme.ToLowerInvariant()
        $dataPath = Join-Path $dataRoot $themeName
        New-Item -ItemType Directory -Path $dataPath | Out-Null
        Set-Content -LiteralPath (Join-Path $dataPath "learner-profile.json") -Value $profile -Encoding utf8NoBOM

        $capturePath = Join-Path $outputPath ("template-gallery-" + $themeName + ".png")
        $performancePath = Join-Path $outputPath ("template-gallery-" + $themeName + "-performance.json")
        $env:LINGUISTICS_DATA_DIRECTORY = $dataPath
        $env:LINGUISTICS_DEVELOPER_MODE = "1"
        $env:LINGUISTICS_DEVELOPER_PAGE = "TEMPLATEGALLERY"
        $env:LINGUISTICS_DEVELOPER_THEME = $theme.ToUpperInvariant()
        $env:LINGUISTICS_REDUCED_MOTION = "1"
        $env:LINGUISTICS_GALLERY_CAPTURE_PATH = $capturePath
        $env:LINGUISTICS_PERFORMANCE_CAPTURE_PATH = $performancePath

        $process = Start-Process -FilePath $executablePath -PassThru
        if (-not $process.WaitForExit(60000)) {
            $process.Kill($true)
            throw "Template gallery capture timed out for theme $theme"
        }

        if ($process.ExitCode -ne 0) {
            $errorPath = $capturePath + ".error.txt"
            $detail = if (Test-Path -LiteralPath $errorPath) {
                Get-Content -LiteralPath $errorPath -Raw
            } else {
                "No capture error file was written."
            }

            throw "Template gallery capture failed for theme $theme with exit code $($process.ExitCode). $detail"
        }

        $bytes = [IO.File]::ReadAllBytes($capturePath)
        if ($bytes.Length -lt 10000 -or
            $bytes[0] -ne 0x89 -or
            $bytes[1] -ne 0x50 -or
            $bytes[2] -ne 0x4E -or
            $bytes[3] -ne 0x47) {
            throw "Template gallery capture for theme $theme is not a valid non-empty PNG."
        }

        $performance = Get-Content -LiteralPath $performancePath -Raw | ConvertFrom-Json
        if ($performance.schemaVersion -ne 1 -or
            $performance.processEntryToReadyMilliseconds -lt 1 -or
            $performance.contentCatalogLoadMilliseconds -lt 1 -or
            $performance.workingSetBytes -lt 1 -or
            $performance.managedHeapBytes -lt 1 -or
            $performance.animationFrames.sampleCount -ne 30 -or
            $performance.decodedImageCount -gt $performance.maximumDecodedImages -or
            $performance.estimatedDecodedBytes -gt $performance.maximumDecodedBytes) {
            throw "Template gallery performance evidence for theme $theme is invalid or outside its cache bounds."
        }
    }

    $evidence = @(
        "Automated template gallery captures from the published app.",
        "Visual evidence only. These files do not prove interaction, accessibility, or approval.",
        "Performance JSON contains aggregate runner-local timings and memory only. It is not a low-resource benchmark.",
        "Runner OS: $([Environment]::OSVersion.Platform)",
        "Commit: $($env:GITHUB_SHA ?? 'local')"
    )
    Set-Content -LiteralPath (Join-Path $outputPath "EVIDENCE.txt") -Value $evidence -Encoding utf8NoBOM
}
finally {
    if (Test-Path -LiteralPath $dataRoot) {
        Remove-Item -LiteralPath $dataRoot -Recurse -Force
    }
}
