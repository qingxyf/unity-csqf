using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RulesDisplay : MonoBehaviour
{
    private const string CombatRules = @"== 战斗与旅途 ==
开战 6 能量、抽 5 张牌；第二回合回复 2 能量，第 3–4 回合回复 3，第 5 回合起回复 4。
每回合抽 2 张牌，手牌保留，上限 10。基础攻击消耗 1 能量并结束回合。
敌人意图在行动前显示：攻击造成所示伤害，防守获得护盾，重击为 1.5 倍基础攻击（显示值已计算虚弱与烧伤）。
冻结、眩晕、束缚或沉默会取消敌人当前行动。普通敌人按攻击、攻击、防守循环；精英按攻击、防守、重击；领袖按攻击、防守、重击、攻击。
前两层不会出现精英；领袖前所有路线均有营地。米饭事件的蓝色大肥鱼每回合攻击。

";
    [Header("UI References")]
    public GameObject rulesPanel;
    public TextMeshProUGUI contentText; // Optional: If you want to set text dynamically
    public Text standardText;           // Fallback

    [TextArea(10, 20)]
    public string rulesContent = @"
== 状态效果 (Status Effects) ==
[烧伤] (Burn): 每回合受到10点火焰伤害，攻击力减半。
[腐蚀] (Corrosion): 每回合受到10点暗影伤害，受到的伤害+5。
[冰霜] (Frost): 每回合受到5点冰霜伤害。
[寄生] (Parasite): 受到的火焰伤害翻倍。
[复活] (Resurrect): 死亡时复活并恢复50%生命值（清除此状态）。
[虚弱] (Weak): 造成的伤害减少25%。
[沉默] (Silenced): 无法使用技能/卡牌。
[净化] (Purified): 免疫下一个负面状态。

== 元素反应 (Elemental Reactions) ==
1. [燃烧] (Combustion): 
   火焰伤害 + (目标有寄生 OR 上次受过自然伤害) -> 额外造成等同于主角攻击力的伤害。
   
2. [蒸发] (Vaporize):
   冰霜伤害 + (目标上次受过火焰伤害) -> 清除目标一个随机增益状态。

3. [丰饶] (Bloom):
   自然伤害 + (目标上次受过冰霜伤害) -> 延长目标身上随机一个负面状态的持续时间。

4. [光暗双生] (Twilight):
   造成光/暗伤害 + (目标上次受过暗/光伤害) -> 主角下一次受到的伤害减少10点。
";

    private GameObject overlay;

    private void Awake()
    {
        // Include the new rules even when an older scene serialized the status text.
        // Keep rulesContent as the complete displayed document for custom callers.
        rulesContent = CombatRules + rulesContent;
    }

    // The serialized panel is already inactive in the map scene.  Closing it
    // again from Start immediately after its first activation swallowed the
    // first rules-button click.
    public bool IsOpen => overlay != null && overlay.activeSelf;

    public void CloseRules()
    {
        if (overlay != null) overlay.SetActive(false);
        if (rulesPanel != null) rulesPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (overlay != null) Destroy(overlay);
    }

    public void ToggleRules()
    {
        if (IsOpen) { CloseRules(); return; }
        if (rulesPanel != null) rulesPanel.SetActive(false);
        if (overlay == null)
        {
            overlay = BackpackDisplay.CreateOverlay("规则", CloseRules, out RectTransform body);
            Transform content = overlay.transform.Find("Body/Viewport/Content");
            GameObject textObject = new GameObject("规则内容", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(content, false);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
            text.text = rulesContent;
            text.fontSize = 20f;
            text.enableWordWrapping = true;
            text.color = Color.white;
            text.raycastTarget = false;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-28f, 0f);
            rect.anchoredPosition = new Vector2(0f, -10f);
            Canvas.ForceUpdateCanvases();
            float contentHeight = Mathf.Max(450f, text.preferredHeight + 20f);
            rect.sizeDelta = new Vector2(-28f, contentHeight - 20f);
            ((RectTransform)content).sizeDelta = new Vector2(0f, contentHeight);
        }
        overlay.SetActive(true);
        UpdateText();
    }

    private void UpdateText()
    {
        if (contentText != null)
        {
            contentText.text = rulesContent;
        }
        else if (standardText != null)
        {
            standardText.text = rulesContent;
        }
    }
}
