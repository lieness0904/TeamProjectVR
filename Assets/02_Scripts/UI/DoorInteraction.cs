using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class DoorInteraction : NetworkBehaviour
{
    [Header("UI")]
    public Canvas doorCanvas;
    public Button exitButton;
    public Button enterButton;

    [Header("Spawn Points")]
    public Transform insideSpawnPoint;
    public Transform outsideSpawnPoint;

    private PlayerInteraction currentPlayer;

    private void Start()
    {
        doorCanvas.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var player = other.GetComponent<PlayerInteraction>();

        if (player != null && player.Object.HasInputAuthority)
        {
            currentPlayer = player;
            doorCanvas.gameObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var player = other.GetComponent<PlayerInteraction>();

        if (player != null && player.Object.HasInputAuthority)
        {
            currentPlayer = null;
            doorCanvas.gameObject.SetActive(false);
        }
    }


    public void Interact(PlayerInteraction player)
    {
        currentPlayer = player;
        Debug.Log("[DoorInteraction] UI is active, wait for button click.");
        doorCanvas.gameObject.SetActive(true);
    }

    public void OnExitClicked()
    {
        if (currentPlayer != null && outsideSpawnPoint != null)
        {
            Debug.Log($"[DoorInteraction] Exit Clicked - OutsidePos: {outsideSpawnPoint.position}");
            currentPlayer.RpcTeleport(outsideSpawnPoint.position);
        }
    }

    public void OnEnterClicked()
    {
        if (currentPlayer != null && insideSpawnPoint != null)
        {
            Debug.Log($"[DoorInteraction] Enter Clicked - InsidePos: {insideSpawnPoint.position}");
            currentPlayer.RpcTeleport(insideSpawnPoint.position);
        }
    }
}
