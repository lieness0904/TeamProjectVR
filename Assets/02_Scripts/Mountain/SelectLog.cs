using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SelectLog : MonoBehaviour
{
    private void OnEnable()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        interactable.selectEntered.AddListener(OnSelect);
    }

    private void OnSelect(SelectEnterEventArgs args)
    {
        Debug.Log("선택됨 (Select Entered): " + args.interactorObject.transform.name);
    }
}