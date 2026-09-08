using UnityEngine;

namespace Flipside
{
    /// <summary>Pulsing warm glow behind a hazard so it reads from a distance, before the player commits to a flip.</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class HazardGlow : MonoBehaviour
    {
        [SerializeField] float pulseSpeed = 3.2f;
        [SerializeField] float minAlpha = 0.55f;
        [SerializeField] float maxAlpha = 1f;
        [SerializeField] float scalePulse = 0.2f;
        [SerializeField] float phaseOffset;

        SpriteRenderer sr;
        Vector3 baseScale;
        Color baseColor;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            baseScale = transform.localScale;
            baseColor = sr.color;
            phaseOffset = transform.position.x * 0.7f; // neighbouring hazards do not pulse in lockstep
        }

        void Update()
        {
            float p = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed + phaseOffset);
            Color c = baseColor;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, p);
            sr.color = c;
            transform.localScale = baseScale * (1f + scalePulse * p);
        }
    }
}
