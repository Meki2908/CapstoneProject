using UnityEngine;

public class WeaponSwapTest : MonoBehaviour
{
    [Header("Test Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private KeyCode testCombatKey = KeyCode.C;
    [SerializeField] private KeyCode testSheathKey = KeyCode.X;

    private WeaponSwapper weaponSwapper;
    private Character character;
    private EnemyDetection enemyDetection;

    private void Start()
    {
        // Find components
        weaponSwapper = FindObjectOfType<WeaponSwapper>();
        character = FindObjectOfType<Character>();
        enemyDetection = FindObjectOfType<EnemyDetection>();

        if (enableDebugLogs)
        {
            Debug.Log("[WeaponSwapTest] Test script initialized");
            Debug.Log($"[WeaponSwapTest] WeaponSwapper found: {weaponSwapper != null}");
            Debug.Log($"[WeaponSwapTest] Character found: {character != null}");
            Debug.Log($"[WeaponSwapTest] EnemyDetection found: {enemyDetection != null}");
        }
    }

    private void Update()
    {
        // Test combat state toggle
        if (Input.GetKeyDown(testCombatKey))
        {
            ToggleCombatState();
        }

        // Test sheath state toggle
        if (Input.GetKeyDown(testSheathKey))
        {
            ToggleSheathState();
        }

        // Display current state
        if (enableDebugLogs && Time.frameCount % 60 == 0) // Every second
        {
            DisplayCurrentState();
        }
    }

    private void ToggleCombatState()
    {
        // Note: EnemyDetection combat state is managed automatically by enemy detection
        // This test method is disabled as CombatDetector has been removed
        Debug.Log("[WeaponSwapTest] Combat state toggle disabled - EnemyDetection manages combat state automatically");
        Debug.Log($"[WeaponSwapTest] Current combat state: {(enemyDetection != null ? enemyDetection.IsInCombat() : false)}");
    }

    private void ToggleSheathState()
    {
        if (character == null) return;

        character.isWeaponDrawn = !character.isWeaponDrawn;
        Debug.Log($"[WeaponSwapTest] Sheath state toggled - Weapon drawn: {character.isWeaponDrawn}");
    }

    private void DisplayCurrentState()
    {
        if (character == null) return;

        bool canSwitch = weaponSwapper != null ? weaponSwapper.CanSwitchWeapon() : false;
        bool isInCombat = enemyDetection != null ? enemyDetection.IsInCombat() : false;

        Debug.Log($"[WeaponSwapTest] State - Weapon Drawn: {character.isWeaponDrawn}, " +
                 $"EnemyDetection: {isInCombat}, " +
                 $"In Combat: {isInCombat}, Can Switch: {canSwitch}");
    }

    private void OnGUI()
    {
        if (!enableDebugLogs) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("Weapon Swap Test Controls", GUI.skin.box);
        GUILayout.Label($"Press {testCombatKey} to toggle combat state");
        GUILayout.Label($"Press {testSheathKey} to toggle sheath state");
        GUILayout.Space(10);

        if (character != null)
        {
            GUILayout.Label($"Weapon Drawn: {character.isWeaponDrawn}");
        }

        if (enemyDetection != null)
        {
            GUILayout.Label($"EnemyDetection: {enemyDetection.IsInCombat()}");
        }

        if (weaponSwapper != null)
        {
            GUILayout.Label($"Can Switch: {weaponSwapper.CanSwitchWeapon()}");
        }

        GUILayout.EndArea();
    }
}
