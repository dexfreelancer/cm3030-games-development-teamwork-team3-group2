using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// One half of a teleport pair. Entering sends the player to the linked portal, keeps
    /// horizontal momentum, and sets gravity to the destination's orientation, so a portal on
    /// the floor can drop the player onto a ceiling upside down.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Portal : MonoBehaviour
    {
        [SerializeField] Portal destination;
        [Tooltip("Gravity direction at this portal: 1 floor-mounted, -1 ceiling-mounted.")]
        [SerializeField] int gravitySign = 1;
        [Tooltip("Where the player appears relative to this portal when arriving here.")]
        [SerializeField] Vector2 arrivalOffset = new Vector2(1.2f, 0f);
        [SerializeField] Transform ring;
        [SerializeField] float spinSpeed = 90f;
        [SerializeField] float cooldown = 0.5f;

        public int GravitySign => gravitySign;
        public Vector2 ArrivalPoint => (Vector2)transform.position + arrivalOffset;
        public static event System.Action<Portal> AnyUsed;

        float lockedUntil;

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Update()
        {
            if (ring != null) ring.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (destination == null || Time.time < lockedUntil) return;
            PlayerMotor motor = other.GetComponentInParent<PlayerMotor>();
            if (motor == null || !motor.MovementEnabled) return;

            Vector2 velocity = motor.Velocity;
            destination.lockedUntil = Time.time + cooldown; // do not bounce straight back
            lockedUntil = Time.time + cooldown;
            motor.Teleport(destination.ArrivalPoint, destination.gravitySign, true);
            motor.SetVelocity(new Vector2(velocity.x, 0f));
            AnyUsed?.Invoke(this);
        }

        void OnDrawGizmosSelected()
        {
            if (destination == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, destination.transform.position);
            Gizmos.DrawWireSphere(destination.ArrivalPoint, 0.3f);
        }
    }
}
