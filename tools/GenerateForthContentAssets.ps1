param(
    [string]$ProjectRoot = (Get-Location).Path
)

$ErrorActionPreference = "Stop"

function New-Guid32 {
    return ([guid]::NewGuid().ToString("N"))
}

function Escape-YamlString([string]$Value) {
    if ($null -eq $Value) { return '""' }
    $escaped = $Value.Replace('\', '\\').Replace('"', '\"').Replace("`r", '').Replace("`n", '\n')
    return '"' + $escaped + '"'
}

function Write-Utf8NoBom([string]$Path, [string]$Text) {
    $encoding = New-Object System.Text.UTF8Encoding($false)
    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($Path)) | Out-Null
    [System.IO.File]::WriteAllText($Path, $Text, $encoding)
}

function Get-MetaGuid([string]$AssetPath) {
    $meta = "$AssetPath.meta"
    if (-not (Test-Path -LiteralPath $meta)) {
        return $null
    }

    $line = Select-String -LiteralPath $meta -Pattern '^guid: ([0-9a-f]{32})' | Select-Object -First 1
    if ($line -and $line.Line -match '^guid: ([0-9a-f]{32})') {
        return $Matches[1]
    }

    return $null
}

function Ensure-TextureMeta([string]$ImagePath) {
    $guid = Get-MetaGuid $ImagePath
    if ($guid) { return $guid }

    $guid = New-Guid32
    $meta = @"
fileFormatVersion: 2
guid: $guid
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: WebGL
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices: 
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {}
  mipmapLimitGroupName: 
  pSDRemoveMatte: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@

    Write-Utf8NoBom "$ImagePath.meta" $meta
    return $guid
}

function Write-NativeAssetMeta([string]$AssetPath) {
    $guid = Get-MetaGuid $AssetPath
    if (-not $guid) { $guid = New-Guid32 }
    $meta = @"
fileFormatVersion: 2
guid: $guid
NativeFormatImporter:
  externalObjects: {}
  mainObjectFileID: 11400000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    Write-Utf8NoBom "$AssetPath.meta" $meta
    return $guid
}

function Write-PrefabMeta([string]$AssetPath) {
    $guid = Get-MetaGuid $AssetPath
    if (-not $guid) { $guid = New-Guid32 }
    $meta = @"
fileFormatVersion: 2
guid: $guid
PrefabImporter:
  externalObjects: {}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"@
    Write-Utf8NoBom "$AssetPath.meta" $meta
    return $guid
}

$eventDataScriptGuid = "d7e979fbed34413468065a17cf115b77"
$eventManagerScriptGuid = "97869656d34d8054b89588d9b8968c1d"
$rewardChoiceScriptGuid = "73511137e9984b043ae5c153ad719f9c"
$shopManagerScriptGuid = "6fce96949741c4b4cbd8a0223c3f7301"
$combatControllerScriptGuid = "69b57864fbea7684db2a898acf71e16f"

$events = @(
    @{
        name="古老祭坛"; file="Event_古老祭坛"; image="event_ancient_altar.png"; desc="你发现了一座被藤蔓缠绕的祭坛。符文微微发亮，像是在等待献祭。";
        choices=@(
            @{button="献上鲜血"; result="符文吸收了鲜血，一股古老的力量强化了你的生命根基。"; health=-15; max=10},
            @{button="祈祷"; result="温暖的光芒笼罩了你，伤口正在愈合。"; health=30},
            @{button="离开"; result="你没有触碰祭坛，继续前进。"}
        )
    },
    @{
        name="受伤的旅人"; file="Event_受伤的旅人"; image="event_wounded_traveler.png"; desc="路边躺着一个奄奄一息的旅人，他虚弱地向你伸出手。";
        choices=@(
            @{button="分享药水"; result="旅人恢复了一些元气，并将珍藏的卡牌交给你作为谢礼。"; health=-10; draw=3},
            @{button="搜刮物品"; result="你找到了一些卡牌，但旅人的眼神让你心里发冷。"; draw=2; max=-5},
            @{button="匆匆离开"; result="你加快脚步离开了。"}
        )
    },
    @{
        name="魔法泉水"; file="Event_魔法泉水"; image="event_magic_fountain.png"; desc="你来到一处被魔力笼罩的泉水边。石碑上刻着：饮者必变。";
        choices=@(
            @{button="饮下泉水"; result="泉水在体内燃烧，你恢复了生命，也获得了新的灵感。"; health=20; draw=1},
            @{button="浸泡伤口"; result="泉水洗去了体内残留的诅咒。你感觉轻松许多。"; health=35},
            @{button="装走泉水"; result="你把泉水灌入瓶中。它化作一张闪烁的卡牌。"; draw=2}
        )
    },
    @{
        name="赌徒的挑战"; file="Event_赌徒的挑战"; image="event_gambler_challenge.png"; desc="戴着面具的赌徒拦住你，把一枚黑白骰子放在桌上。";
        choices=@(
            @{button="接受挑战"; result="骰子停在了幸运的一面，你赢得了赌徒藏起的卡牌。"; gamble=$true; rate=0.55; draw=4; fail="骰子碎裂，阴影反噬了你。"; failHealth=-25},
            @{button="小赌一把"; result="你赢得了一点小奖励。"; gamble=$true; rate=0.75; draw=2; fail="你输掉了一些生命力。"; failHealth=-10},
            @{button="拒绝"; result="你绕开了赌桌。"}
        )
    },
    @{
        name="被遗弃的宝箱"; file="Event_被遗弃的宝箱"; image="event_abandoned_chest.png"; desc="一只落满灰尘的宝箱静静躺在路边，锁孔里透出幽蓝色光芒。";
        choices=@(
            @{button="直接打开"; result="宝箱里有几张保存完好的卡牌。"; draw=3},
            @{button="小心检查"; result="你拆除了机关，只拿走最安全的一部分战利品。"; draw=1; shield=20},
            @{button="强行撬开"; result="机关刺穿了你的手臂，但更深处的奖励也暴露出来。"; health=-15; draw=4}
        )
    },
    @{
        name="被遗忘的图书馆"; file="Event_被遗忘的图书馆"; image="event_forgotten_library.png"; desc="废墟深处有一座安静图书馆。一本书自行翻页，像是在邀请你阅读。";
        choices=@(
            @{button="研读禁忌魔法"; result="知识以疼痛的代价刻入记忆，你获得了更多卡牌技巧。"; health=-20; draw=3; max=5},
            @{button="翻阅治愈术"; result="你按照手册治疗了伤势。"; health=25},
            @{button="整理书架"; result="你在整理书架时重新审视牌组，丢掉了多余负担。"; remove=3}
        )
    },
    @{
        name="黑暗契约"; file="Event_黑暗契约"; image="event_dark_pact.png"; desc="紫色裂隙中传来低语：凡人，我可以给你力量，但一切都有代价。";
        choices=@(
            @{button="交出生命"; result="暗影的力量涌入身体，你用生命上限换来了强大的卡牌。"; max=-15; draw=4},
            @{button="献祭卡牌"; result="裂隙吞噬了你的卡牌，返还为更强韧的生命。"; remove=3; max=15; health=15},
            @{button="拒绝"; result="裂隙缓缓闭合。"}
        )
    },
    @{
        name="流浪商人"; file="Event_流浪商人"; image="event_wandering_merchant.png"; desc="背着巨大背包的商人坐在路边休息，笑眯眯地招呼你。";
        choices=@(
            @{button="查看好货"; result="商人掏出几张闪闪发光的卡牌。"; health=-15; draw=3},
            @{button="清理背包"; result="商人帮你丢掉冗余卡牌，还顺手包扎了伤口。"; remove=2; health=15},
            @{button="聊聊天"; result="你获得了一些旅途经验，精神振奋。"; health=5; shield=10}
        )
    },
    @{
        name="元素神龛"; file="Event_元素神龛"; image="event_element_shrine.png"; desc="五枚元素印记环绕着破碎神龛。每一枚都通向不同的力量。";
        choices=@(
            @{button="触碰火印"; result="火印灼烧掌心，回应你的是躁动的火系牌。"; health=-8; draw=2; useElement=$true; element=1},
            @{button="触碰水印"; result="寒意漫过指尖，你获得了水系卡牌。"; draw=2; useElement=$true; element=3},
            @{button="触碰光印"; result="温和的光回应了你，治愈了伤口。"; health=20; draw=1; useElement=$true; element=0}
        )
    },
    @{
        name="镜中迷宫"; file="Event_镜中迷宫"; image="event_mirror_maze.png"; desc="雾气凝成无数镜面，每个镜中都有一个出牌顺序完全不同的你。";
        choices=@(
            @{button="追逐倒影"; result="倒影消失前留下一把钥匙，打开了隐藏牌匣。"; draw=2; shield=15},
            @{button="打碎镜面"; result="镜片划伤了你，但迷宫随之坍塌，露出核心奖励。"; health=-12; draw=3},
            @{button="闭眼冥想"; result="你不再被倒影诱惑，牌组也因此变得更纯粹。"; remove=2}
        )
    },
    @{
        name="无声花园"; file="Event_无声花园"; image="event_silent_garden.png"; desc="没有风声、虫鸣或脚步声，只有一棵巨树在无声地呼吸。";
        choices=@(
            @{button="在树下休息"; result="无声花粉落在伤口上，你恢复了大量生命。"; health=40},
            @{button="采集种子"; result="种子扎根进牌盒，带来生机系力量。"; draw=2; useElement=$true; element=2},
            @{button="修剪枯枝"; result="你剪去枯枝，也剪去牌组里的累赘。"; remove=1; max=5}
        )
    },
    @{
        name="暴风断桥"; file="Event_暴风断桥"; image="event_storm_bridge.png"; desc="断桥悬在深渊上方，雷雨把桥索打得像琴弦一样颤动。";
        choices=@(
            @{button="冒险冲过"; result="你穿过雷雨，虽然受伤，但捡到散落的卡牌。"; health=-18; draw=3},
            @{button="等待风停"; result="你耐心等待，保存了体力并获得短暂防护。"; shield=25; health=10},
            @{button="呼唤阴影渡鸦"; result="渡鸦载你越过断桥，也带来暗影的馈赠。"; health=-8; draw=2; useElement=$true; element=4}
        )
    }
)

$eventsDir = Join-Path $ProjectRoot "Assets/Resources/Events"
$spritesDir = Join-Path $ProjectRoot "Assets/Sprites/Events"
$prefabDir = Join-Path $ProjectRoot "Assets/Prefabs/NodeContent"
[System.IO.Directory]::CreateDirectory($eventsDir) | Out-Null
[System.IO.Directory]::CreateDirectory($spritesDir) | Out-Null
[System.IO.Directory]::CreateDirectory($prefabDir) | Out-Null

$eventAssetGuids = @{}
foreach ($event in $events) {
    $imagePath = Join-Path $spritesDir $event.image
    if (-not (Test-Path -LiteralPath $imagePath)) {
        throw "Missing generated event image: $imagePath"
    }

    $imageGuid = Ensure-TextureMeta $imagePath
    $assetPath = Join-Path $eventsDir ($event.file + ".asset")
    $assetGuid = Write-NativeAssetMeta $assetPath
    $eventAssetGuids[$event.name] = $assetGuid

    $choiceYaml = New-Object System.Collections.Generic.List[string]
    foreach ($choice in $event.choices) {
        $health = if ($choice.ContainsKey("health")) { $choice.health } else { 0 }
        $max = if ($choice.ContainsKey("max")) { $choice.max } else { 0 }
        $mana = if ($choice.ContainsKey("mana")) { $choice.mana } else { 0 }
        $shield = if ($choice.ContainsKey("shield")) { $choice.shield } else { 0 }
        $draw = if ($choice.ContainsKey("draw")) { $choice.draw } else { 0 }
        $remove = if ($choice.ContainsKey("remove")) { $choice.remove } else { 0 }
        $healFull = if ($choice.ContainsKey("healFull") -and $choice.healFull) { 1 } else { 0 }
        $gamble = if ($choice.ContainsKey("gamble") -and $choice.gamble) { 1 } else { 0 }
        $rate = if ($choice.ContainsKey("rate")) { $choice.rate } else { 0 }
        $fail = if ($choice.ContainsKey("fail")) { $choice.fail } else { "" }
        $failHealth = if ($choice.ContainsKey("failHealth")) { $choice.failHealth } else { 0 }
        $element = if ($choice.ContainsKey("element")) { $choice.element } else { 5 }
        $useElement = if ($choice.ContainsKey("useElement") -and $choice.useElement) { 1 } else { 0 }

        $choiceYaml.Add("  - buttonText: $(Escape-YamlString $choice.button)")
        $choiceYaml.Add("    resultDescription: $(Escape-YamlString $choice.result)")
        $choiceYaml.Add("    healthChange: $health")
        $choiceYaml.Add("    maxHealthChange: $max")
        $choiceYaml.Add("    manaChange: $mana")
        $choiceYaml.Add("    shieldGain: $shield")
        $choiceYaml.Add("    cardsToDraw: $draw")
        $choiceYaml.Add("    cardsToRemove: $remove")
        $choiceYaml.Add("    healToFull: $healFull")
        $choiceYaml.Add("    upgradeRandomCard: 0")
        $choiceYaml.Add("    isGamble: $gamble")
        $choiceYaml.Add("    gambleSuccessRate: $rate")
        $choiceYaml.Add("    gambleFailText: $(Escape-YamlString $fail)")
        $choiceYaml.Add("    gambleFailHealthChange: $failHealth")
        $choiceYaml.Add("    rewardElement: $element")
        $choiceYaml.Add("    useRewardElement: $useElement")
        $choiceYaml.Add("    rewardMaxCost: 99")
        $choiceYaml.Add("    previewCardChoices: 0")
    }

    $asset = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!114 &11400000
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $eventDataScriptGuid, type: 3}
  m_Name: $(Escape-YamlString $event.name)
  m_EditorClassIdentifier: 
  eventName: $(Escape-YamlString $event.name)
  description: $(Escape-YamlString $event.desc)
  illustration: {fileID: 21300000, guid: $imageGuid, type: 3}
  choices:
$($choiceYaml -join "`n")
"@
    Write-Utf8NoBom $assetPath $asset
}

function Write-ContentPrefab([string]$Name, [string]$ScriptGuid, [hashtable]$Fields) {
    $assetPath = Join-Path $prefabDir "$Name.prefab"
    $prefabGuid = Write-PrefabMeta $assetPath
    $rootId = Get-Random -Minimum 1000000000000000000 -Maximum 9000000000000000000
    $transformId = Get-Random -Minimum 1000000000000000000 -Maximum 9000000000000000000
    $monoId = Get-Random -Minimum 1000000000000000000 -Maximum 9000000000000000000

    $fieldLines = New-Object System.Collections.Generic.List[string]
    foreach ($key in $Fields.Keys) {
        $fieldLines.Add("  ${key}: $($Fields[$key])")
    }

    $prefab = @"
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &$rootId
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: $transformId}
  - component: {fileID: $monoId}
  m_Layer: 0
  m_Name: $Name
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &$transformId
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $rootId}
  serializedVersion: 2
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: 0}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
--- !u!114 &$monoId
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: $rootId}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: $ScriptGuid, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
$($fieldLines -join "`n")
"@

    Write-Utf8NoBom $assetPath $prefab
    return @{ guid=$prefabGuid; fileId=$rootId; path=$assetPath }
}

$prefabs = @{}
$prefabs["Event"] = Write-ContentPrefab "EventContent" $eventManagerScriptGuid @{
    eventPool = "[]"
    titleText = "{fileID: 0}"
    descriptionText = "{fileID: 0}"
    illustrationImage = "{fileID: 0}"
    choiceButtonContainer = "{fileID: 0}"
    choiceButtonPrefab = "{fileID: 0}"
    resultPanel = "{fileID: 0}"
    resultText = "{fileID: 0}"
    continueButton = "{fileID: 0}"
    autoBuildUI = 1
    autoLoadResources = 1
}
$prefabs["Treasure"] = Write-ContentPrefab "TreasureContent" $rewardChoiceScriptGuid @{
    choiceCount = 3
    maxCost = 99
    rewardElement = 5
    filterByElement = 0
    title = (Escape-YamlString "宝藏：选择一张卡牌")
    completeNodeAfterPick = 1
    titleText = "{fileID: 0}"
    choiceContainer = "{fileID: 0}"
    choiceButtonPrefab = "{fileID: 0}"
    skipButton = "{fileID: 0}"
}
$prefabs["Shop"] = Write-ContentPrefab "ShopContent" $shopManagerScriptGuid @{
    offerCount = 4
    buyHealthCost = 10
    removeHealAmount = 12
    titleText = "{fileID: 0}"
    offerContainer = "{fileID: 0}"
    offerButtonPrefab = "{fileID: 0}"
    leaveButton = "{fileID: 0}"
}
$prefabs["Battle"] = Write-ContentPrefab "BattleContent" $combatControllerScriptGuid @{
    startCombatOnStart = 1
    cardsPerTurn = 5
    completeNodeOnVictory = 1
    autoCreateManagers = 1
    enemyContainer = "{fileID: 0}"
    enemyPrefab = "{fileID: 0}"
    normalEnemyCount = 2
    eliteEnemyCount = 3
    isEliteBattle = 0
    playerStatsText = "{fileID: 0}"
    deckStatsText = "{fileID: 0}"
    endTurnButton = "{fileID: 0}"
    victoryButton = "{fileID: 0}"
    autoBuildUI = 1
}
$prefabs["EliteBattle"] = Write-ContentPrefab "EliteBattleContent" $combatControllerScriptGuid @{
    startCombatOnStart = 1
    cardsPerTurn = 5
    completeNodeOnVictory = 1
    autoCreateManagers = 1
    enemyContainer = "{fileID: 0}"
    enemyPrefab = "{fileID: 0}"
    normalEnemyCount = 2
    eliteEnemyCount = 3
    isEliteBattle = 1
    playerStatsText = "{fileID: 0}"
    deckStatsText = "{fileID: 0}"
    endTurnButton = "{fileID: 0}"
    victoryButton = "{fileID: 0}"
    autoBuildUI = 1
}
$prefabs["Boss"] = Write-ContentPrefab "BossContent" $combatControllerScriptGuid @{
    startCombatOnStart = 1
    cardsPerTurn = 5
    completeNodeOnVictory = 1
    autoCreateManagers = 1
    enemyContainer = "{fileID: 0}"
    enemyPrefab = "{fileID: 0}"
    normalEnemyCount = 1
    eliteEnemyCount = 1
    isEliteBattle = 1
    playerStatsText = "{fileID: 0}"
    deckStatsText = "{fileID: 0}"
    endTurnButton = "{fileID: 0}"
    victoryButton = "{fileID: 0}"
    autoBuildUI = 1
}

$scenePath = Join-Path $ProjectRoot "Assets/Scenes/forth.unity"
$scene = [System.IO.File]::ReadAllText($scenePath, [System.Text.Encoding]::UTF8)
$scene = [regex]::Replace($scene, 'eventContentPrefab: \{fileID: [^}]+\}', "eventContentPrefab: {fileID: $($prefabs.Event.fileId), guid: $($prefabs.Event.guid), type: 3}")
$scene = [regex]::Replace($scene, 'battleContentPrefab: \{fileID: [^}]+\}', "battleContentPrefab: {fileID: $($prefabs.Battle.fileId), guid: $($prefabs.Battle.guid), type: 3}")
$scene = [regex]::Replace($scene, 'treasureContentPrefab: \{fileID: [^}]+\}', "treasureContentPrefab: {fileID: $($prefabs.Treasure.fileId), guid: $($prefabs.Treasure.guid), type: 3}")
$scene = [regex]::Replace($scene, 'shopContentPrefab: \{fileID: [^}]+\}', "shopContentPrefab: {fileID: $($prefabs.Shop.fileId), guid: $($prefabs.Shop.guid), type: 3}")
$scene = [regex]::Replace($scene, 'eliteBattleContentPrefab: \{fileID: [^}]+\}', "eliteBattleContentPrefab: {fileID: $($prefabs.EliteBattle.fileId), guid: $($prefabs.EliteBattle.guid), type: 3}")
$scene = [regex]::Replace($scene, 'bossContentPrefab: \{fileID: [^}]+\}', "bossContentPrefab: {fileID: $($prefabs.Boss.fileId), guid: $($prefabs.Boss.guid), type: 3}")
Write-Utf8NoBom $scenePath $scene

Write-Output "Generated $($events.Count) EventData assets."
Write-Output "Generated $($prefabs.Count) node content prefabs."
