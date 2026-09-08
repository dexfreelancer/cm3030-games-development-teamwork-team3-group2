using UnityEngine;
using UnityEngine.UI;

namespace Flipside
{
    /// <summary>Minimal HUD: chip counter, delivery clock bar (hidden until the clock starts) and gravity indicator.</summary>
    public class HudController : MonoBehaviour
    {
        [SerializeField] Text chipText;
        [SerializeField] GameObject clockRoot;
        [SerializeField] Image clockFill;
        [SerializeField] Text clockText;
        [SerializeField] PlayerMotor player;

        RunState run;

        void Start()
        {
            run = RunState.Instance;
            if (player == null) player = FindFirstObjectByType<PlayerMotor>();
            if (clockRoot != null) clockRoot.SetActive(run != null && run.ClockRunning);
            if (run != null)
            {
                run.ChipCollected += RefreshChips;
                run.ClockStarted += () => { if (clockRoot != null) clockRoot.SetActive(true); };
            }
            RefreshChips();
        }

        void OnDestroy()
        {
            if (run != null) run.ChipCollected -= RefreshChips;
        }

        void Update()
        {
            if (run != null && clockRoot != null && clockRoot.activeSelf)
            {
                float t = run.ClockDuration > 0f ? run.ClockRemaining / run.ClockDuration : 0f;
                if (clockFill != null) clockFill.fillAmount = t;
                if (clockText != null)
                {
                    int s = Mathf.CeilToInt(run.ClockRemaining);
                    clockText.text = string.Format("{0}:{1:00}", s / 60, s % 60);
                }
            }
        }

        void RefreshChips()
        {
            if (chipText == null || run == null) return;
            chipText.text = string.Format("CHIPS  {0} / {1}", run.ChipsCollected, run.ChipsTotal);
        }
    }
}
