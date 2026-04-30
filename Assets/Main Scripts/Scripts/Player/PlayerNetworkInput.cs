using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNetworkInput : NetworkBehaviour
{
    public static PlayerNetworkInput LocalInstance;
    private PlayerInput playerInput;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
    }

    public override void Spawned()
    {
        if (HasInputAuthority) LocalInstance = this;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority && LocalInstance == this) LocalInstance = null;
    }

    public NetworkInputData GetLocalInput()
    {
        NetworkInputData data = new NetworkInputData();

        if (playerInput != null && playerInput.actions != null)
        {
            data.movementInput = playerInput.actions["Move"].ReadValue<Vector2>();

            bool isJump = playerInput.actions["Jump"].triggered || playerInput.actions["Jump"].IsPressed();
            bool isDash = playerInput.actions["Dash"].triggered;
            bool isCrouch = playerInput.actions["Crouch"].IsPressed();
            bool isSprint = playerInput.actions["Sprint"].IsPressed();
            bool isAttack = playerInput.actions["Attack"].triggered;
            bool isToggleWeapon = playerInput.actions["ToggleWeapon"].triggered;
            // D�NG TR?C TI?P KEYBOARD �? BẮT PHÍM CHO NHANH VÀ CHỐNG LỖI (Bypass Input System)
            bool isSwordSkillE = Keyboard.current != null && Keyboard.current.eKey.isPressed;
            bool isSwordSkillR = Keyboard.current != null && Keyboard.current.rKey.isPressed;
            bool isSwordSkillT = Keyboard.current != null && Keyboard.current.tKey.isPressed;
            bool isSwordSkillQ = Keyboard.current != null && Keyboard.current.qKey.isPressed;

            data.buttons.Set(NetworkInputButtons.Jump, isJump);
            data.buttons.Set(NetworkInputButtons.Dash, isDash);
            data.buttons.Set(NetworkInputButtons.Crouch, isCrouch);
            data.buttons.Set(NetworkInputButtons.Sprint, isSprint);
            data.buttons.Set(NetworkInputButtons.Attack, isAttack);
            data.buttons.Set(NetworkInputButtons.ToggleWeapon, isToggleWeapon);
            data.buttons.Set(NetworkInputButtons.Skill_E, isSwordSkillE);
            data.buttons.Set(NetworkInputButtons.Skill_R, isSwordSkillR);
            data.buttons.Set(NetworkInputButtons.Skill_T, isSwordSkillT);
            data.buttons.Set(NetworkInputButtons.Skill_Q, isSwordSkillQ);
            
            // --- LOG FLOW ---
            // if (playerInput.actions["Move"].triggered) Debug.Log($"[FLOW INPUT] Bắt đầu di chuyển: {data.movementInput}");
            // if (playerInput.actions["Jump"].triggered) Debug.Log("[FLOW INPUT] Thực hiện Nhảy (Jump)");
            // if (isDash) Debug.Log("[FLOW INPUT] Thực hiện Lướt (Dash)");
            // if (isAttack) Debug.Log("[FLOW INPUT] Thực hiện Đánh (Attack)");
        }

        return data;
    }
}




