using UnityEngine;
using Unity.XR.CoreUtils;
using System.Collections;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private Transform spawnPoint;
    IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f);

        xrOrigin.transform.rotation = spawnPoint.rotation; // 추가
        xrOrigin.MoveCameraToWorldLocation(spawnPoint.position);

        Debug.Log("XR Origin Final Pos: " + xrOrigin.transform.position);
        Debug.Log("XR Origin Final Rot: " + xrOrigin.transform.eulerAngles);
        Debug.Log("Main Camera Pos: " + Camera.main.transform.position);
    }

    //private void Start()
    //{
    //    StartCoroutine(SpawnAfterXRInit());
    //}

    //IEnumerator SpawnAfterXRInit()
    //{
    //    yield return new WaitForSeconds(0.1f); // XR 초기화 기다림

    //    if (xrOrigin != null && spawnPoint != null)
    //    {
    //        xrOrigin.MoveCameraToWorldLocation(spawnPoint.position);
    //        Debug.Log("XR Origin 이동 완료: " + xrOrigin.transform.position);
    //    }
    //}
}
