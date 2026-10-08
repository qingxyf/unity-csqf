[CmdletBinding()]
param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")),
    [string]$UnityPath,
    [switch]$RunBuild,
    [switch]$SkipUnity
)

$ErrorActionPreference = "Stop"
$artifactRoot = Join-Path $ProjectRoot "artifacts\ci"
New-Item -ItemType Directory -Force -Path $artifactRoot | Out-Null

Write-Host "[precompletion] architecture" -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "check-architecture.ps1") -ProjectRoot $ProjectRoot
if (-not $?) { throw "Architecture check failed." }

Write-Host "[precompletion] lint" -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "lint.ps1") -ProjectRoot $ProjectRoot
if (-not $?) { throw "Lint failed." }

Write-Host "[precompletion] content bindings" -ForegroundColor Cyan
& (Join-Path $PSScriptRoot "check-content.ps1") -ProjectRoot $ProjectRoot
if (-not $?) { throw "Content binding check failed." }

Write-Host "[precompletion] git diff --check" -ForegroundColor Cyan
git -C $ProjectRoot diff --check
if ($LASTEXITCODE -ne 0) { throw "Whitespace check failed." }

if ($SkipUnity) {
    Write-Warning "Unity verification was explicitly skipped. CI must run EditMode/PlayMode tests and the builds."
    Write-Host "[precompletion] static checks passed; Unity checks skipped" -ForegroundColor Yellow
    exit 0
}

if ([string]::IsNullOrEmpty($UnityPath)) {
    $command = Get-Command Unity.exe, Unity -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($command) { $UnityPath = $command.Source }
}
if ([string]::IsNullOrEmpty($UnityPath)) {
    $candidates = @(
        "E:\unity\2022.3.49f1c1\Editor\Unity.exe",
        "C:\Program Files\Unity\Hub\Editor\2022.3.49f1c1\Editor\Unity.exe",
        "C:\Program Files\Unity Hub\Editor\2022.3.49f1c1\Editor\Unity.exe"
    )
    $UnityPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if ([string]::IsNullOrEmpty($UnityPath) -or -not (Test-Path $UnityPath)) {
    throw "Unity Editor 2022.3.49f1c1 was not found. Install it or rerun with -SkipUnity; CI remains authoritative."
}

function Invoke-Unity([string[]]$Arguments) {
    $argumentString = ($Arguments | ForEach-Object {
        $argument = [string]$_
        if ($argument -match '[\s"]') {
            '"' + ($argument -replace '"', '\\"') + '"'
        }
        else {
            $argument
        }
    }) -join ' '
    $process = Start-Process -FilePath $UnityPath -ArgumentList $argumentString -WindowStyle Hidden -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "Unity exited with code $($process.ExitCode)."
    }
}

function Read-UnityTestSummary([string]$ResultsPath) {
    if (-not (Test-Path -LiteralPath $ResultsPath)) { return $null }
    try {
        $text = Get-Content -LiteralPath $ResultsPath -Raw -ErrorAction Stop
        if ([string]::IsNullOrWhiteSpace($text)) { return $null }
        [xml]$document = $text
        $root = $document.DocumentElement
        if ($root.Name -ne 'test-run' -or -not $root.HasAttribute('total') -or
            -not $root.HasAttribute('failed') -or -not $root.HasAttribute('result')) { return $null }
        return [pscustomobject]@{
            Result = $root.GetAttribute('result')
            Total = [int]$root.GetAttribute('total')
            Failed = [int]$root.GetAttribute('failed')
        }
    }
    catch {
        # The file can exist while Unity is still writing its XML document.
        return $null
    }
}

function Invoke-UnityUntilResults([string[]]$Arguments, [string]$ResultsPath) {
    $argumentString = ($Arguments | ForEach-Object {
        $argument = [string]$_
        if ($argument -match '[\s"]') {
            '"' + ($argument -replace '"', '\\"') + '"'
        }
        else {
            $argument
        }
    }) -join ' '

    $process = Start-Process -FilePath $UnityPath -ArgumentList $argumentString -WindowStyle Hidden -PassThru
    $deadline = (Get-Date).AddMinutes(5)
    $summary = $null
    while ((Get-Date) -lt $deadline) {
        $summary = Read-UnityTestSummary $ResultsPath
        if ($null -ne $summary) { break }
        Start-Sleep -Seconds 2
        $process.Refresh()
        if ($process.HasExited -and $null -eq (Read-UnityTestSummary $ResultsPath)) {
            throw "Unity exited with code $($process.ExitCode) before producing complete test results."
        }
    }

    if ($null -eq $summary) {
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
        throw "Unity did not produce complete test results within 5 minutes."
    }

    $process.Refresh()
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "[precompletion] typecheck (Unity C# compilation)" -ForegroundColor Cyan
foreach ($testPlatform in @("editmode", "playmode")) {
    $testResults = Join-Path $artifactRoot "$testPlatform-results.xml"
    $testLog = Join-Path $artifactRoot "$testPlatform.log"
    Write-Host "[precompletion] $testPlatform tests" -ForegroundColor Cyan
    if (Test-Path -LiteralPath $testResults) {
        Remove-Item -LiteralPath $testResults -Force
    }
    Invoke-UnityUntilResults @(
        "-batchmode", "-nographics", "-projectPath", $ProjectRoot,
        "-runTests", "-testPlatform", $testPlatform, "-testResults", $testResults,
        "-logFile", $testLog
    ) -ResultsPath $testResults
    if (-not (Test-Path -LiteralPath $testResults)) {
        throw "Unity did not produce $testPlatform test results. See $testLog"
    }
    $compileErrors = Select-String -Path $testLog -Pattern "error CS[0-9]+" -SimpleMatch:$false -ErrorAction SilentlyContinue
    if ($compileErrors) {
        throw "Unity typecheck reported compiler errors. See $testLog"
    }
    $testSummary = Read-UnityTestSummary $testResults
    if ($null -eq $testSummary -or $testSummary.Total -lt 1 -or
        $testSummary.Result -ne 'Passed' -or $testSummary.Failed -ne 0) {
        throw "$testPlatform tests did not pass. See $testResults and $testLog"
    }
    Write-Host "[precompletion] $testPlatform passed: $($testSummary.Total) tests, 0 failures" -ForegroundColor Green
}

if ($RunBuild) {
    $buildLog = Join-Path $artifactRoot "build.log"
    $env:UNITY_BUILD_PATH = "artifacts/ci/qingfeng.exe"
    Write-Host "[precompletion] standalone build" -ForegroundColor Cyan
    Invoke-Unity @(
        "-batchmode", "-nographics", "-projectPath", $ProjectRoot,
        "-executeMethod", "CiBuild.PerformBuild", "-logFile", $buildLog, "-quit"
    )
}

Write-Host "[precompletion] all requested checks passed" -ForegroundColor Green
