[CmdletBinding()]
param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\.."))
)

$ErrorActionPreference = "Stop"
$failures = [System.Collections.Generic.List[string]]::new()

function Fail([string]$message) {
    $failures.Add($message)
    Write-Host "Architecture violation: $message" -ForegroundColor Red
    Write-Host "Repair: keep editor-only APIs under Assets/Scripts/Editor, keep runtime code asset-optional, and preserve Unity metadata beside every source file." -ForegroundColor Yellow
}

$runtimeRoot = Join-Path $ProjectRoot "Assets\Scripts"
if (-not (Test-Path $runtimeRoot)) {
    Fail "missing Assets/Scripts"
}

$runtimeFiles = @(Get-ChildItem $runtimeRoot -Recurse -File -Filter *.cs |
    Where-Object { $_.FullName -notmatch "[\\/]Editor[\\/]" })
foreach ($file in $runtimeFiles) {
    $text = Get-Content -Raw $file.FullName
    $editorImportIsConditional = $text -match "(?s)#if\s+UNITY_EDITOR.*?using\s+UnityEditor(\.|;)"
    if ($text -match "using\s+UnityEditor(\.|;)" -and -not $editorImportIsConditional) {
        Fail "$($file.FullName.Substring($ProjectRoot.Length + 1)) imports UnityEditor from runtime code"
    }
}

$sourceFiles = @(
    Get-ChildItem (Join-Path $ProjectRoot "Assets") -Recurse -File -Include *.cs,*.asmdef |
        Where-Object { $_.FullName -notmatch "[\\/](Library|Temp|Logs|obj)[\\/]" }
)
foreach ($file in $sourceFiles) {
    if (-not (Test-Path "$($file.FullName).meta")) {
        Fail "$($file.FullName.Substring($ProjectRoot.Length + 1)) has no Unity .meta file"
    }
}

$gatedTests = Get-ChildItem (Join-Path $ProjectRoot "Assets\Tests") -Recurse -File -Filter *.cs -ErrorAction SilentlyContinue |
    Select-String -Pattern "#if\s+ROGUELIKE_V02_RULES_TESTS" -SimpleMatch:$false
if ($gatedTests) {
    Fail "EditMode tests are hidden behind ROGUELIKE_V02_RULES_TESTS"
}

if ($failures.Count -gt 0) {
    throw ("Architecture check failed with {0} violation(s)." -f $failures.Count)
}

Write-Host "Architecture check passed: runtime/editor boundaries, Unity metadata, and test visibility are valid." -ForegroundColor Green
