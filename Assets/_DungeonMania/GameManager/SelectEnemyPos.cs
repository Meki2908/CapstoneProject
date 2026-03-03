using UnityEngine;

public class SelectEnemyPos : MonoBehaviour
{
    // Có thể giữ nguyên kích thước 10 như code gốc
    public static Transform[] enemyTr = new Transform[10];

    public static Vector3 SelectNewPos(int number)
    {
        // Nếu chưa được set, trả về zero để không crash
        if (enemyTr == null || enemyTr.Length == 0)
        {
            Debug.LogWarning("[SelectEnemyPos] enemyTr is empty, returning Vector3.zero");
            return Vector3.zero;
        }

        // Bọc index để luôn nằm trong [0, Length-1]
        int index = 0;
        if (enemyTr.Length > 0)
            index = Mathf.Abs(number) % enemyTr.Length;

        if (enemyTr[index] == null)
        {
            Debug.LogWarning($"[SelectEnemyPos] enemyTr[{index}] is null, returning Vector3.zero");
            return Vector3.zero;
        }

        return enemyTr[index].position;
    }
}
