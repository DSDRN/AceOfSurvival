using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Hareket")]
    [Tooltip("Balance tablosu: Base Movement Speed = 4.5")]
    [SerializeField] private float moveSpeed = 4.5f;

    [Header("Dash")]
    [Tooltip("Balance: Dash Cooldown = 5 sn")]
    [SerializeField] private float dashCooldown = 5f;

    [Tooltip("Balance: Dash suresi = 0.25 sn")]
    [SerializeField] private float dashDuration = 0.25f;

    [Tooltip("Dash hizi. 18 x 0.25 = ~4.5 birim")]
    [SerializeField] private float dashSpeed = 18f;

    [Header("i-frame")]
    [Tooltip("Balance: hasar sonrasi dokunulmazlik = 0.6 sn")]
    [SerializeField] private float hurtIFrameDuration = 0.6f;

    public bool IsInvincible => isDashing || hurtIFrameTimer > 0f;
    public Vector2 FacingDir { get; private set; } = Vector2.right;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 moveInput;
    private Vector2 lastMoveDir = Vector2.right;
    private bool isDashing;
    private float dashTimer;
    private float cooldownTimer;
    private float hurtIFrameTimer;

    // ---- INPUT ACTIONS ----
    private InputAction moveAction;
    private InputAction dashAction;

    public float MoveSpeed => moveSpeed;
    public float DashCooldown => dashCooldown;
    public float CurrentDashCooldown => cooldownTimer;

    // --- GHOSTING LAYER DEGISKENLERI ---
    private int playerLayer;
    private int enemyLayer;
    private int enemyProjLayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        moveAction.AddBinding("<Gamepad>/leftStick");

        dashAction = new InputAction("Dash", InputActionType.Button);
        dashAction.AddBinding("<Keyboard>/space");
        dashAction.AddBinding("<Gamepad>/buttonSouth");
    }

    private void Start()
    {
        playerLayer = LayerMask.NameToLayer("Player");
        enemyLayer = LayerMask.NameToLayer("Enemy");
        enemyProjLayer = LayerMask.NameToLayer("EnemyProjectile");
    }

    private void OnEnable() { moveAction.Enable(); dashAction.Enable(); }
    private void OnDisable() { moveAction.Disable(); dashAction.Disable(); }
    private void OnDestroy() { moveAction.Dispose(); dashAction.Dispose(); }

    public void SetControlsEnabled(bool enabled)
    {
        if (enabled) { moveAction.Enable(); dashAction.Enable(); }
        else { moveAction.Disable(); dashAction.Disable(); }
    }

    private void Update()
    {
        ReadInput();
        UpdateTimers();
        UpdateFacing();
        UpdateInvincibilityVisual();
    }

    private void FixedUpdate()
    {
        if (isDashing)
            rb.linearVelocity = lastMoveDir * dashSpeed;
        else
            rb.linearVelocity = moveInput * moveSpeed;
    }

    private void ReadInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude > 1f)
            moveInput = moveInput.normalized;

        if (moveInput.sqrMagnitude > 0.01f)
            lastMoveDir = moveInput.normalized;

        if (dashAction.WasPressedThisFrame())
            TryDash();
    }

    private void TryDash()
    {
        if (cooldownTimer > 0f || isDashing) return;

        isDashing = true;
        dashTimer = dashDuration;
        cooldownTimer = dashCooldown;

        // --- GHOSTING BASLAT ---
        if (playerLayer != -1 && enemyLayer != -1)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        if (playerLayer != -1 && enemyProjLayer != -1)
            Physics2D.IgnoreLayerCollision(playerLayer, enemyProjLayer, true);
    }

    private void UpdateTimers()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
        if (hurtIFrameTimer > 0f) hurtIFrameTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
            {
                isDashing = false;

                // --- GHOSTING BITIR ---
                if (playerLayer != -1 && enemyLayer != -1)
                    Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, false);
                if (playerLayer != -1 && enemyProjLayer != -1)
                    Physics2D.IgnoreLayerCollision(playerLayer, enemyProjLayer, false);
            }
        }
    }

    private void UpdateFacing()
    {
        if (moveInput.x > 0.01f)
        {
            sr.flipX = false;
            FacingDir = Vector2.right;
        }
        else if (moveInput.x < -0.01f)
        {
            sr.flipX = true;
            FacingDir = Vector2.left;
        }
    }

    private void UpdateInvincibilityVisual()
    {
        sr.color = IsInvincible ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
    }

    public void TriggerHurtIFrames()
    {
        hurtIFrameTimer = hurtIFrameDuration;
    }

    public void AddMoveSpeed(float amount)
    {
        moveSpeed = Mathf.Max(1.5f, moveSpeed + amount);
    }
}