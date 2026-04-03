using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fallback invoker to call hit methods at specific animation state timings without editing animation clips.
/// Add this to enemy root and configure state names + normalized hit times (0..1).
/// It will call EnemyContactDamage.AttemptDealContactDamage() when the configured state passes the hit time.
/// </summary>
[DisallowMultipleComponent]
public class AnimationHitInvoker : MonoBehaviour
{
    [System.Serializable]
    public struct StateHit
    {
        [Tooltip("Name of the animator state (exact name shown in Animator, e.g. attack1).")]
        public string stateName;
        [Range(0f, 1f), Tooltip("Normalized time (0..1) inside the state when the hit should occur.")]
        public float hitNormalizedTime;
    }

    [Tooltip("Configure state names and hit normalized times here.")]
    public StateHit[] stateHits = new StateHit[0];

    Animator animator;
    // track last fired cycle per state hash to avoid firing repeatedly
    Dictionary<int, int> lastFiredCycle = new Dictionary<int, int>();

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (animator == null || stateHits == null || stateHits.Length == 0) return;

        var info = animator.GetCurrentAnimatorStateInfo(0);

        // If in transition, skip (avoid firing during blending)
        if (animator.IsInTransition(0)) return;

        foreach (var sh in stateHits)
        {
            if (string.IsNullOrEmpty(sh.stateName)) continue;

            if (!info.IsName(sh.stateName)) continue;

            int stateHash = info.shortNameHash;
            int currentCycle = Mathf.FloorToInt(info.normalizedTime);
            float frac = info.normalizedTime - currentCycle;

            int lastCycle = -9999;
            lastFiredCycle.TryGetValue(stateHash, out lastCycle);

            if (frac >= sh.hitNormalizedTime && currentCycle > lastCycle)
            {
                // fire hit
                var contact = GetComponent<EnemyContactDamage>();
                if (contact != null)
                {
                    contact.AttemptDealContactDamage();
                }
                else
                {
                    // fallback: SendMessage so custom implementations still receive call
                    SendMessage("AttemptDealContactDamage", SendMessageOptions.DontRequireReceiver);
                }
                lastFiredCycle[stateHash] = currentCycle;
            }
        }
    }
}



