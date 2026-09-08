using UnityEngine;

namespace Flipside
{
    /// <summary>End of the run. Touching it once finishes the delivery.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class DeliveryPoint : MonoBehaviour
    {
        bool delivered;

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (delivered || other.GetComponentInParent<PlayerMotor>() == null) return;
            delivered = true;
            if (RunState.Instance != null) RunState.Instance.FinishRun();
        }
    }
}
