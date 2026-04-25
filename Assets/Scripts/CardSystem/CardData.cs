using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Card System/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public CardElement element;
    [TextArea(3, 10)]
    public string description;
    public int cost;
    public Sprite cardArt; // 卡牌图片
    
    // 用于后续扩展：卡牌的具体逻辑ID或类型
    // 比如 "Heal_30", "Damage_40" 等，或者配合策略模式使用
    public string effectId; 
}
