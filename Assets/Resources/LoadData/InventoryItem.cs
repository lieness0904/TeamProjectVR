using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public int id;
    public int amount;

    public InventoryItem(int id, int amount = 1)
    {
        this.id = id;
        this.amount = amount;
    }
}


