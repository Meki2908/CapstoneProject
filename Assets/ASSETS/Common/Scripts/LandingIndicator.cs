using System.Collections;
using UnityEngine;

/// <summary>
/// Simple landing indicator behaviour.
/// - Scales up from zero, fades out, then destroys itself.
/// - Works with either a SpriteRenderer (2D sprite) or a Mesh/Renderer (primitive quad/cylinder).
/// </summary>
public class LandingIndicator : MonoBehaviour
{
	[Header("Timing")]
	[SerializeField] private float expandDuration = 0.12f;
	[SerializeField] private float visibleLifetime = 0.48f;

	[Header("Appearance")]
	[SerializeField] private float maxScale = 1.2f;
	[SerializeField] private Color indicatorColor = Color.red;

	private SpriteRenderer spriteRenderer;
	private Renderer meshRenderer;
	private Material runtimeMaterial;

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		if (spriteRenderer == null)
		{
			meshRenderer = GetComponent<Renderer>();
			if (meshRenderer == null)
			{
				Debug.LogWarning("[LandingIndicator] No SpriteRenderer or Renderer found on prefab. Add a SpriteRenderer or a mesh renderer.");
			}
			else
			{
				// Create a runtime material instance so fading doesn't modify shared assets
				if (meshRenderer.sharedMaterial != null)
				{
					// Clone existing material
					runtimeMaterial = new Material(meshRenderer.sharedMaterial);
				}
				else
				{
					// Fallback to Standard shader (or Sprites/Default if Standard is not found)
					Shader shader = Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
					runtimeMaterial = shader != null ? new Material(shader) : new Material(Shader.Find("Sprites/Default"));
				}

				runtimeMaterial.color = indicatorColor;
				meshRenderer.material = runtimeMaterial;
			}
		}
		else
		{
			spriteRenderer.color = indicatorColor;
		}

		// Initialize appearance
		transform.localScale = Vector3.zero;
	}

	private void Start()
	{
		StartCoroutine(AnimateAndDestroy());
	}

	private IEnumerator AnimateAndDestroy()
	{
		// Expand
		float t = 0f;
		while (t < expandDuration)
		{
			t += Time.deltaTime;
			float scale = Mathf.Lerp(0f, maxScale, t / Mathf.Max(0.0001f, expandDuration));
			transform.localScale = new Vector3(scale, scale, scale);
			yield return null;
		}

		// Remain visible while fading out
		float elapsed = 0f;
		Color startColor = spriteRenderer != null ? spriteRenderer.color : (runtimeMaterial != null ? runtimeMaterial.color : Color.white);
		while (elapsed < visibleLifetime)
		{
			elapsed += Time.deltaTime;
			float alpha = Mathf.Lerp(1f, 0f, elapsed / Mathf.Max(0.0001f, visibleLifetime));
			if (spriteRenderer != null)
			{
				Color c = startColor;
				c.a = alpha;
				spriteRenderer.color = c;
			}
			else if (runtimeMaterial != null)
			{
				Color c = startColor;
				c.a = alpha;
				runtimeMaterial.color = c;
			}
			yield return null;
		}

		Destroy(gameObject);
	}

	private void OnDestroy()
	{
		if (runtimeMaterial != null)
		{
			Destroy(runtimeMaterial);
		}
	}
}


