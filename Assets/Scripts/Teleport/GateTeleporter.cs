using UnityEngine;
using UnityEngine.InputSystem;

public class GateTeleporter : MonoBehaviour
{
    public Transform teleportTarget;     // Điểm tele đến đâu
    public GameObject pressFCanvas;      // UI hiển thị khi lại gần

    private Transform player;
    private bool playerInRange = false;

    // --- chống tele vòng lặp ---
    private bool canTeleport = true;
    private float teleportCooldown = 0.3f;

    void Start()
    {
        if (pressFCanvas != null)
            pressFCanvas.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && Keyboard.current.fKey.wasPressedThisFrame)
        {
            TeleportPlayer();
        }
    }

    void TeleportPlayer()
    {
        if (!canTeleport) return;     // CHỐNG TELE NGƯỢC
        canTeleport = false;

        if (player == null || teleportTarget == null) return;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Teleport
        player.position = teleportTarget.position;

        if (cc != null) cc.enabled = true;

        Invoke(nameof(ResetTeleport), teleportCooldown);
    }

    void ResetTeleport()
    {
        canTeleport = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            player = other.transform;

            if (pressFCanvas != null)
                pressFCanvas.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (pressFCanvas != null)
                pressFCanvas.SetActive(false);
        }
    }
}
