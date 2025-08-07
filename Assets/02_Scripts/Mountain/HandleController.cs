using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class HandleController : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[HandleController] Start 실행됨");

        ClimbProvider climbProvider = FindObjectOfType<ClimbProvider>();

        if (climbProvider != null)
        {
            Debug.Log("[HandleController] ClimbProvider 찾음: " + climbProvider.name);
        }
        else
        {
            Debug.LogError("[HandleController] ClimbProvider를 찾을 수 없습니다.");
        }
    }
}