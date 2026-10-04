#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class RiceKeeperVisualPreviewPlayModeTests
{
    private GameObject gameObject;
    private RiceKeeperVisualPreview preview;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        EventPool.ResetForNewRun();
        CollectibleManager.ResetForNewRun();
        gameObject = new GameObject("Rice preview test game");
        gameObject.AddComponent<GameManager>();
        preview = gameObject.AddComponent<RiceKeeperVisualPreview>();
        yield return null;
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (GameManager.Instance != null && GameManager.Instance.contentManager != null)
            GameManager.Instance.contentManager.ClearCurrentContent();
        if (EnemyManager.Instance != null) Object.Destroy(EnemyManager.Instance.gameObject);
        if (CardEffectManager.Instance != null) Object.Destroy(CardEffectManager.Instance.gameObject);
        if (DeckManager.Instance != null) Object.Destroy(DeckManager.Instance.gameObject);
        if (PlayerStats.Instance != null) Object.Destroy(PlayerStats.Instance.gameObject);
        if (gameObject != null) Object.Destroy(gameObject);
        EnemyManager.Instance = null;
        CardEffectManager.Instance = null;
        DeckManager.Instance = null;
        PlayerStats.Instance = null;
        GameManager.Instance = null;
        EventPool.ResetForNewRun();
        CollectibleManager.ResetForNewRun();
        yield return null;
    }

    [UnityTest]
    public IEnumerator PreviewUsesRealEventButtonsAndStartsTheRiceKeeperCombat()
    {
        yield return WaitForEvent("wild-rice-camp");
        Assert.That(preview.CurrentStep, Is.EqualTo(RiceKeeperVisualPreview.PreviewStep.WildRiceCamp));
        Assert.That(DeckManager.Instance.backpack.Count, Is.GreaterThanOrEqualTo(10));

        Vector3 authoredPlayerPosition = new Vector3(-0.66f, -2.13f, 0f);
        PlayerStats.Instance.transform.position = authoredPlayerPosition;
        SpriteRenderer playerBody = PlayerStats.Instance.gameObject.AddComponent<SpriteRenderer>();
        playerBody.sprite = Resources.Load<GameObject>("Enemies/RiceKeeper")
            .GetComponent<RoguelikeEnemyPresentation>().idleSprite;

        Choices(preview.CurrentEventManager)[1].onClick.Invoke();
        Assert.That(EventPool.IsUnlocked("rice-owner-reckoning"), Is.True);
        preview.CurrentEventManager.continueButton.onClick.Invoke();

        yield return WaitForEvent("rice-owner-reckoning");
        Assert.That(preview.CurrentStep, Is.EqualTo(RiceKeeperVisualPreview.PreviewStep.RiceOwnerReckoning));
        Choices(preview.CurrentEventManager)[1].onClick.Invoke();
        yield return null;
        yield return null;

        CombatController combat = preview.ActiveCombat;
        Assert.That(combat, Is.Not.Null);
        Assert.That(combat.GetComponentInChildren<HandView>(), Is.Not.Null);
        Assert.That(DeckManager.Instance.hand.Count, Is.GreaterThan(0));
        Enemy challenger = combat.GetComponentInChildren<Enemy>();
        Assert.That(challenger, Is.Not.Null);
        Assert.That(challenger.enemyName, Is.EqualTo("蓝色大肥鱼"));
        Assert.That(challenger.maxHealth, Is.EqualTo(240));
        Assert.That(challenger.baseAttack, Is.EqualTo(24));
        Assert.That(challenger.currentShield, Is.EqualTo(20));
        Assert.That(challenger.transform.position.x, Is.GreaterThan(PlayerStats.Instance.transform.position.x));
        Assert.That(PlayerStats.Instance.transform.position.y, Is.GreaterThan(authoredPlayerPosition.y));

        int playerHealth = PlayerStats.Instance.currentHealth;
        int enemyShield = challenger.currentShield;
        int attackDamage = PlayerStats.Instance.GetAttackDamage();
        Assert.That(combat.basicAttackButton.interactable, Is.True);
        combat.basicAttackButton.onClick.Invoke();
        Assert.That(challenger.currentShield, Is.EqualTo(enemyShield - attackDamage));
        Assert.That(PlayerStats.Instance.currentHealth, Is.EqualTo(playerHealth - challenger.GetAttackDamage()));
        RoguelikeEnemyPresentation presentation = challenger.GetComponent<RoguelikeEnemyPresentation>();
        Assert.That(presentation, Is.Not.Null);
        Assert.That(presentation.CurrentState, Is.EqualTo(RoguelikeEnemyPresentation.MotionState.Attack));

        combat.gameObject.SetActive(false);
        Assert.That(PlayerStats.Instance.transform.position, Is.EqualTo(authoredPlayerPosition));
    }

    private IEnumerator WaitForEvent(string eventId)
    {
        for (int frame = 0; frame < 20; frame++)
        {
            if (preview.CurrentEventManager != null && preview.CurrentEventManager.CurrentEvent != null &&
                preview.CurrentEventManager.CurrentEvent.eventId == eventId)
                yield break;
            yield return null;
        }
        Assert.Fail("Timed out waiting for event " + eventId);
    }

    private static Button[] Choices(EventManager manager)
    {
        return manager.choiceButtonContainer.GetComponentsInChildren<Button>();
    }
}
#endif
