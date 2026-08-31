[CmdletBinding()]
param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\.."))
)

$ErrorActionPreference = "Stop"
$runtimeRoot = Join-Path $ProjectRoot "Assets\Scripts"
$controllerFiles = @(
    "NodeSystem\CampManager.cs",
    "NodeSystem\Events\EventManager.cs",
    "NodeSystem\RewardChoiceUI.cs",
    "NodeSystem\ShopManager.cs",
    "CardSystem\CombatController.cs"
)

$failures = [System.Collections.Generic.List[string]]::new()
foreach ($relativePath in $controllerFiles) {
    $path = Join-Path $runtimeRoot $relativePath
    if (-not (Test-Path $path)) {
        $failures.Add("missing node controller: $relativePath")
        continue
    }

    $source = Get-Content -Raw $path
    if ($source -notmatch ":\s*NodeContentController\b") {
        $failures.Add("$relativePath must inherit NodeContentController")
    }
    if ($source -match "GameManager\.Instance\.CompleteCurrentNode\s*\(") {
        $failures.Add("$relativePath bypasses the bound node completion gate")
    }
}

$gameManager = Get-Content -Raw (Join-Path $runtimeRoot "NodeSystem\GameManager.cs")
if ($gameManager -notmatch "TryCompleteCurrentNode\(Node expectedNode, int expectedSession\)") {
    $failures.Add("GameManager is missing the session-aware completion gate")
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "Lint violation: $_" -ForegroundColor Red }
    throw ("Lint failed with {0} violation(s)." -f $failures.Count)
}

Write-Host "Lint passed: node controllers use the shared lifecycle and session-aware completion gate." -ForegroundColor Green
