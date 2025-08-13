// CanvasCameraSetter.cs (HUDCanvas에 부착)
using UnityEngine;

public class CanvasCameraSetter : MonoBehaviour
{
    private void OnEnable()
    {
        if (!TryGetComponent<Canvas>(out var canvas)) return;
        if (canvas.renderMode != RenderMode.ScreenSpaceCamera || canvas.worldCamera != null) return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            // 혹시 MainCamera 태그를 안 붙였으면…
            cam = FindObjectOfType<Camera>(true);
        }

        if (cam != null)
            canvas.worldCamera = cam;
        else
            Debug.LogWarning("[CanvasCameraSetter] 카메라 없음. XR 카메라 생성 시점/태그 확인 필요");
    }
}
