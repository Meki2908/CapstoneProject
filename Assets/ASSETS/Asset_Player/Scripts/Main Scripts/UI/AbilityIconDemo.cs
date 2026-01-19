using UnityEngine;
using UnityEngine.UI;

public class AbilityIconDemo : MonoBehaviour
{
    [Header("Demo Settings")]
    [SerializeField] private AbilitySO[] demoAbilities;
    [SerializeField] private Button testButton;

    private AbilityIconManager iconManager;

    private void Start()
    {
        iconManager = FindFirstObjectByType<AbilityIconManager>();

        if (testButton != null)
        {
            testButton.onClick.AddListener(TestAbilityIcons);
        }
    }

    private void TestAbilityIcons()
    {
        if (iconManager != null && demoAbilities != null)
        {
            iconManager.AE_SetAbilityIcons(demoAbilities);
            Debug.Log("[AbilityIconDemo] Set demo ability icons");
        }
        else
        {
            Debug.LogWarning("[AbilityIconDemo] IconManager or Abilities not found");
        }
    }

    // Animation Event: Test clear icons
    public void AE_ClearIcons()
    {
        if (iconManager != null)
        {
            iconManager.AE_ClearAbilityIcons();
            Debug.Log("[AbilityIconDemo] Cleared all icons");
        }
    }
}
