using System.Collections;
using UnityEngine;

namespace Flipside
{
    public enum Sfx { Jump, Land, Footstep, Flip, Chip, Hit, Checkpoint, Menu, Finish, Fail, Portal, Switch, Crumble }

    /// <summary>
    /// Plays one-shot effects through a small pool and cross-fades between the ambient
    /// music loop and its final-district variant. Volumes come from GameSettings.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [System.Serializable]
        public class SfxEntry
        {
            public Sfx id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            public float pitchVariation = 0.05f;
            [Tooltip("Minimum seconds between two plays of this effect.")]
            public float minInterval = 0.03f;
            [System.NonSerialized] public float lastPlayTime = -10f;
        }

        [SerializeField] SfxEntry[] effects;
        [SerializeField] AudioClip musicMain;
        [SerializeField] AudioClip musicFinal;
        [SerializeField] float musicFadeSeconds = 2f;
        [SerializeField] int voices = 6;

        AudioSource[] pool;
        AudioSource musicA;
        AudioSource musicB;
        AudioSource activeMusic;
        int nextVoice;

        void Awake()
        {
            Instance = this;
            pool = new AudioSource[voices];
            for (int i = 0; i < voices; i++)
            {
                pool[i] = gameObject.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
            }
            musicA = gameObject.AddComponent<AudioSource>();
            musicB = gameObject.AddComponent<AudioSource>();
            foreach (AudioSource m in new[] { musicA, musicB })
            {
                m.loop = true;
                m.playOnAwake = false;
            }
            GameSettings.Changed += ApplyVolumes;
        }

        void OnDestroy()
        {
            GameSettings.Changed -= ApplyVolumes;
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (musicMain != null) PlayMusic(musicMain, true);
        }

        public void Play(Sfx id)
        {
            if (effects == null) return;
            foreach (SfxEntry e in effects)
            {
                if (e.id != id || e.clip == null) continue;
                if (Time.unscaledTime - e.lastPlayTime < e.minInterval) return;
                e.lastPlayTime = Time.unscaledTime;
                AudioSource voice = pool[nextVoice];
                nextVoice = (nextVoice + 1) % pool.Length;
                voice.pitch = 1f + Random.Range(-e.pitchVariation, e.pitchVariation);
                voice.PlayOneShot(e.clip, e.volume * GameSettings.SfxVolume);
                return;
            }
        }

        public void PlayFinalDistrictMusic()
        {
            if (musicFinal != null && activeMusic != null && activeMusic.clip != musicFinal) PlayMusic(musicFinal, false);
        }

        void PlayMusic(AudioClip clip, bool immediate)
        {
            AudioSource next = activeMusic == musicA ? musicB : musicA;
            next.clip = clip;
            next.volume = immediate ? GameSettings.MusicVolume : 0f;
            next.Play();
            AudioSource previous = activeMusic;
            activeMusic = next;
            if (!immediate) StartCoroutine(CrossFade(previous, next));
            else if (previous != null) previous.Stop();
        }

        IEnumerator CrossFade(AudioSource from, AudioSource to)
        {
            float t = 0f;
            while (t < musicFadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = t / musicFadeSeconds;
                float target = GameSettings.MusicVolume;
                if (from != null) from.volume = Mathf.Lerp(target, 0f, k);
                to.volume = Mathf.Lerp(0f, target, k);
                yield return null;
            }
            if (from != null) from.Stop();
        }

        void ApplyVolumes()
        {
            if (activeMusic != null) activeMusic.volume = GameSettings.MusicVolume;
        }
    }
}
