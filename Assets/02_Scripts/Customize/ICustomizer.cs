using UnityEngine;

public interface ICustomizer
{
    GameObject GetCustomizationRoot();
    void Equip(string category, GameObject go);
}
