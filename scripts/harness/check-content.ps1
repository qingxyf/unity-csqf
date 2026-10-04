[CmdletBinding()]
param([string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot "../..")))

$ErrorActionPreference = "Stop"
function Read-Asset([string]$RelativePath) {
    $path = Join-Path $ProjectRoot $RelativePath
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing content: $RelativePath" }
    Get-Content -LiteralPath $path -Raw -Encoding utf8
}
function Read-Guid([string]$RelativePath) {
    $meta = Read-Asset "$RelativePath.meta"
    if ($meta -notmatch '(?m)^guid: ([0-9a-f]{32})\r?$') { throw "Invalid GUID: $RelativePath" }
    $Matches[1]
}
function Check-Pool([string]$Folder, [string]$Script, [int]$Minimum) {
    $assets = @(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot $Folder) -Filter '*.asset')
    if ($assets.Count -lt $Minimum) { throw "$Folder has $($assets.Count) assets, expected at least $Minimum" }
    $scriptGuid = Read-Guid $Script
    foreach ($asset in $assets) {
        $relative = "$Folder/$($asset.Name)"
        $text = Read-Asset $relative
        if ($text -notmatch '%TAG !u! tag:unity3d.com,2011:') { throw "Unity YAML header missing: $relative" }
        if ($text -notmatch "m_Script: \{fileID: 11500000, guid: $scriptGuid, type: 3\}") {
            throw "Wrong MonoScript reference: $relative must use $Script"
        }
        $null = Read-Guid $relative
    }
    $assets
}

$cards = @(Check-Pool 'Assets/Resources/Cards' 'Assets/Scripts/CardSystem/CardData.cs' 61)
$null = Check-Pool 'Assets/Resources/Events' 'Assets/Scripts/NodeSystem/Events/EventData.cs' 20
$null = Check-Pool 'Assets/Resources/Collectibles' 'Assets/Scripts/Collectibles/CollectibleData.cs' 15
foreach ($card in $cards) {
    $cardPath = "Assets/Resources/Cards/$($card.Name)"
    $cardGuid = Read-Guid $cardPath
    $prefab = Read-Asset "Assets/Resources/CardPrefabs/$($card.BaseName).prefab"
    if ($prefab -notmatch "cardData: \{fileID: 11400000, guid: $cardGuid, type: 2\}") {
        throw "Card prefab is not bound to its data: $($card.BaseName)"
    }
    $cardText = Read-Asset $cardPath
    if ($cardText -notmatch '(?m)^  effectId: .+' -or $cardText -notmatch 'cardArt: \{fileID: 21300000, guid: [0-9a-f]{32}, type: 3\}') {
        throw "Missing card effect or art: $cardPath"
    }
}

foreach ($pair in @(@('BattleContent', 'DuskScavenger'), @('EliteBattleContent', 'CopperplumeDuelist'), @('BossContent', 'EclipseArchivist'))) {
    $prefabPath = "Assets/Resources/Enemies/$($pair[1]).prefab"
    $enemy = Read-Asset $prefabPath
    foreach ($pose in @('Idle', 'Attack')) {
        $imagePath = "Assets/Art/Roguelike/Enemies/$($pair[1])/$pose.png"
        $imageGuid = Read-Guid $imagePath
        if ($enemy -notmatch "guid: $imageGuid, type: 3") { throw "Enemy pose not bound: $imagePath" }
        $null = Read-Asset "$imagePath.meta"
        if (-not (Test-Path -LiteralPath (Join-Path $ProjectRoot $imagePath))) { throw "Missing pose: $imagePath" }
    }
    $encounter = Read-Asset "Assets/Prefabs/NodeContent/$($pair[0]).prefab"
    $prefabGuid = Read-Guid $prefabPath
    if ($encounter -notmatch "enemyPrefab: \{fileID: 100000, guid: $prefabGuid, type: 3\}" -or
        $encounter -notmatch 'cardsPerTurn: 2') { throw "Encounter content binding failed: $($pair[0])" }
}
$riceEnemy = Read-Asset 'Assets/Resources/Enemies/RiceKeeper.prefab'
foreach ($pose in @('Idle', 'Attack')) {
    $poseGuid = Read-Guid "Assets/Art/Roguelike/Enemies/RiceKeeper/$pose.png"
    if ($riceEnemy -notmatch "guid: $poseGuid, type: 3") { throw "RiceKeeper pose not bound: $pose" }
}
foreach ($eventArt in @(@('野生营地', 'WildCamp'), @('大白饭的讨债人', 'RiceOwnerReckoning'))) {
    $eventAsset = Read-Asset "Assets/Resources/Events/Event_$($eventArt[0]).asset"
    $artGuid = Read-Guid "Assets/Art/Roguelike/Events/$($eventArt[1]).png"
    if ($eventAsset -notmatch "illustration: \{fileID: 21300000, guid: $artGuid, type: 3\}") {
        throw "Event illustration not bound: $($eventArt[0])"
    }
}
Write-Host "Content check passed: card prefabs, event/collectible MonoScripts, event illustrations and original enemy poses are bound."
