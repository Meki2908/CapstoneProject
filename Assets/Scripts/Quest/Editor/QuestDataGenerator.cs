using UnityEngine;
using UnityEditor;
using System.IO;

public class QuestDataGenerator : EditorWindow
{
    [MenuItem("Quest/Generate Default Quest Assets")]
    public static void GenerateQuests()
    {
        string folderPath = "Assets/Quests";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Quests");
        }

        // --- QUEST 1 ---
        QuestData q1 = ScriptableObject.CreateInstance<QuestData>();
        q1.questID = 1;
        q1.questTitle = "Tutorial Quest";
        q1.questDescription = "Find and talk to Leona to receive the mission.";
        
        q1.steps = new QuestStep[1];
        q1.steps[0] = new QuestStep();
        q1.steps[0].stepTitle = "Talk to Leona";
        q1.steps[0].instruction = "Leona is standing near the large red tree in the city, please find her.";
        q1.steps[0].targetTag = "NPC_Leona";

        AssetDatabase.CreateAsset(q1, $"{folderPath}/Quest_01_Tutorial.asset");

        // --- QUEST 2 ---
        QuestData q2 = ScriptableObject.CreateInstance<QuestData>();
        q2.questID = 2;
        q2.questTitle = "Dungeon Desert Quest";
        q2.questDescription = "Use the teleport point to teleport to the dungeon gate.";
        
        q2.steps = new QuestStep[1];
        q2.steps[0] = new QuestStep();
        q2.steps[0].stepTitle = "Find the Gate";
        q2.steps[0].instruction = "Approach the glowing teleport points on the map to find the Dungeon Gate.";
        q2.steps[0].targetTag = "DungeonGate";

        AssetDatabase.CreateAsset(q2, $"{folderPath}/Quest_02_Dungeon.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Quest System", "Successfully generated Quest 1 and Quest 2 assets in Assets/Quests.\n\nNow drag them into your QuestManager component!", "OK");
    }
}
