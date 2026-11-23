using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>
    /// Sfx from Resources/Audio/Sfx/&lt;name&gt;, music from Resources/Audio/Bgm/&lt;name&gt; (title, level, credits).
    /// Missing clips are skipped silently (and only looked up once). Music cross-fades between two sources
    /// on unscaled time. Full list: docs/audio-list.md. Import settings: Assets/Editor/AudioImportRules.cs.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public const string SfxFolder = "Audio/Sfx/";
        public const string BgmFolder = "Audio/Bgm/";
        public const float FadeSeconds = 0.8f;

        private const string KeyMusic = "co_vol_music";
        private const string KeySfx = "co_vol_sfx";

        private AudioSource sfxSource;
        private AudioSource[] bgm = new AudioSource[2];
        private int activeBgm;
        private readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        private readonly HashSet<string> missing = new HashSet<string>();
        private float musicVolume = 0.6f;
        private float sfxVolume = 0.8f;
        private string currentBgm = "";
        private float lastHoverTime;
        private Coroutine fade;

        // ---- debug counters (read by the PlayMode playtest; cheap enough to keep in builds)
        private static readonly Dictionary<string, int> sfxPlays = new Dictionary<string, int>();
        private static readonly List<string> bgmHistory = new List<string>();
        /// <summary>Sfx that actually started (clip found), by name.</summary>
        public static int SfxPlayCount(string name) { int n; return sfxPlays.TryGetValue(name, out n) ? n : 0; }
        public static int TotalSfxPlays { get; private set; }
        /// <summary>Every music track that actually started, in order.</summary>
        public static IList<string> BgmHistory { get { return bgmHistory; } }
        public static void ResetCounters() { sfxPlays.Clear(); bgmHistory.Clear(); TotalSfxPlays = 0; }

        public float MusicVolume { get { return musicVolume; } }
        public float SfxVolume { get { return sfxVolume; } }
        public string CurrentBgm { get { return currentBgm; } }

        private void Awake()
        {
            Instance = this;
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            for (int i = 0; i < 2; i++)
            {
                AudioSource s = gameObject.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.loop = true;
                s.spatialBlend = 0f;
                s.volume = 0f;
                bgm[i] = s;
            }
            musicVolume = PlayerPrefs.GetFloat(KeyMusic, 0.6f);
            sfxVolume = PlayerPrefs.GetFloat(KeySfx, 0.8f);
        }

        public static void PlaySfx(string name)
        {
            if (Instance != null) Instance.PlaySfxInternal(name);
        }

        public static void PlayBgm(string name)
        {
            if (Instance != null) Instance.PlayBgmInternal(name);
        }

        private AudioClip Load(string path)
        {
            AudioClip clip;
            if (cache.TryGetValue(path, out clip)) return clip;
            if (missing.Contains(path)) return null;
            clip = Resources.Load<AudioClip>(path);
            if (clip == null) { missing.Add(path); return null; }
            cache[path] = clip;
            return clip;
        }

        private void PlaySfxInternal(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (name == "hover")
            {
                // Avoid a machine-gun of hover ticks when sweeping over lists.
                if (Time.unscaledTime - lastHoverTime < 0.05f) return;
                lastHoverTime = Time.unscaledTime;
            }
            AudioClip clip = Load(SfxFolder + name);
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
            int n;
            sfxPlays[name] = sfxPlays.TryGetValue(name, out n) ? n + 1 : 1;
            TotalSfxPlays++;
        }

        private void PlayBgmInternal(string name)
        {
            if (bgm[0] == null) return;
            if (string.IsNullOrEmpty(name))
            {
                if (currentBgm.Length == 0) return;
                currentBgm = "";
                StartFade(null);
                return;
            }
            if (name == currentBgm && bgm[activeBgm].isPlaying) return;
            AudioClip clip = Load(BgmFolder + name);
            if (clip == null) return; // keep whatever is playing
            currentBgm = name;
            StartFade(clip);
            bgmHistory.Add(name);
        }

        /// <summary>Fades the current track out while the new one (or silence) fades in.</summary>
        private void StartFade(AudioClip next)
        {
            if (fade != null) StopCoroutine(fade);
            AudioSource from = bgm[activeBgm];
            AudioSource to = null;
            if (next != null)
            {
                activeBgm = 1 - activeBgm;
                to = bgm[activeBgm];
                to.clip = next;
                to.volume = 0f;
                to.Play();
            }
            fade = StartCoroutine(Fade(from, to));
        }

        private IEnumerator Fade(AudioSource from, AudioSource to)
        {
            float startFrom = from != null ? from.volume : 0f;
            float t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / FadeSeconds);
                if (from != null && from != to) from.volume = startFrom * (1f - k);
                if (to != null) to.volume = musicVolume * k;
                yield return null;
            }
            if (from != null && from != to) { from.volume = 0f; from.Stop(); }
            if (to != null) to.volume = musicVolume;
            fade = null;
        }

        public void SetMusicVolume(float v)
        {
            musicVolume = Mathf.Clamp01(v);
            if (fade == null && bgm[activeBgm] != null && bgm[activeBgm].isPlaying) bgm[activeBgm].volume = musicVolume;
            PlayerPrefs.SetFloat(KeyMusic, musicVolume);
            PlayerPrefs.Save();
        }

        public void SetSfxVolume(float v)
        {
            sfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(KeySfx, sfxVolume);
            PlayerPrefs.Save();
        }
    }
}
