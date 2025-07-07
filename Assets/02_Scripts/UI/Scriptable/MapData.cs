using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MapData", menuName = "Scriptable/MapData")]
public class MapData : ScriptableObject
{
    public string mapName;
    public Sprite previewImage;
    public string sceneName; //실제 이동할 씬 이름
    //public string description;
}
