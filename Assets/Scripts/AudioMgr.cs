using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Windows;

[System.Serializable]
public class SoundEffectClipPair
{
    public SoundEffectType type;
    public AudioClip clip;
}

public enum SoundEffectType
{
    // 示例音效，可按需添加
    Jump,
    Attack,
    Collect
}


public class AudioMgr : MonoBehaviour
{
    private static AudioMgr instance;
    public static AudioMgr Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AudioMgr>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("AudioMgr");
                    instance = obj.AddComponent<AudioMgr>();
                }
            }
            return instance;
        }
    }

    [SerializeField] private AudioSource backgroundMusicSource;
    [SerializeField] private AudioSource soundEffectSource;
    
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private List<SoundEffectClipPair> soundEffectClips = new List<SoundEffectClipPair>();
    Dictionary<SoundEffectType, AudioClip> clipDictionary = new Dictionary<SoundEffectType, AudioClip>();
    private void Start()
    {
        foreach (var t in soundEffectClips)
        {
            clipDictionary.Add(t.type, t.clip);
        }
    }

    public void PlayBackgroundMusic()
    {
        if (backgroundMusic != null && !backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.clip = backgroundMusic;
            backgroundMusicSource.Play();
        }
    }

    public void StopBackgroundMusic()
    {
        backgroundMusicSource.Stop();
    }

    public void PlaySoundEffect(SoundEffectType type)
    {
        if (clipDictionary.ContainsKey(type) && clipDictionary[type] != null)
        {
            soundEffectSource.PlayOneShot(clipDictionary[type]);
        }
    }
}