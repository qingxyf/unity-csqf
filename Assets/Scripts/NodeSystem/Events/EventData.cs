using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Node System/Event Data")]
public class EventData : ScriptableObject
{
    [Tooltip("Stable run-pool identity. Leave empty only for legacy authored events.")]
    public string eventId;
    public bool requiresUnlock;
    public string eventName;
    [TextArea(3, 6)]
    public string description;
    public Sprite illustration; // 事件插图（可选）

    public List<EventChoice> choices = new List<EventChoice>();
}

[System.Serializable]
public class EventChoice
{
    public string buttonText;        // 按钮上显示的文字
    [TextArea(2, 4)]
    public string resultDescription; // 选择后显示的结果文字

    [Header("效果")]
    public int healthChange;         // 正数=回血，负数=扣血
    public int maxHealthChange;      // 最大生命值变化
    public int manaChange;           // 法力变化
    public int shieldGain;           // 获得护盾
    public int cardsToDraw;          // 从隐藏牌池抽卡数量
    public int cardsToRemove;        // 从背包随机移除卡牌数量
    public bool healToFull;          // 回满血
    public bool upgradeRandomCard;   // 随机强化一张未强化卡牌

    [Header("风险型效果")]
    public bool isGamble;            // 是否是赌博型选项
    public float gambleSuccessRate;  // 赌博成功率 0~1
    [TextArea(1, 2)]
    public string gambleFailText;    // 失败时的文字
    public int gambleFailHealthChange; // 失败时扣血

    [Header("高级奖励")]
    public CardElement rewardElement = CardElement.Neutral;
    public bool useRewardElement;
    public int rewardMaxCost = 99;
    public int previewCardChoices;
    public bool grantCollectible;

    [Header("事件走向")]
    [Tooltip("Unlocks this event id after the choice resolves.")]
    public string unlockEventId;
    public bool surrenderAllGold;
    public EventCombatEncounter combatEncounter;

    // Unity inline serialization may deserialize an absent class as an empty
    // object. Its resource path, rather than reference nullness, enables combat.
    public bool StartsCombat => combatEncounter != null &&
        !string.IsNullOrWhiteSpace(combatEncounter.enemyResourcePath);
}

[System.Serializable]
public class EventCombatEncounter
{
    public string enemyResourcePath;
    public string enemyName;
    public int maxHealth;
    public int attackDamage;
    public int initialShield;
}
