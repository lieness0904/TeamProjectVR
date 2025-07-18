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
    public float value;
    public int price;
}

public enum ItemType
{
    [Description("장비")]
    Equipment = 0,
    [Description("소모품")]
    Consumable = 1,
    [Description("일반 아이템")]
    Common = 2
}


