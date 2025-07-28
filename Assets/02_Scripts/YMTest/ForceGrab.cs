using UnityEngine;
using UnityEngine.EventSystems; // Unity 이벤트 시스템 사용

// 포인터(광선) 이벤트를 감지하기 위한 인터페이스들을 상속받습니다.
public class ForceGrab : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [Header("설정")]
    [Tooltip("컨트롤러가 달라붙을 위치와 방향을 지정합니다.")]
    public Transform snapPoint;

    [Tooltip("달라붙을 컨트롤러의 Transform. 비워두면 태그로 자동 검색합니다.")]
    public Transform controllerTransform;

    private bool isHovering = false; // 컨트롤러 광선이 위에 있는지 여부

    // 이 스크립트가 활성화될 때 한 번 호출됩니다.
    void Start()
    {
        // Controller Transform 필드가 비어있다면, 태그를 이용해 직접 찾습니다.
        if (controllerTransform == null)
        {
            GameObject controllerObject = GameObject.FindWithTag("LeftHandController");
            if (controllerObject != null)
            {
                controllerTransform = controllerObject.transform;
                Debug.Log("Left Controller를 자동으로 찾아서 할당했습니다.");
            }
            else
            {
                Debug.LogError("'LeftHandController' 태그를 가진 오브젝트를 찾을 수 없습니다! 플레이어 프리팹의 왼손 컨트롤러에 태그가 적용되었는지 확인해주세요.");
            }
        }
    }

    // 광선이 이 오브젝트에 닿기 시작했을 때 호출됩니다.
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        Debug.Log("광선 감지됨: " + gameObject.name);
        // 필요하다면 여기서 릴 손잡이의 색을 바꾸는 등 시각적 피드백을 줄 수 있습니다.
    }

    // 광선이 이 오브젝트에서 벗어났을 때 호출됩니다.
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        Debug.Log("광선 벗어남: " + gameObject.name);
    }

    // 광선이 닿고 있는 상태에서 클릭(Select) 버튼을 눌렀을 때 호출됩니다.
    public void OnPointerDown(PointerEventData eventData)
    {
        // 광선이 위에 있고, 스냅 포인트와 컨트롤러가 모두 할당되어 있다면
        if (isHovering && snapPoint != null && controllerTransform != null)
        {
            Debug.Log("잡기 명령 실행! 컨트롤러를 스냅합니다.");

            // 컨트롤러의 위치와 회전을 스냅 포인트의 위치와 회전으로 강제로 변경합니다.
            controllerTransform.position = snapPoint.position;
            controllerTransform.rotation = snapPoint.rotation;
        }
    }
}