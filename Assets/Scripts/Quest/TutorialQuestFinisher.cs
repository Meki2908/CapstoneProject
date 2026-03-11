using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Đặt script này vào một GameObject ở CUỐI Tutorial
/// (VD: vùng exit, trigger zone cuối phòng, hoặc nút hoàn thành).
/// 
/// Luồng:
/// 1. Player hoàn thành Tutorial (vào trigger hoặc click nút)
/// 2. CompleteQuest(questID=1) → nhận thưởng
/// 3. AcceptQuest(questID=2) → quest mới "Đến cổng Dungeon"
/// 4. Load lại scene Map_Chinh (hoặc scene chứa cổng dungeon)
/// </summary>
public class TutorialQuestFinisher : MonoBehaviour
{
    [Header("Quest cần hoàn thành (Tutorial Quest)")]
    public int tutorialQuestID = 1;

    [Header("Quest tiếp theo sẽ tự động nhận")]
    public int nextQuestID = 2;

    [Header("Scene trở về sau tutorial")]
    [Tooltip("Scene chứa cổng Dungeon (VD: 'Map_Chinh' hoặc 'Setup Scene')")]
    public string returnScene = "Map_Chinh";

    [Header("Tag của Player")]
    public string playerTag = "Player";

    [Header("Chỉ kích hoạt 1 lần")]
    bool _triggered = false;

    [Header("Delay trước khi chuyển scene (giây)")]
    public float delay = 2f;

    // ─── Trigger Zone (đặt ở cuối Tutorial) ─────────────────────────────

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag(playerTag)) return;
        _triggered = true;
        FinishTutorial();
    }

    // ─── Hoặc gọi từ button / event ──────────────────────────────────────

    public void FinishTutorial()
    {
        Debug.Log("[TutorialFinisher] FinishTutorial() called – returning to map.");
        // Quest đã được xử lý trong LeonaDialogue khi accept
        // Chỉ cần save và load scene về Map
        StartCoroutine(ReturnToMap());
    }

    System.Collections.IEnumerator ReturnToMap()
    {
        try { GameController.PlayerSave(); }
        catch (System.Exception e) { Debug.LogWarning($"[TutorialFinisher] PlayerSave failed: {e.Message}"); }

        yield return new WaitForSeconds(delay);
        Debug.Log($"[TutorialFinisher] Loading scene: {returnScene}");
        SceneManager.LoadScene(returnScene);
    }
}
