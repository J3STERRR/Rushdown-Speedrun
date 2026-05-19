using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public float airMultiplier = 0.5f;

    [Header("Jump")]
    public float jumpHeight = 5f;
    public float wallJumpForce = 7f;

    [Header("Gravity")]
    public float gravity = -20f;

    [Header("Wallrun")]
    public float wallStickForce = 10f;
    public float wallRunGravity = -6f;
    public float wallCheckDistance = 1f;
    public LayerMask wallLayer;

    [Header("Wall Chaining")]
    public float wallChainWindow = 0.25f;

    [Header("Wall Grace Stick")]
    public float wallGraceTime = 0.2f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 100f;
    public Transform cameraTransform;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.25f;
    public LayerMask groundLayer;

    private Rigidbody rb;

    private float moveX;
    private float moveZ;

    private float verticalVelocity;
    private Vector3 horizontalVelocity;

    private bool isGrounded;
    private bool isWallRunning;

    private float xRotation;

    private RaycastHit leftWall;
    private RaycastHit rightWall;

    private Vector3 currentWallNormal;

    // Wall chaining
    private float wallChainTimer;
    private Vector3 lastWallNormal;

    // Wall grace
    private float wallGraceTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.freezeRotation = true;

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        moveX = Input.GetAxisRaw("Horizontal");
        moveZ = Input.GetAxisRaw("Vertical");

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryJump();
        }
    }

    void FixedUpdate()
    {
        rb.angularVelocity = Vector3.zero;

        wallChainTimer -= Time.fixedDeltaTime;
        wallGraceTimer -= Time.fixedDeltaTime;

        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundDistance,
            groundLayer
        );

        CheckWalls();
        HandleWallrunState();

        Vector3 inputDir =
            transform.right * moveX +
            transform.forward * moveZ;

        inputDir = inputDir.normalized;

        float speed = isGrounded ? sprintSpeed : moveSpeed;

        if (!isGrounded && !isWallRunning)
            speed *= airMultiplier;

        Vector3 targetMove = inputDir * speed;

        if (!isWallRunning)
        {
            horizontalVelocity = Vector3.Lerp(
                horizontalVelocity,
                targetMove,
                10f * Time.fixedDeltaTime
            );
        }
        else
        {
            ApplyWallrun();
        }

        if (!isWallRunning)
        {
            verticalVelocity += gravity * Time.fixedDeltaTime;
        }

        if (isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;

        rb.velocity = finalVelocity;
    }

    void HandleWallrunState()
    {
        bool touchingWall =
            leftWall.collider != null ||
            rightWall.collider != null;

        Vector3 detectedNormal =
            leftWall.collider != null ? leftWall.normal :
            rightWall.collider != null ? rightWall.normal :
            Vector3.zero;

        bool isNewWall =
            detectedNormal != Vector3.zero &&
            Vector3.Dot(detectedNormal, lastWallNormal) < 0.95f;

        bool hardCanWallRun =
            !isGrounded &&
            touchingWall &&
            moveZ > 0;

        // refresh grace window when we still have wall contact
        if (hardCanWallRun)
        {
            wallGraceTimer = wallGraceTime;
        }

        bool canWallRun =
            hardCanWallRun &&
            (isNewWall || wallChainTimer > 0f || wallGraceTimer > 0f);

        isWallRunning = canWallRun;

        if (isWallRunning)
        {
            currentWallNormal = detectedNormal;

            verticalVelocity = Mathf.Clamp(verticalVelocity, -2f, 2f);

            if (isNewWall)
            {
                wallChainTimer = wallChainWindow;
                lastWallNormal = currentWallNormal;
            }
        }
    }

    void ApplyWallrun()
    {
        Vector3 wallForward =
            Vector3.Cross(currentWallNormal, Vector3.up);

        if (Vector3.Dot(wallForward, transform.forward) < 0)
            wallForward = -wallForward;

        Vector3 projected =
            Vector3.Project(horizontalVelocity, wallForward);

        horizontalVelocity = Vector3.Lerp(
            horizontalVelocity,
            projected,
            6f * Time.fixedDeltaTime
        );

        float stickMultiplier = (wallGraceTimer > 0f) ? 1.25f : 1f;

        rb.AddForce(
            -currentWallNormal * wallStickForce * stickMultiplier,
            ForceMode.Force
        );

        verticalVelocity += wallRunGravity * Time.fixedDeltaTime;
    }

    void TryJump()
    {
        if (isWallRunning)
        {
            WallJump();
            return;
        }

        if (isGrounded)
        {
            verticalVelocity = jumpHeight;
        }
    }

    void WallJump()
    {
        isWallRunning = false;

        Vector3 jumpDir =
            currentWallNormal * wallJumpForce +
            Vector3.up * jumpHeight;

        horizontalVelocity += jumpDir;
        verticalVelocity = 0f;

        wallChainTimer = wallChainWindow;
        lastWallNormal = currentWallNormal;
    }

    void CheckWalls()
    {
        Physics.Raycast(transform.position, transform.right, out rightWall, wallCheckDistance, wallLayer);
        Physics.Raycast(transform.position, -transform.right, out leftWall, wallCheckDistance, wallLayer);
    }
}