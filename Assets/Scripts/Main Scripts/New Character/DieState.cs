using UnityEngine;

public class DieState : State
{
    private float dieDuration = 3f; // Duration of death state (can be adjusted)
    private float dieTimer;
    private WeaponController weaponController;
    private bool weaponLayersWereDisabled = false;

    public DieState(Character _character, StateMachine _stateMachine) : base(_character, _stateMachine)
    {
        character = _character;
        stateMachine = _stateMachine;
    }

    public override void Enter()
    {
        base.Enter();

        dieTimer = dieDuration;
        weaponLayersWereDisabled = false;

        // Find WeaponController if not already found
        if (weaponController == null)
        {
            weaponController = character.GetComponent<WeaponController>();
        }

        // Disable all inputs - player is completely immobilized
        if (character.playerInput != null)
        {
            character.playerInput.enabled = false;
        }

        // Stop all movement
        velocity = Vector3.zero;
        input = Vector2.zero;

        // Trigger die animation based on currentLocomotionState
        if (character.animator != null)
        {
            // Use currentLocomotionState to determine which layer should play die animation
            // If StandingState (weapon not drawn) -> play on base layer only
            // If CombatMoveState (weapon drawn) -> play on weapon layer
            bool isWeaponDrawn = character.currentLocomotionState is CombatMoveState;

            if (isWeaponDrawn)
            {
                // Weapon is drawn - ensure weapon layer can play animation
                // The animation should play on the active weapon layer
                character.animator.SetTrigger("die");
            }
            else
            {
                // Weapon is not drawn - disable weapon layers temporarily to force base layer
                DisableWeaponLayersForBaseDie();

                // Trigger die - will play on base layer since weapon layers are disabled
                character.animator.SetTrigger("die");
            }
        }

        Debug.Log("[DieState] Player entered death state - all controls disabled");
    }

    private void DisableWeaponLayersForBaseDie()
    {
        if (weaponController == null || character.animator == null) return;

        // Store original layer weights and disable weapon layers
        // This ensures die plays on base layer only
        weaponLayersWereDisabled = true;

        int baseLayer = 0; // Base Layer is always 0
        int swordLayer = 1;
        int axeLayer = 2;
        int mageLayer = 3;
        int gunLayer = 4;

        // Disable all weapon layers
        SetLayerWeightSafe(swordLayer, 0f);
        SetLayerWeightSafe(axeLayer, 0f);
        SetLayerWeightSafe(mageLayer, 0f);
        SetLayerWeightSafe(gunLayer, 0f);

        // Ensure base layer is active
        SetLayerWeightSafe(baseLayer, 1f);
    }

    private void SetLayerWeightSafe(int layer, float weight)
    {
        if (character.animator != null && layer >= 0 && layer < character.animator.layerCount)
        {
            character.animator.SetLayerWeight(layer, weight);
        }
    }

    public override void HandleInput()
    {
        // No input handling - player is dead
        // All inputs are disabled in Enter()
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // Update timer (though player can't do anything)
        dieTimer -= Time.deltaTime;

        // Optionally, you can add respawn logic here after dieTimer <= 0
        // For now, player stays dead
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Only apply gravity - no movement allowed
        gravityVelocity.y += character.gravityValue * Time.deltaTime;

        // Keep player grounded
        if (character.controller.isGrounded && gravityVelocity.y < 0)
        {
            gravityVelocity.y = 0f;
        }

        // Only move downward due to gravity (if not grounded)
        character.controller.Move(gravityVelocity * Time.deltaTime);
    }

    public override void Exit()
    {
        base.Exit();

        // Re-enable inputs if player respawns (future feature)
        if (character.playerInput != null)
        {
            character.playerInput.enabled = true;
        }

        Debug.Log("[DieState] Player exited death state");
    }
}

