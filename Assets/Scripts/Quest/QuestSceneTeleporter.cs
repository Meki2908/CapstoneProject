using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Gắn script này lên NPC Leona.
/// Sau khi AcceptQuest(1) thành công → load scene Tutorial với hiệu ứng fade.
/// </summary>
public class QuestSceneTeleporter : MonoBehaviour
{
    [Header("Scene để load sau khi accept quest")]
    [Tooltip("Đặt đúng tên scene trong Build Settings (VD: 'Tutorial')")]
    public string targetScene = "Tutorial";

    [Header("Thời gian delay trước khi chuyển cảnh (giây)")]
    public float delayBeforeLoad = 1.5f;

    [Header("Fade Effect (tuỳ chọn)")]
    public bool useFade = true;

    // Gọi từ LeonaDialogue.cs sau khi AcceptQuest thành công
    public void TeleportToScene()
    {
        StartCoroutine(DoTeleport());
    }

    IEnumerator DoTeleport()
    {
        // Lưu game trước khi chuyển cảnh (an toàn)
        try { GameController.PlayerSave(); }
        catch (System.Exception e) { Debug.LogWarning($"[QuestSceneTeleporter] PlayerSave failed: {e.Message}"); }

        yield return new WaitForSeconds(delayBeforeLoad);

        Debug.Log($"[QuestSceneTeleporter] Loading scene: {targetScene}");
        SceneManager.LoadScene(targetScene);
    }
}
