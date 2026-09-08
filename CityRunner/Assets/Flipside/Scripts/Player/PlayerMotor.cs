using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// Physics-driven 2D platformer motor with a flippable gravity direction.
    /// The world stays upright; only this body's gravity changes sign.
    /// Gravity is applied manually so the flip can ease over a short transition.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("Run")]
        [SerializeField] float moveSpeed = 8f;
        [SerializeField] float groundAcceleration = 90f;
        [SerializeField] float airAcceleration = 60f;

        [Header("Jump")]
        [SerializeField] float jumpHeight = 2.6f;
        [SerializeField] float gravityStrength = 40f;
        [SerializeField] float fallGravityMultiplier = 1.4f;
        [SerializeField] float jumpCutGravityMultiplier = 2.5f;
        [SerializeField] float maxFallSpeed = 22f;
        [SerializeField] float coyoteTime = 0.1f;
        [SerializeField] float jumpBufferTime = 0.12f;

        [Header("Gravity flip")]
        [Tooltip("Seconds over which gravity eases from the old direction to the new one.")]
        [SerializeField] float flipTransitionTime = 0.2f;
        [Tooltip("Extra seconds after a flip completes before another flip is accepted.")]
        [SerializeField] float flipCooldown = 0.05f;
        [Tooltip("Flips allowed while airborne before touching a surface again. Prevents hovering by spamming flip.")]
        [SerializeField] int airFlipsAllowed = 1;

        [Header("Ground check")]
        [SerializeField] LayerMask groundMask = ~0;
        [SerializeField] float groundCheckDistance = 0.08f;

        /// <summary>+1: gravity pulls toward -Y (normal). -1: gravity pulls toward +Y (inverted).</summary>
        public int GravitySign { get; private set; } = 1;
        public int FacingSign { get; private set; } = 1;
        public bool IsGrounded { get; private set; }
        public bool IsFlipping => flipTimer < flipTransitionTime;
        public float FlipProgress => flipTransitionTime <= 0f ? 1f : Mathf.Clamp01(flipTimer / flipTransitionTime);
        public Vector2 GravityDirection => new Vector2(0f, -GravitySign);
        public Vector2 Velocity => body.linearVelocity;
        public bool MovementEnabled { get; set; } = true;
        public int AirFlipsRemaining { get; private set; }
        /// <summary>Set by gravity-lock zones: flips are refused while true.</summary>
        public bool FlipLocked { get; set; }
        public bool CanFlip => MovementEnabled && !FlipLocked && flipCooldownTimer <= 0f && (IsGrounded || coyoteTimer > 0f || AirFlipsRemaining > 0);

        /// <summary>Overrides the current velocity (portals keep momentum across the jump).</summary>
        public void SetVelocity(Vector2 velocity)
        {
            body.linearVelocity = velocity;
        }

        public event System.Action<int> GravityFlipped;
        public event System.Action Jumped;
        public event System.Action Landed;

        Rigidbody2D body;
        BoxCollider2D box;
        ContactFilter2D groundFilter;
        readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];

        float moveInput;
        bool jumpHeld;
        bool flipRequested;
        float jumpBufferTimer;
        float coyoteTimer;
        float flipTimer;
        float flipCooldownTimer;
        int previousGravitySign = 1;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep; // a sleeping body stops reporting new trigger contacts
            groundFilter = new ContactFilter2D { useLayerMask = true, layerMask = groundMask, useTriggers = false };
            flipTimer = flipTransitionTime;
            AirFlipsRemaining = airFlipsAllowed;
        }

        // ---- Input API (called by a keyboard reader, tests, or scripted sequences) ----

        public void SetMoveInput(float horizontal)
        {
            moveInput = Mathf.Clamp(horizontal, -1f, 1f);
        }

        public void RequestJump()
        {
            jumpBufferTimer = jumpBufferTime;
        }

        public void SetJumpHeld(bool held)
        {
            jumpHeld = held;
        }

        public void RequestFlip()
        {
            flipRequested = true;
        }

        /// <summary>Instantly places the player with a given gravity direction and no transition. Used by respawn.</summary>
        public void Teleport(Vector2 position, int gravitySign, bool announceGravityChange = false)
        {
            int newSign = gravitySign >= 0 ? 1 : -1;
            bool changed = newSign != GravitySign;
            GravitySign = newSign;
            previousGravitySign = GravitySign;
            // Every gravity change goes through the same event, so visuals, audio and the
            // flip wave stay in sync no matter what caused it (flip key, portal, respawn).
            if (changed && announceGravityChange) GravityFlipped?.Invoke(GravitySign);
            body.position = position;
            transform.position = position;
            body.linearVelocity = Vector2.zero;
            body.WakeUp();
            flipTimer = flipTransitionTime;
            flipCooldownTimer = 0f;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            flipRequested = false;
            IsGrounded = false;
            AirFlipsRemaining = airFlipsAllowed;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector2 velocity = body.linearVelocity;

            flipTimer += dt;
            flipCooldownTimer -= dt;
            jumpBufferTimer -= dt;

            bool wasGrounded = IsGrounded;
            IsGrounded = !IsFlipping && CheckGround();
            coyoteTimer = IsGrounded ? coyoteTime : coyoteTimer - dt;
            if (IsGrounded)
            {
                AirFlipsRemaining = airFlipsAllowed;
            }
            if (IsGrounded && !wasGrounded)
            {
                Landed?.Invoke();
            }

            if (flipRequested)
            {
                flipRequested = false;
                if (CanFlip)
                {
                    bool fromSurface = IsGrounded || coyoteTimer > 0f;
                    if (!fromSurface) AirFlipsRemaining--;
                    StartFlip(ref velocity);
                }
            }

            // Horizontal: momentum is preserved through flips; only input changes it.
            float targetSpeed = MovementEnabled ? moveInput * moveSpeed : 0f;
            float acceleration = IsGrounded ? groundAcceleration : airAcceleration;
            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, acceleration * dt);
            if (MovementEnabled && Mathf.Abs(moveInput) > 0.01f)
            {
                FacingSign = moveInput > 0f ? 1 : -1;
            }

            // Jump: buffered input plus coyote time, always away from the current floor.
            if (MovementEnabled && jumpBufferTimer > 0f && coyoteTimer > 0f && !IsFlipping)
            {
                float jumpSpeed = Mathf.Sqrt(2f * gravityStrength * jumpHeight);
                velocity.y = GravitySign * jumpSpeed; // away from the floor: +Y under normal gravity
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                IsGrounded = false;
                Jumped?.Invoke();
            }

            // Gravity, eased during a flip.
            float upwardSpeed = velocity.y * GravitySign; // positive while moving away from the current floor
            float gravityMultiplier = 1f;
            if (!IsFlipping)
            {
                if (upwardSpeed < 0f) gravityMultiplier = fallGravityMultiplier;
                else if (upwardSpeed > 0f && !jumpHeld) gravityMultiplier = jumpCutGravityMultiplier;
            }
            velocity.y += CurrentSignedGravity() * gravityMultiplier * dt;

            float downwardSpeed = -velocity.y * GravitySign;
            if (downwardSpeed > maxFallSpeed)
            {
                velocity.y = -GravitySign * maxFallSpeed;
            }

            body.linearVelocity = velocity;
        }

        void StartFlip(ref Vector2 velocity)
        {
            previousGravitySign = GravitySign;
            GravitySign = -GravitySign;
            velocity.y = 0f; // Vertical velocity resets so the new fall starts clean.
            flipTimer = 0f;
            flipCooldownTimer = flipTransitionTime + flipCooldown;
            coyoteTimer = 0f;
            IsGrounded = false;
            GravityFlipped?.Invoke(GravitySign);
        }

        /// <summary>Signed Y acceleration. Negative pulls toward -Y.</summary>
        float CurrentSignedGravity()
        {
            float eased = Mathf.SmoothStep(0f, 1f, FlipProgress);
            float sign = Mathf.Lerp(previousGravitySign, GravitySign, eased);
            return -gravityStrength * sign;
        }

        bool CheckGround()
        {
            int count = box.Cast(GravityDirection, groundFilter, groundHits, groundCheckDistance);
            for (int i = 0; i < count; i++)
            {
                // Only surfaces facing against gravity count as floor (no wall clinging).
                if (Vector2.Dot(groundHits[i].normal, -GravityDirection) > 0.7f)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
