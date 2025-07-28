using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingPanelCtrl : MonoBehaviour
{
    public Slider bgmSlider;
    public Slider sfxSlider;
    public Slider voiceSlider;

    private void Start()
    {
        // 슬라이더 초기값 세팅 (PlayerPrefs 기반)
        bgmSlider.value = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 0.5f);
        voiceSlider.value = PlayerPrefs.GetFloat("VoiceVolume", 0.5f);

        // 리스너 연결
        bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        voiceSlider.onValueChanged.AddListener(OnVoiceChanged);
    }

    private void OnBGMChanged(float value)
    {
        GameManager.Instance.AudioManager.SetBGMVolume(value);
        PlayerPrefs.SetFloat("BGMVolume", value);
    }

    private void OnSFXChanged(float value)
    {
        GameManager.Instance.AudioManager.SetSFXVolume(value);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    private void OnVoiceChanged(float value)
    {
        GameManager.Instance.AudioManager.SetVoiceVolume(value);
        PlayerPrefs.SetFloat("VoiceVolume", value);
    }

    private void OnDestroy()
    {
        PlayerPrefs.Save();
    }
}
