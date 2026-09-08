using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// Light beacon at the start of a section. Stores the player's position and gravity
    /// direction for respawn. Optionally starts the delivery clock the first time it is reached.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("Where the player reappears. Defaults to this transform.")]
        [SerializeField] Transform respawnPoint;
        [Tooltip("Gravity direction restored on respawn: 1 normal, -1 inverted.")]
        [SerializeField] int respawnGravitySign = 1;
        [SerializeField] bool startsClock;
        [SerializeField] SpriteRenderer lightRenderer;
        [SerializeField] Sprite offSprite;
        [SerializeField] Sprite onSprite;
        [SerializeField] Color offColor = new Color(0.35f, 0.35f, 0.4f);
        [SerializeField] Color onColor = new Color(1f, 0.75f, 0.2f);

        public bool Activated { get; private set; }
        public static event System.Action<Checkpoint> AnyActivated;

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Start()
        {
            ApplyColor();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            PlayerRespawn respawn = other.GetComponentInParent<PlayerRespawn>();
            if (respawn == null || Activated) return;

            Activated = true;
            Transform point = respawnPoint != null ? respawnPoint : transform;
            respawn.SetCheckpoint(point.position, respawnGravitySign);
            if (startsClock && RunState.Instance != null) RunState.Instance.StartClock();
            ApplyColor();
            AnyActivated?.Invoke(this);
        }

        void ApplyColor()
        {
            if (lightRenderer == null) return;
            lightRenderer.color = Activated ? onColor : offColor;
            Sprite sprite = Activated ? onSprite : offSprite;
            if (sprite != null) lightRenderer.sprite = sprite;
        }
    }
}
