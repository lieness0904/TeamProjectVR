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
        Debug.Log($"[VoiceManager] 보이스 서버 연결 시도 - Room: {roomName}");
        currentRoomName = roomName;

        VoiceConnection.Client.StateChanged -= OnVoiceClientStateChanged;
        VoiceConnection.Client.StateChanged += OnVoiceClientStateChanged;

        if (!VoiceConnection.Client.IsConnected)
        {
            VoiceConnection.Settings.FixedRegion = "hk"; 
            VoiceConnection.ConnectUsingSettings();
        }
        else
        {
            JoinVoiceRoom(currentRoomName);
        }
    }

    private void OnVoiceClientStateChanged(ClientState oldState, ClientState newState)
    {
        if (newState == ClientState.ConnectedToMasterServer)
        {
            Debug.Log("[VoiceManager] 마스터 서버 연결됨 → 보이스 룸 입장 시도");
            JoinVoiceRoom(currentRoomName);
        }
        else if (newState == ClientState.Joined)
        {
            Debug.Log("[VoiceManager] 보이스 룸 입장 완료!");
        }
    }

    private void JoinVoiceRoom(string roomName)
    {
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogError("[VoiceManager] 룸 이름이 비어있음! 보이스 룸 입장 불가");
            return;
        }

        if (!VoiceConnection.Client.InRoom)
        {
            Debug.Log($"[VoiceManager] 보이스 룸 입장 시도: {roomName}");
            VoiceConnection.Client.OpJoinOrCreateRoom(new EnterRoomParams { RoomName = roomName });
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