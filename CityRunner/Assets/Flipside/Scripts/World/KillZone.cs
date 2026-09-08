using UnityEngine;

namespace Flipside
{
    /// <summary>Trigger volume that kills the player. Used for fall-outs and static hazards.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            PlayerRespawn respawn = other.GetComponentInParent<PlayerRespawn>();
            if (respawn != null) respawn.Kill();
        }
    }
}
