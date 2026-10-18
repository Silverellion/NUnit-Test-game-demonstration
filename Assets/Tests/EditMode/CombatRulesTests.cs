using System.Collections.Generic;
using NUnit.Framework;

[TestFixture]
[Category("Combat")]
public class DamageTests
{
    [Test]
    public void ResolveDamage_NoBlock_ReducesHPByFullAmount()
    {
        int block = 0;
        int hp = 60;

        CombatRules.ResolveDamage(6, ref block, ref hp);

        Assert.That(hp, Is.EqualTo(54), "HP should drop by 6 when there is no block.");
        Assert.That(block, Is.EqualTo(0), "Block should stay at 0.");
    }

    [Test]
    public void ResolveDamage_BlockAbsorbsHitCompletely_HPUntouched()
    {
        int block = 5;
        int hp = 60;

        CombatRules.ResolveDamage(5, ref block, ref hp);

        Assert.That(hp, Is.EqualTo(60), "HP must stay at 60 when block absorbs all damage.");
        Assert.That(block, Is.EqualTo(0), "Block should be fully depleted (5 - 5 = 0).");
    }

    [Test]
    public void ResolveDamage_DamageExceedsBlock_BlockDepletedResidualHitsHP()
    {
        int block = 5;
        int hp = 60;

        CombatRules.ResolveDamage(8, ref block, ref hp);

        Assert.That(block, Is.EqualTo(0), "Block must be wiped to 0.");
        Assert.That(hp, Is.EqualTo(57), "3 residual damage should reduce HP from 60 to 57.");
    }

    [Test]
    public void ResolveDamage_OverkillDamage_HPClampsToZeroNotNegative()
    {
        int block = 0;
        int hp = 5;

        CombatRules.ResolveDamage(12, ref block, ref hp);

        Assert.That(hp, Is.EqualTo(0), "HP must clamp at 0; it cannot go negative.");
    }

    [Test]
    public void ResolveDamage_DamageLessThanBlock_BlockReducedHPUntouched()
    {
        int block = 5;
        int hp = 60;

        CombatRules.ResolveDamage(3, ref block, ref hp);

        Assert.That(hp, Is.EqualTo(60), "HP must stay at 60 when block absorbs all damage.");
        Assert.That(block, Is.EqualTo(2), "Block should be reduced by 3 (5 - 3 = 2).");
    }
}

[TestFixture]
[Category("Energy")]
public class EnergyTests
{
    [Test]
    public void CanAfford_ExactlyEnoughEnergy_ReturnsTrue()
    {
        Assert.That(CombatRules.CanAfford(2, 2), Is.True,
            "Should afford a card when energy equals cost exactly.");
    }

    [Test]
    public void CanAfford_InsufficientEnergy_ReturnsFalse()
    {
        Assert.That(CombatRules.CanAfford(1, 2), Is.False,
            "Should not afford a 2-cost card with only 1 energy.");
    }
}

[TestFixture]
[Category("AI")]
public class EnemyAITests
{
    [TestCase(0, "Attack", 8, TestName = "EnemyIntent_Turn0_Attack8")]
    [TestCase(2, "Defend", 6, TestName = "EnemyIntent_Turn2_Defend6")]
    [TestCase(4, "Attack", 6, TestName = "EnemyIntent_Turn4_Attack6")]
    [TestCase(5, "Attack", 8, TestName = "EnemyIntent_Turn5_WrapsToAttack8")]
    public void GetEnemyIntent_CorrectActionAndValueForEachTurn(
        int turn, string expectedAction, int expectedValue)
    {
        var (action, value) = CombatRules.GetEnemyIntent(turn);

        Assert.That(action, Is.EqualTo(expectedAction), $"Wrong action on turn {turn}.");
        Assert.That(value, Is.EqualTo(expectedValue), $"Wrong value on turn {turn}.");
    }
}

[TestFixture]
[Category("Deck")]
public class DeckTests
{
    private List<string> _draw;
    private List<string> _hand;
    private List<string> _discard;

    [SetUp]
    public void SetUpDeckLists()
    {
        _draw = new List<string>();
        _hand = new List<string>();
        _discard = new List<string>();
    }

    [Test]
    public void DrawCards_DrawPileEmpty_ReshufflesDiscardAndContinues()
    {
        _draw.Add("Strike");
        _discard.AddRange(new[] { "Defend", "Defend", "Strike", "Strategize" });
        int totalBefore = _draw.Count + _hand.Count + _discard.Count;

        int drawn = CombatRules.DrawCards(_draw, _hand, _discard, 3);

        Assert.That(drawn, Is.EqualTo(3), "Should draw 3 even after recycling discard.");
        Assert.That(_hand.Count, Is.EqualTo(3));
        Assert.That(_discard.Count, Is.EqualTo(0), "Discard must be emptied into draw pile.");
        Assert.That(_draw.Count, Is.EqualTo(2), "Draw pile should have remaining cards after draw.");
        Assert.That(_draw.Count + _hand.Count + _discard.Count, Is.EqualTo(totalBefore),
            "Total card count across draw + hand + discard must be conserved.");
    }

    [Test]
    public void DrawCards_BothPilesEmpty_DrawsZeroCards()
    {
        int drawn = CombatRules.DrawCards(_draw, _hand, _discard, 5);

        Assert.That(drawn, Is.EqualTo(0));
        Assert.That(_hand.Count, Is.EqualTo(0));
    }

    [Test]
    public void DiscardHand_EndOfTurn_AllCardsMovedToDiscardPile()
    {
        _hand.AddRange(new[] { "Strike", "Defend", "Heavy Strike" });

        CombatRules.DiscardHand(_hand, _discard);

        Assert.That(_hand.Count, Is.EqualTo(0), "Hand must be empty after discarding.");
        Assert.That(_discard.Count, Is.EqualTo(3), "All 3 cards must move to discard pile.");
    }
}
