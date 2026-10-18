using System;
using System.Collections.Generic;

/// <summary>
/// Combat calculation rules (damage, energy, enemy AI pattern, and deck operations).
/// Zero UnityEngine dependencies so it runs in pure .NET test runners and Unity.
/// </summary>
public static class CombatRules
{
    // ─── DAMAGE / BLOCK ──────────────────────────────────────────────────────

    /// <summary>
    /// Resolves incoming damage against block. Block absorbs first; leftover reduces HP.
    /// Returns the HP lost.
    /// </summary>
    public static int ResolveDamage(int damage, ref int block, ref int hp)
    {
        if (damage <= 0) return 0;

        int remaining = damage;

        // --- BUG 2 DEMO: Uncomment next line and comment the 'if (block > 0)' line below ---
        // if (false && block > 0) // BUG: block is ignored completely
        if (block > 0)
        {
            if (block >= remaining) { block -= remaining; remaining = 0; }
            else                   { remaining -= block;  block = 0;    }
        }

        int hpLost = 0;
        if (remaining > 0)
        {
            // --- BUG 1 DEMO: Uncomment next line and comment the line below ---
            // hpLost = remaining; // BUG: overkill does not clamp, HP goes negative
            hpLost = remaining > hp ? hp : remaining;
            hp    -= hpLost;
            if (hp < 0) hp = 0;
        }
        return hpLost;
    }

    // ─── ENERGY ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when current energy is greater than or equal to card cost.
    /// </summary>
    public static bool CanAfford(int currentEnergy, int cardCost)
    {
        // --- BUG 3 DEMO: Uncomment next line and comment the line below ---
        // return currentEnergy > cardCost; // BUG: cannot play card when energy exactly equals cost
        return currentEnergy >= cardCost;
    }

    // ─── ENEMY AI PATTERN ────────────────────────────────────────────────────

    /// <summary>
    /// Orc enemy action cycle: Attack 8, Attack 12, Defend 6, Attack 10, Attack 6.
    /// </summary>
    public static readonly (string action, int value)[] EnemyPattern =
    {
        ("Attack",  8),
        ("Attack", 12),
        ("Defend",  6),
        ("Attack", 10),
        ("Attack",  6),
    };

    /// <summary>
    /// Returns the intent for the given turn number (0-indexed), wrapping cyclically.
    /// </summary>
    public static (string action, int value) GetEnemyIntent(int turnCount)
    {
        // --- BUG 4 DEMO: Uncomment next line and comment the line below ---
        // return EnemyPattern[turnCount % 4]; // BUG: modulo 4 skips 5th move (Attack 6)
        return EnemyPattern[turnCount % EnemyPattern.Length];
    }

    // ─── DECK HELPERS ────────────────────────────────────────────────────────

    /// <summary>
    /// Starting deck card names: 5 Strike, 3 Defend, 1 Heavy Strike, 1 Strategize (10 cards).
    /// </summary>
    public static List<string> GetStartingDeckNames()
    {
        var deck = new List<string>();
        for (int i = 0; i < 5; i++) deck.Add("Strike");
        for (int i = 0; i < 3; i++) deck.Add("Defend");
        deck.Add("Heavy Strike");
        deck.Add("Strategize");
        return deck;
    }

    /// <summary>
    /// Draws one card from drawPile into hand. If drawPile is empty, moves discardPile into drawPile
    /// and invokes shuffle delegate before drawing. Returns true if a card was drawn.
    /// </summary>
    public static bool DrawOneCard<T>(List<T> drawPile, List<T> hand,
                                      List<T> discardPile,
                                      Action<List<T>> shuffle = null)
    {
        if (drawPile.Count == 0)
        {
            // --- BUG 5 DEMO: Uncomment next line and comment the 4 lines below ---
            // return false; // BUG: fails to recycle discard pile into draw pile
            if (discardPile.Count == 0) return false;
            drawPile.AddRange(discardPile);
            discardPile.Clear();
            shuffle?.Invoke(drawPile);
        }

        if (drawPile.Count == 0) return false;

        hand.Add(drawPile[0]);
        drawPile.RemoveAt(0);
        return true;
    }

    /// <summary>
    /// Draws up to count cards from drawPile into hand using DrawOneCard.
    /// Returns the number of cards drawn.
    /// </summary>
    public static int DrawCards<T>(List<T> drawPile, List<T> hand,
                                  List<T> discardPile, int count,
                                  Action<List<T>> shuffle = null)
    {
        int drawn = 0;
        for (int i = 0; i < count; i++)
        {
            if (!DrawOneCard(drawPile, hand, discardPile, shuffle))
                break;
            drawn++;
        }
        return drawn;
    }

    /// <summary>
    /// Moves all cards from hand into discardPile.
    /// </summary>
    public static void DiscardHand<T>(List<T> hand, List<T> discardPile)
    {
        discardPile.AddRange(hand);
        hand.Clear();
    }
}
