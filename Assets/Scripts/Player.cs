using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private enum AnimState
    {
        Stand,
        Run,
        Jump,
        DoubleJump,
        Fall,
        WallSlide
    }

    private const float runAcceleration = 12;
    public const float maxRunSpeed = 7;
    private const float jumpForce = 10.2f;
    private const float doubleJumpForce = 6;
    private const float gravityForce = 25;
    private const float maxFallSpeed = 40;
    private const float wallSlideGravityForce = 10;
    private const float wallSlideMaxFallSpeed = 1;
    private const float pitchVariation = 0.15f;

    private Rigidbody2D rb;
    private EdgeCollider2D ec;

    private bool jumpQueued = false;
    private bool canDoubleJump = true;
    private float xForce = 0;

    private bool canJump = false;
    private bool wasOnGround = false;
    private Coroutine crtCancelQueuedJump;
    private const float jumpBufferTime = 0.1f; //time before hitting ground a jump will still be queued
    private const float jumpGraceTime = 0.1f; //time after leaving ground player can still jump (coyote time)
    
    private const float wallJumpGraceTime = 0.1f; //time after leaving wall player can still jump
    private int againstWall = 0;
    private bool wallSliding = false;
    private Coroutine crtLeaveWall;

    private bool finishedLevel = false;
    private bool invincible = false;

    private CameraShake cameraShake;
    private Persistent persistent;

    private const float runFrameTime = 0.1f;
    private SpriteRenderer sr;
    private AnimState animState = AnimState.Stand;
    private int animFrame = 0;
    private float frameTime; //max time of frame
    private float frameTimer; //goes from frameTime down to 0
    public bool facingLeft = false; //for animation (images face right)
    public Sprite standSprite;
    public Sprite[] runSprites;
    public Sprite jumpSprite;
    public Sprite doubleJumpSprite;
    public Sprite fallSprite;
    public Sprite wallSlideSprite;

    private AudioSource audioSource;
    public AudioClip jumpSound;
    public AudioClip doubleJumpSound;
    public AudioClip landSound;
    
    public GameObject playerKilledPrefab;
    public GameObject loadingScreenPrefab;

    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        ec = gameObject.GetComponent<EdgeCollider2D>();
        sr = gameObject.GetComponent<SpriteRenderer>();
        audioSource = gameObject.GetComponent<AudioSource>();
        cameraShake = FindObjectOfType<CameraShake>();

        Persistent[] persistents = FindObjectsOfType<Persistent>();
        foreach (Persistent p in persistents)
        {
            if (!p.destroying)
            {
                persistent = p;
                break;
            }
        }
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            TryStopCoroutine(crtCancelQueuedJump);
            jumpQueued = true;
            crtCancelQueuedJump = StartCoroutine(CancelQueuedJump());
        }

        sr.flipX = facingLeft;
        AdvanceAnim();
        sr.sprite = GetAnimSprite();
    }

    private Collider2D RaycastTiles(Vector2 startPoint, Vector2 endPoint)
    {
        RaycastHit2D hit = Physics2D.Raycast(startPoint, endPoint - startPoint, Vector2.Distance(startPoint, endPoint), LayerMask.GetMask("Tiles"));
        return hit.collider;
    }

    private bool CheckSide(int point0, int point1, Vector2 direction)
    {
        Vector2 startPoint = rb.position + ec.points[point0] + direction * 0.02f;
        Vector2 endPoint = rb.position + ec.points[point1] + direction * 0.02f;
        Collider2D collider = RaycastTiles(startPoint, endPoint);
        return collider != null;
    }

    private void FixedUpdate()
    {
        float xInput = Input.GetAxis("Horizontal");
        float prevXVel = rb.velocity.x;
        float xVel;
        float dx = runAcceleration * Time.fixedDeltaTime * xInput;
        if (prevXVel != 0 && Mathf.Sign(xInput) != Mathf.Sign(prevXVel))
        {
            xVel = 0;
        }
        else
        {
            xVel = prevXVel + dx;
            float speedCap = Mathf.Abs(xInput * maxRunSpeed);
            xVel = Mathf.Clamp(xVel, -speedCap, speedCap);
        }

        if (xForce != 0)
        {
            //if not moving: keep xForce
            if (xInput == 0)
            {
                xVel = xForce;
            }
            else
            {
                if (Mathf.Sign(xInput) == Mathf.Sign(xForce))
                {
                    //moving in same direction
                    if (Mathf.Abs(xVel) >= Mathf.Abs(xForce))
                    {
                        //xVel has higher magnitude: set xForce to 0 (replace little momentum push)
                        xForce = 0;
                    }
                    else
                    {
                        //xForce has higher magnitude: set xVel to xForce (pushed by higher momentum)
                        xVel = xForce;
                    }
                }
                else
                {
                    //moving in other direction
                    //decrease xForce by dx (stopping at 0)
                    float prevSign = Mathf.Sign(xForce);
                    xForce += dx;
                    if (Mathf.Sign(xForce) != prevSign)
                    {
                        xForce = 0;
                    }
                    xVel = xForce;
                }
            }
        }

        if (xInput != 0)
        {
            facingLeft = xInput < 0;
        }
        else if (xVel != 0)
        {
            //facingLeft = xVel < 0;
        }

        float yVel;

        bool onGround = CheckSide(4, 3, Vector2.down); //BoxcastTiles(Vector2.down, 0.15f) != null;
        bool onCeiling = CheckSide(1, 2, Vector2.up); //BoxcastTiles(Vector2.up, 0.15f) != null;

        if (onGround)
        {
            canJump = true;
            canDoubleJump = true;

            xForce = 0;
            TryStopCoroutine(crtLeaveWall);
            againstWall = 0;

            if (rb.velocity.y < 0)
            {
                PlaySound(landSound);
            }
            yVel = 0;

            SetAnimState(xVel == 0 ? AnimState.Stand : AnimState.Run);
        }
        else
        {
            if (wasOnGround)
            {
                StartCoroutine(LeaveGround());
            }

            if (CheckSide(0, 1, Vector2.left))
            {
                TryStopCoroutine(crtLeaveWall);
                againstWall = -1;
                wallSliding = true;
            }
            else if (CheckSide(2, 3, Vector2.right))
            {
                TryStopCoroutine(crtLeaveWall);
                againstWall = 1;
                wallSliding = true;
            }
            else if (wallSliding)
            {
                crtLeaveWall = StartCoroutine(LeaveWall());
                wallSliding = false;
            }

            if (wallSliding && rb.velocity.y < 0)
            {
                yVel = Mathf.Max(rb.velocity.y - wallSlideGravityForce * Time.fixedDeltaTime, -wallSlideMaxFallSpeed);
            }
            else
            {
                yVel = Mathf.Max(rb.velocity.y - gravityForce * Time.fixedDeltaTime, -maxFallSpeed);
            }

            if (yVel < 0)
            {
                if (wallSliding)
                {
                    facingLeft = againstWall > 0;
                    SetAnimState(AnimState.WallSlide);
                }
                else
                {
                    SetAnimState(AnimState.Fall);
                }
            }

        }
        wasOnGround = onGround;

        if (onCeiling && yVel > 0)
        {
            yVel = 0;
            PlaySound(landSound);
        }

        //if on ground or just left it: first jump
        //if can double jump: second jump
        //else: keep queued
        if (jumpQueued)
        {
            if (canJump)
            {
                StopCancelQueuedJump();
                jumpQueued = false;
                canJump = false;
                xForce = 0;
                yVel = jumpForce; //Mathf.Max(jumpForce, yVel + jumpForce);
                PlaySound(jumpSound);
                SetAnimState(AnimState.Jump);
            }
            else if (canDoubleJump)
            {
                StopCancelQueuedJump();
                jumpQueued = false;
                canDoubleJump = false;
                xForce = 0;
                yVel = doubleJumpForce; //Mathf.Max(doubleJumpForce, yVel + doubleJumpForce);
                PlaySound(doubleJumpSound);
                SetAnimState(AnimState.DoubleJump);
            }
        }

        Vector2 vel = new Vector2(xVel, yVel);
        rb.velocity = vel;
        rb.MovePosition(rb.position + vel * Time.fixedDeltaTime);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!gameObject.activeSelf || finishedLevel) return;

        GameObject collider = collision.collider.gameObject;

        /*if (collider.CompareTag("BarrierRight"))
        {
            persistent.gems += levelGems;
            levelGems = 0;
            finishedLevel = true;
            //Instantiate(loadingScreenPrefab);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }*/

        if (collider.layer == LayerMask.NameToLayer("Tiles"))
        {
            if (collision.GetContact(0).normal.x != 0)
            {
                //against wall, not ceiling
                if (xForce != 0)
                {
                    PlaySound(landSound);
                }
                xForce = 0;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!gameObject.activeSelf) return;

        GameObject collider = collision.gameObject;

        /*Gem gem = collider.GetComponent<Gem>();
        if (gem != null)
        {
            Destroy(collider);
            PlaySound(collectGemSound);
            Instantiate(collectParticlePrefab, collider.transform.position, Quaternion.identity);
            levelGems++;
        }*/
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!gameObject.activeSelf) return;

        GameObject collider = collision.gameObject;

        /*if (collider.CompareTag("Damage"))
        {
            if (!hurtInvincible && !invincible)
            {
                Damage();
            }
        }*/
    }

    private Sprite GetAnimSprite()
    {
        switch (animState)
        {
            case AnimState.Stand:
                return standSprite;
            case AnimState.Run:
                return runSprites[animFrame];
            case AnimState.Jump:
                return jumpSprite;
            case AnimState.DoubleJump:
                return doubleJumpSprite;
            case AnimState.Fall:
                return fallSprite;
            case AnimState.WallSlide:
                return wallSlideSprite;
        }
        return standSprite;
    }

    private void TryStopCoroutine(Coroutine crt)
    {
        if (crt != null)
        {
            StopCoroutine(crt);
        }
    }

    private void StopCancelQueuedJump()
    {
        TryStopCoroutine(crtCancelQueuedJump);
    }

    private IEnumerator CancelQueuedJump()
    {
        yield return new WaitForSeconds(jumpBufferTime);
        jumpQueued = false;
    }

    private IEnumerator LeaveGround()
    {
        yield return new WaitForSeconds(jumpGraceTime);
        canJump = false;
    }

    private IEnumerator LeaveWall()
    {
        yield return new WaitForSeconds(wallJumpGraceTime);
        againstWall = 0;
    }

    private void SetAnimState(AnimState state)
    {
        animState = state;
    }

    private void AdvanceAnim()
    {
        if (animState == AnimState.Run)
        {
            frameTime = runFrameTime;
            AdvanceFrame(runSprites.Length);
        }
        else
        {
            animFrame = 0;
            frameTimer = frameTime;
        }
    }

    private void AdvanceFrame(int numFrames)
    {
        if (animFrame >= numFrames)
        {
            animFrame = 0;
        }

        frameTimer -= Time.deltaTime;
        if (frameTimer <= 0)
        {
            frameTimer = frameTime;
            animFrame = (animFrame + 1) % numFrames;
        }
    }

    public void PlaySound(AudioClip sound, bool randomizePitch = false)
    {
        if (randomizePitch)
        {
            audioSource.pitch = Random.Range(1 - pitchVariation, 1 + pitchVariation);
        }
        else
        {
            audioSource.pitch = 1;
        }
        audioSource.PlayOneShot(sound);
    }

    public void SetInvincible()
    {
        invincible = true;
    }
}
