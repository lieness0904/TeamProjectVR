using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerIdText;
    [SerializeField] private TextMeshProUGUI pointText;
    [SerializeField] private Button callButton;
    [SerializeField] private Button teleportButton;

    public void Setup(string playerId, int points, System.Action onCall, System.Action onTeleport)
    {
        playerIdText.text = playerId;
        pointText.text = $"{points} 포인트";
        callButton.onClick.AddListener(() => onCall.Invoke());
        teleportButton.onClick.AddListener(() => onTeleport.Invoke());
    }
}
