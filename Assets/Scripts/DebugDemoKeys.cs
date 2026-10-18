using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Demo-only hotkeys (F1-F5) to manipulate combat state for live demonstrations.
/// </summary>
public class DebugDemoKeys : MonoBehaviour
{
    [SerializeField] private CombatManager combatManager;

    private void Awake()
    {
        if (combatManager == null)
            combatManager = FindFirstObjectByType<CombatManager>();
    }

    private void Update()
    {
        if (combatManager == null) return;

#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.f1Key.wasPressedThisFrame) // F1: Set player HP to 5
            combatManager.DebugSetPlayerHP(5);

        if (keyboard.f2Key.wasPressedThisFrame) // F2: Set player Block to 5
            combatManager.DebugSetPlayerBlock(5);

        if (keyboard.f3Key.wasPressedThisFrame) // F3: Set energy to 2
            combatManager.DebugSetEnergy(2);

        if (keyboard.f4Key.wasPressedThisFrame) // F4: Empty draw pile
            combatManager.DebugEmptyDrawPile();

        if (keyboard.f5Key.wasPressedThisFrame) // F5: Set enemy turn to 1 (Attack 12)
            combatManager.DebugSetEnemyTurn(1);
#else
        if (Input.GetKeyDown(KeyCode.F1))
            combatManager.DebugSetPlayerHP(5);

        if (Input.GetKeyDown(KeyCode.F2))
            combatManager.DebugSetPlayerBlock(5);

        if (Input.GetKeyDown(KeyCode.F3))
            combatManager.DebugSetEnergy(2);

        if (Input.GetKeyDown(KeyCode.F4))
            combatManager.DebugEmptyDrawPile();

        if (Input.GetKeyDown(KeyCode.F5))
            combatManager.DebugSetEnemyTurn(1);
#endif
    }
}
