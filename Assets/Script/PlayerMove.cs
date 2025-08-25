using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    private Vector3 moveInput;

    void Update()
    {
        transform.position += moveInput * 3f * Time.deltaTime;
    }

    void OnMove(InputValue value)
    {
        var moveVal = value.Get<Vector2>();

        moveInput = new Vector3(moveVal.x, 0, moveVal.y);
    }
}
