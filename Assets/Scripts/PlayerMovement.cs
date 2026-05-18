using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Gravity")]
    public float gravity = -20f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundLayer;

    // Rigidbody reference
    private Rigidbody rb;

    // Input
    private float moveX;
    private float moveZ;

    // Gravity
    private float verticalVelocity;

    // Grounded check
    private bool isGrounded;

    void Start()
    {
        // Get Rigidbody
        rb = GetComponent<Rigidbody>();

        // Disable Unity gravity
        rb.useGravity = false;
    }

    void Update()
    {
        // Get movement input
        moveX = Input.GetAxisRaw("Horizontal");
        moveZ = Input.GetAxisRaw("Vertical");
    }

    void FixedUpdate()
    {
        // Check if player is on ground
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundLayer
        );

        // Stop falling when grounded
        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        // Movement direction
        Vector3 moveDirection = new Vector3(moveX, 0f, moveZ).normalized;

        // Apply gravity
        verticalVelocity += gravity * Time.fixedDeltaTime;

        // Final velocity
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = verticalVelocity;

        // Move player
        rb.MovePosition(
            rb.position + velocity * Time.fixedDeltaTime
        );
    }
}