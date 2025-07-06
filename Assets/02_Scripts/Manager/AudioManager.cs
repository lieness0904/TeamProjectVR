using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("BGM")]
    public AudioClip lobbyBGM;
    public AudioClip mainBGM;

    [Header("SFX")]
    public AudioClip clickSFX;
    public AudioClip pickupSFX;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void PlayBGM(string sceneName)
    {
        switch (sceneName)
        {
            case "Lobby":
                bgmSource.clip = lobbyBGM;
                break;
            case "MainGameScene":
                bgmSource.clip = mainBGM;
                break;
            default:
                bgmSource.clip = null;
                break;
        }

        if (bgmSource.clip != null)
            bgmSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlayClickSound() => PlaySFX(clickSFX);
    public void PlayPickupSound() => PlaySFX(pickupSFX);
}
