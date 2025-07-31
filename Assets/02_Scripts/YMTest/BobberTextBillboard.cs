using UnityEngine;

public class BobberTextBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        // 1. 카메라를 바라보게 함
        if (Camera.main != null)
        {
            // 아래 코드는 Z축이 카메라를 보게 만듦 (텍스트의 앞면이 +Z라면 그대로 사용)
            transform.forward = Camera.main.transform.forward;
        }
    }
}
