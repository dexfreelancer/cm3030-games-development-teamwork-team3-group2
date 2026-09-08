using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// A marked region where gravity flipping is disabled. The player has to plan the
    /// orientation before entering and cross gaps by jumping.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class GravityZone : MonoBehaviour
    {
        int inside;

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            PlayerMotor motor = other.GetComponentInParent<PlayerMotor>();
            if (motor == null) return;
            inside++;
            motor.FlipLocked = true;
        }

        void OnTriggerExit2D(Collider2D other)
        {
            PlayerMotor motor = other.GetComponentInParent<PlayerMotor>();
            if (motor == null) return;
            inside = Mathf.Max(0, inside - 1);
            if (inside == 0) motor.FlipLocked = false;
        }
    }
}
