using System.Collections.Generic;
using UnityEngine;

public static class CollectibleCatalog
{
    public static CollectibleData CreateFallback(string id)
    {
        switch (id)
        {
            case "life_specimen": return Create(id, "生命标本", "最大生命值 +15。", CollectibleEffectType.MaxHealth, 15, 110, "life_specimen");
            case "vital_core": return Create(id, "活力核心", "最大生命值 +25。", CollectibleEffectType.MaxHealth, 25, 160, "vital_core");
            case "energy_core": return Create(id, "能量核心", "能量上限 +1，并获得 1 点当前能量；每场战斗第二回合额外回复 1 点能量。", CollectibleEffectType.MaxMana, 1, 150, "energy_core", secondTurnMana: 1);
            case "aegis_shard": return Create(id, "圣盾碎片", "每场战斗第 2、4、6…回合获得 8 点护盾。", CollectibleEffectType.EverySecondTurnShield, 8, 90, "aegis_shard");
            case "fire_badge": return Create(id, "火焰徽章", "火焰伤害 +10。", CollectibleEffectType.ElementDamageBonus, 10, 120, "fire_badge", DamageType.Fire);
            case "cold_tide_pendant": return Create(id, "寒潮坠饰", "每回合第一张水系卡牌费用 -1。", CollectibleEffectType.FirstElementCardCostReduction, 1, 130, "cold_tide_pendant", DamageType.Physical, CardElement.Water);
            case "old_wallet": return Create(id, "旧钱包", "商店价格 -20%。", CollectibleEffectType.ShopDiscountPercent, 20, 100, "old_wallet");
            case "sun_medallion": return Create(id, "日耀徽记", "光明伤害 +8。", CollectibleEffectType.ElementDamageBonus, 8, 115, "fire_badge", DamageType.Light);
            case "frost_lens": return Create(id, "霜晶透镜", "冰霜伤害 +8。", CollectibleEffectType.ElementDamageBonus, 8, 115, "cold_tide_pendant", DamageType.Ice);
            case "thorn_charm": return Create(id, "荆棘护符", "自然伤害 +8。", CollectibleEffectType.ElementDamageBonus, 8, 115, "life_specimen", DamageType.Nature);
            case "shade_lantern": return Create(id, "影灯", "暗影伤害 +8。", CollectibleEffectType.ElementDamageBonus, 8, 115, "aegis_shard", DamageType.Shadow);
            case "ember_seal": return Create(id, "余烬印记", "每回合第一张火系卡牌费用 -1。", CollectibleEffectType.FirstElementCardCostReduction, 1, 125, "fire_badge", DamageType.Physical, CardElement.Fire);
            case "verdant_ring": return Create(id, "青藤戒指", "每回合第一张自然卡牌费用 -1。", CollectibleEffectType.FirstElementCardCostReduction, 1, 125, "life_specimen", DamageType.Physical, CardElement.Nature);
            case "warding_coin": return Create(id, "守望硬币", "每场战斗第 2、4、6…回合获得 5 点护盾。", CollectibleEffectType.EverySecondTurnShield, 5, 70, "old_wallet");
            case "mana_prism": return Create(id, "法力棱镜", "能量上限 +2，并获得 2 点当前能量；每场战斗第二回合额外回复 2 点能量。", CollectibleEffectType.MaxMana, 2, 210, "energy_core", secondTurnMana: 2);
            case "special_metal_basin": return Create(id, "特殊的金属盆", "每次受到伤害时，有 30% 概率减免 5 点伤害。", CollectibleEffectType.ChanceDamageReduction, 5, 180, "", procChance: 0.3f, eventExclusive: true);
            case "big_rice": return Create(id, "大米饭", "每场战斗的前三回合，每回合恢复 10 点生命和 1 点能量。", CollectibleEffectType.EarlyBattleRecovery, 10, 180, "", turnMana: 1, durationTurns: 3, eventExclusive: true);
            default: return null;
        }
    }

    public static IEnumerable<string> Ids
    {
        get
        {
            return new[] { "life_specimen", "vital_core", "energy_core", "aegis_shard", "fire_badge", "cold_tide_pendant", "old_wallet", "sun_medallion", "frost_lens", "thorn_charm", "shade_lantern", "ember_seal", "verdant_ring", "warding_coin", "mana_prism", "special_metal_basin", "big_rice" };
        }
    }

    public static void EnsureIcon(CollectibleData collectible)
    {
        if (collectible == null || collectible.icon != null) return;
        string iconName = collectible.collectibleId;
        switch (iconName)
        {
            case "sun_medallion": iconName = "fire_badge"; break;
            case "frost_lens": iconName = "cold_tide_pendant"; break;
            case "thorn_charm": iconName = "life_specimen"; break;
            case "shade_lantern": iconName = "aegis_shard"; break;
            case "ember_seal": iconName = "fire_badge"; break;
            case "verdant_ring": iconName = "life_specimen"; break;
            case "warding_coin": iconName = "old_wallet"; break;
            case "mana_prism": iconName = "energy_core"; break;
        }
        collectible.icon = Resources.Load<Sprite>("Icons/Collectibles/" + iconName);
    }

    private static CollectibleData Create(string id, string name, string description, CollectibleEffectType effect, int amount, int price, string iconName, DamageType damageType = DamageType.Physical, CardElement element = CardElement.Neutral, int secondTurnMana = 0, int turnMana = 0, int durationTurns = 0, float procChance = 0f, bool eventExclusive = false)
    {
        CollectibleData collectible = ScriptableObject.CreateInstance<CollectibleData>();
        collectible.collectibleId = id; collectible.collectibleName = name; collectible.description = description;
        collectible.effectType = effect; collectible.amount = amount; collectible.shopPrice = price;
        collectible.damageType = damageType; collectible.element = element;
        collectible.secondTurnManaBonus = secondTurnMana; collectible.turnManaBonus = turnMana;
        collectible.durationTurns = durationTurns; collectible.procChance = procChance; collectible.eventExclusive = eventExclusive;
        collectible.icon = Resources.Load<Sprite>("Icons/Collectibles/" + iconName);
        return collectible;
    }
}
