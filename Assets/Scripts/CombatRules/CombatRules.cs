using System.Collections.Generic;

/// <summary>
/// Pure C# game-rules helpers. This is REAL game code — CombatManager calls these
/// methods at runtime. Tests exercise the exact same code path.
/// Zero UnityEngine dependency so it also compiles in the standalone VS test project.
/// </summary>
public static class CombatRules
{
    // ─── DAMAGE / BLOCK ──────────────────────────────────────────────────────

    /// <summary>
    /// Resolves one hit of incoming damage against a block pool.
    /// Block absorbs damage first; leftover reduces HP (clamped at 0).
    /// Returns the HP actually lost.
    /// Called by CombatManager.DealDamageToPlayer and DealDamageToEnemy.
    /// </summary>
    public static int ResolveDamage(int damage, ref int block, ref int hp)
    {
        if (damage <= 0) return 0;

        int remaining = damage;
        if (block > 0)
        {
            if (block >= remaining) { block -= remaining; remaining = 0; }
            else                   { remaining -= block;  block = 0;    }
        }

        int hpLost = 0;
        if (remaining > 0)
        {
            hpLost = remaining > hp ? hp : remaining;
            hp    -= hpLost;
            if (hp < 0) hp = 0;
        }
        return hpLost;
    }

    // ─── ENERGY ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when currentEnergy is enough to pay cardCost.
    /// Called by CombatManager.CanPlayCard.
    /// </summary>
    public static bool CanAfford(int currentEnergy, int cardCost)
    {
        return currentEnergy >= cardCost;
    }

    // ─── ENEMY AI PATTERN ────────────────────────────────────────────────────

    /// <summary>
    /// The Orc's cyclic action sequence, exactly as defined in CombatManager's
    /// enemyPattern array. CombatManager.PlanEnemyAction now reads from here.
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
    /// Returns the intent for the given (0-based) turn number, wrapping cyclically.
    /// Called by CombatManager.PlanEnemyAction.
    /// </summary>
    public static (string action, int value) GetEnemyIntent(int turnCount)
    {
        return EnemyPattern[turnCount % EnemyPattern.Length];
    }

    // ─── DECK HELPERS ────────────────────────────────────────────────────────

    /// <summary>
    /// Starting deck card names — mirrors BuildDeck() in CombatManager exactly:
    /// 5× Strike, 3× Defend, 1× Heavy Strike, 1× Strategize = 10 cards.
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
    /// Draws up to <paramref name="count"/> cards from the draw pile into the hand.
    /// When the draw pile is empty the discard pile is recycled — same logic as
    /// CombatManager.DrawCardsSmoothRoutine (minus coroutine/UI calls).
    /// Returns the number of cards actually drawn.
    /// </summary>
    public static int DrawCards(List<string> drawPile, List<string> hand,
                                List<string> discardPile, int count)
    {
        int drawn = 0;
        for (int i = 0; i < count; i++)
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0) break;
                drawPile.AddRange(discardPile);
                discardPile.Clear();
            }
            hand.Add(drawPile[0]);
            drawPile.RemoveAt(0);
            drawn++;
        }
        return drawn;
    }

    /// <summary>
    /// Moves all cards from hand into the discard pile.
    /// Mirrors CombatManager.DiscardHand (without the UI/display calls).
    /// </summary>
    public static void DiscardHand(List<string> hand, List<string> discardPile)
    {
        discardPile.AddRange(hand);
        hand.Clear();
    }
}
