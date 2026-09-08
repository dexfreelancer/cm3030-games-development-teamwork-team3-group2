using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// Maintenance laser. Either rotates around its emitter between two angles or slides
    /// between two points. Movement is a smooth sine so the period is easy to read,
    /// and the beam is always visible so players can plan before committing to a flip.
    /// </summary>
    public class LaserSweep : MonoBehaviour
    {
        public enum Mode { Rotate, Translate }

        [SerializeField] Mode mode = Mode.Rotate;
        [Tooltip("Seconds for one full back-and-forth cycle.")]
        [SerializeField] float period = 4f;
        [Range(0f, 1f)] [SerializeField] float phase;

        [Header("Rotate")]
        [SerializeField] float angleFrom = -35f;
        [SerializeField] float angleTo = 35f;
        [Tooltip("Base direction of the beam in degrees (0 = right, 90 = up).")]
        [SerializeField] float baseAngle = 90f;

        [Header("Translate")]
        [SerializeField] Vector2 pointA;
        [SerializeField] Vector2 pointB;

        [Header("Beam")]
        [SerializeField] Transform beam;
        [SerializeField] float beamLength = 9f;
        [SerializeField] float beamWidth = 0.25f;
        [SerializeField] float beamStartOffset = 0.6f;

        [Header("Blink (0 = always on)")]
        [Tooltip("Seconds the beam stays on in each cycle. 0 disables blinking.")]
        [SerializeField] float blinkOn;
        [SerializeField] float blinkOff = 1.5f;
        [SerializeField] float blinkOffset;
        [Tooltip("Seconds before switching on during which the beam flickers as a warning.")]
        [SerializeField] float warnSeconds = 0.35f;

        float time;
        bool powered = true;
        Collider2D beamCollider;
        SpriteRenderer[] beamRenderers;

        /// <summary>True while the beam can hurt: powered and (if blinking) in its on phase.</summary>
        public bool IsActive { get; private set; } = true;

        public void SetPowered(bool on)
        {
            powered = on;
        }

        void Start()
        {
            time = phase * period;
            if (beam != null)
            {
                beamCollider = beam.GetComponent<Collider2D>();
                beamRenderers = beam.GetComponentsInChildren<SpriteRenderer>();
            }
            Apply();
        }

        void Update()
        {
            time += Time.deltaTime;
            Apply();
            ApplyBlink();
        }

        void ApplyBlink()
        {
            if (beam == null) return;
            bool on = powered;
            float visibleAlpha = 1f;
            if (blinkOn > 0f)
            {
                float cycle = blinkOn + blinkOff;
                float t = Mathf.Repeat(time + blinkOffset, cycle);
                on = powered && t < blinkOn;
                float untilOn = cycle - t;
                if (!on && powered && untilOn < warnSeconds) visibleAlpha = ((int)(Time.time * 18f) & 1) == 0 ? 0.35f : 0f; // warning flicker
                else if (!on) visibleAlpha = 0f;
            }
            else if (!powered)
            {
                visibleAlpha = 0.05f; // powered-down beam stays faintly visible so the player knows where it returns
            }
            IsActive = on;
            if (beamCollider != null) beamCollider.enabled = on;
            if (beamRenderers != null)
            {
                foreach (SpriteRenderer sr in beamRenderers)
                {
                    if (sr == null) continue;
                    sr.enabled = visibleAlpha > 0f;
                    Color c = sr.color;
                    c.a = Mathf.Min(0.95f, visibleAlpha);
                    sr.color = c;
                }
            }
        }

        /// <summary>0..1 ping-pong progress, smooth at both ends.</summary>
        public float Progress => 0.5f - 0.5f * Mathf.Cos(time / period * Mathf.PI * 2f);

        void Apply()
        {
            float t = Progress;
            if (mode == Mode.Rotate)
            {
                float angle = baseAngle + Mathf.Lerp(angleFrom, angleTo, t);
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
            else
            {
                transform.position = Vector2.Lerp(pointA, pointB, t);
                transform.rotation = Quaternion.Euler(0f, 0f, baseAngle);
            }

            if (beam != null)
            {
                // The beam extends along local +X from the emitter.
                beam.localPosition = new Vector3(beamStartOffset + beamLength * 0.5f, 0f, 0f);
                beam.localRotation = Quaternion.identity;
                SpriteRenderer sr = beam.GetComponent<SpriteRenderer>();
                if (sr != null && sr.drawMode != SpriteDrawMode.Simple) sr.size = new Vector2(beamLength, beamWidth);
                else beam.localScale = new Vector3(beamLength, beamWidth, 1f);
                BoxCollider2D box = beam.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    box.size = sr != null && sr.drawMode != SpriteDrawMode.Simple
                        ? new Vector2(beamLength, beamWidth * 0.6f)
                        : new Vector2(1f, 0.6f);
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            if (mode == Mode.Translate) Gizmos.DrawLine(pointA, pointB);
            else
            {
                Vector3 a = transform.position + Quaternion.Euler(0, 0, baseAngle + angleFrom) * Vector3.right * beamLength;
                Vector3 b = transform.position + Quaternion.Euler(0, 0, baseAngle + angleTo) * Vector3.right * beamLength;
                Gizmos.DrawLine(transform.position, a);
                Gizmos.DrawLine(transform.position, b);
            }
        }
    }
}
