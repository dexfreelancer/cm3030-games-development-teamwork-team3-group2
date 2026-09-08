using UnityEngine;

namespace Flipside
{
    /// <summary>Marks the start of a district. Reports it to the run state (HUD banner, music variant).</summary>
    [RequireComponent(typeof(Collider2D))]
    public class DistrictZone : MonoBehaviour
    {
        [SerializeField] string districtName = "Rooftops";
        [SerializeField] int districtIndex = 1;

        bool entered;

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (entered || other.GetComponentInParent<PlayerMotor>() == null) return;
            entered = true;
            if (RunState.Instance != null) RunState.Instance.EnterDistrict(districtIndex, districtName);
        }
    }
}
