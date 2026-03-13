using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Bridge kết nối SettingsManager Key Bindings (KeyCode) → InputSystem Actions (binding overrides).
/// Gắn lên cùng GameObject với PlayerInput hoặc gọi static Apply.
/// </summary>
public static class InputRebindHelper
{
    // Mapping: Settings action name → (InputAction name, binding index for Keyboard&Mouse)
    // binding index: -1 = tìm tự động Keyboard&Mouse binding đầu tiên
    // Composite bindings (Move) cần xử lý riêng
    private static readonly Dictionary<string, string> ActionMapping = new Dictionary<string, string>
    {
        { "Dodge", "Sprint" },       // Settings "Dodge" = InputAction "Sprint"
        { "Sprint", "Dash" },        // Settings "Sprint" = InputAction "Dash"
        { "SneakCrouch", "Crouch" },
        { "Jump", "Jump" },
        { "Attack", "Attack" },
        { "Interact", "Interact" },
        { "Menu", "OpenMenu" },
        { "WeaponWheel", "ToggleWeapon" },
    };

    // Move composite: Settings name → composite part name
    private static readonly Dictionary<string, string> MoveMapping = new Dictionary<string, string>
    {
        { "MoveForward", "up" },
        { "MoveBackward", "down" },
        { "MoveLeft", "left" },
        { "MoveRight", "right" },
    };

    /// <summary>
    /// Chuyển KeyCode sang InputSystem binding path.
    /// VD: KeyCode.Space → "<Keyboard>/space", KeyCode.Mouse0 → "<Mouse>/leftButton"
    /// </summary>
    public static string KeyCodeToBindingPath(KeyCode key)
    {
        switch (key)
        {
            // Mouse buttons
            case KeyCode.Mouse0: return "<Mouse>/leftButton";
            case KeyCode.Mouse1: return "<Mouse>/rightButton";
            case KeyCode.Mouse2: return "<Mouse>/middleButton";

            // Special keys
            case KeyCode.Space: return "<Keyboard>/space";
            case KeyCode.LeftShift: return "<Keyboard>/leftShift";
            case KeyCode.RightShift: return "<Keyboard>/rightShift";
            case KeyCode.LeftControl: return "<Keyboard>/leftCtrl";
            case KeyCode.RightControl: return "<Keyboard>/rightCtrl";
            case KeyCode.LeftAlt: return "<Keyboard>/leftAlt";
            case KeyCode.RightAlt: return "<Keyboard>/rightAlt";
            case KeyCode.Tab: return "<Keyboard>/tab";
            case KeyCode.Escape: return "<Keyboard>/escape";
            case KeyCode.Return: return "<Keyboard>/enter";
            case KeyCode.Backspace: return "<Keyboard>/backspace";
            case KeyCode.CapsLock: return "<Keyboard>/capsLock";
            case KeyCode.BackQuote: return "<Keyboard>/backquote";

            // F keys
            case KeyCode.F1: return "<Keyboard>/f1";
            case KeyCode.F2: return "<Keyboard>/f2";
            case KeyCode.F3: return "<Keyboard>/f3";
            case KeyCode.F4: return "<Keyboard>/f4";
            case KeyCode.F5: return "<Keyboard>/f5";
            case KeyCode.F6: return "<Keyboard>/f6";
            case KeyCode.F7: return "<Keyboard>/f7";
            case KeyCode.F8: return "<Keyboard>/f8";
            case KeyCode.F9: return "<Keyboard>/f9";
            case KeyCode.F10: return "<Keyboard>/f10";
            case KeyCode.F11: return "<Keyboard>/f11";
            case KeyCode.F12: return "<Keyboard>/f12";

            // Arrow keys
            case KeyCode.UpArrow: return "<Keyboard>/upArrow";
            case KeyCode.DownArrow: return "<Keyboard>/downArrow";
            case KeyCode.LeftArrow: return "<Keyboard>/leftArrow";
            case KeyCode.RightArrow: return "<Keyboard>/rightArrow";

            default:
                // Letters and numbers: KeyCode.A = 97, map to "<Keyboard>/a"
                string keyName = key.ToString().ToLower();
                if (keyName.Length == 1) // Single letter or digit
                    return $"<Keyboard>/{keyName}";

                // Alpha numbers: KeyCode.Alpha0..Alpha9
                if (keyName.StartsWith("alpha"))
                    return $"<Keyboard>/{keyName.Replace("alpha", "")}";

                // Keypad
                if (keyName.StartsWith("keypad"))
                    return $"<Keyboard>/numpad{keyName.Replace("keypad", "")}";

                Debug.LogWarning($"[InputRebindHelper] Unknown KeyCode: {key}");
                return null;
        }
    }

    /// <summary>
    /// Apply tất cả key bindings từ SettingsManager vào PlayerInput InputActions.
    /// Gọi sau khi user nhấn Confirm trong Settings hoặc khi Load settings.
    /// </summary>
    public static void ApplyAllBindings(PlayerInput playerInput, System.Func<string, KeyCode> getKeyBinding)
    {
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogWarning("[InputRebindHelper] PlayerInput or actions is null");
            return;
        }

        var actionAsset = playerInput.actions;

        // 1. Apply simple action bindings (non-composite)
        foreach (var kvp in ActionMapping)
        {
            string settingsAction = kvp.Key;
            string inputActionName = kvp.Value;

            KeyCode key = getKeyBinding(settingsAction);
            if (key == KeyCode.None) continue;

            string bindingPath = KeyCodeToBindingPath(key);
            if (string.IsNullOrEmpty(bindingPath)) continue;

            var action = actionAsset.FindAction(inputActionName);
            if (action == null)
            {
                Debug.LogWarning($"[InputRebindHelper] Action '{inputActionName}' not found");
                continue;
            }

            // Tìm binding Keyboard&Mouse (non-composite) để override
            bool applied = false;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];
                if (binding.isComposite || binding.isPartOfComposite) continue;

                // Chỉ override Keyboard&Mouse bindings
                string groups = binding.groups ?? "";
                if (groups.Contains("Keyboard") || groups.Contains("Mouse") || string.IsNullOrEmpty(groups))
                {
                    // Chỉ override binding không phải XR/Gamepad/Joystick
                    if (groups.Contains("Gamepad") || groups.Contains("XR") || groups.Contains("Joystick")) continue;

                    action.ApplyBindingOverride(i, bindingPath);
                    applied = true;
                    Debug.Log($"[InputRebindHelper] {settingsAction}: {inputActionName}[{i}] → {bindingPath}");
                    break;
                }
            }

            if (!applied)
            {
                Debug.LogWarning($"[InputRebindHelper] Could not find Keyboard binding for '{inputActionName}'");
            }
        }

        // 2. Apply Move composite bindings (WASD)
        var moveAction = actionAsset.FindAction("Move");
        if (moveAction != null)
        {
            foreach (var kvp in MoveMapping)
            {
                string settingsAction = kvp.Key;
                string compositePart = kvp.Value;

                KeyCode key = getKeyBinding(settingsAction);
                if (key == KeyCode.None) continue;

                string bindingPath = KeyCodeToBindingPath(key);
                if (string.IsNullOrEmpty(bindingPath)) continue;

                // Tìm composite part "up"/"down"/"left"/"right" trong WASD composite (không phải Arrow keys)
                for (int i = 0; i < moveAction.bindings.Count; i++)
                {
                    var binding = moveAction.bindings[i];
                    if (!binding.isPartOfComposite) continue;
                    if (binding.name != compositePart) continue;

                    string groups = binding.groups ?? "";
                    if (!groups.Contains("Keyboard") && !string.IsNullOrEmpty(groups)) continue;

                    // Chỉ override binding WASD chính (w/s/a/d, không phải arrows)
                    string path = binding.effectivePath ?? "";
                    if (path.Contains("Arrow")) continue; // Skip arrow key bindings

                    moveAction.ApplyBindingOverride(i, bindingPath);
                    Debug.Log($"[InputRebindHelper] {settingsAction}: Move/{compositePart}[{i}] → {bindingPath}");
                    break;
                }
            }
        }

        // 3. Heal — không có InputAction tương ứng, skip
        // Nếu sau này thêm Heal action, thêm vào ActionMapping

        Debug.Log("[InputRebindHelper] All key bindings applied to InputSystem");
    }

    /// <summary>
    /// Lưu binding overrides vào PlayerPrefs (để load lại khi restart game)
    /// </summary>
    public static void SaveBindingOverrides(PlayerInput playerInput)
    {
        if (playerInput == null || playerInput.actions == null) return;
        string overrides = playerInput.actions.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("InputBindingOverrides", overrides);
        PlayerPrefs.Save();
        Debug.Log("[InputRebindHelper] Binding overrides saved");
    }

    /// <summary>
    /// Load binding overrides từ PlayerPrefs
    /// </summary>
    public static void LoadBindingOverrides(PlayerInput playerInput)
    {
        if (playerInput == null || playerInput.actions == null) return;
        string overrides = PlayerPrefs.GetString("InputBindingOverrides", "");
        if (!string.IsNullOrEmpty(overrides))
        {
            playerInput.actions.LoadBindingOverridesFromJson(overrides);
            Debug.Log("[InputRebindHelper] Binding overrides loaded");
        }
    }
}
