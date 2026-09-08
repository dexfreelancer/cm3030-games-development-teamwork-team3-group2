using UnityEngine;

namespace Flipside
{
    /// <summary>Touch to power down the linked lasers for a few seconds. A shrinking bar shows the time left.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class LaserSwitch : MonoBehaviour
    {
        [SerializeField] LaserSweep[] lasers;
        [SerializeField] float openSeconds = 4f;
        [SerializeField] SpriteRenderer button;
        [SerializeField] Transform timerBar;
        [SerializeField] Color idleColor = new Color(0.4f, 1f, 0.5f);
        [SerializeField] Color activeColor = new Color(1f, 0.85f, 0.3f);

        float openUntil = -1f;
        Vector3 barScale;

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Start()
        {
            if (timerBar != null) { barScale = timerBar.localScale; timerBar.gameObject.SetActive(false); }
            Paint(idleColor);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerMotor>() == null) return;
            openUntil = Time.time + openSeconds;
            foreach (LaserSweep l in lasers) if (l != null) l.SetPowered(false);
            if (timerBar != null) timerBar.gameObject.SetActive(true);
            Paint(activeColor);
            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Switch);
        }

        void Update()
        {
            if (openUntil < 0f) return;
            float left = openUntil - Time.time;
            if (timerBar != null)
            {
                Vector3 s = barScale;
                s.x = barScale.x * Mathf.Clamp01(left / openSeconds);
                timerBar.localScale = s;
            }
            if (left <= 0f)
            {
                openUntil = -1f;
                foreach (LaserSweep l in lasers) if (l != null) l.SetPowered(true);
                if (timerBar != null) timerBar.gameObject.SetActive(false);
                Paint(idleColor);
            }
        }

        void Paint(Color c)
        {
            if (button != null) button.color = c;
        }
    }
}
