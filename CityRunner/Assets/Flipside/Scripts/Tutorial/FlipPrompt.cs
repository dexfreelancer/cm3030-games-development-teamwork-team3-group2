using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// The only help the game gives: a flip-key icon shown once when the player first
    /// reaches the flip gap. It disappears after the first successful flip and pulses
    /// if the player stalls in the zone for a few seconds.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FlipPrompt : MonoBehaviour
    {
        [SerializeField] Transform icon;
        [SerializeField] float stallSeconds = 3f;
        [SerializeField] float pulseScale = 1.35f;
        [SerializeField] float pulseSpeed = 5f;

        bool shown;
        bool done;
        bool playerInside;
        float insideTime;
        Vector3 baseScale;
        PlayerMotor motor;

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Start()
        {
            if (icon == null) icon = transform.childCount > 0 ? transform.GetChild(0) : null;
            if (icon != null)
            {
                baseScale = icon.localScale;
                icon.gameObject.SetActive(false);
            }
        }

        void Update()
        {
            if (done || icon == null || !shown) return;

            insideTime += playerInside ? Time.deltaTime : 0f;
            bool stalled = insideTime >= stallSeconds;
            float pulse = stalled ? 1f + (pulseScale - 1f) * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed)) : 1f;
            icon.localScale = baseScale * pulse;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            PlayerMotor m = other.GetComponentInParent<PlayerMotor>();
            if (m == null || done) return;
            playerInside = true;
            if (!shown)
            {
                shown = true;
                icon.gameObject.SetActive(true);
                motor = m;
                motor.GravityFlipped += OnFlipped;
            }
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerMotor>() != null)
            {
                playerInside = false;
                insideTime = 0f;
            }
        }

        void OnFlipped(int gravitySign)
        {
            done = true;
            motor.GravityFlipped -= OnFlipped;
            if (icon != null) icon.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (motor != null) motor.GravityFlipped -= OnFlipped;
        }
    }
}
