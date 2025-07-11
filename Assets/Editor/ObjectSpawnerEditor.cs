// ObjectSpawnerEditor.cs
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectSpawner))]
public class ObjectSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ObjectSpawner spawner = (ObjectSpawner)target;
        if (GUILayout.Button("Spawn Objects"))
        {
            spawner.Spawn();
        }
    }
}
