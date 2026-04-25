using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro; // Assuming TextMeshPro is used, if not, use UnityEngine.UI

    public class BackpackDisplay : MonoBehaviour
    {
        [Header("UI References")]
        public Transform contentContainer;
        public GameObject itemPrefab; // 默认通用模板
        public GameObject backpackPanel;
        public Transform stagePreviewParent;

    // 缓存从 Resources/CardPrefabs 加载过的预制体，避免重复加载
    private Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

    private void Start()
    {
        // Close by default
        if (backpackPanel != null) backpackPanel.SetActive(false);
    }

    public void ToggleBackpack()
    {
        if (backpackPanel != null)
        {
            bool isActive = !backpackPanel.activeSelf;
            backpackPanel.SetActive(isActive);
            
            if (isActive)
            {
                RefreshDisplay();
            }
        }
    }

    public void RefreshDisplay()
    {
        if (contentContainer == null || itemPrefab == null)
        {
            Debug.LogError("BackpackDisplay: Content Container or Item Prefab not assigned!");
            return;
        }

        // 移除可能存在的布局组件，避免它们覆盖我们手动设置的位置
        LayoutGroup[] layoutGroups = contentContainer.GetComponents<LayoutGroup>();
        foreach (var group in layoutGroups)
        {
            DestroyImmediate(group);
        }

        ContentSizeFitter fitter = contentContainer.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            DestroyImmediate(fitter);
        }

        if (DeckManager.Instance == null)
        {
            Debug.LogError("BackpackDisplay: DeckManager not found!");
            return;
        }

        foreach (Transform child in contentContainer)
        {
            Destroy(child.gameObject);
        }

        var cards = DeckManager.Instance.backpack;

        // 手动坐标规则：
        // 第一张卡片在 x=-211, y=150
        // 第二张 x=-96, y 不变
        // 第三张 x=12, 依次类推，每行 5 张
        // 第二行 y=12，第三行 y=-118

        float[] columnXs = new float[] { -211f, -96f, 12f, 120f, 228f };
        float[] rowYs = new float[] { 150f, 12f, -118f };
        int columns = 5;

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            GameObject prefabToUse = itemPrefab;

            // 1. 尝试从 Resources/CardPrefabs 加载同名预制体
            if (card != null && !string.IsNullOrEmpty(card.cardName))
            {
                if (prefabCache.ContainsKey(card.cardName))
                {
                    prefabToUse = prefabCache[card.cardName];
                }
                else
                {
                    GameObject loadedPrefab = Resources.Load<GameObject>("CardPrefabs/" + card.cardName);
                    if (loadedPrefab != null)
                    {
                        prefabCache[card.cardName] = loadedPrefab;
                        prefabToUse = loadedPrefab;
                    }
                }
            }

            if (prefabToUse == null) continue;

            GameObject go = Instantiate(prefabToUse, contentContainer);
            // 修正：不再强制放大10倍，因为 UI Image 使用 SetNativeSize 后已经是像素单位
            // 建议使用 0.3f - 0.5f 之间的值，根据您的图片分辨率调整
            go.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f); 
            go.transform.localRotation = Quaternion.identity; // 重置旋转

            // 计算行列
            int row = i / columns;
            int col = i % columns;

            float posX;
            if (col < columnXs.Length)
            {
                posX = columnXs[col];
            }
            else
            {
                // 超过 5 列时，继续用最后一个间距向右推
                float lastSpacing = columnXs[columnXs.Length - 1] - columnXs[columnXs.Length - 2];
                posX = columnXs[columnXs.Length - 1] + lastSpacing * (col - columnXs.Length + 1);
            }

            float posY;
            if (row < rowYs.Length)
            {
                posY = rowYs[row];
            }
            else
            {
                float lastSpacingY = rowYs[rowYs.Length - 1] - rowYs[rowYs.Length - 2];
                posY = rowYs[rowYs.Length - 1] + lastSpacingY * (row - rowYs.Length + 1);
            }

            // 使用 RectTransform 设置坐标，更稳健
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(posX, posY);
            }
            else
            {
                go.transform.localPosition = new Vector3(posX, posY, 0f);
            }

            CardDisplay display = go.GetComponent<CardDisplay>();
            if (display != null)
            {
                BackpackCardDisplay newDisplay = go.AddComponent<BackpackCardDisplay>();
                newDisplay.nameText = display.nameText;
                newDisplay.descriptionText = display.descriptionText;
                newDisplay.costText = display.costText;
                Destroy(display);
                newDisplay.Setup(card);
            }

            BackpackCardHoverPreview hover = go.AddComponent<BackpackCardHoverPreview>();
            hover.cardData = card;
            hover.stagePreviewParent = stagePreviewParent;
        }

        if (cards.Count == 0)
        {
            Debug.Log("Backpack is empty.");
        }
    }
}
