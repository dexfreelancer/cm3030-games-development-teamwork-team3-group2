using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// Rotates the visual root so the robot's feet always point at its current floor,
    /// and mirrors it to face the movement direction. The world never rotates.
    /// </summary>
    public class PlayerVisual : MonoBehaviour
    {
        [SerializeField] PlayerMotor motor;

        float startAngle;
        float targetAngle;
        int spinDirection = 1;

        void Awake()
        {
            if (motor == null) motor = GetComponentInParent<PlayerMotor>();
        }

        void OnEnable()
        {
            motor.GravityFlipped += OnGravityFlipped;
            targetAngle = motor.GravitySign == 1 ? 0f : 180f;
            startAngle = targetAngle;
        }

        void OnDisable()
        {
            motor.GravityFlipped -= OnGravityFlipped;
        }

        void OnGravityFlipped(int gravitySign)
        {
            startAngle = transform.localEulerAngles.z;
            targetAngle = gravitySign == 1 ? 0f : 180f;
            spinDirection = motor.FacingSign; // head swings forward, in the direction of travel
        }

        void LateUpdate()
        {
            if (!motor.IsFlipping)
            {
                // Keeps the sprite in sync after a respawn/teleport, which changes gravity without a flip.
                targetAngle = motor.GravitySign == 1 ? 0f : 180f;
                startAngle = targetAngle;
            }
            float eased = Mathf.SmoothStep(0f, 1f, motor.FlipProgress);
            float delta = Mathf.DeltaAngle(startAngle, targetAngle);
            if (Mathf.Abs(Mathf.Abs(delta) - 180f) < 0.01f)
            {
                delta = 180f * spinDirection; // choose the spin direction for the ambiguous half-turn
            }
            float angle = motor.IsFlipping ? startAngle + delta * eased : targetAngle;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            // After a half turn, local +X points at world -X, so compensate to keep facing readable.
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * motor.FacingSign * motor.GravitySign;
            transform.localScale = scale;
        }
    }
}
