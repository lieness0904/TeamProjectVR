using UnityEngine;

public class UIGameStartFlow : MonoBehaviour
{
    [SerializeField] private GameObject startPanel; // 처음 보이는 패널 (월드 스페이스)
    [SerializeField] private GameObject infoPanel;  // 설명 패널 (월드 스페이스)

    private void Start()
    {
        ShowOnly(startPanel);
    }

    public void OnClickOpenInfo()
    {
        ShowOnly(infoPanel);
    }

    public void OnClickStartGame()
    {
        FarmGameManager.Instance?.StartGame();
        // 게임 시작하면 이 월드 캔버스는 숨겨도 됨
        gameObject.SetActive(false);
    }

    private void ShowOnly(GameObject target)
    {
        if (startPanel) startPanel.SetActive(target == startPanel);
        if (infoPanel) infoPanel.SetActive(target == infoPanel);
    }
}
