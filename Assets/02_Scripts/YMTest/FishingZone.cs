using UnityEngine;
using System.Collections.Generic;

// 이 컴포넌트는 단순히 낚시가 가능한 구역임을 표시하고,
// 해당 구역에서 낚을 수 있는 물고기 목록을 가지는 역할을 합니다.
public class FishingZone : MonoBehaviour
{
    [Header("구역 설정")]
    [Tooltip("이 구역에서 낚을 수 있는 물고기 프리팹 목록")]
    public List<GameObject> availableFishPrefabs;

    // 트리거 영역을 시각적으로 쉽게 확인하기 위해 Gizmos를 사용합니다.
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f); // 청록색, 반투명
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
            }
        }
    }
}