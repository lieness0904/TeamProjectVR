using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioMixer mainMixer;

    [Header("BGM")]
    public AudioClip houseBGM;
    public AudioClip titleBGM;
    public AudioClip fishingBGM;
    public AudioClip mountainBGM;
    public AudioClip farmBGM;

    [Header("SFX")]
    public AudioClip clickSFX;
    public AudioClip eatSFX;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        LoadSettings();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBGM(scene.name);
    }

    public void PlayBGM(string sceneName)
    {
        switch (sceneName)
        {
            case "TitleScene":
                bgmSource.clip = titleBGM;
                bgmSource.volume = 0.5f;
                break;
            case "HouseScene":
                bgmSource.clip = houseBGM;
                bgmSource.volume = 0.5f;
                break;
            case "FishingScene":
                bgmSource.clip = fishingBGM; 
                break;  
            case "MountainScene":
                bgmSource.clip = mountainBGM;
                break;
            case "FarmScene":
                bgmSource.clip = farmBGM;
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
    public void PlayPickupSound() => PlaySFX(eatSFX);

    // 볼륨 제어 - dB 변환 포함
    public void SetBGMVolume(float volume) => mainMixer.SetFloat("BGMVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
    public void SetSFXVolume(float volume) => mainMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);
    public void SetVoiceVolume(float volume) => mainMixer.SetFloat("VoiceVolume", Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20);

    // 세팅 저장
    public void SaveSettings(float bgm, float sfx, float voice)
    {
        PlayerPrefs.SetFloat("BGMVolume", bgm);
        PlayerPrefs.SetFloat("SFXVolume", sfx);
        PlayerPrefs.SetFloat("VoiceVolume", voice);
        PlayerPrefs.Save();

        SetBGMVolume(bgm);
        SetSFXVolume(sfx);
        SetVoiceVolume(voice);
    }

    public void LoadSettings()
    {
        SetBGMVolume(PlayerPrefs.GetFloat("BGMVolume", 0.5f));
        SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 0.5f));
        SetVoiceVolume(PlayerPrefs.GetFloat("VoiceVolume", 0.5f));
    }
}
