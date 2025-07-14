using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class T_PlayerInventory : MonoBehaviour
{
    [SerializeField] private GameObject inventoryCanvas;
    private bool isInventoryOpen = false;

    private void Start()
    {
        if (inventoryCanvas != null)    
            inventoryCanvas.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            isInventoryOpen = !isInventoryOpen;
            inventoryCanvas.SetActive(isInventoryOpen);
        }
    }

}
