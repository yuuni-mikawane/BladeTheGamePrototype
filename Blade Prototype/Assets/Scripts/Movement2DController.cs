using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement2DController : MonoBehaviour
{
    private Rigidbody2D rb;

    [Header("Movement Variables")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Movement Variables")]
    [SerializeField] private float movementAcceleration;
    [SerializeField] private float maxMoveSpeed;
    [SerializeField] private float groundLinearDrag;
    private float horizontalDirection;
    private bool isChangingDirection => (rb.velocity.x > 0f && horizontalDirection < 0) || (rb.velocity.x < 0f && horizontalDirection > 0);

    [Header("Jump Variables")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float extraJumpForce;
    [SerializeField] private float wallJumpForce;
    [SerializeField] private float airLinearDrag;
    [SerializeField] private float fallMultiplier;
    [SerializeField] private float lowJumpFallMultiplier;
    [SerializeField] private int maxExtraJump;

    private bool pendingJump;
    private float lastGroundedTime;
    private float lastOnWallTime;
    private float jumpPressedTime;
    [SerializeField] private float coyoteTime;

    [Header("Collision Variables")]
    [SerializeField] private float groundRaycastLength;
    [SerializeField] private Vector3 groundRaycastOffset;
    [SerializeField] private Vector3 wallColCircleOffset;
    [SerializeField] private float wallCheckRadius;

    [Header("Corner Push Variables")]
    [SerializeField] private float topRaycastLength;
    [SerializeField] private Vector3 edgeRaycastOffset;
    [SerializeField] private Vector3 innerRaycastOffset;
    private bool canCornerPush;


    [Header("Variables for monitoring only")]
    [SerializeField] private int extraJumpCount;
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isOnWall;
    [SerializeField] private bool canWallJump;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        horizontalDirection = GetInput().x;
        if (Input.GetKeyDown(KeyCode.Space))
            Jump();
        if (isOnWall)
        {
            lastOnWallTime = Time.time;
        }
        if (isGrounded)
        {
            extraJumpCount = maxExtraJump;
            lastGroundedTime = Time.time;
            canWallJump = true;
        }
    }

    private void FixedUpdate()
    {
        CheckCollisions();
        MoveCharacter();

        if (isGrounded)
        {
            ApplyGroundLinearDrag();
        }
        else
        {
            ApplyAirLinearDrag();
            FallMultiplier();
        }

        if (pendingJump)
        {
            ApplyAirLinearDrag();
            if (!NormalJump())
            {
                if (!WallJump())
                    ExtraJump();
            }
        }
        if (canCornerPush)
        {
            CornerPush(rb.velocity.y);
        }
    }

    private Vector2 GetInput()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    private void MoveCharacter()
    {
        if (horizontalDirection == 0 || isChangingDirection)
            rb.velocity = new Vector2(0f, rb.velocity.y);
        else
            rb.AddForce(new Vector2(horizontalDirection, 0f) * movementAcceleration);

        //clamp velocity
        if (Mathf.Abs(rb.velocity.x) > maxMoveSpeed)
        {
            rb.velocity = new Vector2(Mathf.Sign(rb.velocity.x)* maxMoveSpeed, rb.velocity.y);
        }
    }

    private void ApplyGroundLinearDrag()
    {
        if (Mathf.Abs(horizontalDirection) < 0.2f || isChangingDirection)
        {
            rb.drag = groundLinearDrag;
        }
        else
        {
            rb.drag = 0f;
        }
    }

    private void ApplyAirLinearDrag()
    {
        rb.drag = airLinearDrag;
    }

    private void CornerPush(float yVel)
    {
        //push player to the right
        RaycastHit2D hit = Physics2D.Raycast(transform.position - innerRaycastOffset + Vector3.up * topRaycastLength, Vector3.left, topRaycastLength, groundLayer);
        if (hit.collider != null)
        {
            float newPos = Vector3.Distance(new Vector3(hit.point.x, transform.position.y, 0f) + Vector3.up * topRaycastLength, 
                            transform.position - edgeRaycastOffset + Vector3.up * topRaycastLength);
            transform.position = new Vector3(transform.position.x + newPos, transform.position.y, transform.position.z);
            rb.velocity = new Vector2(rb.velocity.x, yVel);
            return;
        }

        //push player to the left
        hit = Physics2D.Raycast(transform.position + innerRaycastOffset + Vector3.up * topRaycastLength, Vector3.right, topRaycastLength, groundLayer);
        if (hit.collider != null)
        {
            float newPos = Vector3.Distance(new Vector3(hit.point.x, transform.position.y, 0f) + Vector3.up * topRaycastLength,
                            transform.position + edgeRaycastOffset + Vector3.up * topRaycastLength);
            transform.position = new Vector3(transform.position.x - newPos, transform.position.y, transform.position.z);
            rb.velocity = new Vector2(rb.velocity.x, yVel);
        }
    }

    private void Jump()
    {
        //saving this for coyote time
        jumpPressedTime = Time.time;
        pendingJump = true;
    }
    private bool NormalJump()
    {
        ApplyAirLinearDrag();
        if (Time.time - lastGroundedTime <= coyoteTime)
        {
            if (Time.time - jumpPressedTime <= coyoteTime)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
                jumpPressedTime = 0;
                lastGroundedTime = 0;
                pendingJump = false;
                return true;
            }
        }

        return false;
    }

    private bool WallJump()
    {
        ApplyAirLinearDrag();
        if (canWallJump && isOnWall && Time.time - lastOnWallTime <= coyoteTime)
        {
            if (Time.time - jumpPressedTime <= coyoteTime)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0f);
                rb.AddForce(Vector2.up * wallJumpForce, ForceMode2D.Impulse);
                canWallJump = false;
                jumpPressedTime = 0;
                lastOnWallTime = 0;
                pendingJump = false;
                return true;
            }
        }

        return false;
    }

    private void ExtraJump()
    {
        if (extraJumpCount > 0)
        {
            ApplyAirLinearDrag();
            extraJumpCount--;
            rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.AddForce(Vector2.up * extraJumpForce, ForceMode2D.Impulse);
            pendingJump = false;
        }
    }

    private void FallMultiplier()
    {
        if (rb.velocity.y < 0)
        {
            rb.gravityScale = fallMultiplier;
        }
        else if (rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space))
        {
            rb.gravityScale = lowJumpFallMultiplier;
        }
        else
        {
            rb.gravityScale = 1f;
        }
    }

    private void CheckCollisions()
    {
        isGrounded = Physics2D.Raycast(transform.position + groundRaycastOffset, Vector2.down, groundRaycastLength, groundLayer) ||
                                Physics2D.Raycast(transform.position - groundRaycastOffset, Vector2.down, groundRaycastLength, groundLayer);

        isOnWall = Physics2D.OverlapCircle(transform.position - wallColCircleOffset, wallCheckRadius, groundLayer) ||
                                Physics2D.OverlapCircle(transform.position + wallColCircleOffset, wallCheckRadius, groundLayer);

        canCornerPush = Physics2D.Raycast(transform.position + edgeRaycastOffset, Vector2.up, topRaycastLength, groundLayer) &&
                        !Physics2D.Raycast(transform.position + innerRaycastOffset, Vector2.up, topRaycastLength, groundLayer) ||
                        Physics2D.Raycast(transform.position - edgeRaycastOffset, Vector2.up, topRaycastLength, groundLayer) &&
                        !Physics2D.Raycast(transform.position - innerRaycastOffset, Vector2.up, topRaycastLength, groundLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        //ground check
        Gizmos.DrawLine(transform.position + groundRaycastOffset, transform.position + groundRaycastOffset + Vector3.down * groundRaycastLength);
        Gizmos.DrawLine(transform.position - groundRaycastOffset, transform.position - groundRaycastOffset + Vector3.down * groundRaycastLength);

        //wall check
        Gizmos.DrawWireSphere(transform.position + wallColCircleOffset, wallCheckRadius);
        Gizmos.DrawWireSphere(transform.position - wallColCircleOffset, wallCheckRadius);

        //corner check
        Gizmos.DrawLine(transform.position + edgeRaycastOffset, transform.position + edgeRaycastOffset + Vector3.up * topRaycastLength);
        Gizmos.DrawLine(transform.position - edgeRaycastOffset, transform.position - edgeRaycastOffset + Vector3.up * topRaycastLength);
        Gizmos.DrawLine(transform.position + innerRaycastOffset, transform.position + innerRaycastOffset + Vector3.up * topRaycastLength);
        Gizmos.DrawLine(transform.position - innerRaycastOffset, transform.position - innerRaycastOffset + Vector3.up * topRaycastLength);

        //corner distance check
        Gizmos.DrawLine(transform.position - innerRaycastOffset + Vector3.up * topRaycastLength,
                        transform.position - innerRaycastOffset + Vector3.up * topRaycastLength + Vector3.left * topRaycastLength);
        Gizmos.DrawLine(transform.position + innerRaycastOffset + Vector3.up * topRaycastLength,
                        transform.position + innerRaycastOffset + Vector3.up * topRaycastLength + Vector3.right * topRaycastLength);
    }
}
