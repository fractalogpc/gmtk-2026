using UnityEngine;
using UnityEngine.InputSystem;

public class BasicPlayerController : MonoBehaviour
{
    private Vector2 movementInput;

    public float speed = 5f;

    public void OnMove(InputAction.CallbackContext context)
    {
        Vector2 inputVector = context.ReadValue<Vector2>();
        Vector3 movement = new Vector3(inputVector.x, 0f, inputVector.y);
        movementInput = movement;
    }

    public void Update()
    {
        // Move the player based on the movement input
        transform.Translate(movementInput * speed * Time.deltaTime);
    }
}
