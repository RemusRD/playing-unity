using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public class PrototypeHeroDemo : MonoBehaviour
{
    [Header("Variables")] [SerializeField] float m_maxSpeed = 4.5f;
    [SerializeField] float m_jumpForce = 7.5f;
    [SerializeField] bool m_hideSword = false;
    [Header("Effects")] [SerializeField] GameObject m_RunStopDust;
    [SerializeField] GameObject m_JumpDust;
    [SerializeField] GameObject m_LandingDust;

    private Animator m_animator;
    private Rigidbody2D m_body2d;
    private AudioSource m_audioSource;
    private AudioManager_PrototypeHero m_audioManager;
    private bool groundedLastFrame = false;
    private bool m_moving = false;
    private bool wallSliding = false;
    private int m_facingDirection = 1;
    private float m_disableMovementTimer = 0.0f;
    private BoxCollider2D boxCollider;
    [SerializeField] LayerMask groundLayer;

    private float coyoteTime;
    private float jumpBufferTime;
    private static readonly int Grounded = Animator.StringToHash("Grounded");
    private static readonly int AnimState = Animator.StringToHash("AnimState");
    private static readonly int AirSpeedY = Animator.StringToHash("AirSpeedY");
    private static readonly int Jump = Animator.StringToHash("Jump");
    private Vector2 wallJumpAngle = new Vector2(1, 3);
    private bool wallJumping = false;
    private float blockedInput = 0f;
    private float dashJump = 0f;
    private float dashCooldown = 0f;
    private bool attacking = false;
    private int currentAttack = 1;
    private PolygonCollider2D swordCollider;
    private int enemyLayer;
    private SimpleFlash flash;
    private Dictionary<string, AnimationClip> clips;
    private float baseDamage;
    private Vector2 lastGroundedPosition;
    public GameObject circlePrefab;

    private List<GameObject> companions;
    // Use this for initialization
    void Start()
    {
        wallJumpAngle.Normalize();
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        m_audioSource = GetComponent<AudioSource>();
        m_audioManager = AudioManager_PrototypeHero.instance;
        boxCollider = GetComponent<BoxCollider2D>();
        groundLayer = LayerMask.GetMask("Ground");
        swordCollider = GetComponentInChildren<PolygonCollider2D>();
        enemyLayer = LayerMask.NameToLayer("Enemies");
        flash = GetComponent<SimpleFlash>();
        clips = m_animator.runtimeAnimatorController.animationClips.ToDictionary(c => c.name, c => c);
        baseDamage = 10f;
        circlePrefab = Resources.Load("Circle") as GameObject;
        
        GameObject circle = Instantiate(circlePrefab, gameObject.transform.position + Vector3.right, Quaternion.identity);
        companions = new List<GameObject>();
        companions.Add(circle);
    }

    // Update is called once per frame
    private void Update()
    {
        //CoolDowns();

        //if (blockedInput > 0) return;

        //Animations();
        
        Movement();
        // if (dashJump > 0 && false)
        // {
        //     dashJump -= Time.deltaTime;
        //     return;
        // }
        //
        // blockedInput -= Time.deltaTime;
        // if (blockedInput > 0f && false) return;
        //
        // IsGrounded();
        // if (groundedLastFrame)
        // {
        //     coyoteTime = 0.2f;
        // }
        // else
        // {
        //     coyoteTime -= Time.deltaTime;
        // }
        //
        // if (Input.GetButtonDown("Jump"))
        // {
        //     jumpBufferTime = 0.1f;
        // }
        // else
        // {
        //     jumpBufferTime -= Time.deltaTime;
        // }
        //
        // if (!groundedLastFrame && IsGrounded())
        // {
        //     groundedLastFrame = true;
        //     m_animator.Play("Landing");
        // }
        //
        // if (groundedLastFrame && !IsGrounded())
        // {
        //     groundedLastFrame = false;
        //     m_animator.Play("Jump");
        // }
        //
        // //Attack();
        // if (!attacking)
        // {
        //     if(groundedLastFrame)
        //     {
        //         lastGroundedPosition = transform.position + Vector3.left * m_facingDirection;
        //     }
        //     //DashJump();
        //     m_animator.SetFloat(AirSpeedY, m_body2d.velocity.y);
        //     //HandleJump();
        //     //allSlide();
        //     //WallJump();
        // }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.layer == enemyLayer)
        {
            flash.Flash();
        }
    }

    private void Animations()
    {
        if (attacking)
        {
        }
    }

    private void Attack()
    {
        //check if attack button is hold down
        attacking = Input.GetButton("Attack");
        if (attacking)
        {
            //stop the character from moving
            m_body2d.velocity = Vector2.zero;
            m_animator.Play("attack" + currentAttack);
            blockedInput = clips["attack" + currentAttack].length - 0.1f;
            currentAttack += 1;
        }

        if (currentAttack > 3)
        {
            currentAttack = 1;
        }
    }

    private void CoolDowns()
    {
        dashCooldown -= Time.deltaTime;
        blockedInput -= Time.deltaTime;
    }

    private void DashJump()
    {
        if (Input.GetButtonDown("Dash") && dashCooldown < 0)
        {
            //move y velocity 
            m_body2d.velocity = new Vector2(m_body2d.velocity.x, 0);
            m_body2d.AddForce(new Vector2(m_facingDirection * m_maxSpeed * 3, 0), ForceMode2D.Impulse);
            dashJump = 0.1f;
            dashCooldown = 1f;
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.gameObject.layer == enemyLayer && attacking)
        {
            col.gameObject.GetComponent<DamagableScript>().Damage(baseDamage);
            //spawn a circle in the center of the enemys collider2d
            GameObject circle = Instantiate(circlePrefab, col.bounds.center, Quaternion.identity);


        }
    }

    private void HandleJump()
    {
        if (!wallSliding && Input.GetButtonDown("Jump") && jumpBufferTime > 0f && coyoteTime > 0f)
        {
            m_animator.Play(Jump);
            groundedLastFrame = false;
            m_animator.SetBool(Grounded, groundedLastFrame);
            m_body2d.velocity = new Vector2(m_body2d.velocity.x, m_jumpForce);
            jumpBufferTime = 0f;
        }

        if (!wallSliding && Input.GetButtonUp("Jump") && (m_body2d.velocity.y > 0))
        {
            var velocity = m_body2d.velocity;
            velocity = new Vector2(velocity.x, velocity.y * 0.5f);
            m_body2d.velocity = velocity;
            coyoteTime = 0f;
        }
    }
    

    private void Movement()
    {
        var inputX = Input.GetAxisRaw("Horizontal");
        if (inputX != 0)
        {
            m_moving = true;
            m_facingDirection = (int) Mathf.Sign(inputX);
            GetComponent<SpriteRenderer>().flipX = m_facingDirection != 1;
        }
        else
        {
            m_moving = false;
        }

        // if (groundedLastFrame)
        // {
        //     if (inputX != 0)
        //     {
        //         m_animator.Play("Run");
        //     }
        //     else
        //     {
        //         m_animator.Play("Idle");
        //     }
        // }

        if (!(blockedInput > 0))
        {
            if (groundedLastFrame)
            {
                m_body2d.AddForce(new Vector2(m_maxSpeed * inputX, 0));
            }
            else if (!wallSliding && inputX != 0)
            {
                m_body2d.AddForce(new Vector2(m_maxSpeed * inputX, 0));
            }

            if (Mathf.Abs(m_body2d.velocity.x) > m_maxSpeed)
            {
                m_body2d.velocity = new Vector2(inputX * m_maxSpeed, m_body2d.velocity.y);
            }


            if (inputX != 0 && groundedLastFrame)
            {
                m_moving = true;
                m_facingDirection = (int) Mathf.Sign(inputX);
                GetComponent<SpriteRenderer>().flipX = m_facingDirection != 1;
                m_body2d.velocity = new Vector2(inputX * m_maxSpeed, m_body2d.velocity.y);
            }
            else if (groundedLastFrame && !wallSliding)
            {
                m_body2d.velocity = new Vector2(inputX * m_maxSpeed, m_body2d.velocity.y);
                m_moving = false;
                m_animator.SetInteger(AnimState, 0);
            }
            else if (!groundedLastFrame && !wallSliding)
            {
                m_body2d.velocity = new Vector2(inputX * m_maxSpeed, m_body2d.velocity.y);
            }
        }
    }

    bool IsGrounded()
    {
        var bounds = boxCollider.bounds;
        var hit = Physics2D.BoxCast(bounds.center, bounds.size, 0f, Vector2.down, 0.1f, groundLayer);
        var isGrounded = hit.collider != null;
        return isGrounded;
    }

    void Flip()
    {
        if (!wallSliding)
        {
            m_facingDirection *= -1;
            GetComponent<SpriteRenderer>().flipX = m_facingDirection == 1;
        }
    }

    void WallJump()
    {
        if (wallSliding && Input.GetButtonDown("Jump") && jumpBufferTime > 0f && coyoteTime > 0f)
        {
            m_facingDirection *= -1;
            m_body2d.velocity = new Vector2(m_facingDirection * m_maxSpeed, m_jumpForce);
            GetComponent<SpriteRenderer>().flipX = m_facingDirection != 1;
            blockedInput = 0.18f;
        }
    }

    void WallSlide()
    {
        var inputX = Input.GetAxisRaw("Horizontal");

        var bounds = boxCollider.bounds;
        var hit = Physics2D.BoxCast(bounds.center, bounds.size, 0.0f, new Vector2(inputX, 0.0f), 0.1f,
            groundLayer);
        if (hit.collider != null && !groundedLastFrame)
        {
            wallSliding = true;
            m_body2d.velocity = new Vector2(m_body2d.velocity.x, -1.0f);
            jumpBufferTime = 0.2f;
            coyoteTime = 0.2f;
        }
        else
        {
            wallSliding = false;
        }
    }

    void SpawnDustEffect(GameObject dust, float dustXOffset = 0)
    {
        if (dust == null) return;
        // Set dust spawn position
        var dustSpawnPosition = transform.position + new Vector3(dustXOffset * m_facingDirection, 0.0f, 0.0f);
        var newDust = Instantiate(dust, dustSpawnPosition, Quaternion.identity);
        // Turn dust in correct X direction
        newDust.transform.localScale = newDust.transform.localScale.x * new Vector3(m_facingDirection, 1, 1);
    }

    // Animation Events
    // These functions are called inside the animation files
    void AE_runStop()
    {
        m_audioManager.PlaySound("RunStop");
        // Spawn Dust
        float dustXOffset = 0.6f;
        SpawnDustEffect(m_RunStopDust, dustXOffset);
    }

    void AE_footstep()
    {
        m_audioManager.PlaySound("Footstep");
    }

    void AE_Jump()
    {
        m_audioManager.PlaySound("Jump");
        // Spawn Dust
        SpawnDustEffect(m_JumpDust);
    }

    void AE_Landing()
    {
        m_audioManager.PlaySound("Landing");
        // Spawn Dust
        SpawnDustEffect(m_LandingDust);
    }

    public void Respawn()
    {
        transform.position = lastGroundedPosition;
        flash.Flash();
    }
}
