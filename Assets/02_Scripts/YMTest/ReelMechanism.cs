using UnityEngine;

public class ReelMechanism : MonoBehaviour
{
    [Header("연결할 오브젝트")]
    [Tooltip("회전의 기준이 될 손잡이 오브젝트 (Part_7)")]
    public Transform handle; // 손잡이

    [Tooltip("손잡이의 회전에 따라 함께 회전할 몸통 오브젝트 (Part_4)")]
    public Transform reelBody; // 릴 몸통

    [Header("회전 설정")]
    [Tooltip("손잡이 회전 속도에 대한 몸통 회전 속도의 비율입니다.")]
    public float rotationMultiplier = 1.0f;

    // 매 프레임마다 호출되어 회전을 동기화합니다.
    void Update()
    {
        reelBody.localRotation = Quaternion.Euler(handle.localEulerAngles.z * rotationMultiplier, 0, 0);
    }
}