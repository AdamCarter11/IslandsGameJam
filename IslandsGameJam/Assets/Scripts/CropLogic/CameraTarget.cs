using UnityEngine;
using UnityEngine.InputSystem;

public class CameraTarget : MonoBehaviour
{
    [Header("Runtime")]
    [SerializeField]
    private Selector selector;

    [Header("Runtime")]
    [SerializeField]
    private bool enableMovement = false;

    private void Update()
    {
        var enableMovement = Keyboard.current?.spaceKey.isPressed ?? false;
    }
}
