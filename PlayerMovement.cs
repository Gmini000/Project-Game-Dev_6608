
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Dash")]
    public float dashSpeed = 15f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public int DashCapacity { get; private set; } = 1;
    public int DashCharges { get; private set; } = 1;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Vector2 dashDirection;
    private Vector2 lastMoveDirection = Vector2.down;

    private bool isDashing;
    private float dashTimer;
    private float cooldownTimer;
    private int enemyLayerMask;
    private LayerMask layersExcludedBeforeDash;

    public Vector2 MoveDirection => isDashing ? dashDirection : movement;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        enemyLayerMask = LayerMask.GetMask("Enemy");
    }

    void OnDisable()
    {
        if (isDashing)
            FinishDash();
    }

    void Update()
    {
        if (Time.timeScale == 0f)
            return;

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        movement = new Vector2(x, y).normalized;

        if (movement != Vector2.zero)
        {
            lastMoveDirection = movement;
        }

        if (DashCharges < DashCapacity)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                DashCharges++;
                cooldownTimer = dashCooldown;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space)
            && !isDashing
            && DashCharges > 0)
        {
            StartDash();
        }
    }

    void FixedUpdate()
    {
        // Keep the exclusion active through the physics step that applies the last dash velocity.
        if (isDashing && dashTimer <= 0f)
            FinishDash();

        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            dashTimer -= Time.fixedDeltaTime;
        }
        else
        {
            rb.linearVelocity = movement * moveSpeed;
        }
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        if (DashCharges == DashCapacity)
            cooldownTimer = dashCooldown;
        DashCharges--;

        dashDirection = lastMoveDirection;
        layersExcludedBeforeDash = rb.excludeLayers;
        rb.excludeLayers = layersExcludedBeforeDash.value | enemyLayerMask;
    }

    public void SetDashCapacity(int capacity)
    {
        capacity = Mathf.Max(1, capacity);
        DashCharges = Mathf.Clamp(DashCharges + capacity - DashCapacity, 0, capacity);
        DashCapacity = capacity;
    }

    void FinishDash()
    {
        rb.excludeLayers = layersExcludedBeforeDash;
        isDashing = false;
    }
}
