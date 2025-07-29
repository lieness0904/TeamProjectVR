using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Voice.Unity;
using Photon.Realtime;

public class VoiceManager : MonoBehaviour
{
    public VoiceConnection VoiceConnection { get; private set; }
    public Recorder recorder { get; private set; }

    private void Awake()
    {
        VoiceConnection = GetComponent<VoiceConnection>();
        recorder = GetComponent<Recorder>();

        VoiceConnection.SpeakerLinked += OnSpeakerLinked;
    }
    
    public void ConnectToVoiceRoom(string roomName)
    {
        if (!VoiceConnection.Client.IsConnected)
        {
            VoiceConnection.ConnectUsingSettings();
        }

        if (!VoiceConnection.Client.InRoom && VoiceConnection.Client.IsConnected)
        {
            VoiceConnection.Client.OpJoinOrCreateRoom(new EnterRoomParams
            {
                RoomName = roomName
            });
        }
    }
    private void OnSpeakerLinked(Speaker speaker)
    {
        Debug.Log($"[VoiceManager] Speaker 연결됨: {speaker.name}");
        // 필요시 speaker.AudioSource 조정 가능
    }
    public void SetVoice(bool enabled)
    {
        if (recorder != null)
        {
            recorder.TransmitEnabled = enabled;
            recorder.RecordingEnabled = enabled;
        }
    }

}
