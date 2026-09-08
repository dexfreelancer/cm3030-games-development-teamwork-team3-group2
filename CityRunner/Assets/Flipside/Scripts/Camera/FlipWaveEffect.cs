using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// Drives the full-screen flip wave: a ring expands from the robot and turns the whole
    /// world orange while gravity is inverted (or back to normal colours when it returns).
    /// The material is shared with the URP full-screen pass on the renderer.
    /// </summary>
    public class FlipWaveEffect : MonoBehaviour
    {
        [SerializeField] Material material;
        [SerializeField] PlayerMotor player;
        [SerializeField] float duration = 0.75f;
        [SerializeField] float maxRadius = 2.4f; // viewport units (aspect-corrected), enough to cover any corner

        static readonly int CenterId = Shader.PropertyToID("_Center");
        static readonly int RadiusId = Shader.PropertyToID("_Radius");
        static readonly int AspectId = Shader.PropertyToID("_Aspect");
        static readonly int InsideId = Shader.PropertyToID("_Inside");
        static readonly int OutsideId = Shader.PropertyToID("_Outside");

        Camera cam;
        float timer = -1f;
        int targetSign = 1;
        float settledOrange;

        void Awake()
        {
            cam = GetComponent<Camera>();
            if (player == null) player = FindFirstObjectByType<PlayerMotor>();
        }

        PlayerRespawn respawn;

        void OnEnable()
        {
            if (player != null)
            {
                player.GravityFlipped += OnFlipped;
                respawn = player.GetComponent<PlayerRespawn>();
                if (respawn != null) respawn.Respawned += OnRespawned;
            }
            ApplySettled(player != null && player.GravitySign == -1 ? 1f : 0f);
        }

        void OnDisable()
        {
            if (player != null) player.GravityFlipped -= OnFlipped;
            if (respawn != null) respawn.Respawned -= OnRespawned;
            ApplySettled(0f); // leave the shared material clean for the editor
        }

        /// <summary>Respawn restores gravity without a flip, so the colours snap to match with no wave.</summary>
        void OnRespawned()
        {
            timer = -1f;
            ApplySettled(player.GravitySign == -1 ? 1f : 0f);
        }

        void OnFlipped(int sign)
        {
            targetSign = sign;
            timer = 0f;
            if (material == null) return;
            material.SetFloat(OutsideId, settledOrange);
            material.SetFloat(InsideId, sign == -1 ? 1f : 0f);
        }

        void LateUpdate()
        {
            if (material == null || player == null) return;
            Vector3 vp = cam.WorldToViewportPoint(player.transform.position);
            material.SetVector(CenterId, new Vector4(vp.x, vp.y, 0f, 0f));
            material.SetFloat(AspectId, cam.aspect);

            if (timer < 0f) return;
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float eased = 1f - (1f - t) * (1f - t); // fast start, soft finish
            material.SetFloat(RadiusId, eased * maxRadius);
            if (t >= 1f)
            {
                timer = -1f;
                ApplySettled(targetSign == -1 ? 1f : 0f);
            }
        }

        void ApplySettled(float orange)
        {
            settledOrange = orange;
            if (material == null) return;
            material.SetFloat(RadiusId, 0f);
            material.SetFloat(OutsideId, orange);
            material.SetFloat(InsideId, orange);
        }
    }
}
