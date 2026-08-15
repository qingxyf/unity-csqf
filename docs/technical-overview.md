# 卡牌肉鸽游戏 — 项目技术文档

## 目录

1. [项目概述](#1-项目概述)
2. [项目结构](#2-项目结构)
3. [核心系统详解](#3-核心系统详解)
4. [场景说明](#4-场景说明)
5. [Unity 编辑器配置指南](#5-unity-编辑器配置指南)
6. [扩展指南](#6-扩展指南)
7. [已知问题与待办](#7-已知问题与待办)

---

## 1. 项目概述

一款融合多种玩法的 Unity 2D 游戏：

- **肉鸽地图** — 类杀戮尖塔的节点地图，包含战斗、事件、商店、宝藏、营地、Boss 节点
- **卡牌战斗** — 蓄能式回合制卡牌战斗，初始 6 能量、上限 10、每回合回复 2，含元素反应和收藏品系统
- **弹幕 Boss 战** — 类东方 Project 的弹幕射击 Boss 战，10 种攻击模式 + 狂暴二阶段
- **街机小游戏** — 吃积分 + 连击系统 + 道具 + 追踪敌人
- **音游** — MIDI 驱动的节奏游戏

### 技术栈

- Unity 2D
- C# (Mono)
- TextMeshPro (UI 文本)
- DryWetMIDI (MIDI 解析)

---

## 2. 项目结构

```
Assets/Scripts/
├── CardSystem/                     # 卡牌系统
│   ├── CardData.cs                 # 卡牌数据 (ScriptableObject)
│   ├── CardElement.cs              # 元素枚举
│   ├── CardEffectManager.cs        # 卡牌效果管理器（出牌入口）
│   ├── CardCaster.cs               # 卡牌点击 → 出牌
│   ├── DeckManager.cs              # 牌组管理（抽牌/弃牌/消耗）
│   ├── CardDisplayBase.cs          # 卡牌 UI 显示基类
│   ├── CardDisplay.cs              # 手牌卡牌显示
│   ├── BackpackCardDisplay.cs      # 背包卡牌显示
│   ├── BackpackCardHoverPreview.cs # 卡牌悬停预览
│   └── Effects/                    # 卡牌效果（策略模式）
│       ├── CardEffect.cs           # 抽象基类 + CardEffectContext
│       ├── CardEffectHelper.cs     # 共享工具方法
│       ├── Light/                  # 光系 6 张
│       ├── Fire/                   # 火系 6 张
│       ├── Nature/                 # 草系 6 张
│       ├── Water/                  # 水系 6 张
│       ├── Shadow/                 # 暗影系 6 张
│       └── Neutral/                # 无属性 5 张
│
├── Combat/                         # 战斗系统
│   ├── Enemy.cs                    # 敌人（IDamageable, 状态效果, 元素反应）
│   ├── EnemyManager.cs             # 敌人管理器（单例, 注册/注销）
│   ├── BossController.cs           # Boss 弹幕 AI（10种攻击 + 狂暴阶段）
│   ├── HomingBullet.cs             # 追踪弹组件
│   ├── BossBulletBehavior.cs       # Boss 子弹行为
│   ├── BulletBehavior.cs           # 玩家子弹行为
│   ├── BulletSpawner.cs            # 子弹生成器
│   ├── StatusEffect.cs             # 状态效果基类
│   └── StatusEffects/              # 状态效果实现
│       ├── ElementalEffects.cs     # Burn, Corrosion, Frost, Freeze
│       ├── DamageOverTimeEffects.cs# Poison, Bleed, Stun
│       ├── DebuffEffects.cs        # Weak, Confused, Silenced, Vulnerable, Purified
│       ├── BuffEffects.cs          # Parasite, Regeneration
│       └── StatusEffectFactory.cs  # 工厂方法
│
├── NodeSystem/                     # 肉鸽地图系统
│   ├── Node.cs                     # 节点基类 + NodeType 枚举
│   ├── MapGenerator.cs             # 地图生成（固定每层节点数, 权重随机, 智能连线）
│   ├── GameManager.cs              # 游戏流程控制（单例, 选择节点/完成节点）
│   ├── NodeContentManager.cs       # 节点内容加载（预制体实例化）
│   ├── NodeVisualizer.cs           # 节点连线可视化
│   ├── PathManager.cs              # 路径管理（已精简, 逻辑合并到 GameManager）
│   ├── CampManager.cs              # 营地逻辑（休息/补给/精简卡组）
│   ├── CampCardGiver.cs            # 营地 UI 点击适配器
│   └── Events/                     # 事件系统
│       ├── EventData.cs            # 事件数据 (ScriptableObject)
│       ├── EventManager.cs         # 事件 UI + 选项处理
│       └── EventDataCreator.cs     # 编辑器工具: 批量创建事件资产
│
├── Player/                         # 玩家控制
│   ├── PlayerController.cs         # 场景一玩家（街机模式: 连击/道具/难度递增）
│   └── SimplePlayerController.cs   # Boss 战玩家（移动/射击/血条）
│
├── Interfaces/                     # 接口
│   ├── IDamageable.cs              # 可受伤接口 + DamageType/StatusType 枚举
│   └── ICollectible.cs             # 可收集物接口
│
├── Core/                           # 核心/通用
│   ├── AnimationSwitcher.cs        # 动画切换
│   ├── CameraAspectRatio.cs        # 摄像机比例
│   ├── ChaserEnemy.cs              # 追踪敌人（场景一）
│   ├── MovingTarget.cs             # 移动目标（场景一）
│   ├── StartMenuManager.cs         # 开始菜单
│   ├── SpeedUP.cs                  # 定时道具生成
│   └── NewBehaviourScript.cs       # 定时陷阱生成（待重命名）
│
├── UI/                             # UI 系统
│   ├── BackpackDisplay.cs          # 背包界面
│   ├── MapUIController.cs          # 地图 UI 控制
│   └── RulesDisplay.cs             # 规则展示
│
├── Unused/                         # 音游 / 未使用
│   ├── MidiNoteGenerator.cs        # MIDI 音符生成器
│   └── scoremidi.cs                # 音游计分 (GameTimer)
│
├── PlayerStats.cs                  # 玩家属性（单例, IDamageable）
├── ObjectMover.cs                  # 物体进场移动
└── Editor/
    └── CardImporter.cs             # 卡牌导入工具
```

---

## 3. 核心系统详解

### 3.1 卡牌效果系统

**设计模式：策略模式 (Strategy Pattern)**

```
CardData (ScriptableObject)
  ├── cardName, element, description, cost, cardArt
  └── effect → CardEffect (ScriptableObject, 抽象基类)
                └── Execute(CardEffectContext context)

CardEffectContext:
  ├── Player: PlayerStats
  ├── Target: Enemy
  ├── AllEnemies: List<Enemy>
  ├── Deck: DeckManager
  └── EffectManager: CardEffectManager
```

**出牌流程：**
1. `CardCaster.Cast()` → 从手牌移除卡牌
2. `CardEffectManager.PlayCard()` → 扣能量 → 创建 Context → 调用 `card.effect.Execute(context)`
3. `DeckManager.OnCardPlayed()` → 弃牌/消耗处理

**添加新卡牌：**
1. 在 `Effects/[Element]/` 下创建新的 `XXXEffect.cs`，继承 `CardEffect`
2. Unity 中 Create → Card Effects → [Element] → [Name] 创建资产
3. 创建 CardData 资产，把 Effect 拖上去

### 3.2 状态效果系统

```
StatusEffect (抽象基类)
  ├── Type: StatusType
  ├── Duration: int
  ├── Value: int
  ├── OnTurnStart(IDamageable target)   — 回合开始触发
  ├── OnTurnEnd(IDamageable target)     — 回合结束触发
  ├── OnDamageTaken(target, ref damage) — 受伤时触发
  └── TickDuration() / IsExpired

StatusEffectFactory.Create(type, duration, value) → 具体实例
```

**Enemy 和 PlayerStats 都使用 `List<StatusEffect>` 管理状态。**

**元素反应（在 Enemy.TakeDamageInternal 中触发）：**

| 反应 | 条件 | 效果 |
|------|------|------|
| 燃烧 | 火伤 + (寄生 or 上次草伤) | +基础攻击力伤害 |
| 蒸发 | 冰伤 + 上次火伤 | 移除敌人一个 Buff |
| 丰饶 | 草伤 + 上次冰伤 | 延长敌人一个 Debuff |
| 光暗 | 光+暗 or 暗+光 | 玩家获得 10 平伤减免 |

### 3.3 肉鸽地图系统

**地图结构：**
```
深度 0: 营地（起点）
深度 1~N: 每层 nodesPerLayer 个节点（默认 3）
深度 N+1: Boss（终点）
```

**连线算法 (`ConnectLayers`)：**
1. 每个父节点连接距离最近的子节点（保证父节点不断路）
2. 检查孤立子节点，补连最近的父节点（保证子节点可达）
3. 30% 概率额外连接相邻节点（增加路线多样性）

**特殊层规则：**
- 每 3 层保证有商店
- 每 5 层保证有精英战
- Boss 前一层保证有营地

**节点类型权重（Inspector 可调）：**

| 类型 | 默认权重 | 说明 |
|------|----------|------|
| Battle | 35% | 普通战斗 |
| Event | 25% | 随机事件 |
| Treasure | 15% | 宝藏奖励 |
| Shop | 10% | 商店 |
| EliteBattle | 10% | 精英战 |
| Camp | 5% | 中途营地 |

**流程控制（GameManager 单例）：**
```
Node.OnMouseDown() → GameManager.SelectNode(node)
  → MapGenerator.MoveToNode() — 标记完成, 激活下层
  → 隐藏地图
  → NodeContentManager.LoadNodeContent() — 实例化节点预制体
  → ... 节点内容进行中 ...
  → GameManager.CompleteCurrentNode() — 清除内容, 重开地图
```

### 3.4 事件系统

**数据驱动：**
```
EventData (ScriptableObject)
  ├── eventName: string
  ├── description: string (多行)
  ├── illustration: Sprite (可选)
  └── choices: List<EventChoice>
        ├── buttonText — 按钮文字
        ├── resultDescription — 结果描述
        ├── healthChange — 生命变化 (正=回血, 负=扣血)
        ├── maxHealthChange — 最大生命变化
        ├── cardsToDraw — 抽卡数量
        ├── cardsToRemove — 移除卡牌数量
        ├── healToFull — 回满血
        ├── shieldGain — 获得护盾
        ├── isGamble — 是否赌博型
        ├── gambleSuccessRate — 成功率 (0~1)
        ├── gambleFailText — 失败文字
        └── gambleFailHealthChange — 失败时扣血
```

**内置 8 个事件：** 古老祭坛、受伤的旅人、魔法泉水、赌徒的挑战、被遗弃的宝箱、被遗忘的图书馆、黑暗契约、流浪商人

**添加新事件：** Create → Node System → Event Data，填写字段，拖到 EventManager 的 eventPool 列表

**v0.2 规则：** 负数生命变化视为支付生命，直接扣除 HP，不会被护盾、闪避或减伤吸收。

### 3.5 战斗与节点奖励

- 玩家默认初始能量 6、能量上限 10；每个新回合回复 2 点能量。
- 战斗开始抽 5 张牌，之后每回合抽 2 张；回合结束不弃手牌，手牌上限为 10。
- 战斗 UI 提供常驻基础攻击：消耗 1 能量，造成玩家基础攻击伤害，并立即结束玩家回合。
- 普通战斗胜利奖励金币并展示 3 张卡，玩家可以选择 0-2 张加入卡组。
- 精英战胜利奖励更多金币、自动获得 1 个收藏品，并展示 4 张卡，玩家可以选择 0-2 张加入卡组。
- 跳过奖励可获得少量治疗。
- 商店使用金币购买卡牌、收藏品、回血、刷新货架或移除背包中的随机卡牌；移除服务每次使用后涨价。
- 收藏品第一版支持最大生命提升、能量上限提升、开战护盾、指定元素伤害提升、每回合第一张指定元素卡减费、商店折扣；精英战、部分事件和商店都能获得。

### 3.6 Boss 战斗系统

**10 种攻击模式：**

| # | 名称 | 阶段 | 描述 |
|---|------|------|------|
| 0 | 扇形攻击 | 普通 | 朝玩家方向发射扇形弹幕 |
| 1 | 冲刺攻击 | 普通 | 对准玩家 X 轴后俯冲 |
| 2 | 螺旋攻击 | 普通 | 双螺旋对称弹幕 |
| 3 | 弹幕雨 | 普通 | 屏幕上方随机落下子弹 |
| 4 | 十字弹幕 | 普通 | 旋转十字形弹幕（4臂） |
| 5 | 追踪弹 | 普通 | 发射追踪玩家的子弹 |
| 6 | 散射冲刺 | 普通 | 冲到中央 + 360° 爆发两波 |
| 7 | 连锁闪现 | 狂暴 | 多点瞬移 + 每点释放圆形弹幕 |
| 8 | 引力漩涡 | 狂暴 | 黑洞吸引玩家 + 弹幕攻击 |
| 9 | 狂暴连射 | 狂暴 | 三波交错扇形 + 全屏散射终结 |

**狂暴触发：** Boss 血量降到 50% 以下 → 播放转场动画 → 解锁技能 7-9 → 攻击间隔缩短（3s→2s）

**血条 UI：**
- 玩家血条：绿→黄→红 渐变，Image Type = Filled
- Boss 血条：红色，Image Type = Filled

### 3.7 场景一（街机小游戏）

**核心机制：**
- **连击系统：** 2 秒窗口内连续吃目标触发连击，每连击 +25% 积分倍率
- **随机道具：** 碰到 Target2 随机获得 加速/磁铁/双倍积分/护盾
- **追踪敌人：** ChaserEnemy 有漫游/追踪/冲刺三种行为
- **移动目标：** MovingTarget 四处漂移，可选逃跑模式
- **难度递增：** 每 10 分敌人速度 +15%

### 3.8 音游系统

**MIDI 解析流程：**
1. 加载 MIDI 文件 → 分析所有音轨
2. 自动选择最佳旋律音轨（排除鼓轨 Channel 9，按音域集中度评分）
3. 同一时间点只保留最高音，最小间隔过滤（默认 0.15s）
4. 按音高决定上/下方向（自动取中位音高为分界线）
5. 同方向最多连续 3 个，超过强制切换

---

## 4. 场景说明

| 场景名 | 类型 | 主要脚本 |
|--------|------|----------|
| Start | 开始菜单 | StartMenuManager |
| SampleScene | 场景一（街机） | PlayerController, MovingTarget, ChaserEnemy, SpeedUP |
| thirdscene | Boss 弹幕战 | SimplePlayerController, BossController |
| final | 肉鸽卡牌 | GameManager, MapGenerator, CardEffectManager, DeckManager |
| （音游场景） | 音游 | MidiNoteGenerator, GameTimer |

---

## 5. Unity 编辑器配置指南

### 5.1 首次配置（必须）

#### 战斗场景

1. 创建空 GameObject `EnemyManager` → 挂 `EnemyManager` 组件
2. 确保场景中有 `CardEffectManager`、`DeckManager`、`PlayerStats` 单例对象

#### 卡牌效果资产（35 个）

Project 面板右键 → Create → Card Effects → [元素] → [效果名]

然后打开每个 CardData 资产，将对应的 CardEffect 拖到 `Effect` 字段。

#### 事件资产（12 个）

菜单栏 → Tools → Create Default Events

自动在 `Resources/Events/` 下创建所有事件。

### 5.2 场景一配置

1. PlayerController 绑定 `scoreText`、`comboText`（新建）、`statusText`（新建）
2. 积分目标挂 `MovingTarget` 组件
3. （可选）创建追踪敌人挂 `ChaserEnemy`，Tag 设为 `Target1`
4. （可选）绑定音效字段

### 5.3 Boss 场景配置

1. SimplePlayerController 绑定 `healthBarFill`（玩家血条）和 `bossHealthBarFill`（Boss 血条）
2. 血条制作方法：
   - UI → Image → 设置 Source Image 为任意 Sprite
   - Image Type 改为 `Filled`，Fill Method = Horizontal，Fill Origin = Left
   - 拖到对应字段

### 5.4 肉鸽地图配置

1. 确保场景中有 `GameManager`、`MapGenerator`、`NodeContentManager` 对象
2. MapGenerator 绑定 `nodeTemplate`（节点预制体）、`mapContainer`（地图父对象）
3. NodeContentManager 绑定各类型的内容预制体
4. 事件预制体上挂 `EventManager`，把 EventData 资产拖到 `eventPool`

### 5.5 音游场景配置

1. MIDI 文件放在 `StreamingAssets/` 下
2. MidiNoteGenerator 设置 `midiFileName`
3. `startDelay` 配合音乐播放时机
4. 运行后看 Console 日志调整 `preferredTrackIndex` 和 `minNoteInterval`

---

## 6. 扩展指南

### 添加新卡牌效果

```csharp
// 1. 创建 Scripts/CardSystem/Effects/[Element]/MyNewEffect.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/[Element]/My New Effect")]
public class MyNewEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        // context.Player — 玩家
        // context.Target — 目标敌人
        // context.AllEnemies — 所有敌人
        // context.Deck — 牌组管理器
        // context.EffectManager — 效果管理器
    }
}
```

### 添加新状态效果

```csharp
// 1. 在 IDamageable.cs 的 StatusType 枚举中添加新类型
// 2. 创建具体类
public class MyStatusEffect : StatusEffect
{
    public MyStatusEffect(int duration) : base(StatusType.MyType, duration) { }
    public override void OnTurnStart(IDamageable target) { /* ... */ }
}
// 3. 在 StatusEffectFactory.Create() 中添加 case
```

### 添加新事件

Create → Node System → Event Data，填写字段，拖到 EventManager.eventPool。

不需要写任何代码。

### 添加新节点类型

1. 在 `Node.cs` 的 `NodeType` 枚举中添加
2. 在 `NodeContentManager` 中添加对应的预制体字段
3. 在 `MapGenerator.GetWeightedRandomType()` 中添加权重
4. 创建内容预制体，完成时调用 `GameManager.Instance.CompleteCurrentNode()`

### 添加新 Boss 技能

在 `BossController.cs` 中：
1. 添加参数字段（Header + SerializeField）
2. 写 `private IEnumerator MyNewAttack()` 协程
3. 在 `GetAttackCoroutine()` 的 switch 中添加 case
4. 调整 `AttackRoutine()` 中的 `Random.Range` 上限

---

## 7. 已知问题与待办

### 待清理

- [ ] `Core/sce.cs` — 空文件，可删除
- [ ] `Core/score.cs` — 空文件，可删除
- [ ] `Core/NewBehaviourScript.cs` — 应重命名为 `FreezePickup.cs`
- [ ] `NewBehaviourScript` 和 `SpeedUP` 代码重复，可合并为 `TimedPickup` 基类

### 待实现

- [ ] 商店节点内容预制体
- [ ] 宝藏节点内容预制体
- [ ] 精英战节点内容预制体（更强的敌人配置）
- [ ] 卡牌强化系统（EventChoice 中已预留 `upgradeRandomCard` 字段）
- [x] 战斗结束奖励界面（选卡/加血/加金币）
- [x] 金币系统（商店需要）
- [ ] Boss 死亡动画和通关结算
- [ ] 存档/读档系统

### 关键单例

运行时必须存在以下单例对象，否则相关系统会报错：

| 单例 | 脚本 | 必须存在的场景 |
|------|------|----------------|
| `PlayerStats.Instance` | PlayerStats.cs | 所有战斗相关场景 |
| `DeckManager.Instance` | DeckManager.cs | 所有卡牌相关场景 |
| `CardEffectManager.Instance` | CardEffectManager.cs | 战斗场景 |
| `EnemyManager.Instance` | EnemyManager.cs | 战斗场景 |
| `GameManager.Instance` | GameManager.cs | 肉鸽地图场景 |
