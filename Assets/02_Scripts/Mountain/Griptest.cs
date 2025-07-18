using UnityEngine;
using UnityEngine.InputSystem;

public class Griptest : MonoBehaviour
{
    public InputActionProperty gripAction; // XRI LeftHand / RightHand의 Select 액션 연결

    void Update()
    {
        float gripValue = gripAction.action.ReadValue<float>();
        if (gripValue > 0.5f)
        {
            Debug.Log($"{gameObject.name} GripPressed: True ({gripValue})");
        }
    }
}
