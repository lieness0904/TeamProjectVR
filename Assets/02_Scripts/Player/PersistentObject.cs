// PersistentObject.cs
using UnityEngine;

public class PersistentObject : MonoBehaviour
{
    void Awake()
    {
        // 씬 전환 시에도 이 GameObject를 파괴하지 않고 유지합니다.
        DontDestroyOnLoad(gameObject);
    }
}