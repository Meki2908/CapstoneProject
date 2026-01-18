using UnityEngine;

/// <summary>
/// Spawns a landing indicator prefab at a world position aligned to the ground normal.
/// Attach this to a manager object in the scene (for example: "EffectManagers") and assign the prefab in the inspector.
/// </summary>
public class LandingIndicatorSpawner : MonoBehaviour
{
	[Header("Prefab")]
	[SerializeField] private GameObject landingIndicatorPrefab;

	[Header("Spawn")]
	[SerializeField] private float groundOffset = 0.02f;

	/// <summary>
	/// Spawn the assigned landing indicator prefab at worldPosition aligned to groundNormal.
	/// </summary>
	/// <param name="worldPosition">Position on or above the ground where indicator should appear.</param>
	/// <param name="groundNormal">Normal of the ground surface (use Vector3.up if unknown).</param>
	public void SpawnIndicatorAt(Vector3 worldPosition, Vector3 groundNormal)
	{
		if (landingIndicatorPrefab == null)
		{
			Debug.LogWarning("[LandingIndicatorSpawner] No landingIndicatorPrefab assigned in inspector.");
			return;
		}

		Vector3 spawnPosition = worldPosition + groundNormal * groundOffset;
		Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, groundNormal);

		Instantiate(landingIndicatorPrefab, spawnPosition, spawnRotation);
	}
}


