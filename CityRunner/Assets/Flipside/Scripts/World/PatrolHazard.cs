using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// A maintenance drone that patrols between two points with a readable sine rhythm.
    /// Touching it resets the player. It faces its direction of travel and bobs slightly.
    /// </summary>
    public class PatrolHazard : MonoBehaviour
    {
        [SerializeField] Vector2 pointA;
        [SerializeField] Vector2 pointB;
        [SerializeField] float period = 4f;
        [Range(0f, 1f)] [SerializeField] float phase;
        [SerializeField] float bobAmplitude = 0.12f;
        [SerializeField] Transform visual;

        float time;
        float lastX;

        void Start()
        {
            time = phase * period;
            lastX = transform.position.x;
            Apply();
        }

        void Update()
        {
            time += Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            float t = 0.5f - 0.5f * Mathf.Cos(time / period * Mathf.PI * 2f);
            Vector2 p = Vector2.Lerp(pointA, pointB, t);
            transform.position = p;
            if (visual != null)
            {
                float dx = p.x - lastX;
                if (Mathf.Abs(dx) > 0.001f)
                {
                    Vector3 s = visual.localScale;
                    s.x = Mathf.Abs(s.x) * (dx > 0f ? 1f : -1f);
                    visual.localScale = s;
                }
                visual.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 5f + pointA.x) * bobAmplitude, 0f);
            }
            lastX = p.x;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pointA, pointB);
        }
    }
}
