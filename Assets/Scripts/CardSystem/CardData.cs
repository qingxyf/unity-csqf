using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Card System/Card Data")]
public class CardData : ScriptableObject
{
    public string cardName;
    public CardElement element;
    [TextArea(3, 10)]
    public string description;
    public int cost;
    public Sprite cardArt;
    public string effectId;
    public CardEffect effect;

    [Header("Run-time upgrade state")]
    [Tooltip("Runtime clones use this field; source cards in Resources remain at level 0.")]
    public int upgradeLevel;
    [HideInInspector]
    public string baseCardName;
}
