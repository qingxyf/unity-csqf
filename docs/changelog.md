# 代码重构改动清单

## 概览

本次重构涉及 **6大步骤** 的代码架构改造 + Boss新技能 + 场景一玩法改写 + 音游MIDI修复。

---

## 一、代码改动汇总

### 第1步：卡牌效果 — 策略模式重构

**新建文件：**
- `Scripts/CardSystem/Effects/CardEffect.cs` — 抽象基类 + CardEffectContext
- `Scripts/CardSystem/Effects/CardEffectHelper.cs` — 共享工具方法
- `Scripts/CardSystem/Effects/Light/HolyProtectionEffect.cs` — 圣光庇护
- `Scripts/CardSystem/Effects/Light/DivineSmiteEffect.cs` — 神圣惩击
- `Scripts/CardSystem/Effects/Light/LightPrayerEffect.cs` — 光明祈愿
- `Scripts/CardSystem/Effects/Light/MagicSparkleEffect.cs` — 魔法闪耀
- `Scripts/CardSystem/Effects/Light/DevoutChantEffect.cs` — 虔心吟诵
- `Scripts/CardSystem/Effects/Light/ShiningGloryEffect.cs` — 照耀的荣光
- `Scripts/CardSystem/Effects/Fire/DoubleChantEffect.cs` — 二重吟唱
- `Scripts/CardSystem/Effects/Fire/FlameShieldEffect.cs` — 火焰护盾
- `Scripts/CardSystem/Effects/Fire/IgniteEffect.cs` — 点燃
- `Scripts/CardSystem/Effects/Fire/LifeSparkEffect.cs` — 生命火种
- `Scripts/CardSystem/Effects/Fire/BlazeStrikeEffect.cs` — 烈焰打击
- `Scripts/CardSystem/Effects/Fire/VolcanoEffect.cs` — 火山
- `Scripts/CardSystem/Effects/Nature/LifeNourishEffect.cs` — 生命滋养
- `Scripts/CardSystem/Effects/Nature/ThornEntangleEffect.cs` — 荆棘缠绕
- `Scripts/CardSystem/Effects/Nature/NatureGuardEffect.cs` — 自然守护
- `Scripts/CardSystem/Effects/Nature/ParasiteSeedEffect.cs` — 寄生种子
- `Scripts/CardSystem/Effects/Nature/TreeOfLifeEffect.cs` — 生命之树
- `Scripts/CardSystem/Effects/Nature/SilentMoistureEffect.cs` — 无声润物
- `Scripts/CardSystem/Effects/Water/FrostArrowEffect.cs` — 冰霜箭
- `Scripts/CardSystem/Effects/Water/IceBarrierEffect.cs` — 寒冰护体
- `Scripts/CardSystem/Effects/Water/TorrentEffect.cs` — 激流冲刷
- `Scripts/CardSystem/Effects/Water/CorrosiveTideEffect.cs` — 蚀骨丰泽
- `Scripts/CardSystem/Effects/Water/FrozenDomainEffect.cs` — 冰封领域
- `Scripts/CardSystem/Effects/Water/FrostTidalEffect.cs` — 霜涛覆岭
- `Scripts/CardSystem/Effects/Shadow/ShadowErosionEffect.cs` — 暗影侵蚀
- `Scripts/CardSystem/Effects/Shadow/NightRaidEffect.cs` — 暗夜突袭
- `Scripts/CardSystem/Effects/Shadow/ShadowMoonEffect.cs` — 影月庇护
- `Scripts/CardSystem/Effects/Shadow/UnfinishedCurseEffect.cs` — 未完成之咒
- `Scripts/CardSystem/Effects/Shadow/DevourLifeEffect.cs` — 吞噬生命
- `Scripts/CardSystem/Effects/Shadow/DespairAbyssEffect.cs` — 绝望深渊
- `Scripts/CardSystem/Effects/Neutral/CreateFromNothingEffect.cs` — 无中生有
- `Scripts/CardSystem/Effects/Neutral/SupplyFirstEffect.cs` — 粮草先行
- `Scripts/CardSystem/Effects/Neutral/SetUpCampEffect.cs` — 安营扎寨
- `Scripts/CardSystem/Effects/Neutral/WaitAndSeeEffect.cs` — 现行等待
- `Scripts/CardSystem/Effects/Neutral/CarefulCalculationEffect.cs` — 精打细算

**修改文件：**
- `Scripts/CardSystem/CardData.cs` — 删除 `effectId`，新增 `CardEffect effect` 字段
- `Scripts/CardSystem/CardEffectManager.cs` — 460行→110行，删除整个switch，改为调用 `card.effect.Execute(context)`

### 第2步：CardDisplay 去重

**新建文件：**
- `Scripts/CardSystem/CardDisplayBase.cs` — 抽象基类，包含 RefreshDisplay、ReplaceKeywords、InsertLineBreaks、EnsureArtForUI

**修改文件：**
- `Scripts/CardSystem/CardDisplay.cs` — 231行→18行，继承 CardDisplayBase，只保留 FixTextSizeForUI
- `Scripts/CardSystem/BackpackCardDisplay.cs` — 247行→35行，继承 CardDisplayBase，只保留 FixTextSizeForUI

### 第3步：状态效果系统重构

**新建文件：**
- `Scripts/Combat/StatusEffect.cs` — 状态效果基类
- `Scripts/Combat/StatusEffects/ElementalEffects.cs` — BurnEffect, CorrosionEffect, FrostEffect, FreezeEffect
- `Scripts/Combat/StatusEffects/DamageOverTimeEffects.cs` — PoisonEffect, BleedEffect, StunEffect
- `Scripts/Combat/StatusEffects/DebuffEffects.cs` — WeakEffect, ConfusedEffect, SilencedEffect, VulnerableEffect, PurifiedEffect
- `Scripts/Combat/StatusEffects/BuffEffects.cs` — ParasiteEffect, RegenerationEffect
- `Scripts/Combat/StatusEffects/StatusEffectFactory.cs` — 工厂方法

**修改文件：**
- `Scripts/Combat/Enemy.cs` — 用 `List<StatusEffect>` 替代 `Dictionary<StatusType, StatusEffectData>`，删除 ProcessTurnStart/ProcessTurnEnd 中的 switch

### 第4步：PlayerStats 重构

**修改文件：**
- `Scripts/PlayerStats.cs` — 实现 IDamageable 接口，`burnTurns`/`corrosionTurns`/`frostTurns`/`bleedTurns` 改为 StatusEffect 系统的属性访问器，删除死代码 `DrawCards()`

### 第5步：EnemyManager

**新建文件：**
- `Scripts/Combat/EnemyManager.cs` — 单例管理器，维护 ActiveEnemies 列表

**修改文件：**
- `Scripts/Combat/Enemy.cs` — OnEnable/OnDisable 中注册/注销
- 所有 `FindObjectsOfType<Enemy>()` 调用已替换为 `EnemyManager.Instance.ActiveEnemies`

### 第6步：清理与修复

**修改文件：**
- `Scripts/CardSystem/CardCaster.cs` — 修复双重移除 bug（OnCardPlayed 只调用一次），替换 FindObjectOfType 为 EnemyManager
- 删除死代码：`effectId`、`volcanoStacks`、`DrawCards()`

### Boss 新技能

**新建文件：**
- `Scripts/Combat/HomingBullet.cs` — 追踪弹组件

**修改文件：**
- `Scripts/Combat/BossController.cs` — 新增4个技能（弹幕雨、十字弹幕、追踪弹、散射冲刺），提取 SpawnBullet 工具方法
- `Scripts/Player/SimplePlayerController.cs` — 修复 `hitCount` 未递增 bug，修复胜利判定逻辑，修复血量显示逻辑

### 场景一玩法改写

**新建文件：**
- `Scripts/Interfaces/ICollectible.cs` — 可收集物接口
- `Scripts/Core/ChaserEnemy.cs` — 追踪敌人（漫游/追踪/冲刺三种行为）
- `Scripts/Core/MovingTarget.cs` — 会移动的积分目标（可选逃跑模式）

**修改文件：**
- `Scripts/Player/PlayerController.cs` — 完全重写，新增连击系统、随机道具（加速/磁铁/双倍积分/护盾）、扣分惩罚、无敌帧、难度递增

### 音游 MIDI 修复

**修改文件：**
- `Scripts/Unused/MidiNoteGenerator.cs` — 完全重写：自动选择旋律音轨、按音高决定上下方向、最小间隔过滤、同方向连续限制、未接统计
- `Scripts/Unused/scoremidi.cs` — 结算面板增强：显示得分/未接/总计/准确率

---

## 二、你需要在 Unity 编辑器中完成的操作

### 必须做（否则无法运行）

#### 2.1 创建 EnemyManager

1. 在**战斗场景**中创建一个空 GameObject，命名为 `EnemyManager`
2. 挂上 `EnemyManager` 组件
3. 确保该对象在战斗开始时就存在（Enemy 的 OnEnable 会自动注册）

#### 2.2 创建 35 个 CardEffect ScriptableObject 资产

对每张卡牌，需要创建对应的效果资产并绑定到 CardData：

| 操作 | 步骤 |
|------|------|
| 创建资产 | Project 面板右键 → Create → Card Effects → [元素] → [效果名] |
| 绑定到卡牌 | 打开每个 CardData 资产 → 在 Inspector 中将创建的 CardEffect 拖到 `Effect` 字段 |

完整清单（35个）：

**光系（Create → Card Effects → Light →）**
- Holy Protection → 绑定到"圣光庇护"的 CardData
- Divine Smite → 绑定到"神圣惩击"
- Light Prayer → 绑定到"光明祈愿"
- Magic Sparkle → 绑定到"魔法闪耀"
- Devout Chant → 绑定到"虔心吟诵"
- Shining Glory → 绑定到"照耀的荣光"

**火系（Create → Card Effects → Fire →）**
- Double Chant → 绑定到"二重吟唱"
- Flame Shield → 绑定到"火焰护盾"
- Ignite → 绑定到"点燃"
- Life Spark → 绑定到"生命火种"
- Blaze Strike → 绑定到"烈焰打击"
- Volcano → 绑定到"火山"

**草系（Create → Card Effects → Nature →）**
- Life Nourish → 绑定到"生命滋养"
- Thorn Entangle → 绑定到"荆棘缠绕"
- Nature Guard → 绑定到"自然守护"
- Parasite Seed → 绑定到"寄生种子"
- Tree of Life → 绑定到"生命之树"
- Silent Moisture → 绑定到"无声润物"

**水系（Create → Card Effects → Water →）**
- Frost Arrow → 绑定到"冰霜箭"
- Ice Barrier → 绑定到"寒冰护体"
- Torrent → 绑定到"激流冲刷"
- Corrosive Tide → 绑定到"蚀骨丰泽"
- Frozen Domain → 绑定到"冰封领域"
- Frost Tidal → 绑定到"霜涛覆岭"

**暗影系（Create → Card Effects → Shadow →）**
- Shadow Erosion → 绑定到"暗影侵蚀"
- Night Raid → 绑定到"暗夜突袭"
- Shadow Moon → 绑定到"影月庇护"
- Unfinished Curse → 绑定到"未完成之咒"
- Devour Life → 绑定到"吞噬生命"
- Despair Abyss → 绑定到"绝望深渊"

**无属性（Create → Card Effects → Neutral →）**
- Create From Nothing → 绑定到"无中生有"
- Supply First → 绑定到"粮草先行"
- Set Up Camp → 绑定到"安营扎寨"
- Wait And See → 绑定到"现行等待"
- Careful Calculation → 绑定到"精打细算"

#### 2.3 CardData 资产检查

旧的 `effectId` 字段已被删除。打开每个 CardData 确认 Inspector 中不再报错，且 `Effect` 字段已正确赋值。

---

### 场景一相关配置

#### 2.4 PlayerController UI 绑定

PlayerController 新增了3个 UI 引用，需要在 Inspector 中绑定：

| 字段 | 用途 | 建议 |
|------|------|------|
| `scoreText` | 显示积分 | 原有的就行 |
| `comboText` | 显示连击数 | 新建一个 TextMeshProUGUI，放在屏幕上方 |
| `statusText` | 显示当前状态效果 | 新建一个 TextMeshProUGUI，放在积分下方 |

#### 2.5 音效配置（可选）

PlayerController 新增了3个音效字段：
- `collectSound` — 吃到积分目标时播放
- `comboBreakSound` — 连击被打断时播放
- `powerUpSound` — 吃到道具时播放
- `chunriyingSound` — 碰到 Target3 时播放春日影（原有功能保留）

没有音效也能正常运行，只是没有声音反馈。`chunriyingSound` 如果原来已经绑定过需要重新拖一次。

#### 2.6 让积分目标动起来（可选但推荐）

给场景中的积分目标（Tag="Target"的物体）挂上 `MovingTarget` 组件：
- `moveSpeed` — 移动速度，建议 1~2
- `fleesFromPlayer` — 勾选后目标会在玩家靠近时逃跑（适合高分目标）
- `fleeRange` — 逃跑触发距离

#### 2.7 添加追踪敌人（可选但推荐）

1. 创建一个带 SpriteRenderer 和 Collider2D（Is Trigger 勾选）的 GameObject
2. 设置 Tag 为 `Target1`
3. 挂上 `ChaserEnemy` 组件
4. 调整参数：
   - `baseChaseSpeed` — 追踪速度（建议 2~3）
   - `detectionRange` — 感知范围（建议 4~6）
   - `chargeSpeed` — 冲刺速度（建议 6~10）

---

### Boss 场景配置

#### 2.8 Boss 新技能参数调整

BossController 新增了以下 Inspector 参数，使用默认值即可，也可按需调整：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `rainBulletCount` | 40 | 弹幕雨的子弹数量 |
| `rainWidth` | 8 | 弹幕雨的横向覆盖宽度 |
| `crossBulletsPerArm` | 8 | 十字弹幕每条臂的子弹数 |
| `crossRotateSpeed` | 45 | 十字弹幕每波旋转角度 |
| `homingBulletCount` | 5 | 追踪弹数量 |
| `homingTurnSpeed` | 120 | 追踪弹转向速度（越大越准） |
| `homingLifetime` | 4 | 追踪弹存在秒数 |
| `dashBurstBullets` | 12 | 散射冲刺每波子弹数 |

不需要额外操作，新技能会自动加入 Boss 的随机攻击池。

---

### 音游场景配置

#### 2.9 MidiNoteGenerator 参数调整

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `preferredTrackIndex` | -1 | -1=自动选最佳旋律音轨，可手动指定（看Console日志的音轨评分） |
| `startDelay` | 0 | 开始前延迟秒数，配合音乐播放时机（原来的 TIME_OFFSET 已删除） |
| `minNoteInterval` | 0.15 | 最小音符间隔（秒），调大=音符更稀疏 |
| `pitchThreshold` | 60 | 音高分界线，默认自动检测 |
| `autoDetectThreshold` | true | 自动用中位音高划分上下 |
| `maxConsecutiveSameDirection` | 3 | 同方向最多连续几个 |

**调试建议：**
1. 先运行一次，看 Console 日志：会打印每个音轨的音符数、音域、评分
2. 如果自动选的音轨不对，手动设置 `preferredTrackIndex` 为你想要的音轨编号
3. 如果音符太密，调大 `minNoteInterval`（比如 0.25）
4. 如果上下分布不均，关闭 `autoDetectThreshold`，手动调 `pitchThreshold`

---

## 三、可选清理（非必须）

以下是发现的其他问题，可以后续处理：

- [ ] 删除 `Scripts/Core/sce.cs` — 空文件，所有方法都被注释了
- [ ] 删除 `Scripts/Core/score.cs` — 空文件
- [ ] 重命名 `Scripts/Core/NewBehaviourScript.cs` → 更有意义的名字（如 `FreezePickup.cs`）
- [ ] `NewBehaviourScript` 和 `SpeedUP` 代码几乎完全相同，可以合并为一个 `TimedPickup` 基类
- [ ] `CampManager.cs` 和 `CampCardGiver.cs` 有重复逻辑，可以合并
- [ ] `Node.cs` 中有多处 `FindObjectOfType` 调用，可以改为缓存引用
