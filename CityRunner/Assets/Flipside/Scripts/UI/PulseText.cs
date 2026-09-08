using UnityEngine;
using UnityEngine.UI;

namespace Flipside
{
    /// <summary>Slow alpha pulse for "press any key" style prompts. Uses unscaled time so it works while paused.</summary>
    [RequireComponent(typeof(Text))]
    public class PulseText : MonoBehaviour
    {
        [SerializeField] float speed = 2.5f;
        [SerializeField] float minAlpha = 0.35f;

        Text text;

        void Awake()
        {
            text = GetComponent<Text>();
        }

        void Update()
        {
            Color c = text.color;
            c.a = Mathf.Lerp(minAlpha, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * speed));
            text.color = c;
        }
    }
}
