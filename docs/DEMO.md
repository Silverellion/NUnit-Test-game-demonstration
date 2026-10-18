# NUnit Demo Guide

## How to Run Tests in Visual Studio
1. In Unity, double-click any `.cs` script (Visual Studio opens the default Unity solution).
2. In Visual Studio, open **Test > Test Explorer** (`Ctrl+E, T`).
3. Click **Run All** (`Ctrl+R, A`). All 14 tests run immediately.

---

## Demo Hotkeys (In Unity Play Mode)
Attach `DebugDemoKeys.cs` to any GameObject in the scene (e.g. `CombatManager`).
* **F1**: Player HP = 5
* **F2**: Player Block = 5
* **F3**: Energy = 2
* **F4**: Empty draw pile (0 cards)
* **F5**: Enemy Turn 1 (Intent: Attack 12)

---

## 5 Demo Bugs (Comment / Uncomment in `CombatRules.cs`)

### Bug 1: HP Goes Negative
* **File:** `Assets/Scripts/Rules/CombatRules.cs` in `ResolveDamage()`
* **How to toggle:**
  * Uncomment: `hpLost = remaining;`
  * Comment out: `hpLost = remaining > hp ? hp : remaining;`
* **Test that fails:** `ResolveDamage_OverkillDamage_HPClampsToZeroNotNegative`
* **In Game:** Press **F1** (HP 5), **F5** (Enemy Attack 12), click End Turn. Player HP drops to `-7`.

### Bug 2: Block Ignored Completely
* **File:** `Assets/Scripts/Rules/CombatRules.cs` in `ResolveDamage()`
* **How to toggle:**
  * Uncomment: `if (false && block > 0)`
  * Comment out: `if (block > 0)`
* **Test that fails:** `ResolveDamage_BlockAbsorbsHitCompletely_HPUntouched`
* **In Game:** Press **F2** (Block 5), let enemy attack. Damage pierces straight to HP while block shield stays at 5.

### Bug 3: Can't Play Card When Energy Equals Cost (`>=` to `>`)
* **File:** `Assets/Scripts/Rules/CombatRules.cs` in `CanAfford()`
* **How to toggle:**
  * Uncomment: `return currentEnergy > cardCost;`
  * Comment out: `return currentEnergy >= cardCost;`
* **Test that fails:** `CanAfford_ExactlyEnoughEnergy_ReturnsTrue`
* **In Game:** Press **F3** (Energy 2). *Heavy Strike* (cost 2) is disabled and cannot be played.

### Bug 4: Enemy Skips Turn 4 Move (`% 5` to `% 4`)
* **File:** `Assets/Scripts/Rules/CombatRules.cs` in `GetEnemyIntent()`
* **How to toggle:**
  * Uncomment: `return EnemyPattern[turnCount % 4];`
  * Comment out: `return EnemyPattern[turnCount % EnemyPattern.Length];`
* **Test that fails:** `EnemyIntent_Turn4_Attack6`
* **In Game:** On turn 4, enemy intends Attack 8 instead of Attack 6 (subtle bug; players won't notice, but unit tests catch it).

### Bug 5: Empty Draw Pile Does Not Reshuffle Discard
* **File:** `Assets/Scripts/Rules/CombatRules.cs` in `DrawOneCard()`
* **How to toggle:**
  * Uncomment: `return false;` inside `if (drawPile.Count == 0)`
  * Comment out the 4 recycling lines below it.
* **Test that fails:** `DrawCards_DrawPileEmpty_ReshufflesDiscardAndContinues`
* **In Game:** Press **F4** (empty draw pile), click End Turn. Game stops dealing cards.

---

## 3-Minute Live Demo Flow

1. **Green Baseline (0:00 - 0:45)**:
   * Double-click a script in Unity to open Visual Studio.
   * Open Test Explorer, click **Run All** -> All 14 tests Green.
   * Show Unity Test Runner (`Window > General > Test Runner`) -> Green.
2. **Inject Bug 3 (0:45 - 1:30)**:
   * In `CombatRules.cs`, swap the comment in `CanAfford()`.
   * Save file (`Ctrl+S`).
3. **Run Tests - Red (1:30 - 2:00)**:
   * In Visual Studio Test Explorer, click **Run All**.
   * `CanAfford_ExactlyEnoughEnergy_ReturnsTrue` turns **RED**.
4. **Show in Game (2:00 - 2:45)**:
   * Hit Play in Unity.
   * Press **F3** (energy becomes 2).
   * Try dragging *Heavy Strike* (cost 2) -> card is locked and cannot be played.
5. **Revert - Green (2:45 - 3:00)**:
   * Revert the comment in `CombatRules.cs`.
   * Re-run tests in Visual Studio -> **GREEN**.

> **Note on test coverage:** Unit tests cover pure game calculations in `CombatRules.cs`. They do not cover Unity UI elements, animations, or coroutines.
