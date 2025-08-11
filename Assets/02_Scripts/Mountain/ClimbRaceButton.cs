using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ClimbRaceButton : MonoBehaviour
{
    public enum Kind { Start, Finish }

    [SerializeField] private Kind kind = Kind.Start;
    [SerializeField] private ClimbTimer ui;
    [SerializeField] private XRBaseInteractable interactable;

    void Reset()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (!ui) ui = FindObjectOfType<ClimbTimer>(true);
    }

    void Awake()
    {
        if (!interactable) interactable = GetComponent<XRBaseInteractable>();
        if (!ui) ui = FindObjectOfType<ClimbTimer>(true);
    }

    void OnEnable()
    {
        if (interactable) interactable.selectEntered.AddListener(OnSelect);
    }

    void OnDisable()
    {
        if (interactable) interactable.selectEntered.RemoveListener(OnSelect);
    }

    void OnSelect(SelectEnterEventArgs _)
    {
        if (!ui) return;
        if (kind == Kind.Start) ui.StartCountdown();
        else ui.StopAndShowRecord();
    }
}