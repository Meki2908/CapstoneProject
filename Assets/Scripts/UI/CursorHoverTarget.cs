using UnityEngine;

/// <summary>
/// Optional: attach to any UI element to make GameCursorManager show Button or Item cursor when hovering.
/// If not present, manager detects Button component and ItemUI automatically.
/// </summary>
public class CursorHoverTarget : MonoBehaviour
{
    public enum HoverType
    {
        None,
        Button,
        Item,
        Settings
    }

    [SerializeField] private HoverType hoverType = HoverType.Button;

    public HoverType CurrentHoverType => hoverType;
}
