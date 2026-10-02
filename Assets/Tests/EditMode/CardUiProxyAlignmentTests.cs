using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class CardUiProxyAlignmentTests
{
    [Test]
    public void UiSpriteProxyTracksSpriteRendererTransform()
    {
        Texture2D frameTexture = null;
        Texture2D artTexture = null;
        Sprite frameSprite = null;
        Sprite artSprite = null;
        GameObject canvasObject = null;

        try
        {
            frameTexture = CreateTexture(400, 600);
            artTexture = CreateTexture(300, 120);
            frameSprite = CreateSprite(frameTexture);
            artSprite = CreateSprite(artTexture);

            canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));

            GameObject cardObject = new GameObject(
                "Card",
                typeof(RectTransform),
                typeof(SpriteRenderer));
            cardObject.transform.SetParent(canvasObject.transform, false);
            cardObject.transform.localScale = new Vector3(0.25f, 0.5f, 1f);
            Component cardDisplay = AddRuntimeCardDisplay(cardObject);

            SpriteRenderer frameRenderer = cardObject.GetComponent<SpriteRenderer>();
            frameRenderer.sprite = frameSprite;
            frameRenderer.sortingOrder = 1;

            GameObject artObject = new GameObject("MovedArt", typeof(SpriteRenderer));
            artObject.transform.SetParent(cardObject.transform, false);
            artObject.transform.localPosition = new Vector3(1.2f, -0.4f, 0f);
            artObject.transform.localScale = new Vector3(2f, 0.5f, 1f);

            SpriteRenderer artRenderer = artObject.GetComponent<SpriteRenderer>();
            artRenderer.sprite = artSprite;
            artRenderer.sortingOrder = 2;

            InvokeEnsureArtForUI(cardDisplay);

            RectTransform artProxy = cardObject.transform
                .Find("[UI_Proxy]_MovedArt")
                .GetComponent<RectTransform>();
            Image artImage = artProxy.GetComponent<Image>();

            Assert.That(artProxy.anchoredPosition.x, Is.EqualTo(30f).Within(0.01f));
            Assert.That(artProxy.anchoredPosition.y, Is.EqualTo(-20f).Within(0.01f));
            Assert.That(artProxy.localScale.x, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(artProxy.localScale.y, Is.EqualTo(0.25f).Within(0.01f));
            Assert.That(artImage.rectTransform.sizeDelta.x, Is.EqualTo(300f).Within(0.01f));
            Assert.That(artImage.rectTransform.sizeDelta.y, Is.EqualTo(120f).Within(0.01f));
            Assert.That(frameRenderer.enabled, Is.False);
            Assert.That(artRenderer.enabled, Is.False);
        }
        finally
        {
            if (canvasObject != null) Object.DestroyImmediate(canvasObject);
            if (frameSprite != null) Object.DestroyImmediate(frameSprite);
            if (artSprite != null) Object.DestroyImmediate(artSprite);
            if (frameTexture != null) Object.DestroyImmediate(frameTexture);
            if (artTexture != null) Object.DestroyImmediate(artTexture);
        }
    }

    [Test]
    public void CardUiProxyDoesNotUseSharedFixedArtworkSlot()
    {
        string source = File.ReadAllText("Assets/Scripts/CardSystem/CardDisplayBase.cs");

        Assert.That(source, Does.Not.Contain("new Vector2(567, 505)"));
        Assert.That(source, Does.Not.Contain("new Vector2(0, 67)"));
    }

    [Test]
    public void HoverPreviewInitializesPreviewCardDisplay()
    {
        string source = File.ReadAllText("Assets/Scripts/CardSystem/BackpackCardHoverPreview.cs");

        Assert.That(source, Does.Contain("InitializePreviewDisplay"));
        Assert.That(source, Does.Contain("Setup(cardData)"));
    }

    [Test]
    public void CombatHandViewUsesFanLayoutAndHoverBringToFront()
    {
        string handSource = File.ReadAllText("Assets/Scripts/CardSystem/HandView.cs");
        string hoverSource = File.ReadAllText("Assets/Scripts/CardSystem/HandCardHover.cs");

        Assert.That(handSource, Does.Contain("fanAngle"));
        Assert.That(handSource, Does.Contain("arcDepth"));
        Assert.That(handSource, Does.Contain("CreateCardSlot"));
        Assert.That(handSource, Does.Contain("Instantiate(prefab, slot.transform)"));
        Assert.That(handSource, Does.Contain("GetComponentInChildren<CardDisplay>"));
        Assert.That(handSource, Does.Contain("HandCardHover"));
        Assert.That(handSource, Does.Contain("EnsureCardRaycastTarget"));
        Assert.That(handSource, Does.Contain("raycastTarget = true"));
        Assert.That(handSource, Does.Contain("slot.AddComponent<CardCaster>"));
        Assert.That(hoverSource, Does.Contain("SetAsLastSibling"));
        Assert.That(hoverSource, Does.Contain("hoverLift"));
    }

    [Test]
    public void HandBaselineKeepsTheOuterFanCardsAboveTheCanvasBottom()
    {
        GameObject canvasObject = new GameObject("Combat Canvas", typeof(RectTransform), typeof(Canvas));
        GameObject handObject = new GameObject("Hand Area", typeof(RectTransform));
        try
        {
            RectTransform canvas = canvasObject.GetComponent<RectTransform>();
            canvas.sizeDelta = new Vector2(960f, 600f);

            RectTransform hand = handObject.GetComponent<RectTransform>();
            hand.SetParent(canvas, false);
            hand.anchorMin = new Vector2(0.5f, 0f);
            hand.anchorMax = new Vector2(0.5f, 0f);
            hand.sizeDelta = new Vector2(1180f, 330f);
            hand.anchoredPosition = new Vector2(0f, 92f);

            HandView handView = handObject.AddComponent<HandView>();
            handView.handContainer = hand;
            handView.yOffset = -68f;
            handView.arcDepth = 36f;

            MethodInfo method = typeof(HandView).GetMethod(
                "GetSafeHandBaseline",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            float baseline = (float)method.Invoke(handView, null);

            MethodInfo fanWidthMethod = typeof(HandView).GetMethod(
                "GetSafeFanWidth",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fanWidthMethod, Is.Not.Null);
            float fanWidth = (float)fanWidthMethod.Invoke(handView, null);
            float spacing = fanWidth / 9f;
            Assert.That(spacing, Is.GreaterThan(50f), "Ten cards must retain an exposed click region.");

            // Validate every corner of a full ten-card hand.  The outer cards
            // use 18-degree rotation and the inner cards use their real fan
            // angles, so this catches both vertical and horizontal clipping.
            for (int i = 0; i < 10; i++)
            {
                float normalized = Mathf.Lerp(-1f, 1f, i / 9f);
                GameObject cardObject = new GameObject("Hand card " + i, typeof(RectTransform));
                RectTransform card = cardObject.GetComponent<RectTransform>();
                card.SetParent(hand, false);
                card.sizeDelta = new Vector2(196f, 292f);
                card.anchoredPosition = new Vector2(
                    -fanWidth * 0.5f + i * spacing,
                    baseline - Mathf.Abs(normalized) * handView.arcDepth);
                card.localRotation = Quaternion.Euler(0f, 0f, -normalized * handView.fanAngle);

                Vector3[] corners = new Vector3[4];
                card.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector3 local = canvas.InverseTransformPoint(corner);
                    Assert.That(local.x, Is.InRange(canvas.rect.xMin, canvas.rect.xMax));
                    Assert.That(local.y, Is.InRange(canvas.rect.yMin, canvas.rect.yMax));
                }
                Object.DestroyImmediate(cardObject);
            }

        }
        finally
        {
            Object.DestroyImmediate(handObject);
            Object.DestroyImmediate(canvasObject);
        }
    }

    [Test]
    public void RuntimeCombatUiUsesChineseLabels()
    {
        string source = File.ReadAllText("Assets/Scripts/CardSystem/CombatController.cs");

        Assert.That(source, Does.Contain("攻击 1"));
        Assert.That(source, Does.Contain("结束回合"));
        Assert.That(source, Does.Contain("生命"));
        Assert.That(source, Does.Contain("能量"));
        Assert.That(source, Does.Contain("handView.cardScale = 0.34f"));
        Assert.That(source, Does.Contain("handView.hoverScale = 1.4f"));
        Assert.That(source, Does.Not.Contain("Attack 1"));
        Assert.That(source, Does.Not.Contain("End Turn"));
    }

    [Test]
    public void EventAndCampRuntimeTextUsesChineseLabels()
    {
        string eventSource = File.ReadAllText("Assets/Scripts/NodeSystem/Events/EventManager.cs");
        string campSource = File.ReadAllText("Assets/Scripts/NodeSystem/CampManager.cs");

        Assert.That(eventSource, Does.Contain("完全恢复"));
        Assert.That(eventSource, Does.Contain("获得藏品"));
        Assert.That(eventSource, Does.Contain("事件"));
        Assert.That(eventSource, Does.Contain("继续"));
        Assert.That(eventSource, Does.Not.Contain("Full heal"));
        Assert.That(eventSource, Does.Not.Contain("Obtained Collectible"));
        Assert.That(eventSource, Does.Not.Contain("Continue\""));

        Assert.That(campSource, Does.Contain("营地"));
        Assert.That(campSource, Does.Contain("休息"));
        Assert.That(campSource, Does.Contain("补给"));
        Assert.That(campSource, Does.Contain("移除"));
        Assert.That(campSource, Does.Not.Contain("Rest  +"));
        Assert.That(campSource, Does.Not.Contain("Take "));
        Assert.That(campSource, Does.Not.Contain("Remove 1 random card"));
    }

    [Test]
    public void CardDescriptionsDoNotExposeEnglishCombatTerms()
    {
        string displaySource = File.ReadAllText("Assets/Scripts/CardSystem/CardDisplayBase.cs");
        string formatterSource = File.ReadAllText("Assets/Scripts/CardSystem/Effects/CardEffect.cs");
        string cardDisplaySource = File.ReadAllText("Assets/Scripts/CardSystem/CardDisplay.cs");
        string tmpSettings = File.ReadAllText("Assets/TextMesh Pro/Resources/TMP Settings.asset");

        Assert.That(formatterSource, Does.Contain("基础攻击"));
        Assert.That(cardDisplaySource, Does.Contain("TMP_Settings.defaultFontAsset"));
        Assert.That(cardDisplaySource, Does.Contain("enableAutoSizing = true"));
        Assert.That(cardDisplaySource, Does.Contain("TextOverflowModes.Overflow"));
        Assert.That(cardDisplaySource, Does.Not.Contain("parentScale = 0.3f"));
        Assert.That(tmpSettings, Does.Contain("guid: 7071615201164bd45877cd86d7a4eb78"));
        foreach (string path in Directory.GetFiles("Assets/Resources/Cards", "*.asset"))
        {
            string asset = File.ReadAllText(path);
            Assert.That(asset, Does.Not.Contain("Base attack"), path);
            Assert.That(asset, Does.Not.Contain("HP"), path);
            Assert.That(asset, Does.Not.Contain("hp"), path);
        }
    }

    [Test]
    public void InitialCampIsSpecialAndGrantsTenCards()
    {
        string gameManagerSource = File.ReadAllText("Assets/Scripts/NodeSystem/GameManager.cs");
        string contentSource = File.ReadAllText("Assets/Scripts/NodeSystem/NodeContentManager.cs");
        string campSource = File.ReadAllText("Assets/Scripts/NodeSystem/CampManager.cs");

        Assert.That(gameManagerSource, Does.Contain("LoadNodeContent(node)"));
        Assert.That(contentSource, Does.Contain("ConfigureForNodeDepth"));
        Assert.That(campSource, Does.Contain("nodeDepth == 0"));
        Assert.That(campSource, Does.Contain("initialCampCardsToGive = 10"));
        Assert.That(campSource, Does.Contain("起始篝火"));
        Assert.That(campSource, Does.Contain("领取 10 张初始牌"));
    }

    [Test]
    public void ExpandedCardPoolAddsPlaceholderCardsForEachElement()
    {
        string prefabAliasSource = File.ReadAllText("Assets/Scripts/CardSystem/CardResourceUtility.cs");
        string[] expectedCards =
        {
            "晨曦护符", "辉光裁决", "祈星余响",
            "余烬连唱", "焦土火环", "灼心斩",
            "苔痕愈合", "藤蔓反击", "树根缠缚",
            "冷泉箭", "潮汐护幕", "断浪回能",
            "月影潜袭", "蚀影触", "残魂汲取",
            "行囊整理", "粮线补给", "暂避锋芒"
        };

        foreach (string cardName in expectedCards)
        {
            string cardPath = Path.Combine("Assets/Resources/Cards", cardName + ".asset");
            Assert.That(File.Exists(cardPath), Is.True, cardName);
            Assert.That(File.ReadAllText(cardPath), Does.Contain("effectId:"), cardName);
            Assert.That(prefabAliasSource, Does.Contain(cardName), cardName);
        }
    }

    [Test]
    public void DynamicCardPreviewAndCostUseRuntimeCalculators()
    {
        string cardEffectSource = File.ReadAllText("Assets/Scripts/CardSystem/Effects/CardEffect.cs");
        string displaySource = File.ReadAllText("Assets/Scripts/CardSystem/CardDisplayBase.cs");
        string managerSource = File.ReadAllText("Assets/Scripts/CardSystem/CardEffectManager.cs");

        Assert.That(cardEffectSource, Does.Contain("GetPreviewDescription"));
        Assert.That(displaySource, Does.Contain("CardDescriptionFormatter.GetDescription"));
        Assert.That(displaySource, Does.Contain("CardCostUtility.GetEffectiveCardCost"));
        Assert.That(managerSource, Does.Contain("CardCostUtility.GetEffectiveCardCost"));
        Assert.That(displaySource, Does.Not.Contain("ReplaceKeywords"));
    }

    [Test]
    public void DynamicCardTextResolvesAttackPowerFormulas()
    {
        string formatterSource = File.ReadAllText("Assets/Scripts/CardSystem/Effects/CardEffect.cs");

        Assert.That(formatterSource, Does.Contain("基础攻击|攻击力|Base attack"));
        Assert.That(formatterSource, Does.Contain("\\s*至\\s*"));
        Assert.That(formatterSource, Does.Contain("attackValue + int.Parse"));
        Assert.That(formatterSource, Does.Contain("(?:{attackFormulaToken})\\\\s*\\\\+\\\\s*(\\\\d+)"));
        Assert.That(formatterSource, Does.Contain("(\\\\d+)\\\\s*\\\\+\\\\s*(?:{attackFormulaToken})"));
    }

    [Test]
    public void TemporaryCostReductionsCanReduceCardsToZero()
    {
        string managerSource = File.ReadAllText("Assets/Scripts/CardSystem/CardEffectManager.cs");

        Assert.That(managerSource, Does.Contain("return Mathf.Min(reduction, Mathf.Max(0, card.cost));"));
        Assert.That(managerSource, Does.Not.Contain("card.cost - 1"));
    }

    [Test]
    public void NewCardsUseIndependentBalancedEffects()
    {
        string catalogSource = File.ReadAllText("Assets/Scripts/CardSystem/CardEffectCatalog.cs");
        string[] expectedCards =
        {
            "晨曦护符", "辉光裁决", "祈星余响",
            "余烬连唱", "焦土火环", "灼心斩",
            "苔痕愈合", "藤蔓反击", "树根缠缚",
            "冷泉箭", "潮汐护幕", "断浪回能",
            "月影潜袭", "蚀影触", "残魂汲取",
            "行囊整理", "粮线补给", "暂避锋芒"
        };

        foreach (string cardName in expectedCards)
        {
            string cardPath = Path.Combine("Assets/Resources/Cards", cardName + ".asset");
            string asset = File.ReadAllText(cardPath);
            Assert.That(asset, Does.Contain($"effectId: {cardName}"), cardName);
            Assert.That(catalogSource, Does.Contain(cardName), cardName);
        }

        Assert.That(File.ReadAllText("Assets/Resources/Cards/余烬连唱.asset"), Does.Contain("cost: 5"));
        Assert.That(File.ReadAllText("Assets/Resources/Cards/晨曦护符.asset"), Does.Contain("cost: 1"));
    }

    [Test]
    public void BasicAttackUsesDynamicAttackDamageAndEndsTurn()
    {
        string combatSource = File.ReadAllText("Assets/Scripts/CardSystem/CombatController.cs");

        Assert.That(combatSource, Does.Contain("GetBasicAttackLabel"));
        Assert.That(combatSource, Does.Contain("GetAttackDamage()"));
        Assert.That(combatSource, Does.Contain("造成{PlayerStats.Instance.GetAttackDamage()}点物理伤害"));
        Assert.That(combatSource, Does.Contain("RequestEndPlayerTurn()"));
    }

    [Test]
    public void CardEffectsDeferEndTurnUntilAfterPlayedCardIsMoved()
    {
        string contextSource = File.ReadAllText("Assets/Scripts/CardSystem/Effects/CardEffect.cs");
        string managerSource = File.ReadAllText("Assets/Scripts/CardSystem/CardEffectManager.cs");
        string setUpCampSource = File.ReadAllText("Assets/Scripts/CardSystem/Effects/Neutral/SetUpCampEffect.cs");
        string waitSource = File.ReadAllText("Assets/Scripts/CardSystem/Effects/Neutral/WaitAndSeeEffect.cs");
        string newCardsSource = File.ReadAllText("Assets/Scripts/CardSystem/Effects/BalancedNewCardEffects.cs");

        Assert.That(contextSource, Does.Contain("EndPlayerTurnAfterPlay"));
        Assert.That(managerSource, Does.Contain("if (context.EndPlayerTurnAfterPlay)"));
        Assert.That(setUpCampSource, Does.Not.Contain("RequestEndPlayerTurn()"));
        Assert.That(waitSource, Does.Not.Contain("RequestEndPlayerTurn()"));
        Assert.That(newCardsSource, Does.Not.Contain("RequestEndPlayerTurn()"));
    }

    [Test]
    public void CardDesignDocumentDefinesBalanceAndDynamicPreviewRules()
    {
        string doc = File.ReadAllText("Assets/Scripts/CardSystem/卡牌设计文档.md");

        Assert.That(doc, Does.Contain("动态卡面规则"));
        Assert.That(doc, Does.Contain("收益预算"));
        Assert.That(doc, Does.Contain("[星祈]"));
        Assert.That(doc, Does.Contain("[冷泉]"));
        Assert.That(doc, Does.Contain("普通攻击"));
        Assert.That(doc, Does.Not.Contain("当前复用效果"));
    }

    private static Texture2D CreateTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return texture;
    }

    private static Sprite CreateSprite(Texture2D texture)
    {
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
    }

    private static Component AddRuntimeCardDisplay(GameObject cardObject)
    {
        System.Type cardDisplayType = typeof(CardDisplay);
        Assert.That(cardDisplayType, Is.Not.Null);

        return cardObject.AddComponent(cardDisplayType);
    }

    private static void InvokeEnsureArtForUI(Component cardDisplay)
    {
        MethodInfo method = null;
        System.Type current = cardDisplay.GetType();
        while (current != null && method == null)
        {
            method = current.GetMethod(
                "EnsureArtForUI",
                BindingFlags.Instance | BindingFlags.NonPublic);
            current = current.BaseType;
        }

        Assert.That(method, Is.Not.Null);
        method.Invoke(cardDisplay, null);
    }
}
