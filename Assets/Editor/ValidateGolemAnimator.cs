using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

/// <summary>
/// Editor utility to validate that the Golem Animator Controller contains parameters the script expects.
/// Menu: Tools/Validate/Golem Animator Parameters
/// - Scans the Golem Animator at known path and reports missing parameters.
/// - Optionally adds missing parameters (Walk as Float, others as Trigger).
/// </summary>
public static class ValidateGolemAnimator
{
	private const string controllerPath = "Assets/ASSETS/Dungeon_SaMac/Asset_Enemy_SaMac/Boss_Golem/Animators/GolemAnimator.controller";

	[MenuItem("Tools/Validate/Golem Animator Parameters")]
	public static void Validate()
	{
		var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
		if (controller == null)
		{
			Debug.LogError($"Golem Animator Controller not found at: {controllerPath}");
			return;
		}

		// Parameters referenced by GolemAI (including hard-coded literals used in script)
		string[] expected = new[]
		{
			"Walk","IdleAction","Hit","Hit2","Damage","Die","SleepStart","SleepEnd","Jump","Land","Rage",
			// Additional triggers referenced in code
			"HeavyAttack","SpinAttack","GroundSlam","RageSmash","WakeUp"
		};

		var existing = controller.parameters.Select(p => p.name).ToHashSet();
		var missing = expected.Where(e => !existing.Contains(e)).ToArray();

		if (missing.Length == 0)
		{
			Debug.Log("[ValidateGolemAnimator] All expected parameters exist in GolemAnimator.controller");
			return;
		}

		string msg = $"Missing {missing.Length} parameter(s) in GolemAnimator:\n- {string.Join("\n- ", missing)}\n\nAdd missing parameters?";
		bool add = EditorUtility.DisplayDialog("Golem Animator Validation", msg, "Add Missing", "Cancel");
		if (!add) return;

		// Add missing parameters: Walk -> Float, others -> Trigger
		foreach (var name in missing)
		{
			AnimatorControllerParameter param = new AnimatorControllerParameter();
			param.name = name;
			if (name == "Walk")
			{
				param.type = AnimatorControllerParameterType.Float;
				param.defaultFloat = 0f;
			}
			else
			{
				param.type = AnimatorControllerParameterType.Trigger;
			}
			controller.AddParameter(param);
			Debug.Log($"[ValidateGolemAnimator] Added parameter: {name} ({param.type})");
		}

		EditorUtility.SetDirty(controller);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		Debug.Log("[ValidateGolemAnimator] Missing parameters added. Re-open Animator to see changes.");
	}
}




