using UnityEngine;

namespace Flipside
{
    /// <summary>Remembers the last safe position and gravity direction and returns the player there on death.</summary>
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] float respawnDelay = 0.25f;

        public event System.Action Died;
        public event System.Action Respawned;
        public int Deaths { get; private set; }
        public bool IsDead { get; private set; }

        PlayerMotor motor;
        Vector2 safePosition;
        int safeGravitySign = 1;

        void Awake()
        {
            motor = GetComponent<PlayerMotor>();
            safePosition = transform.position;
        }

        public void SetCheckpoint(Vector2 position, int gravitySign)
        {
            safePosition = position;
            safeGravitySign = gravitySign;
        }

        public void Kill()
        {
            if (IsDead) return; // one hazard contact produces exactly one death
            IsDead = true;
            Deaths++;
            motor.MovementEnabled = false;
            Died?.Invoke();
            Invoke(nameof(Respawn), respawnDelay);
        }

        void Respawn()
        {
            motor.Teleport(safePosition, safeGravitySign);
            motor.MovementEnabled = true;
            IsDead = false;
            Respawned?.Invoke();
        }
    }
}
