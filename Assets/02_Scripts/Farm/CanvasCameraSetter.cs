using UnityEngine;

public class CanvasCameraSetter : MonoBehaviour
{
    private void OnEnable()
    {
        if (TryGetComponent<Canvas>(out var canvas))
        {
            if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
            {
                Camera xrCam = Camera.main;
                if (xrCam == null)
                    xrCam = FindObjectOfType<Camera>();
                if (xrCam != null)
                    canvas.worldCamera = xrCam;
            }
        }
    }
}
