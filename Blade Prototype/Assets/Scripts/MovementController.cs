using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] private float speed;
    [SerializeField] public float jumpStrength;
    [SerializeField] public float extraJumpStrength;
    [SerializeField] public float wallJumpStrength;
    [SerializeField] public float jumpTime;
    [SerializeField] public float coyoteTime;
    private float jumpTimeCounter;
    private float horizontalInput;

    [SerializeField] private Transform feetPos;
    [SerializeField] private Transform sidePosL;
    [SerializeField] private Transform sidePosR;
    [SerializeField] private float groundCheckRadius;
    [SerializeField] private float wallCheckRadius;
    [SerializeField] public LayerMask groundLayer;
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isOnWall;
    [SerializeField] private bool canWallJump;
    [SerializeField] private int extraJumpCount;
    [SerializeField] private int maxExtraJump;
    private bool boostingJump;
    private float? lastGroundedTime;
    private float? jumpPressedTime;
    private float? lastOnWallTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        extraJumpCount = maxExtraJump;
    }

    private void FixedUpdate()
    {
        HorizontalMovementInputCheck();
    }

    void Update()
    {
        GroundCheck();
        WallCheck();
        JumpInputCheck();
    }

    private void HorizontalMovementInputCheck()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        rb.velocity = new Vector2(horizontalInput * speed, rb.velocity.y);
    }

    private void GroundCheck()
    {
        isGrounded = Physics2D.OverlapCircle(feetPos.position, groundCheckRadius, groundLayer);
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            canWallJump = true;
            extraJumpCount = maxExtraJump;
        }
    }

    private void WallCheck()
    {
        isOnWall = Physics2D.OverlapCircle(sidePosL.position, wallCheckRadius, groundLayer) || Physics2D.OverlapCircle(sidePosR.position, wallCheckRadius, groundLayer);
        if (isOnWall)
        {
            lastOnWallTime = Time.time;
        }
    }

    private void JumpInputCheck()
    {
        //the jump btn
        if (Input.GetKeyDown(KeyCode.Space))
        {
            //saving this for coyote time
            jumpPressedTime = Time.time;

            if (!NormalJump())
            {
                if (!WallJump())
                    ExtraJump();
            }
        }

        if (!NormalJump())
        {
            WallJump();
        }

        //gain more heights if jump btn is held
        if (Input.GetKey(KeyCode.Space) && jumpTimeCounter > 0 && boostingJump)
        {
            rb.velocity = Vector2.up * jumpStrength;
            jumpTimeCounter -= Time.deltaTime;
        }

        //no moar height gain upon jump btn release
        if (Input.GetKeyUp(KeyCode.Space))
        {
            boostingJump = false;
        }
    }

    private bool NormalJump()
    {
        if (Time.time - lastGroundedTime <= coyoteTime)
        {
            if (Time.time - jumpPressedTime <= coyoteTime)
            {
                rb.velocity = Vector2.up * jumpStrength;
                jumpTimeCounter = jumpTime;
                boostingJump = true;
                jumpPressedTime = null;
                lastGroundedTime = null;
                return true;
            }
        }

        return false;
    }

    private bool WallJump()
    {
        if (canWallJump && Time.time - lastOnWallTime <= coyoteTime)
        {
            if (Time.time - jumpPressedTime <= coyoteTime)
            {
                rb.velocity = Vector2.up * wallJumpStrength;
                canWallJump = false;
                boostingJump = true;
                jumpTimeCounter = jumpTime;

                jumpPressedTime = null;
                lastOnWallTime = null;
                return true;
            }
        }
        
        return false;
    }

    private void ExtraJump()
    {
        if (extraJumpCount > 0)
        {
            rb.velocity = Vector2.up * extraJumpStrength;
            jumpTimeCounter = jumpTime;
            boostingJump = true;
            extraJumpCount--;
        }
    }
}
