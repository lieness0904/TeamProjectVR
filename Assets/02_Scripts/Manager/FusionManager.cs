using UnityEngine;
using Fusion;

public class FusionManager : MonoBehaviour
{
    public static FusionManager Instance { get; private set; }

    public NetworkRunner Runner { get; private set; }  

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);  

        Runner = GetComponent<NetworkRunner>();
        if (Runner == null)
        {
            Debug.LogWarning("FusionManager에 NetworkRunner가 붙어있지 않음! 수동으로 설정 필요.");
        }
    }

    public void RegisterRunner(NetworkRunner runner)
    {
        Runner = runner;
    }
}