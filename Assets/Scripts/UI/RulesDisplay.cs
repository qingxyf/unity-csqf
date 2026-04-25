using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RulesDisplay : MonoBehaviour
{
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

    private void Start()
    {
        if (rulesPanel != null) rulesPanel.SetActive(false);
    }

    public void ToggleRules()
    {
        if (rulesPanel != null)
        {
            bool isActive = !rulesPanel.activeSelf;
            rulesPanel.SetActive(isActive);
            
            if (isActive)
            {
                UpdateText();
            }
        }
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
