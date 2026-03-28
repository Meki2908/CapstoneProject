using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
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

    [Header("Tutorial Callback")]
    [Tooltip("Gán TutorialTextDisplay.OnWeaponChanged vào đây trong Inspector")]
    public UnityEvent OnWeaponSwapped;

    private WeaponType pendingWeaponType;

    private void Awake()
    {
        // Auto-find references if not assigned
        if (weaponController == null)
            weaponController = FindFirstObjectByType<WeaponController>();

        if (character == null)
            character = FindFirstObjectByType<Character>();

        if (enemyDetection == null)
            enemyDetection = FindFirstObjectByType<EnemyDetection>();
    }

    private void Start()
    {
        SetupButtons();
        SetupConfirmationDialog();
        SetupUpgradeButtons();
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

        if (weaponController == null)
            weaponController = FindFirstObjectByType<WeaponController>();

        if (weaponController == null)
        {
            Debug.LogError("[WeaponSwapper] WeaponController not found! Make sure it exists in the scene.");
            return;
        }

        WeaponSO targetWeapon = GetWeaponSO(pendingWeaponType);
        if (targetWeapon == null)
        {
            Debug.LogError($"[WeaponSwapper] No weapon data found for {pendingWeaponType}!");
            return;
        }

        // Switch weapon
        weaponController.EquipWeapon(targetWeapon);

        // Notify tutorial (hoặc bất kỳ listener nào)
        OnWeaponSwapped?.Invoke();

        // Hide confirmation dialog
        if (confirmationDialog != null)
            confirmationDialog.SetActive(false);

        Debug.Log($"[WeaponSwapper] Switched to {pendingWeaponType} weapon");
        ShowMessage($"Switched to {GetWeaponDisplayName(pendingWeaponType)}!");
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
        if (weaponController == null) return;

        WeaponSO targetWeapon = GetWeaponSO(weaponType);
        if (targetWeapon != null)
        {
            weaponController.EquipWeapon(targetWeapon);
            Debug.Log($"[WeaponSwapper] Force switched to {weaponType}");
        }
    }
}
