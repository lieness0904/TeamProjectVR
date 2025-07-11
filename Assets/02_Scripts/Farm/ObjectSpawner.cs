using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    public GameObject prefab;

    [Header("배치 설정")]
    [Space(10)]
    [Header("시작 위치")]
    [SerializeField] private Vector2 startPos = new Vector2(500f, 450f);
    [SerializeField] private float yPosition = 0f;

    [Header("X 간격")]
    [SerializeField] private float defaultXSpacing = 4.5f;
    [Header("Z 간격")]
    [SerializeField] private float defaultZSpacing = 4.5f;
    [Header("한줄에 몇개까지 심을지")]
    [SerializeField] private int columns = 30;
    [Header("몇 줄 심을지")]
    [SerializeField] private int rows = 10;

    [Header("특정 간격을 줄 간격마다 적용")]
    [SerializeField] private int everyNRow = 5;
    [SerializeField] private float specialXSpacing = 10f;

    [Header("프리팹 회전값 (Euler)")]
    [SerializeField] private Vector3 rotationEuler = Vector3.zero;

    public void Spawn()
    {
        // 기존 오브젝트 삭제
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        float currentX = startPos.x;

        for (int row = 0; row < rows; row++)
        {
            if (row > 0)
            {
                if (row % everyNRow == 0)
                    currentX += specialXSpacing;
                else
                    currentX += defaultXSpacing;
            }

            for (int col = 0; col < columns; col++)
            {
                float z = startPos.y + col * defaultZSpacing;
                Vector3 spawnPos = new Vector3(currentX, yPosition, z);
                Quaternion rotation = Quaternion.Euler(rotationEuler);

                GameObject obj = Instantiate(prefab, spawnPos, rotation, transform);
                obj.name = $"Spawned_{row}_{col}";
            }
        }
    }
}
