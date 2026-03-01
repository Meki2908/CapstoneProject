using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Helper script to test inventory functionality
/// Attach this to a button and assign it to the button's OnClick event
/// </summary>
public class InventoryTestButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    /// <summary>
    /// Called when button is clicked - adds a random item to inventory
    /// </summary>
    public void OnButtonClicked()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddRandomItem();
        }
        else
        {
            Debug.LogWarning("[InventoryTestButton] InventoryManager.Instance is null! Make sure InventoryManager exists in the scene.");
        }
    }
}

