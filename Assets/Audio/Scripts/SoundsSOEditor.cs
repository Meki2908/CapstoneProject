#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

[CustomEditor(typeof(SoundsSO))]
public class SoundsSOEditor : Editor
{
    private void OnEnable()
    {
        SoundsSO so = (SoundsSO)target;
        if (so == null)
        {
            return;
        }

        ref SoundList[] soundList = ref so.sounds;
        if (soundList == null)
        {
            soundList = Array.Empty<SoundList>();
        }

        string[] names = Enum.GetNames(typeof(SoundType));
        bool differentSize = names.Length != soundList.Length;
        Dictionary<string, SoundList> oldMap = new Dictionary<string, SoundList>();

        if (differentSize)
        {
            for (int i = 0; i < soundList.Length; i++)
            {
                if (!oldMap.ContainsKey(soundList[i].name))
                {
                    oldMap.Add(soundList[i].name, soundList[i]);
                }
            }
        }

        Array.Resize(ref soundList, names.Length);
        for (int i = 0; i < soundList.Length; i++)
        {
            string currentName = names[i];
            SoundList current = soundList[i];
            current.name = currentName;
            if (current.volume <= 0f)
            {
                current.volume = 1f;
            }

            if (differentSize && oldMap.TryGetValue(currentName, out SoundList old))
            {
                current.volume = old.volume <= 0f ? 1f : old.volume;
                current.mixer = old.mixer;
                current.sounds = old.sounds ?? Array.Empty<AudioClip>();
            }

            soundList[i] = current;
        }

        EditorUtility.SetDirty(so);
    }
}
#endif
