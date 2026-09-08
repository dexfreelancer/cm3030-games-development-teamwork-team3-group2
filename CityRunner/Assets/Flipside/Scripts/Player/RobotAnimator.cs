using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// Procedural animation for the rigged robot: legs swing from the hip while running,
    /// the body bobs and leans, jumps stretch, landings squash, idle breathes and hits flash.
    /// Sits under PlayerVisual, which handles the gravity rotation, so all motion here is local.
    /// </summary>
    public class RobotAnimator : MonoBehaviour
    {
        [SerializeField] PlayerMotor motor;
        [SerializeField] SpriteRenderer body;
        [SerializeField] Transform legLeft;
        [SerializeField] Transform legRight;
        [SerializeField] SpriteRenderer[] flashRenderers;

        [Header("Run")]
        [SerializeField] float strideFrequency = 11f;
        [SerializeField] float legSwingDegrees = 32f;
        [SerializeField] float runBobAmplitude = 0.05f;
        [SerializeField] float maxLeanDegrees = 7f;

        [Header("Air")]
        [SerializeField] float airLegSpread = 18f;
        [SerializeField] float landSquash = 0.2f;
        [SerializeField] float jumpStretch = 0.12f;
        [SerializeField] float recoverSpeed = 9f;
        [SerializeField] Color hitColor = new Color(1f, 0.35f, 0.35f);

        Vector3 baseScale;
        Vector3 basePosition;
        Vector2 squash;
        float runPhase;
        float hitFlash;
        float legL, legR; // current smoothed leg angles
        PlayerRespawn respawn;

        void Awake()
        {
            if (motor == null) motor = GetComponentInParent<PlayerMotor>();
            baseScale = transform.localScale;
            basePosition = transform.localPosition;
            respawn = motor.GetComponent<PlayerRespawn>();
            if (flashRenderers == null || flashRenderers.Length == 0) flashRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        void OnEnable()
        {
            motor.Landed += OnLanded;
            motor.Jumped += OnJumped;
            if (respawn != null)
            {
                respawn.Died += OnDied;
                respawn.Respawned += OnRespawned;
            }
        }

        void OnDisable()
        {
            motor.Landed -= OnLanded;
            motor.Jumped -= OnJumped;
            if (respawn != null)
            {
                respawn.Died -= OnDied;
                respawn.Respawned -= OnRespawned;
            }
        }

        void OnLanded() => squash = new Vector2(landSquash, -landSquash);
        void OnJumped() => squash = new Vector2(-jumpStretch, jumpStretch);
        void OnDied() => hitFlash = 1f;
        void OnRespawned() => squash = new Vector2(-jumpStretch, jumpStretch);

        void Update()
        {
            float dt = Time.deltaTime;
            float speed = Mathf.Abs(motor.Velocity.x);
            float speedFactor = Mathf.Clamp01(speed / 8f);
            bool grounded = motor.IsGrounded;
            bool running = grounded && speed > 0.5f && motor.MovementEnabled;

            // ---- legs ----
            float targetL, targetR;
            if (running)
            {
                runPhase += dt * strideFrequency * Mathf.Max(0.4f, speedFactor);
                float swing = Mathf.Sin(runPhase) * legSwingDegrees * speedFactor;
                targetL = swing;
                targetR = -swing;
            }
            else if (!grounded)
            {
                // Legs trail against the vertical motion: tuck on the way up, reach on the way down.
                float up = Mathf.Clamp(motor.Velocity.y * motor.GravitySign / 10f, -1f, 1f);
                targetL = airLegSpread * (0.4f + up * 0.6f);
                targetR = -airLegSpread * (0.4f - up * 0.6f);
                runPhase = 0f;
            }
            else
            {
                targetL = targetR = 0f;
                runPhase = 0f;
            }
            legL = Mathf.Lerp(legL, targetL, dt * 18f);
            legR = Mathf.Lerp(legR, targetR, dt * 18f);
            if (legLeft != null) legLeft.localRotation = Quaternion.Euler(0f, 0f, legL);
            if (legRight != null) legRight.localRotation = Quaternion.Euler(0f, 0f, legR);

            // ---- body ----
            float bob = running ? Mathf.Abs(Mathf.Sin(runPhase)) * runBobAmplitude : Mathf.Sin(Time.time * 2f) * 0.012f;
            squash = Vector2.Lerp(squash, Vector2.zero, dt * recoverSpeed);
            transform.localScale = new Vector3(baseScale.x * (1f + squash.x), baseScale.y * (1f + squash.y), baseScale.z);
            transform.localPosition = basePosition + Vector3.up * bob;
            float lean = running ? -maxLeanDegrees * speedFactor : 0f;
            transform.localRotation = Quaternion.Euler(0f, 0f, lean);

            // ---- hit flash / blink ----
            hitFlash = Mathf.Max(0f, hitFlash - dt * 3f);
            bool visible = respawn == null || !respawn.IsDead || ((int)(Time.unscaledTime * 20f) & 1) == 0;
            Color tint = Color.Lerp(Color.white, hitColor, hitFlash);
            foreach (SpriteRenderer sr in flashRenderers)
            {
                if (sr == null) continue;
                sr.color = tint;
                sr.enabled = visible;
            }
        }
    }
}
