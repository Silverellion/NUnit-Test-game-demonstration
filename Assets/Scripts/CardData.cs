using UnityEngine;

public enum CardTargetType
{
    Enemy,
    Self
}

/// <summary>
/// Defines the data for a single card type.
/// </summary>
[System.Serializable]
public class CardData
{
    public string cardName;
    public int energyCost;
    public int damage;
    public int block;
    public int drawCards;
    public string description;
    public Color cardColor;

    public CardTargetType targetType => damage > 0 ? CardTargetType.Enemy : CardTargetType.Self;

    public CardData(string name, int cost, int dmg, int blk, int draw, string desc, Color color)
    {
        cardName = name;
        energyCost = cost;
        damage = dmg;
        block = blk;
        drawCards = draw;
        description = desc;
        cardColor = color;
    }

    /// <summary>
    /// Creates a deep copy of this card data.
    /// </summary>
    public CardData Clone()
    {
        return new CardData(cardName, energyCost, damage, block, drawCards, description, cardColor);
    }
}

