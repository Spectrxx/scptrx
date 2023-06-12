using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{   

    Rigidbody2D rb;
    Animator animator;
    [SerializeField] public Collider2D standingCollider, crouchingCollider;
    [SerializeField] public Transform groundCheckCollider;
    [SerializeField] public Transform overheadCheckCollider;
    [SerializeField] public LayerMask groundLayer;
    [SerializeField] public Transform wallCheckCollider;
    [SerializeField] public LayerMask wallLayer;
    Vector3 originalPosition;
    

    const float groundCheckRadius = 0.2f;
    const float overheadCheckRadius = 0.2f;
    const float wallCheckRadius = 0.2f;
    [SerializeField] float speed = 1;
    [SerializeField] float jumpPower = 500;
    [SerializeField] float slideFactor = 0.2f;
    [SerializeField] int totalJumps;
    int availableJumps;
    float horizontalValue;
    float runSpeedModifier = 2.0f;
    float crouchSpeedModifier = 0.5f;

    bool isGrounded;
    bool isRunning;
    bool facingRight = true;
    bool crouchPressed;
    bool multipleJump;
    bool isSliding;
    bool coyoteJump;
    bool isDead = false;

    void Awake()
    {
        availableJumps = totalJumps;

        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        originalPosition = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y, gameObject.transform.position.z);
    }

    void Update()
    {
        //if (CanMove() == false) 
        //    return;       

        horizontalValue = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.LeftShift))
            isRunning = true;
        if (Input.GetKeyUp(KeyCode.LeftShift))
            isRunning = false;

        //Jump
        if (Input.GetButtonDown("Jump"))
        {
            Jump();
        }
        

        //Crouch
        if (Input.GetButtonDown("Crouch"))
        {
            crouchPressed = true;
        }
        else if (Input.GetButtonUp("Crouch"))
        {
            crouchPressed = false;
        }

        animator.SetFloat("yVelocity", rb.velocity.y);

        WallCheck();
    }

    void FixedUpdate()
    {
        GroundCheck();

        Move(horizontalValue, crouchPressed);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(groundCheckCollider.position, groundCheckRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(overheadCheckCollider.position, overheadCheckRadius);
    }

    bool CanMove()
    {
        bool can = true;

        if (FindObjectOfType<InteractionSystem>().isExamining)
        {
            can = false;
        }
        if (isDead)
        {
            can = false;
        }

        return can;
    }

    void GroundCheck()
    {
        bool wasGrounded = isGrounded;
        isGrounded = false;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheckCollider.position, groundCheckRadius, groundLayer);
        if (colliders.Length > 0)
        {
            isGrounded = true;
            if (!wasGrounded)
            {
                availableJumps = totalJumps;
                multipleJump = false;
            }
        }
        else
        {
            if (wasGrounded)
            {
                StartCoroutine(CoyoteJumpDelay());
            }
        }

        animator.SetBool("Jump", !isGrounded);
    }

    void WallCheck()
    {
        if (Physics2D.OverlapCircle(wallCheckCollider.position, wallCheckRadius, wallLayer)
            && Mathf.Abs(horizontalValue) > 0
            && rb.velocity.y < 0
            && !isGrounded)
        {
            if (!isSliding)
            {
                availableJumps = totalJumps;
                multipleJump = false;
            }

            Vector2 v = rb.velocity;
            v.y = -slideFactor;
            rb.velocity = v;
            isSliding = true;

            if (Input.GetButtonDown("Jump"))
            {
                availableJumps--;

                rb.velocity = Vector2.up * jumpPower;
                animator.SetBool("Jump", true);
            }
        }
        else
        {
            isSliding = false;
        }
    }

    IEnumerator CoyoteJumpDelay()
    {
        coyoteJump = true;
        yield return new WaitForSeconds(0.2f);
        coyoteJump = false;
    }

    void Jump()
    {
        if (isGrounded)
        {  
            multipleJump = true;
            availableJumps--;

            rb.velocity = Vector2.up * jumpPower;
            animator.SetBool("Jump", true);
        }
        else
        {
            if (coyoteJump)
            {
                multipleJump = true;
                availableJumps--;

                rb.velocity = Vector2.up * jumpPower;
                animator.SetBool("Jump", true);
            }

            if (multipleJump && availableJumps > 0)
            {
                availableJumps--;

                rb.velocity = Vector2.up * jumpPower;
                animator.SetBool("Jump", true);
            }
        }
    }

    void Move(float dir, bool crouchFlag)
    {
        #region Crouch

        if (!crouchFlag)
        {
            if (Physics2D.OverlapCircle(overheadCheckCollider.position, overheadCheckRadius, groundLayer))
            {
                crouchFlag = true;
            }
        }

        animator.SetBool("Crouch", crouchFlag);
        standingCollider.enabled = !crouchFlag;
        crouchingCollider.enabled = crouchFlag;
        #endregion

        #region Move & Run
        float xVal = dir * speed * 100 * Time.fixedDeltaTime;

        if (isRunning)
        {
            xVal *= runSpeedModifier;
        }

        if (crouchFlag)
        {
            xVal *= crouchSpeedModifier;
        }

        Vector2 targetVelocity = new Vector2(xVal, rb.velocity.y);
        rb.velocity = targetVelocity;

        if (facingRight && dir < 0)
        {
            transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
            facingRight = false;
        }
        //If looking left and press right, turn to the right
        else if (!facingRight && dir > 0)
        {
            transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
            facingRight = true;
        }

        animator.SetFloat("xVelocity", Mathf.Abs(rb.velocity.x));
        #endregion
    }

    public void Die()
    {
        isDead = true;
        FindObjectOfType<LevelManager>().RestartScene();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.tag == "Enemy")
        {
            gameObject.transform.position = originalPosition;
        }
    }           
}
