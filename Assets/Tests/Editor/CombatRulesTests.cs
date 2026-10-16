using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// NUnit tests for the card game's combat logic.
///
/// ALL tests exercise CombatRules.cs — a static helper class that
/// CombatManager actually calls at runtime.  Nothing here is invented;
/// every number comes directly from the game's source code.
///
/// NUnit concepts showcased:
///   [TestFixture]  – groups related tests
///   [SetUp]        – re-initialises state before every test
///   [Test]         – a single test case
///   [TestCase]     – parameterised / data-driven test
///   Assert.That    – constraint-model assertions
/// </summary>
[TestFixture]
public class CombatRulesTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // SECTION 1 – Damage & Block  (CombatRules.ResolveDamage)
    // Mirrors DealDamageToPlayer / DealDamageToEnemy in CombatManager.
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void ResolveDamage_NoBlock_ReducesHPByFullAmount()
    {
        // Arrange
        int block = 0;
        int hp    = 60;   // player starts at 60 HP

        // Act – Strike deals 6 damage
        CombatRules.ResolveDamage(6, ref block, ref hp);

        // Assert
        Assert.That(hp,    Is.EqualTo(54), "HP should drop by 6 when there is no block.");
        Assert.That(block, Is.EqualTo(0),  "Block should stay at 0.");
    }

    [Test]
    public void ResolveDamage_BlockAbsorbsHitCompletely_HPUntouched()
    {
        // Arrange – player played Defend (grants 5 block)
        int block = 5;
        int hp    = 60;

        // Act – enemy Strike deals 5 damage
        CombatRules.ResolveDamage(5, ref block, ref hp);

        // Assert – block exactly eats the hit
        Assert.That(hp,    Is.EqualTo(60), "HP must stay at 60 when block absorbs all damage.");
        Assert.That(block, Is.EqualTo(0),  "Block should be fully depleted (5 - 5 = 0).");
    }

    [Test]
    public void ResolveDamage_DamageExceedsBlock_BlockDepletedResidualHitsHP()
    {
        // Arrange – enemy attacks; player has some block left
        int block = 5;
        int hp    = 60;

        // Act – enemy Attack(8): 8 - 5 block = 3 remaining hits HP
        CombatRules.ResolveDamage(8, ref block, ref hp);

        // Assert
        Assert.That(block, Is.EqualTo(0),  "Block must be wiped to 0.");
        Assert.That(hp,    Is.EqualTo(57), "3 residual damage should reduce HP from 60 to 57.");
    }

    [Test]
    public void ResolveDamage_OverkillDamage_HPClampsToZeroNotNegative()
    {
        // Arrange – enemy lands a huge hit on a wounded player
        int block = 0;
        int hp    = 5;

        // Act – enemy Heavy Strike (12 damage) on 5 HP target
        CombatRules.ResolveDamage(12, ref block, ref hp);

        // Assert – HP must never go below 0
        Assert.That(hp, Is.EqualTo(0), "HP must clamp at 0; it cannot go negative.");
    }

    [Test]
    public void ResolveDamage_ZeroDamage_NothingChanges()
    {
        // Arrange
        int block = 8;
        int hp    = 50;

        // Act
        int hpLost = CombatRules.ResolveDamage(0, ref block, ref hp);

        // Assert – zero-damage calls are harmless
        Assert.That(hpLost, Is.EqualTo(0), "Zero damage must return 0 HP lost.");
        Assert.That(hp,    Is.EqualTo(50), "HP must not change.");
        Assert.That(block, Is.EqualTo(8),  "Block must not change.");
    }

    /// <summary>
    /// Parameterised test covering all meaningful damage/block combinations
    /// with the exact values used in the game (Strike=6, Defend=5, Attack=8, 10, 12).
    /// </summary>
    [TestCase(6,   0,  60,  54,  0, Description = "Strike vs no block")]
    [TestCase(5,   5,  60,  60,  0, Description = "Strike absorbed by Defend")]
    [TestCase(8,   5,  60,  57,  0, Description = "Enemy Attack(8) partially blocked")]
    [TestCase(12,  0,  60,  48,  0, Description = "Heavy Strike vs no block")]
    [TestCase(10,  5,  60,  55,  0, Description = "Enemy Attack(10) partially blocked")]
    public void ResolveDamage_GameCardValues_ProduceCorrectHPAndBlock(
        int damage, int startBlock, int startHP, int expectedHP, int expectedBlock)
    {
        // Arrange
        int block = startBlock;
        int hp    = startHP;

        // Act
        CombatRules.ResolveDamage(damage, ref block, ref hp);

        // Assert
        Assert.That(hp,    Is.EqualTo(expectedHP),    "Unexpected HP after damage.");
        Assert.That(block, Is.EqualTo(expectedBlock),  "Unexpected block after damage.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SECTION 2 – Energy  (CombatRules.CanAfford)
    // Mirrors CombatManager.CanPlayCard's energy check.
    // ─────────────────────────────────────────────────────────────────────────

    [Test]
    public void CanAfford_EnoughEnergy_ReturnsTrue()
    {
        // Player starts with 3 energy; Strike costs 1
        Assert.That(CombatRules.CanAfford(3, 1), Is.True,
            "Should afford a 1-cost card with 3 energy.");
    }

    [Test]
    public void CanAfford_ExactlyEnoughEnergy_ReturnsTrue()
    {
        // Heavy Strike costs 2; player has exactly 2 energy left
        Assert.That(CombatRules.CanAfford(2, 2), Is.True,
            "Should afford a card when energy equals cost exactly.");
    }

    [Test]
    public void CanAfford_InsufficientEnergy_ReturnsFalse()
    {
        // Player has 1 energy left but tries to play Heavy Strike (cost 2)
        Assert.That(CombatRules.CanAfford(1, 2), Is.False,
            "Should not afford a 2-cost card with only 1 energy.");
    }

    [Test]
    public void CanAfford_ZeroEnergy_ReturnsFalseForAnyCost()
    {
        Assert.That(CombatRules.CanAfford(0, 1), Is.False,
            "Cannot play any card at 0 energy.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SECTION 3 – Enemy AI Pattern  (CombatRules.GetEnemyIntent)
    // The Orc follows: Attack 8, Attack 12, Defend 6, Attack 10, Attack 6
    // and then wraps back to the start.
    // ─────────────────────────────────────────────────────────────────────────

    [TestCase(0, "Attack",  8,  Description = "Turn 0: Attack 8")]
    [TestCase(1, "Attack", 12,  Description = "Turn 1: Attack 12")]
    [TestCase(2, "Defend",  6,  Description = "Turn 2: Defend 6")]
    [TestCase(3, "Attack", 10,  Description = "Turn 3: Attack 10")]
    [TestCase(4, "Attack",  6,  Description = "Turn 4: Attack 6")]
    [TestCase(5, "Attack",  8,  Description = "Turn 5: wraps back to Attack 8")]
    public void GetEnemyIntent_CorrectActionAndValueForEachTurn(
        int turn, string expectedAction, int expectedValue)
    {
        var (action, value) = CombatRules.GetEnemyIntent(turn);

        Assert.That(action, Is.EqualTo(expectedAction), $"Wrong action on turn {turn}.");
        Assert.That(value,  Is.EqualTo(expectedValue),  $"Wrong value on turn {turn}.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SECTION 4 – Deck / Draw / Discard  (CombatRules.DrawCards, DiscardHand)
    // Mirrors CombatManager.DrawCardsSmoothRoutine and DiscardHand.
    // ─────────────────────────────────────────────────────────────────────────

    private List<string> _draw;
    private List<string> _hand;
    private List<string> _discard;

    [SetUp]
    public void SetUpDeckLists()
    {
        _draw    = new List<string>();
        _hand    = new List<string>();
        _discard = new List<string>();
    }

    [Test]
    public void StartingDeck_HasCorrectComposition_TenCards()
    {
        // BuildDeck creates exactly: 5×Strike + 3×Defend + 1×Heavy Strike + 1×Strategize
        var deck = CombatRules.GetStartingDeckNames();

        Assert.That(deck.Count, Is.EqualTo(10), "Starting deck must have 10 cards.");
        Assert.That(deck.FindAll(c => c == "Strike").Count,       Is.EqualTo(5));
        Assert.That(deck.FindAll(c => c == "Defend").Count,       Is.EqualTo(3));
        Assert.That(deck.FindAll(c => c == "Heavy Strike").Count, Is.EqualTo(1));
        Assert.That(deck.FindAll(c => c == "Strategize").Count,   Is.EqualTo(1));
    }

    [Test]
    public void DrawCards_DrawsFiveCards_MovesThemFromDrawToHand()
    {
        // StartPlayerTurn draws 5 cards
        _draw.AddRange(CombatRules.GetStartingDeckNames());

        int drawn = CombatRules.DrawCards(_draw, _hand, _discard, 5);

        Assert.That(drawn,       Is.EqualTo(5), "Must report 5 cards drawn.");
        Assert.That(_hand.Count, Is.EqualTo(5), "Hand must hold the 5 drawn cards.");
        Assert.That(_draw.Count, Is.EqualTo(5), "5 cards must remain in draw pile.");
    }

    [Test]
    public void DrawCards_DrawPileEmpty_ReshufflesDiscardAndContinues()
    {
        // Simulate: draw pile runs out mid-draw, discard gets recycled
        _draw.Add("Strike");                       // 1 card in draw pile
        _discard.AddRange(new[] { "Defend", "Defend", "Strike", "Strategize" }); // 4 in discard

        int drawn = CombatRules.DrawCards(_draw, _hand, _discard, 3);

        Assert.That(drawn,          Is.EqualTo(3), "Should draw 3 even after recycling discard.");
        Assert.That(_hand.Count,    Is.EqualTo(3));
        Assert.That(_discard.Count, Is.EqualTo(0), "Discard must be emptied into draw pile.");
    }

    [Test]
    public void DrawCards_BothPilesEmpty_DrawsZeroCards()
    {
        // No cards left anywhere — must not throw, must return 0
        int drawn = CombatRules.DrawCards(_draw, _hand, _discard, 5);

        Assert.That(drawn,       Is.EqualTo(0));
        Assert.That(_hand.Count, Is.EqualTo(0));
    }

    [Test]
    public void DiscardHand_EndOfTurn_AllCardsMovedToDiscardPile()
    {
        // After a turn the hand gets discarded (mirrors CombatManager.DiscardHand)
        _hand.AddRange(new[] { "Strike", "Defend", "Heavy Strike" });

        CombatRules.DiscardHand(_hand, _discard);

        Assert.That(_hand.Count,    Is.EqualTo(0), "Hand must be empty after discarding.");
        Assert.That(_discard.Count, Is.EqualTo(3), "All 3 cards must move to discard pile.");
    }
}
