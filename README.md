# 超市晴风 — 卡牌 Roguelike 游戏

一款基于 Unity 开发的回合制卡牌 Roguelike 游戏，灵感来源于《杀戮尖塔》。玩家在程序生成的节点地图上探索，收集卡牌，通过元素搭配与策略组合击败敌人和 Boss。

## 文档

- [技术总览](docs/technical-overview.md)
- [Roguelike 事件与内容](docs/roguelike/events-and-content.md)
- [Unity 编辑器配置指南](docs/guides/unity-editor-setup.md)
- [云端测试、WebGL 网页与 Windows 构建](docs/guides/ci-cd.md)
- [原创战斗小人与动画](docs/roguelike/enemy-art.md)
- [变更记录](docs/changelog.md)

## 游戏玩法

### 节点地图探索

玩家在程序生成的节点地图上推进，每个节点代表不同的事件：

| 节点类型 | 说明 |
|---------|------|
| 营地 (Camp) | 选择休息、补给或精简卡组 |
| 战斗 (Battle) | 普通战斗遭遇 |
| 劲敌 (EliteBattle) | 高难度战斗，高回报 |
| Boss | 单敌人的卡牌 Boss 战；胜利后结束本局 |
| 宝藏 (Treasure) | 获取奖励 |
| 商店 (Shop) | 使用金币购买卡牌、回血、刷新或移除卡牌 |
| 事件 (Event) | 随机事件 |

### 回合制卡牌战斗

- 初始 6 点能量、上限 10 点；每回合回复 2 点能量
- 初始抽 5 张牌，之后每回合抽 2 张；回合结束不弃手牌，手牌上限 10
- 战斗中有常驻 1 能量基础攻击，命中后立即结束玩家回合
- 从背包组成的牌堆中抽牌，使用后进入弃牌堆
- 牌堆抽完后弃牌堆重新洗入
- 普通战斗展示 3 张奖励牌，精英战展示 4 张；都可以选择至多 2 张加入卡组
- 事件可以强化一张未强化卡牌；强化使用运行时克隆，不会修改 Resources 中的原始卡牌
- 精英战、事件和商店可以获得收藏品，例如最大生命值提升、能量上限提升、开战护盾、火焰伤害加成、元素减费
- 通过护盾、回复、控制等手段存活并击败敌人

### 单局流程与结算

`forth.unity` 是独立、可重复游玩的卡牌肉鸽模式：从营地开始，经过地图节点并击败卡牌 Boss 即获胜；任意卡牌战斗中生命归零则失败。胜利和失败都会在本场景显示结算面板，可选择“新开一局”（清除本局卡牌强化、藏品和角色状态后重建地图）或“返回主菜单”。

`final.unity`、`thirdscene.unity` 与其他街机/弹幕内容不属于这条肉鸽流程，Boss 节点不会跳转到它们。本版本不包含存档或读档。

### 六大元素

| 元素 | 关键词 |
|------|--------|
| 光 (Light) | 护盾、净化、圣光伤害 |
| 火 (Fire) | 烧伤、AOE、自残换伤害 |
| 草 (Nature) | 回复、中毒、寄生 |
| 水 (Water) | 冰霜、冰冻、控制 |
| 暗影 (Shadow) | 腐蚀、吸血、高风险高回报 |
| 无属性 (Neutral) | 摸牌、资源管理（使用后移出游戏） |

当前 Resources 池有 **61 张卡牌**，每张都有独立绑定数据的预制体。新增晨辉壁垒、焚烬突袭、荆棘复苏、霜潮回环、幽影收割与远行补给，分别覆盖六种元素。事件池共 **18 个事件**，同轮抽完前不重复；藏品池共 **15 件藏品**，抽取排除已拥有的藏品，集齐后精英奖励改为额外金币。

### 元素反应

不同元素伤害按顺序命中同一敌人时触发元素反应：

| 反应 | 触发条件 | 效果 |
|------|---------|------|
| 燃烧 | 火 → 寄生/草后 | 伤害增加 Base Attack |
| 蒸发 | 冰 → 火后 | 随机清除敌方一个增益 |
| 丰饶生长 | 草 → 冰后 | 延长敌方随机一个减益 1 回合 |
| 光暗双生 | 光↔暗 | 自身下次受伤 -10 |

### 状态效果

**减益：** 烧伤、腐蚀、冰霜、冰冻、中毒、眩晕、流血、混乱、虚弱、沉默、寄生、净化

**增益：** 复苏（每回合回复 20% 最大生命值）

## 项目结构

```
Assets/
├── Animations/              # 动画资源
│   ├── Characters/          # 角色动画（薄绿、羊待机、艾雅法拉）
│   └── Effects/             # 特效动画
├── Audio/                   # 音频
│   ├── Music/               # 背景音乐
│   └── MIDI/                # MIDI 文件
├── Fonts/                   # 字体
├── Plugins/                 # 外部插件 (DryWetMidi)
├── Prefabs/                 # 预制体
│   ├── Cards/               # 卡牌预制体 (35张)
│   ├── Combat/              # 战斗相关（子弹、敌人、营地）
│   └── Map/                 # 地图相关（节点、连线、容器）
├── Resources/               # 运行时加载资源
│   ├── Cards/               # 卡牌数据 (ScriptableObject)
│   └── CardPrefabs/         # 卡牌预制体 (Resources.Load 加载)
├── Scenes/                  # 场景
│   ├── start.unity          # 主菜单
│   ├── SampleScene.unity    # 示例场景
│   ├── thirdscene.unity     # 第三场景
│   ├── forth.unity          # 独立卡牌肉鸽单局场景
│   └── final.unity          # 旧版独立玩法（非肉鸽路由）
├── Scripts/                 # 脚本
│   ├── CardSystem/          # 卡牌系统（数据、显示、施放、效果）
│   ├── Combat/              # 战斗系统（敌人 AI、状态效果）
│   ├── Core/                # 核心管理器（菜单、相机等）
│   ├── Editor/              # 编辑器工具
│   ├── Interfaces/          # 接口定义（IDamageable、枚举）
│   ├── NodeSystem/          # 节点地图（生成、路径、可视化）
│   ├── Player/              # 玩家控制器
│   ├── UI/                  # UI 脚本（背包、地图、规则）
│   └── Unused/              # 旧版脚本（MIDI 相关等）
├── Sprites/                 # 图片资源
│   ├── Backgrounds/         # 背景图
│   ├── Cards/               # 卡牌图片
│   ├── Characters/          # 角色图片
│   └── Icons/               # 图标
├── StreamingAssets/          # 流式加载资源
├── TextMesh Pro/             # TextMeshPro 文本渲染
└── Video/                   # 视频文件
```

## 核心脚本说明

| 脚本 | 路径 | 职责 |
|------|------|------|
| `CardData` | Scripts/CardSystem/ | 卡牌数据定义 (ScriptableObject) |
| `CardEffectManager` | Scripts/CardSystem/ | 所有卡牌效果的实现（switch 分发） |
| `DeckManager` | Scripts/CardSystem/ | 牌组管理：隐藏牌池、背包、抽牌堆、手牌、弃牌堆、移除堆 |
| `CardCaster` | Scripts/CardSystem/ | 点击出牌逻辑 |
| `CardDisplay` | Scripts/CardSystem/ | 卡牌 UI 渲染 |
| `Enemy` | Scripts/Combat/ | 敌人 AI：属性、状态效果处理、元素反应、回合逻辑 |
| `PlayerStats` | Scripts/ | 玩家属性：HP、能量、金币、护盾、增减益、回合处理 |
| `GameManager` | Scripts/NodeSystem/ | 游戏主控制器 |
| `RoguelikeRunController` | Scripts/NodeSystem/ | 新局、胜利/失败结算与返回菜单 |
| `RoguelikeResultPanel` | Scripts/NodeSystem/ | 肉鸽结算面板（缺少 UI 素材时自动生成） |
| `MapGenerator` | Scripts/NodeSystem/ | 程序化地图生成 |
| `PathManager` | Scripts/NodeSystem/ | 玩家路径追踪和节点连接 |
| `Node` | Scripts/NodeSystem/ | 节点数据和类型定义 |

## 技术栈

- **引擎：** Unity 2022.3.49f1c1 (LTS)
- **语言：** C#
- **UI：** TextMesh Pro
- **外部依赖：** Melanchall.DryWetMidi（MIDI 处理，旧版功能）

## 如何运行

1. 使用 **Unity 2022.3.x** 打开项目根目录
2. 等待 Unity 导入资源
3. 打开场景 `Assets/Scenes/start.unity`
4. 点击 Play 运行游戏

## 自动验证

本机有 Unity Editor 时运行：

```powershell
./scripts/harness/precompletion.ps1 -RunBuild
```

如果本机没有 Unity，可先运行静态检查：

```powershell
./scripts/harness/precompletion.ps1 -SkipUnity
```

GitHub Actions 位于 `.github/workflows/unity-ci.yml`，会执行架构/元数据检查、EditMode 与 PlayMode 测试、完整 Windows 构建及仅包含 forth 的 WebGL 构建。仓库需要配置 `UNITY_EMAIL`、`UNITY_PASSWORD`，并提供 Personal 的 `UNITY_LICENSE` 或 Professional 的 `UNITY_SERIAL`。Pages 发布配置见 [CI/CD 指南](docs/guides/ci-cd.md)。事件插画、节点图标和其他可选图片缺失时，运行时会隐藏插画或生成基础节点/连线，不会阻止代码验证。
