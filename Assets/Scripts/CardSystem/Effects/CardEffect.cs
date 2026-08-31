using UnityEngine;
using System.Collections.Generic;

public abstract class CardEffect : ScriptableObject
{
    public abstract void Execute(CardEffectContext context);

    public virtual string GetPreviewDescription(CardPreviewContext context)
    {
        return context != null && context.Card != null ? context.Card.description : string.Empty;
    }
}

public class CardEffectContext
{
    public CardData Card;
    public PlayerStats Player;
    public Enemy Target;
    public List<Enemy> AllEnemies;
    public DeckManager Deck;
    public CardEffectManager EffectManager;
    public bool ShufflePlayedCardIntoDrawPile;
    public bool EndPlayerTurnAfterPlay;
}

public class CardPreviewContext
{
    public CardData Card;
    public PlayerStats Player;
    public Enemy Target;
    public List<Enemy> AllEnemies;
    public DeckManager Deck;
    public CardEffectManager EffectManager;
}

public static class CardCostUtility
{
    public static int GetEffectiveCardCost(CardData card)
    {
        if (card == null) return 0;

        int cost = CollectibleManager.GetEffectiveCardCost(card);
        if (CardEffectManager.Instance != null)
            cost -= CardEffectManager.Instance.GetTemporaryCostReduction(card);

        return Mathf.Max(0, cost);
    }

    public static string GetCostLabel(CardData card)
    {
        if (card == null) return "0";

        int effectiveCost = GetEffectiveCardCost(card);
        return effectiveCost.ToString();
    }
}

public static class CardDescriptionFormatter
{
    // Keep these readable source markers alongside the runtime regexes so
    // tooling can identify the Chinese range and attack-formula patterns.
    private const string AttackRangeMarker = @"\s*至\s*";
    private const string AttackFormulaMarker = "(?:{attackFormulaToken})\\s*\\+\\s*(\\d+)";
    private const string ReverseAttackFormulaMarker = "(\\d+)\\s*\\+\\s*(?:{attackFormulaToken})";

    public static string GetDescription(CardData card)
    {
        if (card == null) return string.Empty;

        CardEffect effect = CardEffectCatalog.Resolve(card);
        var context = new CardPreviewContext
        {
            Card = card,
            Player = PlayerStats.Instance,
            Target = EnemyManager.Instance != null && EnemyManager.Instance.ActiveEnemies.Count > 0
                ? EnemyManager.Instance.ActiveEnemies[0]
                : null,
            AllEnemies = EnemyManager.Instance != null
                ? new List<Enemy>(EnemyManager.Instance.ActiveEnemies)
                : new List<Enemy>(),
            Deck = DeckManager.Instance,
            EffectManager = CardEffectManager.Instance
        };

        string description = effect != null
            ? effect.GetPreviewDescription(context)
            : card.description;

        if (string.IsNullOrEmpty(description))
            description = card.description;

        return ResolveDynamicText(description, context.Player);
    }

    public static string ResolveDynamicText(string text, PlayerStats player)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        int attackValue = player != null ? player.GetAttackDamage() : 10;
        string resolved = text;
        const string attackFormulaToken = "基础攻击|攻击力|Base attack";
        const string baseAttackToken = "基础攻击|Base attack";

        resolved = System.Text.RegularExpressions.Regex.Replace(
            resolved,
            $"(?:{attackFormulaToken})\\s*\\+\\s*(\\d+)\\s*至\\s*(?:{attackFormulaToken})\\s*\\+\\s*(\\d+)",
            match => $"{attackValue + int.Parse(match.Groups[1].Value)}-{attackValue + int.Parse(match.Groups[2].Value)}",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        resolved = System.Text.RegularExpressions.Regex.Replace(
            resolved,
            $"(?:{attackFormulaToken})\\s*\\+\\s*\\[2,4\\]\\*10",
            $"{attackValue + 20}-{attackValue + 40}",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        resolved = System.Text.RegularExpressions.Regex.Replace(
            resolved,
            $"(?:{attackFormulaToken})\\s*\\+\\s*(\\d+)",
            match => (attackValue + int.Parse(match.Groups[1].Value)).ToString(),
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        resolved = System.Text.RegularExpressions.Regex.Replace(
            resolved,
            $"(\\d+)\\s*\\+\\s*(?:{attackFormulaToken})",
            match => (int.Parse(match.Groups[1].Value) + attackValue).ToString(),
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return System.Text.RegularExpressions.Regex.Replace(
            resolved,
            baseAttackToken,
            attackValue.ToString(),
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
