using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.ComponentModel;

[System.Serializable]
public class ItemData
{
    public int id;
    public string name;
    public ItemType itemType;
    public string description;
    public string iconPath;
    public int value;
}

public enum ItemType
{
    Equipment = 0,
    Consumable = 1,
    Common = 2
}
