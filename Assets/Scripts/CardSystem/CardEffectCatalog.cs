using System;
using System.Collections.Generic;
using UnityEngine;

public static class CardEffectCatalog
{
    private static readonly Dictionary<string, Type> EffectTypes = new Dictionary<string, Type>
    {
        { "圣光庇护", typeof(HolyProtectionEffect) },
        { "神圣惩击", typeof(DivineSmiteEffect) },
        { "神圣惩戒", typeof(DivineSmiteEffect) },
        { "光明祈愿", typeof(LightPrayerEffect) },
        { "晨曦护符", typeof(DawnAmuletEffect) },
        { "辉光裁决", typeof(RadiantVerdictEffect) },
        { "祈星余响", typeof(StarEchoEffect) },
        { "魔法闪耀", typeof(MagicSparkleEffect) },
        { "虔心吟诵", typeof(DevoutChantEffect) },
        { "照耀的荣光", typeof(ShiningGloryEffect) },
        { "火山回响", typeof(VolcanoEchoEffect) },
        { "晨辉壁垒", typeof(MorningGlowBulwarkEffect) },

        { "二重吟唱", typeof(DoubleChantEffect) },
        { "火焰护盾", typeof(FlameShieldEffect) },
        { "点燃", typeof(IgniteEffect) },
        { "生命火种", typeof(LifeSparkEffect) },
        { "烈焰打击", typeof(BlazeStrikeEffect) },
        { "火山", typeof(VolcanoEffect) },
        { "余烬连唱", typeof(EmberChantEffect) },
        { "焦土火环", typeof(ScorchedRingEffect) },
        { "灼心斩", typeof(SearingHeartSlashEffect) },
        { "焚烬突袭", typeof(CinderRushEffect) },

        { "生命滋养", typeof(LifeNourishEffect) },
        { "荆棘缠绕", typeof(ThornEntangleEffect) },
        { "自然守护", typeof(NatureGuardEffect) },
        { "苔痕愈合", typeof(MossHealEffect) },
        { "藤蔓反击", typeof(VineCounterEffect) },
        { "树根缠缚", typeof(RootBindEffect) },
        { "寄生种子", typeof(ParasiteSeedEffect) },
        { "生命之树", typeof(TreeOfLifeEffect) },
        { "无声润物", typeof(SilentMoistureEffect) },
        { "棘藤棒", typeof(ThornStaffEffect) },
        { "荆棘复苏", typeof(ThornRenewalEffect) },

        { "冰霜箭", typeof(FrostArrowEffect) },
        { "寒冰护体", typeof(IceBarrierEffect) },
        { "激流冲刷", typeof(TorrentEffect) },
        { "蚀骨丰泽", typeof(CorrosiveTideEffect) },
        { "冰封领域", typeof(FrozenDomainEffect) },
        { "霜涛覆岭", typeof(FrostTidalEffect) },
        { "冷泉箭", typeof(ColdSpringArrowEffect) },
        { "潮汐护幕", typeof(TidalVeilEffect) },
        { "断浪回能", typeof(WaveEnergyEffect) },
        { "霜潮回环", typeof(FrostTideLoopEffect) },

        { "暗影侵蚀", typeof(ShadowErosionEffect) },
        { "暗夜突袭", typeof(NightRaidEffect) },
        { "月影潜袭", typeof(MoonShadowRaidEffect) },
        { "蚀影触", typeof(ErodingTouchEffect) },
        { "残魂汲取", typeof(SoulSiphonEffect) },
        { "影月庇护", typeof(ShadowMoonEffect) },
        { "未完成之咒", typeof(UnfinishedCurseEffect) },
        { "吞噬生命", typeof(DevourLifeEffect) },
        { "绝望深渊", typeof(DespairAbyssEffect) },
        { "幽影收割", typeof(UmbralHarvestEffect) },

        { "无中生有", typeof(CreateFromNothingEffect) },
        { "粮草先行", typeof(SupplyFirstEffect) },
        { "安营扎寨", typeof(SetUpCampEffect) },
        { "现行等待", typeof(WaitAndSeeEffect) },
        { "精打细算", typeof(CarefulCalculationEffect) },
        { "行囊整理", typeof(PackSortEffect) },
        { "粮线补给", typeof(SupplyLineEffect) },
        { "暂避锋芒", typeof(DodgeEdgeEffect) },
        { "远行补给", typeof(JourneySupplyEffect) }
    };

    private static readonly Dictionary<string, CardEffect> RuntimeEffects = new Dictionary<string, CardEffect>();

    public static CardEffect Resolve(CardData card)
    {
        if (card == null) return null;
        if (card.effect != null) return card.effect;

        string key = card.effectId;
        if (string.IsNullOrEmpty(key))
            key = string.IsNullOrEmpty(card.baseCardName) ? card.cardName : card.baseCardName;
        return Resolve(key);
    }

    public static CardEffect Resolve(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (RuntimeEffects.TryGetValue(key, out CardEffect cached) && cached != null)
            return cached;

        if (!EffectTypes.TryGetValue(key, out Type effectType))
            return null;

        CardEffect effect = ScriptableObject.CreateInstance(effectType) as CardEffect;
        if (effect == null) return null;

        effect.name = key + "_RuntimeEffect";
        RuntimeEffects[key] = effect;
        return effect;
    }

    public static bool HasEffect(CardData card)
    {
        return Resolve(card) != null;
    }
}
