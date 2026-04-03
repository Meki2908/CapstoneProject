using UnityEngine;

/// <summary>
/// Place on an active boss root so inactive child <see cref="TimelineControlVfxPool"/> objects
/// get prewarmed when the boss spawns (avoids first-play Instantiate spike when the Timeline enables them).
/// </summary>
[DisallowMultipleComponent]
public class BossTimelineVfxPrewarmer : MonoBehaviour
{
    void Awake()
    {
        foreach (var pool in GetComponentsInChildren<TimelineControlVfxPool>(true))
            pool.Prewarm();
    }
}
