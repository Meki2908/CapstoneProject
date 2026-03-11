using System;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Sounds Database", fileName = "SoundsSO")]
public class SoundsSO : ScriptableObject
{
    [Header("Enum-based sounds")]
    public SoundList[] sounds;

    [Header("Scene-specific footsteps")]
    public SceneFootstepEntry[] sceneFootsteps;

    public SoundList GetSound(SoundType soundType)
    {
        int index = (int)soundType;
        if (sounds == null || index < 0 || index >= sounds.Length)
        {
            return default;
        }

        return sounds[index];
    }

    public bool TryGetFootstepForScene(string sceneName, out SoundList soundList)
    {
        soundList = default;
        if (string.IsNullOrWhiteSpace(sceneName) || sceneFootsteps == null)
        {
            return false;
        }

        for (int i = 0; i < sceneFootsteps.Length; i++)
        {
            SceneFootstepEntry entry = sceneFootsteps[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.sceneName))
            {
                continue;
            }

            if (string.Equals(entry.sceneName.Trim(), sceneName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                soundList = entry.soundList;
                return true;
            }
        }

        return false;
    }
}

[Serializable]
public struct SoundList
{
    [HideInInspector] public string name;
    [Range(0f, 1f)] public float volume;
    public AudioMixerGroup mixer;
    public AudioClip[] sounds;

    public bool IsValid()
    {
        return sounds != null && sounds.Length > 0;
    }

    public AudioClip GetRandomClip()
    {
        if (!IsValid())
        {
            return null;
        }

        return sounds[UnityEngine.Random.Range(0, sounds.Length)];
    }
}

[Serializable]
public class SceneFootstepEntry
{
    public string sceneName;
    public SoundList soundList;
}
