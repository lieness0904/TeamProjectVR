using UnityEngine;

/// <summary>
/// 키보드와 마우스를 사용하여 플레이어를 조작하는 컨트롤러입니다.
/// 이 스크립트가 제대로 작동하려면 게임 오브젝트에 'CharacterController' 컴포넌트가 필요합니다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class DesktopPlayerController : MonoBehaviour
{
    [Header("조작 설정")]
    public float moveSpeed = 5f;
    public float lookSensitivity = 2f;
    public float castPower = 15f; // 캐스팅 시 낚시찌에 가해질 기본 힘

    [Header("참조")]
    public Transform cameraTransform; // 플레이어의 시점을 담당할 카메라의 Transform

    private CharacterController characterController;
    private float cameraPitch = 0f; // 카메라의 상하 회전값을 저장할 변수

    void Awake()
    {
        // CharacterController 컴포넌트를 미리 찾아 저장해 둡니다.
        characterController = GetComponent<CharacterController>();
    }

    void Start()
    {
        // 게임 모드가 '데스크톱'일 경우에만 커서를 잠그고 보이지 않게 합니다.
        if (GameModeManager.Instance.CurrentMode == GameModeManager.ControlMode.Desktop)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        // 현재 게임 모드가 '데스크톱'이 아니면, 이 스크립트의 조작 로직을 실행하지 않습니다.
        if (GameModeManager.Instance.CurrentMode != GameModeManager.ControlMode.Desktop)
        {
            return;
        }

        // ESC 키를 눌러 커서 잠금을 해제하거나 다시 잠글 수 있습니다.
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        // 커서가 잠겨 있을 때만 플레이어 조작이 가능하도록 합니다.
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            HandleMovement();
            HandleLook();
            HandleCasting();
        }
    }

    // WASD 키 입력을 받아 플레이어를 이동시키는 함수
    private void HandleMovement()
    {
        float horizontalInput = Input.GetAxis("Horizontal"); // A, D 키 또는 좌우 화살표
        float verticalInput = Input.GetAxis("Vertical");     // W, S 키 또는 상하 화살표

        Vector3 moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;
        characterController.SimpleMove(moveDirection * moveSpeed);
    }

    // 마우스 움직임을 받아 시점을 회전시키는 함수
    private void HandleLook()
    {
        // 마우스의 좌우 움직임으로 플레이어 전체를 Y축 기준으로 회전시킵니다.
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        transform.Rotate(Vector3.up, mouseX);

        // 마우스의 상하 움직임으로 카메라의 시야만 X축 기준으로 회전시킵니다.
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f); // 카메라가 180도 뒤집히는 것을 방지

        // 카메라의 회전값을 실제로 적용합니다.
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
        }
    }

    // 마우스 클릭으로 낚시 캐스팅을 처리하는 함수
    private void HandleCasting()
    {
        // FishingManager가 존재하고, 낚싯대가 활성화된 상태인지 확인합니다.
        if (FishingManager.instance != null && FishingManager.instance.fishingRodObject.activeInHierarchy)
        {
            // 마우스 왼쪽 버튼을 클릭했을 때
            if (Input.GetMouseButtonDown(0))
            {
                if (cameraTransform != null)
                {
                    Debug.Log("데스크톱 모드에서 캐스팅을 시도합니다.");

                    Vector3 castDirection = cameraTransform.forward;
                    Vector3 castVelocity = castDirection * castPower;

                    // FishingManager에 있는 기존의 CastBobber 함수를 호출합니다.
                    FishingManager.instance.CastBobber(castVelocity);
                }
            }
        }
    }
}