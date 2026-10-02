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
    Write-Warning "Unity verification was explicitly skipped. CI must run EditMode tests and the build."
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
    $process = Start-Process -FilePath $UnityPath -ArgumentList $argumentString -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "Unity exited with code $($process.ExitCode)."
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

    $process = Start-Process -FilePath $UnityPath -ArgumentList $argumentString -PassThru
    $deadline = (Get-Date).AddMinutes(5)
    while (-not (Test-Path -LiteralPath $ResultsPath) -and (Get-Date) -lt $deadline) {
        Start-Sleep -Seconds 2
        $process.Refresh()
        if ($process.HasExited -and -not (Test-Path -LiteralPath $ResultsPath)) {
            throw "Unity exited with code $($process.ExitCode) before producing test results."
        }
    }

    if (-not (Test-Path -LiteralPath $ResultsPath)) {
        if (-not $process.HasExited) {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
        throw "Unity did not produce test results within 5 minutes."
    }

    $process.Refresh()
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
    }
}

$testResults = Join-Path $artifactRoot "editmode-results.xml"
$testLog = Join-Path $artifactRoot "editmode.log"
Write-Host "[precompletion] typecheck (Unity C# compilation)" -ForegroundColor Cyan
Write-Host "[precompletion] EditMode tests" -ForegroundColor Cyan
if (Test-Path -LiteralPath $testResults) {
    Remove-Item -LiteralPath $testResults -Force
}
    Invoke-UnityUntilResults @(
        "-batchmode", "-nographics", "-projectPath", $ProjectRoot,
        "-runTests", "-testPlatform", "editmode", "-testResults", $testResults,
        "-logFile", $testLog
    ) -ResultsPath $testResults
if (-not (Test-Path -LiteralPath $testResults)) {
    throw "Unity did not produce EditMode test results. See $testLog"
}
$compileErrors = Select-String -Path $testLog -Pattern "error CS[0-9]+" -SimpleMatch:$false -ErrorAction SilentlyContinue
if ($compileErrors) {
    throw "Unity typecheck reported compiler errors. See $testLog"
}
$testSummary = Get-Content -LiteralPath $testResults -Raw
if ($testSummary -notmatch '<test-run\b[^>]*result="Passed"' -or
    $testSummary -match '<test-run\b[^>]*failed="[1-9][0-9]*"') {
    throw "EditMode tests did not pass. See $testResults and $testLog"
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
