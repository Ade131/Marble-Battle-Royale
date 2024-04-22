using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    private static AudioManager _singleton;

    public static AudioManager Singleton
    {
        get => _singleton;
        set
        {
            if (value == null)
            {
                _singleton = null;
            }
            else if (_singleton == null)
            {
                _singleton = value;
            }
            else if (_singleton != value)
            {
                Destroy(value);
                Debug.LogError($"There should only ever be one instance of {nameof(AudioManager)}!");
            }
        }
    }

    private const float Volume = 0.02f;
    private AudioSource _audioSource;
    [SerializeField] private AudioClip winnerSound;
    [SerializeField] private AudioClip startSound;
    [SerializeField] private AudioClip gameMusic;

    private void Awake()
    {
        Singleton = this;
        _audioSource = GetComponent<AudioSource>();
        PlayMusic(gameMusic, Volume);
    }

    private void OnDestroy()
    {
        if (Singleton == this)
            Singleton = null;
    }

    public void PlayMusic(AudioClip music, float volume)
    {
        _audioSource.clip = music;
        _audioSource.loop = true;
        _audioSource.volume = volume;
        _audioSource.Play();
    }

    public void PlayWinnerSound()
    {
        if (_audioSource && winnerSound)
        {
            _audioSource.PlayOneShot(winnerSound, 7);
        }
    }

    public void PlayStartSound()
    {
        if (_audioSource && startSound)
        {
            _audioSource.PlayOneShot(startSound, 1);
        }
    }
}
