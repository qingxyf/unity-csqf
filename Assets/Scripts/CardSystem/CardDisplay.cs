using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    [Header("UI Components")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText; // 如果以后要显示费用

    [Header("Data")]
    public CardData cardData;

    // 在编辑器中或者运行时调用此方法来刷新显示
    public void RefreshDisplay()
    {
        if (cardData == null) return;

        if (nameText != null)
        {
            nameText.text = cardData.cardName;
        }

        if (descriptionText != null)
        {
            // 1. 替换关键词 (Base attack -> 数值)
            string processedText = ReplaceKeywords(cardData.description);
            // 2. 每9个字符插入一个换行符 (支持富文本)
            descriptionText.text = InsertLineBreaks(processedText, 9);
        }

        if (costText != null)
        {
            costText.text = cardData.cost.ToString();
        }

        EnsureArtForUI();
        FixTextSizeForUI(); // 修复文字过小的问题
    }

    // 修复文字在 UI 缩放模式下过小的问题
    private void FixTextSizeForUI()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        // 因为父物体被缩放到 0.3，文字需要反向放大约 3.33 倍才能保持可读性
        // 或者直接设置一个固定的缩放值
        float parentScale = 0.3f; // 假设父物体缩放是 0.3
        float targetScale = 1f / parentScale;

        TextMeshProUGUI[] texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in texts)
        {
            // 只调整缩放，不调整位置
            txt.transform.localScale = new Vector3(targetScale, targetScale, 1f);
        }
    }

    // 在背包等 UI 中使用：自动为每个 SpriteRenderer 创建一个 UI Image 代理
    // 解决 World Space 预制体在 Overlay Canvas 中无法显示的问题
    private void EnsureArtForUI()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        // 1. 找到所有 SpriteRenderer（包括隐藏的）
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
        
        // 2. 按 SortingOrder 排序，确保渲染层级正确（从小到大）
        System.Array.Sort(renderers, (a, b) => a.sortingOrder.CompareTo(b.sortingOrder));

        int insertIndex = 0; // 始终插在最前面，保证在文字下方

        // 3. 为每个 SpriteRenderer 创建一个对应的 UI Image 对象
        foreach (var sr in renderers)
        {
            if (sr.sprite == null) continue;

            // 检查是否已经创建过代理（防止重复创建）
            string proxyName = $"[UI_Proxy]_{sr.gameObject.name}";
            Transform existingProxy = transform.Find(proxyName);
            if (existingProxy != null) continue;

            // 创建新的 UI 对象
            GameObject uiObj = new GameObject(proxyName);
            uiObj.transform.SetParent(this.transform, false); // 设为当前卡牌的子物体
            
            // 确保渲染顺序正确：背景在最下（Index 0），文字在最上
            uiObj.transform.SetSiblingIndex(insertIndex);
            insertIndex++;
            
            // 添加 Image 组件
            Image img = uiObj.AddComponent<Image>();
            img.sprite = sr.sprite;
            img.color = sr.color;
            img.raycastTarget = false;

            // 设置尺寸和位置
            // 假设 Sprite 的 PixelsPerUnit 是 100（Unity 默认）
            // 我们尝试还原原本的相对位置和大小
            
            // 1. 还原位置 (将 Unity Unit 转换为 UI Pixels, 假设 1 Unit = 100 Pixels)
            float pixelsPerUnit = 100f;
            if (sr.sprite != null) pixelsPerUnit = sr.sprite.pixelsPerUnit;
            
            // 使用 anchoredPosition 代替 localPosition 以获得更好的 UI 兼容性
            // 忽略 Z 轴差异，UI 通常是平面的
            if (sr.gameObject == this.gameObject)
            {
                // 如果是根节点本身的 SpriteRenderer，它应该位于 (0,0)，
                // 因为根节点的位置已经由父级容器（BackpackDisplay）控制了
                img.rectTransform.anchoredPosition = Vector2.zero;
            }
            else
            {
                // 如果是子物体，则保持其相对于根节点的偏移
                img.rectTransform.anchoredPosition = (Vector2)sr.transform.localPosition * pixelsPerUnit;
            }
            
            uiObj.transform.localScale = sr.transform.localScale;
            uiObj.transform.localRotation = sr.transform.localRotation;

            // 2. 设置大小
            img.SetNativeSize(); 

            // 针对插画（非根节点）进行特殊修正：
            // 用户指定参数：缩放0.3，宽567，高505，Pos(0, 67)
            if (sr.gameObject != this.gameObject)
            {
                // 假设非根节点的 SpriteRenderer 就是插画
                // 注意：这里的缩放 0.3 是相对于父物体（已经是0.3）的，所以如果用户是在 Inspector 看到的 0.3，那就是 localScale
                uiObj.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
                img.rectTransform.sizeDelta = new Vector2(567, 505);
                img.rectTransform.anchoredPosition = new Vector2(0, 67);
            }

            // 禁用原始 SpriteRenderer，避免混乱
            sr.enabled = false;
        }
    }

    // 关键词替换方法
    private string ReplaceKeywords(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        
        int attackValue = 10; // 默认值
        
        // 尝试获取玩家属性
        if (PlayerStats.Instance != null)
        {
            attackValue = PlayerStats.Instance.baseAttack;
        }
        else
        {
            var stats = FindObjectOfType<PlayerStats>();
            if (stats != null) attackValue = stats.baseAttack;
        }

        // 替换 "Base attack" (忽略大小写)
        // 使用黄色和粗体高亮
        string replacement = $"<color=yellow><b>{attackValue}</b></color>";
        return System.Text.RegularExpressions.Regex.Replace(text, "Base attack", replacement, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }

    // 辅助方法：每隔指定长度插入换行符 (支持富文本，忽略标签长度)
    private string InsertLineBreaks(string text, int lineLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        int visibleCount = 0;
        bool inTag = false;
        
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            sb.Append(c);
            
            if (c == '<')
            {
                inTag = true;
            }
            else if (c == '>')
            {
                inTag = false;
            }
            else if (!inTag)
            {
                // 如果是换行符，重置计数
                if (c == '\n')
                {
                    visibleCount = 0;
                }
                else
                {
                    visibleCount++;
                    
                    // 检查是否需要换行
                    if (visibleCount >= lineLength)
                    {
                        // 避免在最后添加换行
                        if (i < text.Length - 1)
                        {
                            sb.Append('\n');
                            visibleCount = 0;
                        }
                    }
                }
            }
        }
        
        return sb.ToString();
    }

    // 运行时初始化
    public void Setup(CardData data)
    {
        this.cardData = data;
        RefreshDisplay();
    }

    // 编辑器下如果修改了Data，自动刷新（可选）
    private void OnValidate()
    {
        RefreshDisplay();
    }
}
