using UnityEngine;
using System.Collections.Generic;

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
    [SerializeField] private Dictionary<SoundEffectType, AudioClip> soundEffectClips = new Dictionary<SoundEffectType, AudioClip>();

    private void Start()
    {
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
        if (soundEffectClips.ContainsKey(type) && soundEffectClips[type] != null)
        {
            soundEffectSource.PlayOneShot(soundEffectClips[type]);
        }
    }
}