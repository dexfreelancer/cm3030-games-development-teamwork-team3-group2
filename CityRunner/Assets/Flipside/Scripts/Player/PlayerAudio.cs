using UnityEngine;

namespace Flipside
{
    /// <summary>Maps player events to sound effects, including a speed-paced footstep.</summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerAudio : MonoBehaviour
    {
        [SerializeField] float footstepInterval = 0.28f;

        PlayerMotor motor;
        PlayerRespawn respawn;
        float stepTimer;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            respawn = GetComponent<PlayerRespawn>();
        }

        void OnEnable()
        {
            motor.Jumped += OnJumped;
            motor.Landed += OnLanded;
            motor.GravityFlipped += OnFlipped;
            if (respawn != null) respawn.Died += OnDied;
            DataChip.AnyCollected += OnChip;
            Checkpoint.AnyActivated += OnCheckpoint;
            Portal.AnyUsed += OnPortal;
        }

        void OnPortal(Portal portal) => Play(Sfx.Portal);

        void OnDisable()
        {
            Portal.AnyUsed -= OnPortal;
            motor.Jumped -= OnJumped;
            motor.Landed -= OnLanded;
            motor.GravityFlipped -= OnFlipped;
            if (respawn != null) respawn.Died -= OnDied;
            DataChip.AnyCollected -= OnChip;
            Checkpoint.AnyActivated -= OnCheckpoint;
        }

        void Update()
        {
            bool running = motor.IsGrounded && Mathf.Abs(motor.Velocity.x) > 1f && motor.MovementEnabled;
            if (!running)
            {
                stepTimer = footstepInterval * 0.5f;
                return;
            }
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                stepTimer = footstepInterval;
                Play(Sfx.Footstep);
            }
        }

        static void Play(Sfx sfx)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play(sfx);
        }

        void OnJumped() => Play(Sfx.Jump);
        void OnLanded() => Play(Sfx.Land);
        void OnFlipped(int sign) => Play(Sfx.Flip);
        void OnDied() => Play(Sfx.Hit);
        void OnChip(DataChip chip) => Play(Sfx.Chip);
        void OnCheckpoint(Checkpoint checkpoint) => Play(Sfx.Checkpoint);
    }
}
