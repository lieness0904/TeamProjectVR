using UnityEngine;

/// <summary>
/// 게임의 전반적인 컨트롤 모드(VR 또는 데스크톱)를 관리하는 싱글톤입니다.
/// </summary>
public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    // 컨트롤 모드를 명확하게 구분하기 위한 열거형(Enum)
    public enum ControlMode
    {
        VR,
        Desktop
    }

    // 현재 컨트롤 모드를 저장하고 어디서든 접근할 수 있는 변수.
    // 기본값은 VR 모드입니다.
    public ControlMode CurrentMode { get; set; } = ControlMode.VR;

    void Awake()
    {
        // 싱글톤 패턴 구현
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 전환되어도 이 오브젝트는 파괴되지 않습니다.
        }
        else
        {
            // 이미 인스턴스가 존재하면 이 새로운 인스턴스는 파괴합니다.
            Destroy(gameObject);
        }
    }
}