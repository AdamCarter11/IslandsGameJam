using UnityEngine;
using UnityEngine.InputSystem;

public class CameraTarget : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        Vector3 direction = Vector3.zero;

        if (Keyboard.current.wKey.isPressed)
            direction += Vector3.up;

        if (Keyboard.current.sKey.isPressed)
            direction += Vector3.down;

        if (Keyboard.current.aKey.isPressed)
            direction += Vector3.left;

        if (Keyboard.current.dKey.isPressed)
            direction += Vector3.right;

        transform.position += direction.normalized * moveSpeed * Time.deltaTime;
    }
}