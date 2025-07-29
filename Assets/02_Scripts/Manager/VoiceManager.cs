using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Voice.Unity;
using Photon.Realtime;

public class VoiceManager : MonoBehaviour
{
    public VoiceConnection VoiceConnection { get; private set; }
    public Recorder Recorder { get; private set; }
    private string currentRoomName;

    private void Awake()
    {
        VoiceConnection = GetComponent<VoiceConnection>();
        Recorder = GetComponent<Recorder>();

        // Speaker 자동 연결
        VoiceConnection.SpeakerLinked += OnSpeakerLinked;
    }


    public void ConnectToVoiceRoom(string roomName)
    {
        currentRoomName = roomName;

        // 이미 보이스 서버에 연결돼있으면 바로 방 입장
        if (VoiceConnection.Client.IsConnected)
        {
            JoinVoiceRoom(roomName);
            return;
        }

        // Photon Voice 서버 연결 시도
        VoiceConnection.Client.StateChanged += OnVoiceClientStateChanged;
        VoiceConnection.ConnectUsingSettings();
    }

    private void OnVoiceClientStateChanged(ClientState oldState, ClientState newState)
    {
        if (newState == ClientState.ConnectedToMasterServer)
        {
            JoinVoiceRoom(currentRoomName);
        }
    }

 
    private void JoinVoiceRoom(string roomName)
    {
        if (!VoiceConnection.Client.InRoom)
        {
            VoiceConnection.Client.OpJoinOrCreateRoom(new EnterRoomParams
            {
                RoomName = roomName
            });

            Debug.Log($"[VoiceManager] 보이스 룸 입장: {roomName}");
        }
    }

    private void OnSpeakerLinked(Speaker speaker)
    {
        Debug.Log($"[VoiceManager] Speaker 연결됨: {speaker.name}");

        var audioSource = speaker.gameObject.AddComponent<AudioSource>();
        if (speaker == null)
        {
            audioSource = speaker.gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 33f;
        }
    
    }
    public void SetVoice(bool enabled)
    {
        if (Recorder != null)
        {
            Recorder.TransmitEnabled = enabled;
            Recorder.RecordingEnabled = enabled;
        }
    }
}