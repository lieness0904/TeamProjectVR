// CanvasCameraSetter.cs  (World Space도 지원)
using UnityEngine;

public class CanvasCameraSetter : MonoBehaviour
{
    private void OnEnable()
    {
        if (!TryGetComponent<Canvas>(out var canvas)) return;

        // Overlay 빼고는 둘 다 worldCamera 필요(ScreenSpaceCamera, WorldSpace)
        bool needsCamera =
            canvas.renderMode == RenderMode.ScreenSpaceCamera ||
            canvas.renderMode == RenderMode.WorldSpace;

        if (!needsCamera || canvas.worldCamera != null) return;

        Camera cam = Camera.main ?? FindObjectOfType<Camera>(true);
        if (cam != null)
            canvas.worldCamera = cam;   // World Space에선 Event Camera 역할도 이걸로 처리
        else
            Debug.LogWarning("[CanvasCameraSetter] 카메라 없음. XR 카메라 생성/태그 확인");
    }
}
