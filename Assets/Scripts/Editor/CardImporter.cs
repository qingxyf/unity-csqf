using UnityEngine;
using UnityEditor;
using System.IO;

public class CardImporter : EditorWindow
{
    // 在 Unity 顶部菜单栏添加一个按钮
    [MenuItem("Tools/Import Cards from Markdown")]
    public static void ImportCards()
    {
        // Markdown 文件路径（根据您的实际路径）
        string markdownPath = "Assets/Assets/卡牌/卡牌.md";
        // 生成的 Asset 保存路径
        string exportPath = "Assets/Resources/Cards";
        
        // 确保导出目录存在
        if (!Directory.Exists(exportPath))
        {
            Directory.CreateDirectory(exportPath);
        }

        // 读取所有行
        if (!File.Exists(markdownPath))
        {
            Debug.LogError($"未找到 Markdown 文件: {markdownPath}");
            return;
        }

        string[] lines = File.ReadAllLines(markdownPath);
        int importedCount = 0;

        foreach (string line in lines)
        {
            // 简单的 Markdown 表格解析
            if (!line.Trim().StartsWith("|")) continue;
            if (line.Contains("----")) continue; // 跳过分隔线
            if (line.Contains("流派")) continue; // 跳过表头

            string[] parts = line.Split('|');
            // 格式: | 空 | 流派 | 牌名 | 效果描述 | 耗法力 | 空 |
            if (parts.Length < 5) continue;

            string elementStr = parts[1].Trim();
            string cardName = parts[2].Trim();
            string description = parts[3].Trim();
            string costStr = parts[4].Trim();

            if (string.IsNullOrEmpty(cardName)) continue;

            // 创建 CardData 实例
            CardData card = ScriptableObject.CreateInstance<CardData>();
            card.cardName = cardName;
            card.description = description;
            
            // 解析费用
            int.TryParse(costStr, out card.cost);

            // 解析元素属性
            switch (elementStr)
            {
                case "光": card.element = CardElement.Light; break;
                case "火": card.element = CardElement.Fire; break;
                case "草": card.element = CardElement.Nature; break;
                case "水": card.element = CardElement.Water; break;
                case "暗影": card.element = CardElement.Shadow; break;
                case "无属性": card.element = CardElement.Neutral; break;
                default: 
                    Debug.LogWarning($"未知流派: {elementStr} (卡牌: {cardName})"); 
                    continue;
            }

            // 保存为 Asset 文件
            string assetPath = $"{exportPath}/{cardName}.asset";
            
            // 如果已存在则更新，否则创建
            CardData existing = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
            if (existing != null)
            {
                existing.cardName = cardName;
                existing.description = description;
                existing.cost = card.cost;
                existing.element = card.element;
                EditorUtility.SetDirty(existing);
            }
            else
            {
                AssetDatabase.CreateAsset(card, assetPath);
            }
            
            importedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"成功导入 {importedCount} 张卡牌数据到 {exportPath}");
    }
}
