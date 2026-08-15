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
}
