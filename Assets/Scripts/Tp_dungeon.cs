using UnityEngine;

public class TeleportButton : MonoBehaviour
{
    public Transform player;
    public Transform targetPoint; // Empty trong world
    public CharacterController cc; // optional
    public Vector3 offset = Vector3.up * 0.1f;

    public void Teleport()
    {
        if (!player || !targetPoint) return;

        if (cc) cc.enabled = false;
        player.position = targetPoint.position + offset;
        if (cc) cc.enabled = true;
    }
}
