using UnityEngine;

namespace Flipside
{
    /// <summary>A pulsing arrow at the start that shows which way to go. Hides once the player has moved on.</summary>
    public class DirectionPrompt : MonoBehaviour
    {
        [SerializeField] float hideBeyondX = 9f;
        [SerializeField] float slide = 0.35f;
        [SerializeField] float speed = 4f;

        PlayerMotor player;
        Vector3 basePosition;
        SpriteRenderer[] renderers;
        bool done;

        void Start()
        {
            player = FindFirstObjectByType<PlayerMotor>();
            basePosition = transform.position;
            renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        void Update()
        {
            if (done) return;
            if (player != null && player.transform.position.x > hideBeyondX)
            {
                done = true;
                gameObject.SetActive(false);
                return;
            }
            float p = 0.5f + 0.5f * Mathf.Sin(Time.time * speed);
            transform.position = basePosition + Vector3.right * (p * slide);
            foreach (SpriteRenderer sr in renderers)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0.45f, 1f, p);
                sr.color = c;
            }
        }
    }
}
