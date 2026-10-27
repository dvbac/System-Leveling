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

    private Rigidbody2D rb;
    PlayerStats player;

    private void Start()
    {
        player = GetComponent<PlayerStats>();

    }


    private void Awake()
    {
        Instance = this;


        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;


        weaponHolder =
            GetComponentInChildren<WeaponHolder>();
    }



    private void Update()
    {
        if (LevelUpSelection.InputBlocked)
        {
            moveInput = Vector2.zero;
            return;
        }
        if (move != null)
        {
            // đọc input
            moveInput =
                move.action.ReadValue<Vector2>();
        }


        pointerInput =
            GetPointerWorldPosition();



        // animation
        if (animator != null)
        {
            animator.SetBool(
                "isMoving",
                moveInput.sqrMagnitude > 0.01f
            );
        }



        // gửi vị trí chuột cho WeaponHolder
        if (weaponHolder != null)
        {
            weaponHolder.PointerPosition =
                pointerInput;
        }


        // quay player
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
        if (animator != null) animator.SetBool("isMoving", false);
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

        Vector2 mouseScreenPosition =
            pointerPosition.action.ReadValue<Vector2>();

        // Camera.main bị null nếu chưa có camera nào tag MainCamera.
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

}
