using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;


public enum AudioType
{
    EarnMoney
}
// EventBus üzerinden gelen eventlere göre ilgili ses efektlerini oynatan servis sinifi 
//TODO: MonoBehaviour olmayan bir servis olarak düzenlenecek
public class AudioController : MonoBehaviour
{
    [SerializeField] private List<AudioData> audioDatas = new List<AudioData>();
    private AudioSource audioSource;


    [Inject]
    public void Construct(EventBus eventBus)
    {
        
    }

    private void Awake()
    {
        audioSource = Camera.main.GetComponent<AudioSource>();
    }
   
    private void PlayAudio(AudioType type)
    {
        var audioData = audioDatas.Find(a => a.audioType == type);
        if (audioData != null && audioSource != null)
        {
            audioSource.PlayOneShot(audioData.GetRandomClip(),audioData.Volume());
        }
    }
}
