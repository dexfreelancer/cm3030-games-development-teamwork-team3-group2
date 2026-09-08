using UnityEngine;

namespace Flipside
{
    /// <summary>
    /// Per-run bookkeeping: data chips, the delivery clock, districts and completion.
    /// One instance per gameplay scene. UI reads from it; world objects write to it.
    /// </summary>
    public class RunState : MonoBehaviour
    {
        public static RunState Instance { get; private set; }

        [Tooltip("Seconds on the delivery clock once it starts.")]
        [SerializeField] float clockDuration = 600f;

        public int ChipsCollected { get; private set; }
        public int ChipsTotal { get; private set; }
        public float ElapsedTime { get; private set; }
        public float ClockRemaining { get; private set; }
        public float ClockDuration => clockDuration;
        public bool ClockRunning { get; private set; }
        public bool RunStarted { get; private set; }
        public bool RunFinished { get; private set; }
        public bool RunFailed { get; private set; }
        public int CurrentDistrict { get; private set; }
        public string CurrentDistrictName { get; private set; } = "";
        public int Deaths => respawn != null ? respawn.Deaths : 0;

        public event System.Action ChipCollected;
        public event System.Action ClockStarted;
        public event System.Action ClockExpired;
        public event System.Action Finished;
        public event System.Action<int, string> DistrictEntered;

        PlayerRespawn respawn;

        void Awake()
        {
            Instance = this;
            ChipsTotal = FindObjectsByType<DataChip>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            ClockRemaining = clockDuration;
            respawn = FindFirstObjectByType<PlayerRespawn>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            if (!RunStarted || RunFinished || RunFailed) return;
            ElapsedTime += Time.deltaTime;
            if (!ClockRunning) return;

            ClockRemaining -= Time.deltaTime;
            if (ClockRemaining <= 0f)
            {
                ClockRemaining = 0f;
                ClockRunning = false;
                RunFailed = true;
                ClockExpired?.Invoke();
            }
        }

        public void BeginRun()
        {
            RunStarted = true;
        }

        public void CollectChip()
        {
            ChipsCollected++;
            ChipCollected?.Invoke();
        }

        public void StartClock()
        {
            if (ClockRunning || RunFinished || RunFailed) return;
            ClockRunning = true;
            ClockStarted?.Invoke();
        }

        public void EnterDistrict(int index, string name)
        {
            CurrentDistrict = index;
            CurrentDistrictName = name;
            DistrictEntered?.Invoke(index, name);
        }

        public void FinishRun()
        {
            if (RunFinished || RunFailed) return;
            RunFinished = true;
            ClockRunning = false;
            Finished?.Invoke();
        }
    }
}
