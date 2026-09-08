using UnityEngine;

namespace Flipside
{
    /// <summary>Optional collectible. Collected once; stays collected after death.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class DataChip : MonoBehaviour
    {
        [SerializeField] float bobAmplitude = 0.15f;
        [SerializeField] float bobSpeed = 2.5f;

        public static event System.Action<DataChip> AnyCollected;

        Vector3 restPosition;
        bool collected;

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void Start()
        {
            restPosition = transform.position;
        }

        void Update()
        {
            float offset = Mathf.Sin(Time.time * bobSpeed + restPosition.x) * bobAmplitude;
            transform.position = restPosition + Vector3.up * offset;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (collected || other.GetComponentInParent<PlayerMotor>() == null) return;
            collected = true;
            if (RunState.Instance != null) RunState.Instance.CollectChip();
            AnyCollected?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
