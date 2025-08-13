using UnityEngine;

public class CanvasCameraSetter : MonoBehaviour
{
    private void Start()
    {
        if (TryGetComponent<Canvas>(out var canvas))
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
            {
                Camera xrCam = Camera.main; // 태그 "MainCamera" 붙어있으면 이렇게 찾음
                if (xrCam != null)
                {
                    canvas.worldCamera = xrCam;
                }
                else
                {
                    // XR 카메라가 MainCamera 태그가 없으면 이름이나 컴포넌트로 검색
                    xrCam = FindObjectOfType<Camera>();
                    if (xrCam != null)
                        canvas.worldCamera = xrCam;
                }
            }
        }
    }
}
