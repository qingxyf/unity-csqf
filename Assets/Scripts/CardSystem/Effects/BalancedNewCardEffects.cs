using UnityEngine;

public class DawnAmuletEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Player == null) return;

        bool alreadyShielded = context.Player.currentShield > 0;
        context.Player.AddShield(12);
        if (alreadyShielded)
            context.Player.Heal(8);
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        return "获得12护盾，若自身已有护盾，则回复8生命";
    }
}

public class RadiantVerdictEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target == null) return;

        int damage = 24;
        if (context.Target.HasAnyDebuff())
            damage += 16;

        context.Target.TakeDamage(damage, DamageType.Light);
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        int damage = 24 + CollectibleManager.GetDamageBonus(DamageType.Light);
        if (context != null && context.Target != null && context.Target.HasAnyDebuff())
            damage += 16;

        return $"造成{damage}点圣光伤害，若目标有负面效果，则额外造成16点圣光伤害";
    }
}

public class StarEchoEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Player != null)
            context.Player.ApplyStarPrayer(3, 12, 8);
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        return "获得[星祈]3回合";
    }
}

public class EmberChantEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target != null)
            context.Target.TakeDamage(48, DamageType.Fire);
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        int damage = 48 + CollectibleManager.GetDamageBonus(DamageType.Fire);
        return $"造成{damage}点火焰伤害，本回合每使用1张火牌，本牌费用-1";
    }
}

public class ScorchedRingEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.AllEnemies == null) return;

        foreach (Enemy enemy in context.AllEnemies)
        {
            if (enemy == null) continue;

            bool alreadyBurning = enemy.HasStatus(StatusType.Burn);
            enemy.TakeDamage(alreadyBurning ? 36 : 26, DamageType.Fire);
            enemy.ApplyStatus(StatusType.Burn, 2);
        }
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        int damage = 26 + CollectibleManager.GetDamageBonus(DamageType.Fire);
        return $"对所有敌人造成{damage}点火焰伤害并施加[烧伤]，若目标已有[烧伤]，额外造成10点火焰伤害";
    }
}

public class SearingHeartSlashEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Player == null || context.Target == null) return;

        int damage = context.Player.GetAttackDamage() + 18;
        if (context.Target.HasStatus(StatusType.Burn))
        {
            context.Target.RemoveStatus(StatusType.Burn);
            damage += 22;
        }

        context.Target.TakeDamage(damage, DamageType.Fire);
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        int attack = context != null && context.Player != null ? context.Player.GetAttackDamage() : 10;
        int damage = attack + 18 + CollectibleManager.GetDamageBonus(DamageType.Fire);
        if (context != null && context.Target != null && context.Target.HasStatus(StatusType.Burn))
            damage += 22;

        return $"造成{damage}点火焰伤害，若目标有[烧伤]，移除[烧伤]并额外造成22点火焰伤害";
    }
}

public class MossHealEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Player == null) return;

        context.Player.Heal(18);
        if (context.Player.currentHealth < context.Player.maxHealth * 0.5f)
            context.Player.AddShield(10);
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        return "回复18生命，若生命低于50%，获得10护盾";
    }
}

public class VineCounterEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Player == null) return;

        context.Player.AddShield(10);
        context.Player.lifeDamageReductionThisTurn = Mathf.Max(context.Player.lifeDamageReductionThisTurn, 0.1f);
        context.Player.thornCounterAttackDamage = Mathf.Max(context.Player.thornCounterAttackDamage, 8);
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        return "获得10护盾，本回合生命伤害-10%，受击时反击8点生机伤害";
    }
}

public class RootBindEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target == null) return;

        bool alreadyPoisoned = context.Target.HasStatus(StatusType.Poison);
        context.Target.TakeDamage(16, DamageType.Nature);
        if (alreadyPoisoned)
            context.Target.ExtendStatus(StatusType.Poison, 1);
        else
            context.Target.ApplyStatus(StatusType.Poison, 3);
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        int damage = 16 + CollectibleManager.GetDamageBonus(DamageType.Nature);
        return $"造成{damage}点生机伤害并施加[中毒]，若目标已有[中毒]，延长1回合";
    }
}

public class ColdSpringArrowEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target == null) return;

        context.Target.TakeDamage(14, DamageType.Ice);
        context.Target.ApplyStatus(StatusType.Frost, 2);
        if (context.EffectManager != null)
            context.EffectManager.EnableColdSpringBonus(10);
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        int damage = 14 + CollectibleManager.GetDamageBonus(DamageType.Ice);
        return $"造成{damage}点冰霜伤害并施加[冰霜]，获得[冷泉]";
    }
}

public class TidalVeilEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Player != null)
            context.Player.AddShield(18);

        if (context.EffectManager != null)
            context.EffectManager.AddWaterCostReduction(1);
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        return "获得18护盾，下一张水牌费用-1";
    }
}

public class WaveEnergyEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target == null) return;

        bool hasFrost = context.Target.HasStatus(StatusType.Frost);
        context.Target.TakeDamage(22, DamageType.Ice);

        if (hasFrost)
        {
            if (context.Deck != null)
                context.Deck.DrawCardInCombat(1);
            if (context.Player != null)
                context.Player.RestoreMana(1);
        }
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        int damage = 22 + CollectibleManager.GetDamageBonus(DamageType.Ice);
        return $"造成{damage}点冰霜伤害，若目标有[冰霜]，摸1张牌并获得1点能量";
    }
}

public class MoonShadowRaidEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target == null) return;

        context.Target.TakeDamage(20, DamageType.Shadow);
        int roll = Random.Range(0, 3);
        if (roll == 0) context.Target.ApplyStatus(StatusType.Weak, 2);
        else if (roll == 1) context.Target.ApplyStatus(StatusType.Bleed, 2);
        else context.Target.ApplyStatus(StatusType.Corrosion, 2);
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        int damage = 20 + CollectibleManager.GetDamageBonus(DamageType.Shadow);
        return $"造成{damage}点暗影伤害，并随机施加[虚弱]、[流血]、[腐蚀]之一";
    }
}

public class ErodingTouchEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target == null) return;

        bool alreadyCorroded = context.Target.HasStatus(StatusType.Corrosion);
        context.Target.TakeDamage(12, DamageType.Shadow);
        if (alreadyCorroded)
            context.Target.TakeDamage(8, DamageType.Shadow);
        context.Target.ApplyStatus(StatusType.Corrosion, 2);
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        int damage = 12 + CollectibleManager.GetDamageBonus(DamageType.Shadow);
        return $"造成{damage}点暗影伤害并施加[腐蚀]，若目标已有[腐蚀]，额外失去8生命";
    }
}

public class SoulSiphonEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Target == null || context.Player == null) return;

        int debuffCount = Mathf.Min(3, context.Target.CountDebuffs());
        int damage = 40 + debuffCount * 6;
        context.Target.TakeDamage(damage, DamageType.Shadow);
        context.Player.Heal(damage / 2);
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        int debuffCount = context != null && context.Target != null ? Mathf.Min(3, context.Target.CountDebuffs()) : 0;
        int damage = 40 + debuffCount * 6 + CollectibleManager.GetDamageBonus(DamageType.Shadow);
        return $"造成{damage}点暗影伤害，目标每有1个负面效果额外+6，回复一半伤害";
    }
}

public class PackSortEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Deck == null) return;

        CardData discarded = context.Deck.hand.Count > 0 ? context.Deck.hand[0] : null;
        if (discarded != null)
        {
            context.Deck.DiscardSpecificHandCard(discarded);
            if (discarded.element == CardElement.Neutral)
                context.ShufflePlayedCardIntoDrawPile = true;
            context.Deck.DrawCardInCombat(2);
        }
        else
        {
            context.Deck.DrawCardInCombat(1);
        }
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        return "弃置1张牌，摸2张牌；若弃置无属性牌，将本牌洗入抽牌堆";
    }
}

public class SupplyLineEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Player != null)
        {
            context.Player.extraDrawsNextTurn += 1;
            context.Player.extraManaNextTurn += 1;
        }

        if (context.EffectManager != null)
            context.EffectManager.ScheduleSupplyLineReturn(context.Card);
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        return "下回合开始时摸1张牌并获得1点能量；若摸到无属性牌，将本牌洗入抽牌堆";
    }
}

public class DodgeEdgeEffect : CardEffect
{
    public override void Execute(CardEffectContext context)
    {
        if (context.Player == null) return;

        int preserved = Mathf.Min(2, context.Player.currentMana);
        context.Player.extraManaNextTurn += preserved;
        context.Player.AddShield(preserved);

        if (context.EffectManager != null)
        {
            context.EffectManager.ScheduleDodgeReturn(context.Card);
            context.EndPlayerTurnAfterPlay = true;
        }
    }

    public override string GetPreviewDescription(CardPreviewContext context)
    {
        return "结束回合，保留最多2点能量并获得等量护盾；若下回合未损失生命，将本牌洗入抽牌堆";
    }
}
