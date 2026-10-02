using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class RoguelikeEnemyLifecycleTests
{
    private GameObject arena;
    private CardData blockedCard;

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Object.Destroy(arena);
        Object.Destroy(blockedCard);
        if (EnemyManager.Instance != null) Object.Destroy(EnemyManager.Instance.gameObject);
        if (DeckManager.Instance != null) Object.Destroy(DeckManager.Instance.gameObject);
        if (CardEffectManager.Instance != null) Object.Destroy(CardEffectManager.Instance.gameObject);
        if (PlayerStats.Instance != null) Object.Destroy(PlayerStats.Instance.gameObject);
        EnemyManager.Instance = null;
        DeckManager.Instance = null;
        CardEffectManager.Instance = null;
        PlayerStats.Instance = null;
        CollectibleManager.ResetForNewRun();
        yield return null;
    }

    [UnityTest]
    public IEnumerator BossExitFinishesBeforeCombatCompletes()
    {
        arena = new GameObject("Boss exit lifecycle");
        CombatController combat = arena.AddComponent<CombatController>();
        combat.startCombatOnStart = false;
        combat.autoBuildUI = false;
        combat.completeNodeOnVictory = false;
        combat.isBossBattle = true;
        combat.StartCombat();
        yield return null;

        Enemy boss = arena.GetComponentInChildren<Enemy>();
        RoguelikeEnemyPresentation visual = boss.GetComponent<RoguelikeEnemyPresentation>();
        Assert.That(visual.body.GetComponent<Animation>().GetClip("Attack"), Is.Not.Null);
        boss.AttackPlayer();
        Assert.That(visual.CurrentState, Is.EqualTo(RoguelikeEnemyPresentation.MotionState.Attack));
        PlayerStats.Instance.currentHealth = 1;
        PlayerStats.Instance.currentShield = 0;
        PlayerStats.Instance.ApplyStatus(StatusType.Bleed, 2);
        boss.currentHealth = 1;
        Assert.That(combat.UseBasicAttack(boss), Is.True);
        Assert.That(PlayerStats.Instance.currentHealth, Is.EqualTo(1),
            "The lethal basic attack must not advance to a damaging turn.");
        Assert.That(combat.AcceptsPlayerActions, Is.False,
            "Victory must lock input before the next frame.");
        EnemyManager.Instance.ClearNulls();
        Assert.That(EnemyManager.Instance.ActiveEnemies, Has.No.Member(boss));
        Assert.That(visual.CurrentState, Is.EqualTo(RoguelikeEnemyPresentation.MotionState.Death));

        FieldInfo active = typeof(CombatController).GetField("combatActive", BindingFlags.Instance | BindingFlags.NonPublic);
        yield return new WaitForSeconds(0.06f);
        Assert.That(boss != null, Is.True, "The final boss must remain visible during its exit.");
        Assert.That((bool)active.GetValue(combat), Is.True);
        Assert.That(combat.AcceptsPlayerActions, Is.False);
        PlayerStats.Instance.currentMana = 0;
        int healthAfterVictory = PlayerStats.Instance.currentHealth;
        combat.RequestEndPlayerTurn();
        Assert.That(PlayerStats.Instance.currentMana, Is.EqualTo(0));
        Assert.That(PlayerStats.Instance.currentHealth, Is.EqualTo(healthAfterVictory));
        blockedCard = ScriptableObject.CreateInstance<CardData>();
        blockedCard.cardName = "晨辉壁垒";
        blockedCard.cost = 0;
        Assert.That(CardEffectManager.Instance.CanPlayCard(blockedCard), Is.False);
        Assert.That(CardEffectManager.Instance.PlayCard(blockedCard, null), Is.False);
        yield return new WaitForSeconds(0.5f);
        Assert.That(boss == null, Is.True);
        Assert.That((bool)active.GetValue(combat), Is.False);
    }
}
