using UnityEngine;

[CreateAssetMenu(fileName = "NewCollectible", menuName = "Roguelike/Collectible")]
public class CollectibleData : ScriptableObject
{
    public string collectibleId;
    public string collectibleName;
    public Sprite icon;
    [TextArea(2, 4)]
    public string description;
    public CollectibleEffectType effectType;
    public int amount;
    public CardElement element = CardElement.Neutral;
    public DamageType damageType = DamageType.Physical;
    public int shopPrice = 100;
}
