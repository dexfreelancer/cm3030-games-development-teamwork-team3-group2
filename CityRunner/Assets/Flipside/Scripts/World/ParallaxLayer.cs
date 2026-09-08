using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// A horizontally tiling background layer that moves at a fraction of the camera speed.
    /// The sprite renderer uses Tiled draw mode, so the layer is wider than the view and
    /// wraps by whole tiles to hide the seam.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class ParallaxLayer : MonoBehaviour
    {
        [Range(0f, 1f)] [SerializeField] float horizontalFactor = 0.3f;
        [Range(0f, 1f)] [SerializeField] float verticalFactor = 0.1f;
        [SerializeField] float baseY;
        [SerializeField] float depth = 10f;

        Camera cam;
        SpriteRenderer sr;
        float tileWidth;

        void Start()
        {
            cam = Camera.main;
            sr = GetComponent<SpriteRenderer>();
            tileWidth = sr.sprite != null ? sr.sprite.bounds.size.x : 10f;
            LateUpdate();
        }

        void LateUpdate()
        {
            if (cam == null) return;
            Vector3 c = cam.transform.position;
            float x = c.x * (1f - horizontalFactor);
            // Wrap in whole tiles so the visible strip always covers the camera.
            float offset = Mathf.Repeat(c.x - x, tileWidth);
            float y = baseY + (c.y - baseY) * (1f - verticalFactor);
            transform.position = new Vector3(c.x - offset, y, depth);
        }
    }
}
