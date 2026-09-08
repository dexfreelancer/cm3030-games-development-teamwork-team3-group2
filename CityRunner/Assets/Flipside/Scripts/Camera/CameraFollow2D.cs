using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// Upright follow camera. Never rotates. Smoothly re-centres on the player and
    /// looks slightly ahead in the run direction and in the direction of gravity,
    /// so hazards are visible before the player commits to a flip.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] PlayerMotor target;
        [SerializeField] float smoothTime = 0.15f;
        [SerializeField] float lookAheadHorizontal = 2.5f;
        [Tooltip("Vertical offset against gravity, so the surface you stand on sits low in the frame and the surfaces you can flip to stay visible.")]
        [SerializeField] float verticalBias = 3f;
        [SerializeField] float lookAheadSmoothTime = 0.35f;
        [SerializeField] bool useBounds;
        [SerializeField] Rect bounds = new Rect(-10f, -10f, 100f, 40f);

        Camera cam;
        Vector3 velocity;
        Vector2 lookAhead;
        Vector2 lookAheadVelocity;

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        void Start()
        {
            if (target == null) return;
            lookAhead = DesiredLookAhead();
            transform.position = Clamp(TargetPosition());
        }

        void LateUpdate()
        {
            if (target == null) return;
            lookAhead = Vector2.SmoothDamp(lookAhead, DesiredLookAhead(), ref lookAheadVelocity, lookAheadSmoothTime);
            Vector3 desired = Clamp(TargetPosition());
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
            transform.rotation = Quaternion.identity;
        }

        Vector2 DesiredLookAhead()
        {
            // GravitySign +1 pulls toward -Y, so the bias points +Y: the camera looks toward the flip targets.
            return new Vector2(target.FacingSign * lookAheadHorizontal, target.GravitySign * verticalBias);
        }

        Vector3 TargetPosition()
        {
            Vector2 p = (Vector2)target.transform.position + lookAhead;
            return new Vector3(p.x, p.y, transform.position.z);
        }

        Vector3 Clamp(Vector3 position)
        {
            if (!useBounds || cam == null || !cam.orthographic) return position;
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            position.x = Mathf.Clamp(position.x, bounds.xMin + halfWidth, Mathf.Max(bounds.xMin + halfWidth, bounds.xMax - halfWidth));
            position.y = Mathf.Clamp(position.y, bounds.yMin + halfHeight, Mathf.Max(bounds.yMin + halfHeight, bounds.yMax - halfHeight));
            return position;
        }
    }
}
