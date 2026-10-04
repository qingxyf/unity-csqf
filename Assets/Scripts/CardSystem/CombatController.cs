using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatController : NodeContentController
{
    private static Sprite runtimeEnemySprite;

    [Header("Combat Settings")]
    public bool startCombatOnStart = true;
    public int cardsPerTurn = 2;
    public bool completeNodeOnVictory = true;
    public bool autoCreateManagers = true;

    [Header("Enemy Spawning")]
    public Transform enemyContainer;
    public GameObject enemyPrefab;
    public int normalEnemyCount = 2;
    public int eliteEnemyCount = 3;
    public bool isEliteBattle;
    public bool isBossBattle;

    [Header("Rewards")]
    public int battleGoldReward = 15;
    public int eliteGoldReward = 35;
    public bool showCardRewardOnVictory = true;

    [Header("UI")]
    public TextMeshProUGUI playerStatsText;
    public TextMeshProUGUI deckStatsText;
    public Button endTurnButton;
    public Button basicAttackButton;
    public Button victoryButton;
    public Transform collectibleIconContainer;
    public bool autoBuildUI = true;

    private bool combatActive;
    private bool resolvingTurn;
    private bool rewardShown;
    private int displayedCollectibleCount = -1;
    public bool AcceptsPlayerActions => combatActive && !resolvingTurn && !rewardShown;
    public EventCombatEncounter EventEncounter { get; private set; }

    /// <summary>Configure a single event challenger before Start runs.</summary>
    public bool ConfigureEventEncounter(EventCombatEncounter encounter)
    {
        if (combatActive || encounter == null ||
            string.IsNullOrWhiteSpace(encounter.enemyResourcePath) ||
            encounter.maxHealth <= 0 || encounter.attackDamage < 0 || encounter.initialShield < 0)
            return false;

        GameObject challenger = Resources.Load<GameObject>(encounter.enemyResourcePath);
        if (challenger == null || challenger.GetComponent<Enemy>() == null)
            return false;

        EventEncounter = encounter;
        enemyPrefab = challenger;
        eliteEnemyCount = 1;
        isEliteBattle = true;
        isBossBattle = false;
        return true;
    }

    private void Awake()
    {
        if (autoCreateManagers)
        {
            DeckManager.EnsureInstance();
            CardEffectManager.EnsureInstance();
            EnemyManager.EnsureInstance();
            PlayerStats.EnsureInstance();
        }
    }

    private void Start()
    {
        if (autoBuildUI)
            EnsureRuntimeUI();

        if (endTurnButton != null) endTurnButton.onClick.AddListener(EndPlayerTurn);
        if (basicAttackButton != null) basicAttackButton.onClick.AddListener(() => UseBasicAttack());
        if (victoryButton != null)
        {
            victoryButton.onClick.AddListener(CompleteCombat);
            victoryButton.gameObject.SetActive(false);
        }

        if (startCombatOnStart)
            StartCombat();
    }

    private void Update()
    {
        // Once victory is recorded, only the visual exit/reward remains.
        if (!combatActive || rewardShown) return;

        if (PlayerStats.Instance != null && PlayerStats.Instance.IsDead())
        {
            ResolvePlayerDefeat();
            return;
        }

        UpdateUI();
        CheckVictory();
    }

    public void StartCombat()
    {
        PlayerStats.EnsureInstance();
        DeckManager.EnsureInstance();
        CardEffectManager.EnsureInstance();
        EnemyManager.EnsureInstance();
        combatActive = true;
        resolvingTurn = false;
        rewardShown = false;

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.ClearNulls();

        if (EnemyManager.Instance == null || EnemyManager.Instance.ActiveEnemies.Count == 0)
            SpawnDefaultEnemies();

        if (DeckManager.Instance != null)
            DeckManager.Instance.StartCombat();

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.BeginCombat();

        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.OnPlayerTurnStart();

        UpdateUI();
    }

    public void EndPlayerTurn()
    {
        RequestEndPlayerTurn();
    }

    public void RequestEndPlayerTurn()
    {
        if (!AcceptsPlayerActions) return;
        // A lethal attack can request a turn before the next Update sees victory.
        CheckVictory();
        if (!AcceptsPlayerActions) return;

        resolvingTurn = true;

        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.OnPlayerTurnEnd();

        RunEnemyTurn();

        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.OnEnemyTurnEnd();

        if (PlayerStats.Instance != null && PlayerStats.Instance.IsDead())
        {
            ResolvePlayerDefeat();
            return;
        }

        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.DrawCardInCombat(cardsPerTurn);
        }

        if (CardEffectManager.Instance != null)
            CardEffectManager.Instance.OnPlayerTurnStart();

        resolvingTurn = false;
        UpdateUI();
    }

    public bool UseBasicAttack()
    {
        return UseBasicAttack(GetDefaultTarget());
    }

    public bool UseBasicAttack(Enemy target)
    {
        if (!AcceptsPlayerActions) return false;
        if (target == null || target.IsDead()) return false;
        if (PlayerStats.Instance == null) return false;
        if (PlayerStats.Instance.currentMana < 1) return false;

        PlayerStats.Instance.currentMana -= 1;
        target.TakeDamage(PlayerStats.Instance.GetAttackDamage(), DamageType.Physical);
        RequestEndPlayerTurn();
        return true;
    }

    public void CompleteCombat()
    {
        if (!combatActive) return;

        combatActive = false;

        if (DeckManager.Instance != null)
            DeckManager.Instance.EndCombat();

        if (completeNodeOnVictory)
            TryCompleteNode();
    }

    /// <summary>
    /// Finishes the active encounter as a run defeat. The terminal run
    /// controller is idempotent, while this combat guard prevents the deck
    /// from being returned twice when damage is observed on later frames.
    /// </summary>
    public bool ResolvePlayerDefeat()
    {
        if (!combatActive)
            return false;

        combatActive = false;
        resolvingTurn = false;
        if (DeckManager.Instance != null)
            DeckManager.Instance.EndCombat();

        RoguelikeRunController run = GameManager.Instance != null
            ? GameManager.Instance.RunController
            : null;
        if (run == null)
            run = FindObjectOfType<RoguelikeRunController>();

        return run != null && run.FailRun();
    }

    private void RunEnemyTurn()
    {
        if (EnemyManager.Instance == null) return;

        foreach (Enemy enemy in new List<Enemy>(EnemyManager.Instance.ActiveEnemies))
        {
            if (enemy != null && !enemy.IsDead())
                enemy.AttackPlayer();
        }
    }

    private void CheckVictory()
    {
        if (EnemyManager.Instance == null) return;
        EnemyManager.Instance.ClearNulls();

        if (EnemyManager.Instance.ActiveEnemies.Count > 0) return;
        if (rewardShown) return;

        if (isBossBattle)
        {
            rewardShown = true;
            UpdateUI();
            float exitDelay = 0f;
            foreach (RoguelikeEnemyPresentation visual in GetComponentsInChildren<RoguelikeEnemyPresentation>())
                exitDelay = Mathf.Max(exitDelay, visual.RemainingDeathDuration);
            if (Application.isPlaying && exitDelay > 0f)
                StartCoroutine(CompleteBossAfterExit(exitDelay));
            else
                CompleteCombat();
            return;
        }

        if (TryShowVictoryReward())
            return;

        if (victoryButton != null)
            victoryButton.gameObject.SetActive(true);
        else
            CompleteCombat();
    }

    private IEnumerator CompleteBossAfterExit(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (combatActive) CompleteCombat();
    }

    private bool TryShowVictoryReward()
    {
        if (!showCardRewardOnVictory) return false;
        if (DeckManager.Instance == null) return false;

        int goldReward = isEliteBattle ? eliteGoldReward : battleGoldReward;
        if (PlayerStats.Instance != null)
            PlayerStats.Instance.GainGold(goldReward);

        CollectibleData collectibleReward = null;
        if (isEliteBattle)
        {
            collectibleReward = CollectibleManager.CreateRandomCollectible();
            CollectibleManager.AddCollectible(collectibleReward);
            if (collectibleReward == null && PlayerStats.Instance != null)
                PlayerStats.Instance.GainGold(25);
        }

        DeckManager.Instance.EndCombat();

        rewardShown = true;
        combatActive = false;

        GameObject rewardObject = new GameObject(isEliteBattle ? "EliteVictoryReward" : "BattleVictoryReward");
        // Keep the reward under the active combat content so it is destroyed
        // together with the node and cannot complete a later node.
        rewardObject.transform.SetParent(transform, false);
        RewardChoiceUI reward = rewardObject.AddComponent<RewardChoiceUI>();
        reward.BindNode(BoundNode, BoundSessionToken);
        reward.title = isEliteBattle
            ? $"精英奖励  +{goldReward} 金币  +{(collectibleReward != null ? collectibleReward.collectibleName : "25 金币（藏品已集齐）") }"
            : $"战斗奖励  +{goldReward} 金币";
        reward.choiceCount = isEliteBattle ? 4 : 3;
        reward.maxPicks = 2;
        reward.maxCost = isEliteBattle ? 99 : 3;
        reward.skipHealAmount = isEliteBattle ? 12 : 6;
        return true;
    }

    private void SpawnDefaultEnemies()
    {
        int count = EventEncounter != null ? 1 :
            (isBossBattle ? 1 : (isEliteBattle ? eliteEnemyCount : normalEnemyCount));
        if (enemyPrefab == null)
            enemyPrefab = Resources.Load<GameObject>("Enemies/" +
                (isBossBattle ? "EclipseArchivist" : (isEliteBattle ? "CopperplumeDuelist" : "DuskScavenger")));
        if (enemyPrefab == null)
        {
            for (int i = 0; i < count; i++)
                CreateRuntimeEnemy(i, count);
            return;
        }

        Transform parent = enemyContainer != null ? enemyContainer : transform;
        for (int i = 0; i < count; i++)
        {
            GameObject enemyObject = Instantiate(enemyPrefab, parent);
            enemyObject.transform.localPosition = GetEnemyPosition(i, count);
            Enemy enemy = enemyObject.GetComponent<Enemy>();
            if (enemy != null)
            {
                ConfigureEnemy(enemy, i);
                if (enemy.hpText == null) CreateRuntimeEnemyHealthLabel(enemyObject, enemy);
            }
        }
    }

    private void CreateRuntimeEnemy(int index, int count)
    {
        string enemyName = isBossBattle ? "CardBoss" : (isEliteBattle ? $"EliteEnemy_{index + 1}" : $"Enemy_{index + 1}");
        GameObject enemyObject = new GameObject(enemyName);
        enemyObject.transform.SetParent(enemyContainer != null ? enemyContainer : transform, false);
        enemyObject.transform.localPosition = GetEnemyPosition(index, count);

        SpriteRenderer renderer = enemyObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetRuntimeEnemySprite();
        renderer.color = isBossBattle
            ? new Color(0.44f, 0.17f, 0.12f)
            : (isEliteBattle ? new Color(0.7f, 0.2f, 0.9f) : new Color(0.8f, 0.25f, 0.25f));
        renderer.sortingOrder = 4;

        CircleCollider2D collider = enemyObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.55f;

        Enemy enemy = enemyObject.AddComponent<Enemy>();
        ConfigureEnemy(enemy, index);
        CreateRuntimeEnemyHealthLabel(enemyObject, enemy);
    }

    private static Sprite GetRuntimeEnemySprite()
    {
        if (runtimeEnemySprite != null)
            return runtimeEnemySprite;

        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;

        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.45f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                byte alpha = distance <= radius ? (byte)255 : (byte)0;
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        runtimeEnemySprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        runtimeEnemySprite.name = "RuntimeEnemySprite";
        runtimeEnemySprite.hideFlags = HideFlags.HideAndDontSave;
        return runtimeEnemySprite;
    }

    private void CreateRuntimeEnemyHealthLabel(GameObject enemyObject, Enemy enemy)
    {
        GameObject canvasObject = new GameObject("EnemyHealthCanvas");
        canvasObject.transform.SetParent(enemyObject.transform, false);
        canvasObject.transform.localPosition = new Vector3(0f, 2.35f, 0f);
        canvasObject.transform.localScale = new Vector3(0.01f, 0.01f, 1f);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(180f, 64f);

        GameObject textObject = new GameObject("HPText");
        textObject.transform.SetParent(canvasObject.transform, false);
        TextMeshProUGUI hpText = textObject.AddComponent<TextMeshProUGUI>();
        hpText.fontSize = 18;
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.color = Color.white;
        hpText.raycastTarget = false;
        hpText.rectTransform.sizeDelta = canvasRect.sizeDelta;
        hpText.rectTransform.anchoredPosition = Vector2.zero;

        enemy.hpText = hpText;
        hpText.text = $"{enemy.enemyName}\n生命 {enemy.currentHealth}/{enemy.maxHealth}  护盾 {enemy.currentShield}\n意图：攻击 {enemy.GetAttackDamage()}";
    }

    private void ConfigureEnemy(Enemy enemy, int index)
    {
        if (EventEncounter != null)
        {
            enemy.enemyName = EventEncounter.enemyName;
            enemy.maxHealth = EventEncounter.maxHealth;
            enemy.baseAttack = EventEncounter.attackDamage;
            enemy.currentShield = EventEncounter.initialShield;
        }
        else
        {
            if (string.IsNullOrEmpty(enemy.enemyName) || enemy.enemyName == "Enemy")
                enemy.enemyName = isBossBattle ? "终途领袖" : (isEliteBattle ? $"精英敌人 {index + 1}" : $"敌人 {index + 1}");
            enemy.maxHealth = isBossBattle ? 320 : (isEliteBattle ? 120 + index * 20 : 70 + index * 15);
            enemy.baseAttack = isBossBattle ? 26 : (isEliteBattle ? 18 + index * 3 : 10 + index * 2);
        }
        enemy.currentHealth = enemy.maxHealth;

        EnemyManager.EnsureInstance();
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.Register(enemy);
    }

    private Vector3 GetEnemyPosition(int index, int count)
    {
        float spacing = 3.2f;
        float startX = -((count - 1) * spacing) * 0.5f;
        return new Vector3(startX + index * spacing, 0.35f, 0f);
    }

    private void UpdateUI()
    {
        if (basicAttackButton != null) basicAttackButton.interactable = AcceptsPlayerActions;
        if (endTurnButton != null) endTurnButton.interactable = AcceptsPlayerActions;
        if (PlayerStats.Instance != null && playerStatsText != null)
        {
            PlayerStats player = PlayerStats.Instance;
            playerStatsText.text = $"生命 {player.currentHealth}/{player.maxHealth}  护盾 {player.currentShield}  能量 {player.currentMana}/{player.maxMana}  金币 {player.gold}  藏品 {CollectibleManager.OwnedCollectibles.Count}";
        }

        if (basicAttackButton != null)
            SetButtonText(basicAttackButton, GetBasicAttackLabel());

        if (collectibleIconContainer != null && displayedCollectibleCount != CollectibleManager.OwnedCollectibles.Count)
        {
            CollectibleUiUtility.RebuildIconBar(collectibleIconContainer, CollectibleManager.OwnedCollectibles);
            displayedCollectibleCount = CollectibleManager.OwnedCollectibles.Count;
        }

        if (DeckManager.Instance != null && deckStatsText != null)
        {
            DeckManager deck = DeckManager.Instance;
            deckStatsText.text = $"牌堆 {deck.drawPile.Count}  手牌 {deck.hand.Count}  弃牌 {deck.discardPile.Count}  消耗 {deck.exhaustPile.Count}";
        }
    }

    private void EnsureRuntimeUI()
    {
        if (playerStatsText != null && deckStatsText != null && endTurnButton != null && basicAttackButton != null && collectibleIconContainer != null)
            return;

        if (playerStatsText != null && deckStatsText != null && endTurnButton != null && basicAttackButton != null)
        {
            Transform parent = playerStatsText.transform.parent != null ? playerStatsText.transform.parent : transform;
            collectibleIconContainer = CreateCollectibleIconBar(parent, new Vector2(70f, -42f));
            return;
        }

        Canvas canvas = GetComponentInChildren<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("RuntimeCombatCanvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        GameObject topPanel = CreatePanel("CombatTopPanel", canvas.transform, new Vector2(960f, 128f), new Color(0.04f, 0.04f, 0.06f, 0.78f));
        RectTransform topRect = topPanel.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.5f, 1f);
        topRect.anchorMax = new Vector2(0.5f, 1f);
        topRect.anchoredPosition = new Vector2(0f, -76f);

        playerStatsText = CreateText("PlayerStats", topPanel.transform, "", 18, TextAlignmentOptions.Left, new Vector2(430f, 36f), new Vector2(-255f, 30f));
        deckStatsText = CreateText("DeckStats", topPanel.transform, "", 17, TextAlignmentOptions.Left, new Vector2(430f, 36f), new Vector2(-255f, -8f));

        GameObject basicAttackObject = CreateButtonObject("BasicAttackButton", "攻击 1", new Vector2(130f, 44f));
        basicAttackObject.transform.SetParent(topPanel.transform, false);
        basicAttackObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(245f, 20f);
        basicAttackButton = basicAttackObject.GetComponent<Button>();

        GameObject endTurnObject = CreateButtonObject("EndTurnButton", "结束回合", new Vector2(130f, 44f));
        endTurnObject.transform.SetParent(topPanel.transform, false);
        endTurnObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(390f, 20f);
        endTurnButton = endTurnObject.GetComponent<Button>();

        GameObject victoryObject = CreateButtonObject("VictoryButton", "领取", new Vector2(130f, 44f));
        victoryObject.transform.SetParent(topPanel.transform, false);
        victoryObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(390f, -34f);
        victoryButton = victoryObject.GetComponent<Button>();

        collectibleIconContainer = CreateCollectibleIconBar(topPanel.transform, new Vector2(50f, -42f));

        GameObject handRoot = new GameObject("HandArea");
        handRoot.transform.SetParent(canvas.transform, false);
        RectTransform handRect = handRoot.AddComponent<RectTransform>();
        handRect.anchorMin = new Vector2(0.5f, 0f);
        handRect.anchorMax = new Vector2(0.5f, 0f);
        handRect.sizeDelta = new Vector2(1180f, 330f);
        handRect.anchoredPosition = new Vector2(0f, 92f);

        HandView handView = handRoot.AddComponent<HandView>();
        handView.handContainer = handRoot.transform;
        handView.cardScale = 0.34f;
        handView.cardSpacing = 104f;
        handView.yOffset = -68f;
        handView.fanAngle = 18f;
        handView.arcDepth = 36f;
        handView.maxFanWidth = 860f;
        handView.hoverLift = 132f;
        handView.hoverScale = 1.4f;
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private Transform CreateCollectibleIconBar(Transform parent, Vector2 position)
    {
        GameObject bar = new GameObject("CollectibleIconBar");
        bar.transform.SetParent(parent, false);

        RectTransform rect = bar.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360f, 50f);
        rect.anchoredPosition = position;

        HorizontalLayoutGroup layout = bar.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;

        displayedCollectibleCount = -1;
        return bar.transform;
    }

    private TextMeshProUGUI CreateText(string name, Transform parent, string text, int size, TextAlignmentOptions alignment, Vector2 sizeDelta, Vector2 position)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = true;
        tmp.rectTransform.sizeDelta = sizeDelta;
        tmp.rectTransform.anchoredPosition = position;
        return tmp;
    }

    private GameObject CreateButtonObject(string name, string label, Vector2 size)
    {
        GameObject buttonObject = new GameObject(name);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = size;
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.2f, 0.3f, 1f);
        Button button = buttonObject.AddComponent<Button>();

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.28f, 0.32f, 0.45f, 1f);
        colors.pressedColor = new Color(0.1f, 0.12f, 0.18f, 1f);
        button.colors = colors;

        CreateText("Text", buttonObject.transform, label, 18, TextAlignmentOptions.Center, size, Vector2.zero);
        return buttonObject;
    }

    private string GetBasicAttackLabel()
    {
        if (PlayerStats.Instance == null)
            return "攻击 1";

        return $"攻击 1\n<size=70%>造成{PlayerStats.Instance.GetAttackDamage()}点物理伤害并结束回合</size>";
    }

    private void SetButtonText(Button button, string label)
    {
        if (button == null) return;

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
            text.text = label;
    }

    private Enemy GetDefaultTarget()
    {
        if (EnemyManager.Instance == null) return null;
        EnemyManager.Instance.ClearNulls();

        foreach (Enemy enemy in EnemyManager.Instance.ActiveEnemies)
        {
            if (enemy != null && !enemy.IsDead())
                return enemy;
        }

        return null;
    }
}
