using UnityEngine;
using Fusion;

public enum NetworkInputButtons
{
    Jump = 0,
    Dash = 1,
    Crouch = 2,
    Sprint = 3,
    Attack = 4,
    ToggleWeapon = 5,
    Skill_E = 6,
    Skill_R = 7,
    Skill_T = 8,
    Skill_Q = 9
}

public struct NetworkInputData : INetworkInput
{
    public Vector2 movementInput;
    public NetworkButtons buttons;
}
