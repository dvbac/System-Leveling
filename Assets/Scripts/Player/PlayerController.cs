
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Input")]
    [SerializeField] private InputActionReference move;
    [SerializeField] private InputActionReference pointerPosition;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private WeaponHolder weaponHolder;
    private Camera mainCamera;

    private Vector2 moveInput;
    private Vector2 pointerInput;

    // Cho MapController đọc hướng di chuyển
    public Vector2 moveDir => moveInput;

    private Rigidbody2D rb;
    private PlayerStats player;

    private void Awake()
    {
        Instance = this;

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        mainCamera = Camera.main;

        weaponHolder = GetComponentInChildren<WeaponHolder>();
    }

    private void Start()
    {
        player = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (GameOverController.GameplayStopped)
        {
            moveInput = Vector2.zero;
            return;
        }

        if (LevelUpSelection.InputBlocked)
        {
            moveInput = Vector2.zero;

            if (animator != null)
                animator.SetBool("isMoving", false);

            return;
        }

        // Đọc input di chuyển
        if (move != null && move.action != null)
        {
            moveInput = move.action.ReadValue<Vector2>();
            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
        }
        else
        {
            moveInput = Vector2.zero;
        }

        // Lấy vị trí chuột
        pointerInput = GetPointerWorldPosition();

        // Animation di chuyển
        if (animator != null)
        {
            animator.SetBool(
                "isMoving",
                moveInput.sqrMagnitude > 0.01f
            );
        }

        // Gửi vị trí chuột cho WeaponHolder
        if (weaponHolder != null)
        {
            weaponHolder.PointerPosition = pointerInput;
        }

        // Xoay nhân vật
        FlipPlayer();
    }

    private void FixedUpdate()
    {
        if (GameOverController.GameplayStopped)
        {
            HoldForGameOver();
            return;
        }

        if (rb == null || player == null)
            return;

        rb.MovePosition(
            rb.position +
            moveInput *
            player.currentMoveSpeed *
            Time.fixedDeltaTime
        );
    }

    public void HoldForGameOver()
    {
        moveInput = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("isMoving", false);
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.simulated = false;
        }
    }

    private Vector2 GetPointerWorldPosition()
    {
        if (pointerPosition == null)
            return transform.position;

        if (pointerPosition.action == null)
            return transform.position;

        Vector2 mouseScreenPosition =
            pointerPosition.action.ReadValue<Vector2>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return transform.position;

        return mainCamera.ScreenToWorldPoint(
            mouseScreenPosition
        );
    }

    private void FlipPlayer()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX =
            pointerInput.x < transform.position.x;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
