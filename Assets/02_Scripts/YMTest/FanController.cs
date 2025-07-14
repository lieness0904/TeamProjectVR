using UnityEngine;

public class FanController : MonoBehaviour
{
    // 인스펙터 창에서 회전 속도를 조절할 수 있도록 public 변수로 선언합니다.
    [Tooltip("팬의 분당 회전 속도 (RPM)")]
    public float rotationSpeed = 80.0f;

    // Update is called once per frame
    void Update()
    {
        // Y축(위쪽 방향)을 기준으로 팬을 회전시킵니다.
        // rotationSpeed * 6.0f => RPM을 초당 각도로 변환 (360도 / 60초)
        // Time.deltaTime을 곱해 프레임 속도와 관계없이 일정한 속도로 회전하게 합니다.
        transform.Rotate(Vector3.up, rotationSpeed * 6.0f * Time.deltaTime);
    }
}