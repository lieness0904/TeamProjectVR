using UnityEngine;
using UnityEngine.InputSystem;

public class InputTest : MonoBehaviour
{
    public InputActionReference testAction;

    void OnEnable()
    {
        Debug.Log(gameObject.name + " - InputTest.OnEnable() 호출됨!");

        // 이 액션이 포함된 '액션 맵'을 찾아서 활성화합니다. (이것이 핵심!)
        if (testAction.action.actionMap != null)
        {
            Debug.Log("'" + testAction.action.actionMap.name + "' 액션 맵을 활성화합니다.");
            testAction.action.actionMap.Enable();
        }

        testAction.action.performed += OnActionPerformed;
    }

    void OnDisable()
    {
        if (testAction.action.actionMap != null)
        {
            testAction.action.actionMap.Disable();
        }
        testAction.action.performed -= OnActionPerformed;
    }

    private void OnActionPerformed(InputAction.CallbackContext context)
    {
        Debug.LogError("!!! 입력 성공: " + context.action.name + " 액션이 감지되었습니다! !!!");
    }
}