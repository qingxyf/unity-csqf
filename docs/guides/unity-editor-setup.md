# Unity 内部配置与操作清单

本文档记录为了运行当前卡牌 Roguelike 系统，需要在 Unity 编辑器中检查或配置的内容。大部分内容已经有运行时兜底逻辑，但手动绑定后表现会更稳定、视觉更好。

## 1. 推荐打开场景

1. 打开 `Assets/Scenes/forth.unity` 作为肉鸽地图主场景。
2. 确认 `File > Build Settings` 中包含：`start`、`forth`、`final`。
3. 点击 Play 后，地图会从营地节点开始；如果已有场景对象缺少控制器，`NodeContentManager` 会自动补组件。

## 2. 场景核心对象

### GameManager

- 场景中应有一个 `GameManager` 组件。
- `mapGenerator` 指向场景中的 `MapGenerator`。
- `contentManager` 指向场景中的 `NodeContentManager`。

### MapGenerator

- `nodeTemplate`：拖入 `Assets/Prefabs/Map/NodeTemplate.prefab`。
- `mapContainer`：拖入地图节点父物体或 `Assets/Prefabs/Map/MapContainer.prefab` 实例。
- `nodeVisualizer`：拖入用于画线的 `NodeVisualizer`，可选但推荐。
- `nodeIcons`：为 `Camp/Event/Battle/Treasure/Shop/EliteBattle/Boss` 配图标，可选。

### NodeContentManager

- 可选绑定各节点预制体：`campContentPrefab`、`eventContentPrefab`、`battleContentPrefab`、`treasureContentPrefab`、`shopContentPrefab`、`eliteBattleContentPrefab`、`bossContentPrefab`。
- 如果不绑定，系统会自动创建运行时内容：营地、事件、宝藏、商店、普通战斗、精英战、Boss 兜底战。
- 如果绑定了旧预制体但没有脚本，系统会自动挂上对应控制器。

## 3. 管理器对象

以下管理器可以手动放在场景里，也可以由战斗/商店/宝藏脚本自动创建：

| 管理器 | 建议 |
| --- | --- |
| `DeckManager` | 建议场景中放一个，`cardResourcePath` 保持 `Cards`。 |
| `CardEffectManager` | 建议场景中放一个，用于统一结算出牌。 |
| `EnemyManager` | 可自动创建，战斗中管理敌人列表。 |
| `PlayerStats` | 建议场景中放一个，设置生命、攻击、法力。 |

## 4. 卡牌资源检查

- 卡牌数据位于 `Assets/Resources/Cards/`，运行时通过 `Resources.LoadAll<CardData>("Cards")` 加载。
- 卡牌预制体位于 `Assets/Resources/CardPrefabs/`，用于手牌和背包展示。
- `神圣惩击.asset` 对应预制体当前可命名为 `神圣惩戒.prefab`，代码已做兼容。
- 旧 `CardData.asset` 即使没有手动绑定 `effect`，`CardEffectCatalog` 也会按牌名自动绑定效果。

## 5. 战斗 UI 配置

如果使用自动 UI：

- `CombatController.autoBuildUI` 保持勾选。
- 进入战斗后会自动创建顶部状态栏、结束回合按钮、胜利按钮和手牌区域。

如果使用自定义 UI：

- 给战斗内容物体挂 `CombatController`。
- 绑定 `playerStatsText`、`deckStatsText`、`endTurnButton`、`victoryButton`。
- 给手牌父物体挂 `HandView`，设置 `handContainer`、`cardScale`、`cardSpacing`。

## 6. 事件 UI 配置

如果使用自动 UI：

- `EventManager.autoBuildUI` 保持勾选。
- `eventPool` 可留空，系统会自动读取 `Assets/Resources/Events/`，没有资源时使用内置 12 个事件。

如果使用自定义 UI：

- 绑定 `titleText`、`descriptionText`、`choiceButtonContainer`、`choiceButtonPrefab`、`resultPanel`、`resultText`、`continueButton`。
- `choiceButtonPrefab` 需要包含 `Button` 和子级 `TextMeshProUGUI`。

## 7. 营地、宝藏、商店配置

- `CampManager`：设置 `cardsToGive` 和 `maxCardCost`，默认休息后发 6 张 3 费及以下卡。
- `RewardChoiceUI`：设置 `choiceCount`，默认从隐藏牌池展示 3 张，选择 1 张。
- `ShopManager`：设置 `offerCount`、`buyHealthCost`、`removeHealAmount`，默认用生命购买卡牌。

## 8. 测试流程

1. 打开 `forth.unity`。
2. 点击 Play。
3. 点击起点营地，确认获得卡牌并返回地图。
4. 点击战斗节点，确认自动生成敌人和手牌 UI。
5. 点击手牌出牌，观察敌人扣血；点击“结束回合”进入敌人行动。
6. 击败敌人后点击“领取胜利”，确认回到地图并解锁下一层。
7. 分别测试事件、宝藏、商店节点，确认选项能结算并返回地图。
8. 到达 Boss 节点并胜利后，确认进入 `final` 场景或 Console 显示缺少场景提示。

## 9. 已完成自动化验证

已运行 Unity 批处理编译命令：

```powershell
E:\unity\2022.3.49f1c1\Editor\Unity.exe -batchmode -nographics -quit -projectPath "E:\unity\My project (1)" -logFile "E:\unity\My project (1)\Logs\codex-unity-compile-3.log"
```

结果：脚本编译成功，仅有旧代码未使用字段警告，不影响运行。
