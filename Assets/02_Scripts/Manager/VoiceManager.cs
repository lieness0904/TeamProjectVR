using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Voice.Unity;
using Photon.Realtime;
public class VoiceManager : MonoBehaviour
{
    public VoiceConnection VoiceConnection { get; private set; }

    private void Awake()
    {
        VoiceConnection = GetComponent<VoiceConnection>();
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
}
