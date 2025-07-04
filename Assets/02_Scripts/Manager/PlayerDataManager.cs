using UnityEngine;

// �÷��̾� �����͸� ���� ��ü���� ������ �� �ְ� �����ϴ� �̱��� Ŭ����
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    public string UserID; // �α����� ������ ���̵� ������ ����

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ���� �ٲ� �� ������Ʈ�� �ı����� ����
        }
        else
        {
            Destroy(gameObject);
        }
    }
}