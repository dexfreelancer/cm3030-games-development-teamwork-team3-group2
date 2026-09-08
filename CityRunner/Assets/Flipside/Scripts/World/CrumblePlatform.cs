using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// A platform that shakes and drops away shortly after the player stands on it (from either side),
    /// then returns after a while. Keeps the player moving and sets up flip chains.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class CrumblePlatform : MonoBehaviour
    {
        [SerializeField] float holdSeconds = 0.6f;
        [SerializeField] float goneSeconds = 2.5f;
        [SerializeField] float shakeAmount = 0.06f;

        BoxCollider2D box;
        SpriteRenderer[] renderers;
        Vector3 basePosition;
        float touchedAt = -1f;
        float goneAt = -1f;
        bool gone;

        void Awake()
        {
            box = GetComponent<BoxCollider2D>();
            renderers = GetComponentsInChildren<SpriteRenderer>();
            basePosition = transform.position;
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            if (gone || touchedAt >= 0f) return;
            if (collision.collider.GetComponentInParent<PlayerMotor>() == null) return;
            touchedAt = Time.time;
            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Crumble);
        }

        void Update()
        {
            if (gone)
            {
                if (Time.time - goneAt >= goneSeconds) Restore();
                return;
            }
            if (touchedAt < 0f) return;
            float k = (Time.time - touchedAt) / holdSeconds;
            transform.position = basePosition + new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * shakeAmount * k;
            if (k >= 1f) Collapse();
        }

        void Collapse()
        {
            gone = true;
            goneAt = Time.time;
            box.enabled = false;
            foreach (SpriteRenderer sr in renderers) sr.enabled = false;
            transform.position = basePosition;
        }

        void Restore()
        {
            gone = false;
            touchedAt = -1f;
            box.enabled = true;
            foreach (SpriteRenderer sr in renderers) sr.enabled = true;
        }
    }
}
