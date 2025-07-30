using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Voice.Unity;

public class VoiceTest : MonoBehaviour
{
    public Recorder recorder;
    public string testClipName = "TestVoice";
    public KeyCode micToggleKey = KeyCode.M;
    public KeyCode testToggleKey = KeyCode.T;
    public bool useDebugEcho = true;

    private bool isMicMode = false;
    private bool isTestMode = false;

    private void Start()
    {
        if (useDebugEcho)
        {
            recorder.DebugEchoMode = true;
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(testToggleKey))
        {
            ToggleTestAudioClip();
        }

        if (Input.GetKeyDown(micToggleKey))
        {
            ToggleMicrophone();
        }
    }

    private void ToggleTestAudioClip()
    {
        if (isTestMode)
        {
            recorder.TransmitEnabled = false;
            recorder.StopRecordingWhenPaused = true; // 녹음 중지
            Debug.Log("[Voice] 테스트 음성 중단");
            isTestMode = false;
        }
        else
        {
            AudioClip clip = Resources.Load<AudioClip>(testClipName);
            if (clip == null)
            {
                Debug.LogError($"[Voice] AudioClip '{testClipName}' not found!");
                return;
            }

            recorder.SourceType = Recorder.InputSourceType.AudioClip;
            recorder.AudioClip = clip;
            recorder.LoopAudioClip = true;
            recorder.RestartRecording(); // 설정 반영
            recorder.TransmitEnabled = true;

            Debug.Log("[Voice] 테스트 음성 송신 시작");
            isTestMode = true;
            isMicMode = false;
        }
    }

    private void ToggleMicrophone()
    {
        if (isMicMode)
        {
            recorder.TransmitEnabled = false;
            recorder.StopRecordingWhenPaused = true; // 녹음 중지
            Debug.Log("[Voice] 마이크 송신 중단");
            isMicMode = false;
        }
        else
        {
            recorder.SourceType = Recorder.InputSourceType.Microphone;
            recorder.MicrophoneType = Recorder.MicType.Unity; // 또는 .Photon 가능
            recorder.RestartRecording(); // 설정 반영
            recorder.TransmitEnabled = true;

            Debug.Log("[Voice] 마이크 송신 시작");
            isMicMode = true;
            isTestMode = false;
        }
    }
}
