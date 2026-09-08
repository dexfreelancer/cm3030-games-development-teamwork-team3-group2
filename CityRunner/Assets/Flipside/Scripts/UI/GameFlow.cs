using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Flipside
{
    /// <summary>
    /// Title, pause, settings, finish and failure screens, all keyboard driven.
    /// The run starts frozen behind the title until the player presses a key.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] GameObject titlePanel;
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject endPanel;
        [SerializeField] GameObject failPanel;
        [SerializeField] GameObject creditsPanel;
        [SerializeField] GameObject districtBanner;
        [SerializeField] GameObject hudRoot;

        [Header("Texts")]
        [SerializeField] Text endSummary;
        [SerializeField] Text musicVolumeText;
        [SerializeField] Text sfxVolumeText;
        [SerializeField] Text districtBannerText;

        [Header("Refs")]
        [SerializeField] PlayerMotor player;

        public enum State { Title, Playing, Paused, Finished, Failed, Credits }
        public State Current { get; private set; } = State.Title;

        RunState run;
        float bannerTimer;

        void Start()
        {
            run = RunState.Instance;
            if (player == null) player = FindFirstObjectByType<PlayerMotor>();
            if (run != null)
            {
                run.Finished += OnFinished;
                run.ClockExpired += OnFailed;
                run.DistrictEntered += OnDistrict;
            }
            Time.timeScale = 1f;
            SetState(State.Title);
            RefreshVolumeTexts();
        }

        void OnDestroy()
        {
            if (run != null)
            {
                run.Finished -= OnFinished;
                run.ClockExpired -= OnFailed;
                run.DistrictEntered -= OnDistrict;
            }
            Time.timeScale = 1f;
        }

        void Update()
        {
            Keyboard k = Keyboard.current;
            if (k == null) return;

            if (districtBanner != null && districtBanner.activeSelf)
            {
                bannerTimer -= Time.unscaledDeltaTime;
                if (bannerTimer <= 0f) districtBanner.SetActive(false);
            }

            switch (Current)
            {
                case State.Title:
                    if (k.cKey.wasPressedThisFrame) { SetState(State.Credits); Menu(); }
                    else if (k.anyKey.wasPressedThisFrame && !k.escapeKey.wasPressedThisFrame) StartRun();
                    break;
                case State.Credits:
                    if (k.anyKey.wasPressedThisFrame) { SetState(State.Title); Menu(); }
                    break;
                case State.Playing:
                    if (k.escapeKey.wasPressedThisFrame) { SetState(State.Paused); Menu(); }
                    break;
                case State.Paused:
                    if (k.escapeKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) { SetState(State.Playing); Menu(); }
                    else if (k.rKey.wasPressedThisFrame) Restart();
                    else if (k.qKey.wasPressedThisFrame) Restart(); // back to title: the scene reloads into the title state
                    HandleVolumeKeys(k);
                    break;
                case State.Finished:
                case State.Failed:
                    if (k.rKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) Restart();
                    break;
            }
        }

        void HandleVolumeKeys(Keyboard k)
        {
            float step = 0.1f;
            if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) GameSettings.MusicVolume -= step;
            if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) GameSettings.MusicVolume += step;
            if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) GameSettings.SfxVolume -= step;
            if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) GameSettings.SfxVolume += step;
            if (k.leftArrowKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame
                || k.aKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame)
            {
                RefreshVolumeTexts();
                Menu();
            }
        }

        void StartRun()
        {
            SetState(State.Playing);
            if (run != null) run.BeginRun();
            Menu();
        }

        void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnFinished()
        {
            if (endSummary != null && run != null)
            {
                int s = Mathf.FloorToInt(run.ElapsedTime);
                endSummary.text = string.Format("PACKAGE DELIVERED\n\nTime  {0}:{1:00}\nChips  {2} / {3}\nResets  {4}\n\nPress R to run again",
                    s / 60, s % 60, run.ChipsCollected, run.ChipsTotal, run.Deaths);
            }
            SetState(State.Finished);
            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Finish);
        }

        void OnFailed()
        {
            SetState(State.Failed);
            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Fail);
        }

        void OnDistrict(int index, string name)
        {
            if (districtBanner != null && districtBannerText != null)
            {
                districtBannerText.text = string.Format("DISTRICT {0}\n{1}", index, name.ToUpperInvariant());
                districtBanner.SetActive(true);
                bannerTimer = 2.5f;
            }
            if (index >= 3 && AudioManager.Instance != null) AudioManager.Instance.PlayFinalDistrictMusic();
        }

        void SetState(State state)
        {
            Current = state;
            bool playing = state == State.Playing;
            Time.timeScale = state == State.Paused ? 0f : 1f;
            if (player != null) player.MovementEnabled = playing;
            if (hudRoot != null) hudRoot.SetActive(state == State.Playing || state == State.Paused);
            if (titlePanel != null) titlePanel.SetActive(state == State.Title);
            if (pausePanel != null) pausePanel.SetActive(state == State.Paused);
            if (endPanel != null) endPanel.SetActive(state == State.Finished);
            if (failPanel != null) failPanel.SetActive(state == State.Failed);
            if (creditsPanel != null) creditsPanel.SetActive(state == State.Credits);
        }

        void RefreshVolumeTexts()
        {
            if (musicVolumeText != null) musicVolumeText.text = string.Format("Music  {0}%   (A/D)", Mathf.RoundToInt(GameSettings.MusicVolume * 100f));
            if (sfxVolumeText != null) sfxVolumeText.text = string.Format("Sound  {0}%   (S/W)", Mathf.RoundToInt(GameSettings.SfxVolume * 100f));
        }

        static void Menu()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.Play(Sfx.Menu);
        }
    }
}
