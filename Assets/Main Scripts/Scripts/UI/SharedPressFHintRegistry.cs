using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks shared "Press F" canvases that are referenced by multiple interactables.
/// Prevents one trigger from hiding the hint while another valid trigger is still active.
/// </summary>
public static class SharedPressFHintRegistry
{
    static readonly Dictionary<int, HashSet<int>> HoldersByCanvasId = new();

    public static void SetVisible(GameObject canvas, int holderId, bool visible)
    {
        if (canvas == null)
            return;

        int canvasId = canvas.GetInstanceID();
        if (!HoldersByCanvasId.TryGetValue(canvasId, out var holders))
        {
            holders = new HashSet<int>();
            HoldersByCanvasId[canvasId] = holders;
        }

        if (visible)
            holders.Add(holderId);
        else
            holders.Remove(holderId);

        if (holders.Count == 0)
        {
            HoldersByCanvasId.Remove(canvasId);
            if (canvas.activeSelf)
                canvas.SetActive(false);
            return;
        }

        if (!canvas.activeSelf)
            canvas.SetActive(true);
    }
}
