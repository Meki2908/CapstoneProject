using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(WeaponMasteryManager))]
public class WeaponMasteryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WeaponMasteryManager manager = (WeaponMasteryManager)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mastery Level Editor (For Demo)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use these controls to adjust mastery levels for demo purposes.", MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sword Mastery", EditorStyles.boldLabel);
        int swordLevel = manager.GetMasteryLevel(WeaponType.Sword);
        EditorGUILayout.LabelField("Current Level", swordLevel.ToString());
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Level 1"))
        {
            manager.SetMasteryLevel(WeaponType.Sword, 1);
        }
        if (GUILayout.Button("Set Level 30"))
        {
            manager.SetMasteryLevel(WeaponType.Sword, 30);
        }
        if (GUILayout.Button("Set Level 60"))
        {
            manager.SetMasteryLevel(WeaponType.Sword, 60);
        }
        EditorGUILayout.EndHorizontal();
        int newSwordLevel = EditorGUILayout.IntField("Custom Level", swordLevel);
        if (newSwordLevel != swordLevel && GUILayout.Button("Apply Custom Level"))
        {
            manager.SetMasteryLevel(WeaponType.Sword, newSwordLevel);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Axe Mastery", EditorStyles.boldLabel);
        int axeLevel = manager.GetMasteryLevel(WeaponType.Axe);
        EditorGUILayout.LabelField("Current Level", axeLevel.ToString());
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Level 1"))
        {
            manager.SetMasteryLevel(WeaponType.Axe, 1);
        }
        if (GUILayout.Button("Set Level 30"))
        {
            manager.SetMasteryLevel(WeaponType.Axe, 30);
        }
        if (GUILayout.Button("Set Level 60"))
        {
            manager.SetMasteryLevel(WeaponType.Axe, 60);
        }
        EditorGUILayout.EndHorizontal();
        int newAxeLevel = EditorGUILayout.IntField("Custom Level", axeLevel);
        if (newAxeLevel != axeLevel && GUILayout.Button("Apply Custom Level"))
        {
            manager.SetMasteryLevel(WeaponType.Axe, newAxeLevel);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mage Mastery", EditorStyles.boldLabel);
        int mageLevel = manager.GetMasteryLevel(WeaponType.Mage);
        EditorGUILayout.LabelField("Current Level", mageLevel.ToString());
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Level 1"))
        {
            manager.SetMasteryLevel(WeaponType.Mage, 1);
        }
        if (GUILayout.Button("Set Level 30"))
        {
            manager.SetMasteryLevel(WeaponType.Mage, 30);
        }
        if (GUILayout.Button("Set Level 60"))
        {
            manager.SetMasteryLevel(WeaponType.Mage, 60);
        }
        EditorGUILayout.EndHorizontal();
        int newMageLevel = EditorGUILayout.IntField("Custom Level", mageLevel);
        if (newMageLevel != mageLevel && GUILayout.Button("Apply Custom Level"))
        {
            manager.SetMasteryLevel(WeaponType.Mage, newMageLevel);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Reset All Mastery Data", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Reset All Mastery Data",
                "Are you sure you want to reset all mastery data? This cannot be undone.",
                "Yes", "No"))
            {
                manager.ResetAllMasteryData();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Add EXP (For Testing)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add 100 EXP to Sword"))
        {
            manager.AddExp(WeaponType.Sword, 100f);
        }
        if (GUILayout.Button("Add 100 EXP to Axe"))
        {
            manager.AddExp(WeaponType.Axe, 100f);
        }
        if (GUILayout.Button("Add 100 EXP to Mage"))
        {
            manager.AddExp(WeaponType.Mage, 100f);
        }
        EditorGUILayout.EndHorizontal();
    }
}

