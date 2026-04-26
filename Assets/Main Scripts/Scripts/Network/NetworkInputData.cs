using UnityEngine;
using Fusion;

public enum NetworkInputButtons
{
    Jump = 0,
    Dash = 1,
    Crouch = 2,
    Sprint = 3,
    Attack = 4,
    ToggleWeapon = 5
}

public struct NetworkInputData : INetworkInput
{
    public Vector2 movementInput;
    public NetworkButtons buttons;
}
