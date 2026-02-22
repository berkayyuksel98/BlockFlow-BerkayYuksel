using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AudioData
{
    public AudioType audioType;
    [SerializeField] List<AudioClip> audioClips;
    [SerializeField] float volume = 1f;
    public AudioClip GetRandomClip()
    {
        if (audioClips == null || audioClips.Count == 0)
            return null;

        int index = Random.Range(0, audioClips.Count);
        return audioClips[index];
    }
    public float Volume()=>volume;
}
