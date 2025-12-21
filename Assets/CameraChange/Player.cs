using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{

    private Rigidbody rb;
    Vector3 velocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // InputAction から呼ばれる
    public void OnMove(InputAction.CallbackContext context)
    {
        var axis = context.ReadValue<Vector2>();

        velocity = new Vector3(axis.x, 0f, axis.y) * 5f;
    }

    void FixedUpdate()
    {
        if (velocity.sqrMagnitude > 0f)
        {
            // 移動
            var nextPosition = rb.position + velocity * Time.fixedDeltaTime;
            rb.MovePosition(nextPosition);

            // 移動方向を向く
            var rotation = Quaternion.LookRotation(velocity, Vector3.up);
            rb.MoveRotation(rotation);
        }
    }
}
