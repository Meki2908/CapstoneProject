using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

public class WeaponSwapper : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button swordButton;
    [SerializeField] private Button axeButton;
    [SerializeField] private Button mageButton;

    [Header("Upgrade Buttons (Weapon Forge)")]
    [SerializeField] private Button swordUpgradeButton;
    [SerializeField] private Button axeUpgradeButton;
    [SerializeField] private Button mageUpgradeButton;
    [SerializeField] private WeaponForgeUI weaponForgeUI;

    [Header("Confirmation Dialog")]
    [SerializeField] private GameObject confirmationDialog;
    [SerializeField] private Button yesButton;

    [Header("Warning Dialog")]
    [SerializeField] private GameObject warningDialog;

    [Header("Combat Warning Dialog")]
    [SerializeField] private GameObject combatWarningDialog;

    [Header("Weapon Data")]
    [SerializeField] private WeaponSO swordWeapon;
    [SerializeField] private WeaponSO axeWeapon;
    [SerializeField] private WeaponSO mageWeapon;

    [Header("References")]
    [SerializeField] private WeaponController weaponController;
    [SerializeField] private Character character;
    [SerializeField] private EnemyDetection enemyDetection;
    [Header("Debug")]
    [Tooltip("Log luồng đổi vũ khí theo network (UI request -> Character.SyncEquippedWeaponType).")]
    [SerializeField] private bool debugNetworkSwapFlow = true;

    [Header("Tutorial Callback")]
    [Tooltip("Gán TutorialTextDisplay.OnWeaponChanged vào đây trong Inspector")]
    public UnityEvent OnWeaponSwapped;

    // ── Tutorial mode: bypass sheath / combat checks ──────────────────────
    bool _tutorialMode = false;
    /// <summary>Gọi từ TutorialTextDisplay để bỏ qua cảnh báo vũ khí / combat.</summary>
    public void SetTutorialMode(bool active) => _tutorialMode = active;

    private WeaponType pendingWeaponType;

    private void Start()
    {
        SetupButtons();
        SetupConfirmationDialog();
        SetupUpgradeButtons();
    }

    private void Update()
    {
        var loc = Character.LocalCharacter;
        if (loc == null)
        {
            if (character != null)
            {
                character = null;
                weaponController = null;
                enemyDetection = null;
            }
            return;
        }

        if (character != loc)
        {
            character = loc;
            weaponController = character.GetComponentInChildren<WeaponController>();
            enemyDetection = character.GetComponentInChildren<EnemyDetection>();
        }
    }

    private void SetupUpgradeButtons()
    {
        if (weaponForgeUI == null)
            weaponForgeUI = FindFirstObjectByType<WeaponForgeUI>();

        if (swordUpgradeButton != null)
            swordUpgradeButton.onClick.AddListener(() => OnUpgradeButtonClicked(WeaponType.Sword));

        if (axeUpgradeButton != null)
            axeUpgradeButton.onClick.AddListener(() => OnUpgradeButtonClicked(WeaponType.Axe));

        if (mageUpgradeButton != null)
            mageUpgradeButton.onClick.AddListener(() => OnUpgradeButtonClicked(WeaponType.Mage));
    }

    private void OnUpgradeButtonClicked(WeaponType weaponType)
    {
        if (weaponForgeUI == null)
        {
            Debug.LogWarning("[WeaponSwapper] WeaponForgeUI not found!");
            return;
        }

        // Get the weapon SO for this type
        WeaponSO weapon = GetWeaponSO(weaponType);
        if (weapon != null)
        {
            weaponForgeUI.OpenForge(weapon);
        }
        else
        {
            Debug.LogWarning($"[WeaponSwapper] No weapon found for {weaponType}");
        }
    }

    private void SetupButtons()
    {
        if (swordButton != null)
            swordButton.onClick.AddListener(() => OnWeaponButtonClicked(WeaponType.Sword));

        if (axeButton != null)
            axeButton.onClick.AddListener(() => OnWeaponButtonClicked(WeaponType.Axe));

        if (mageButton != null)
            mageButton.onClick.AddListener(() => OnWeaponButtonClicked(WeaponType.Mage));
    }

    private void SetupConfirmationDialog()
    {
        if (yesButton != null)
            yesButton.onClick.AddListener(OnConfirmWeaponSwitch);

        if (confirmationDialog != null)
            confirmationDialog.SetActive(false);

        if (warningDialog != null)
            warningDialog.SetActive(false);

        // Setup combat warning dialog
        if (combatWarningDialog != null)
            combatWarningDialog.SetActive(false);
    }

    private void OnWeaponButtonClicked(WeaponType weaponType)
    {
        // Check if already using this weapon
        if (weaponController != null && weaponController.GetCurrentWeapon() != null)
        {
            WeaponType currentWeaponType = weaponController.GetCurrentWeapon().weaponType;
            if (currentWeaponType == weaponType)
            {
                Debug.Log($"[WeaponSwapper] Already using {weaponType} weapon!");
                ShowMessage($"You are already using {GetWeaponDisplayName(weaponType)}!");
                return;
            }
        }

        // Show confirmation dialog
        pendingWeaponType = weaponType;
        confirmationDialog.SetActive(true);
    }

    private void OnConfirmWeaponSwitch()
    {
        if (!_tutorialMode)
        {
            // Priority 1: Sheath warning if weapon is drawn
            if (character != null && character.isWeaponDrawn)
            {
                if (confirmationDialog != null)
                    confirmationDialog.SetActive(false);
                ShowWarningDialog();
                return;
            }

            // Priority 2: Combat warning if currently in combat
            bool isInCombat = false;
            if (enemyDetection != null)
                isInCombat = enemyDetection.IsInCombat();

            if (isInCombat)
            {
                if (confirmationDialog != null)
                    confirmationDialog.SetActive(false);
                ShowCombatWarningDialog();
                return;
            }
        }

        WeaponSO targetWeapon = GetWeaponSO(pendingWeaponType);
        if (targetWeapon == null)
        {
            Debug.LogError($"[WeaponSwapper] No weapon data found for {pendingWeaponType}!");
            return;
        }

        if (character != null)
        {
            var wcCh = weaponController != null ? weaponController.GetComponentInParent<Character>() : null;
            string wcOwner = wcCh != null ? wcCh.name : "null";
            var before = weaponController != null && weaponController.GetCurrentWeapon() != null
                ? weaponController.GetCurrentWeapon().weaponType.ToString()
                : "None";
            character.LogCritFsm(
                "WeaponSwap",
                $"CONFIRM swap {before} -> {targetWeapon.weaponType} | wc={(weaponController != null ? weaponController.name : "null")} wcOwner={wcOwner} sameOwner={(wcCh == character)} netType={character.NetEquippedWeaponType} drawn={character.isWeaponDrawn}");
        }

        // Network-first flow: UI chỉ request sync; visuals sẽ apply từ Character.Render change detection.
        RequestNetworkWeaponSwap(targetWeapon.weaponType, "Confirm");

        // Notify tutorial (hoặc bất kỳ listener nào)
        OnWeaponSwapped?.Invoke();

        // Hide confirmation dialog
        if (confirmationDialog != null)
            confirmationDialog.SetActive(false);

        Debug.Log($"[WeaponSwapper] Requested network swap to {pendingWeaponType}");
        ShowMessage($"Requested {GetWeaponDisplayName(pendingWeaponType)} swap...");
    }

    private void OnCancelWeaponSwitch()
    {
        if (confirmationDialog != null)
            confirmationDialog.SetActive(false);

        Debug.Log("[WeaponSwapper] Weapon switch cancelled");
    }

    private void ShowCombatWarningDialog()
    {
        if (combatWarningDialog != null)
            combatWarningDialog.SetActive(true);
    }

    private void ShowWarningDialog()
    {
        if (warningDialog != null)
            warningDialog.SetActive(true);
    }

    public WeaponSO GetWeaponSO(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Sword:
                return swordWeapon;
            case WeaponType.Axe:
                return axeWeapon;
            case WeaponType.Mage:
                return mageWeapon;
            default:
                return null;
        }
    }

    private string GetWeaponDisplayName(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Sword:
                return "Sword";
            case WeaponType.Axe:
                return "Axe";
            case WeaponType.Mage:
                return "Mage Staff";
            default:
                return weaponType.ToString();
        }
    }

    private void ShowMessage(string message)
    {
        // You can implement a message system here (e.g., UI popup, console log, etc.)
        Debug.Log($"[WeaponSwapper] {message}");

        // Example: Show message in UI (you can customize this)
        // if (messageText != null)
        // {
        //     messageText.text = message;
        //     messageText.gameObject.SetActive(true);
        //     Invoke(nameof(HideMessage), 2f);
        // }
    }

    private void HideMessage()
    {
        // Hide message UI if you implement it
        // if (messageText != null)
        //     messageText.gameObject.SetActive(false);
    }

    // Public methods for external calls
    public bool CanSwitchWeapon()
    {
        if (character == null) return false;

        // Check combat state from EnemyDetection
        bool isInCombat = false;
        if (enemyDetection != null)
            isInCombat = enemyDetection.IsInCombat();

        return !character.isWeaponDrawn && !isInCombat;
    }

    public void ForceSwitchWeapon(WeaponType weaponType)
    {
        WeaponSO targetWeapon = GetWeaponSO(weaponType);
        if (targetWeapon != null)
        {
            RequestNetworkWeaponSwap(targetWeapon.weaponType, "ForceSwitch");
        }
    }

    void RequestNetworkWeaponSwap(WeaponType targetType, string source)
    {
        if (character == null)
        {
            Debug.LogError($"[WeaponSwapper] ({source}) Character is null -> cannot request network swap to {targetType}.");
            return;
        }

        bool canSendNetwork =
            character.Object != null &&
            character.Object.IsValid &&
            character.Runner != null &&
            character.Runner.IsRunning;

        if (debugNetworkSwapFlow)
        {
            Debug.Log(
                $"[WeaponSwapFlow] source={source} target={targetType} canSendNetwork={canSendNetwork} SA={character.HasStateAuthority} IA={character.HasInputAuthority} netType={character.NetEquippedWeaponType} obj={character.name}",
                character);
        }

        if (!canSendNetwork)
        {
            Debug.LogWarning($"[WeaponSwapper] ({source}) Runner/Object not ready -> skip network swap request {targetType}.");
            return;
        }

        if (!character.HasInputAuthority && !character.HasStateAuthority)
        {
            Debug.LogWarning(
                $"[WeaponSwapper] ({source}) No authority to request swap {targetType} on {character.name}. UI may be bound to wrong player.",
                character);
            return;
        }

        character.LogCritFsm(
            "WeaponSwap",
            $"REQUEST from {source} -> target={targetType} (idx={(int)targetType}) netBefore={character.NetEquippedWeaponType}");
        character.SyncEquippedWeaponType(targetType);
        character.DebugLogNetEquippedWeaponDelayed();
    }
}


