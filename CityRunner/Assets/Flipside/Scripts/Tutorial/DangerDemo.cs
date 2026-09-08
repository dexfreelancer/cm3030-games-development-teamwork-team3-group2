using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// Wordless warning before the first hazard: a ghost of the robot slides into a red
    /// hazard, flashes and dissolves, on a loop. Shown while the player is in the zone and
    /// retired after the first death or once the player has passed the hazard.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class DangerDemo : MonoBehaviour
    {
        [SerializeField] Transform ghost;
        [SerializeField] SpriteRenderer ghostRenderer;
        [SerializeField] SpriteRenderer hazardIcon;
        [SerializeField] float travel = 1.4f;
        [SerializeField] float retireBeyondX = 80f;

        bool shown;
        bool retired;
        float t;
        Vector3 ghostStart;
        PlayerMotor player;
        PlayerRespawn respawn;

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Start()
        {
            player = FindFirstObjectByType<PlayerMotor>();
            respawn = player != null ? player.GetComponent<PlayerRespawn>() : null;
            if (respawn != null) respawn.Died += Retire;
            if (ghost != null)
            {
                ghostStart = ghost.localPosition;
                ghost.gameObject.SetActive(false);
            }
            if (hazardIcon != null) hazardIcon.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (respawn != null) respawn.Died -= Retire;
        }

        void Retire()
        {
            retired = true;
            if (ghost != null) ghost.gameObject.SetActive(false);
            if (hazardIcon != null) hazardIcon.gameObject.SetActive(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (retired || shown || other.GetComponentInParent<PlayerMotor>() == null) return;
            shown = true;
            if (ghost != null) ghost.gameObject.SetActive(true);
            if (hazardIcon != null) hazardIcon.gameObject.SetActive(true);
        }

        void Update()
        {
            if (!shown || retired || ghost == null) return;
            if (player != null && player.transform.position.x > retireBeyondX) { Retire(); return; }

            // Loop: 1.0 s approach, 0.35 s hit flash, 0.5 s fade, 0.6 s pause.
            t += Time.deltaTime;
            const float approach = 1.0f, hit = 0.35f, fade = 0.5f, pause = 0.6f;
            float total = approach + hit + fade + pause;
            if (t > total) t -= total;

            Color c = new Color(1f, 1f, 1f, 0.6f);
            Vector3 pos = ghostStart;
            Vector3 scale = Vector3.one;
            if (t < approach)
            {
                float k = t / approach;
                pos = ghostStart + Vector3.right * (travel * k);
                pos.y += Mathf.Abs(Mathf.Sin(k * Mathf.PI * 4f)) * 0.05f; // little run bob
            }
            else if (t < approach + hit)
            {
                float k = (t - approach) / hit;
                pos = ghostStart + Vector3.right * travel;
                c = Color.Lerp(new Color(1f, 0.3f, 0.3f, 0.95f), new Color(1f, 1f, 1f, 0.9f), Mathf.PingPong(k * 6f, 1f));
                scale = Vector3.one * (1f + 0.15f * Mathf.Sin(k * Mathf.PI));
            }
            else if (t < approach + hit + fade)
            {
                float k = (t - approach - hit) / fade;
                pos = ghostStart + Vector3.right * travel + Vector3.up * (k * 0.6f);
                c = new Color(1f, 0.4f, 0.4f, 0.9f * (1f - k));
                scale = Vector3.one * (1f - 0.3f * k);
            }
            else
            {
                c.a = 0f;
            }
            ghost.localPosition = pos;
            ghost.localScale = scale;
            if (ghostRenderer != null) ghostRenderer.color = c;
        }
    }
}
