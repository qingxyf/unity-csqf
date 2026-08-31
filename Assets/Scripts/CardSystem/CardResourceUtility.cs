using System.Collections.Generic;
using UnityEngine;

public static class CardResourceUtility
{
    private static readonly Dictionary<string, string> PrefabAliases = new Dictionary<string, string>
    {
        { "神圣惩击", "神圣惩戒" },
        { "棘藤棒", "荆棘缠绕" },
        { "火山回响", "火山" },
        { "晨曦护符", "圣光庇护" },
        { "辉光裁决", "神圣惩戒" },
        { "祈星余响", "光明祈愿" },
        { "余烬连唱", "二重吟唱" },
        { "焦土火环", "火山" },
        { "灼心斩", "烈焰打击" },
        { "苔痕愈合", "生命滋养" },
        { "藤蔓反击", "自然守护" },
        { "树根缠缚", "荆棘缠绕" },
        { "冷泉箭", "冰霜箭" },
        { "潮汐护幕", "寒冰护体" },
        { "断浪回能", "激流冲刷" },
        { "月影潜袭", "暗夜突袭" },
        { "蚀影触", "暗影侵蚀" },
        { "残魂汲取", "吞噬生命" },
        { "行囊整理", "精打细算" },
        { "粮线补给", "粮草先行" },
        { "暂避锋芒", "现行等待" }
    };

    public static GameObject LoadCardPrefab(CardData card)
    {
        if (card == null) return null;

        GameObject prefab = LoadCardPrefab(card.cardName);
        if (prefab != null) return prefab;

        if (PrefabAliases.TryGetValue(card.cardName, out string aliasName))
            return LoadCardPrefab(aliasName);

        if (!string.IsNullOrEmpty(card.baseCardName) && card.baseCardName != card.cardName)
        {
            prefab = LoadCardPrefab(card.baseCardName);
            if (prefab != null)
                return prefab;

            if (PrefabAliases.TryGetValue(card.baseCardName, out string baseAliasName))
                return LoadCardPrefab(baseAliasName);
        }

        return null;
    }

    public static GameObject LoadCardPrefab(string cardName)
    {
        if (string.IsNullOrEmpty(cardName)) return null;
        return Resources.Load<GameObject>("CardPrefabs/" + cardName);
    }
}
