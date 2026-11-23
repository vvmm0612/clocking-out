using System.Collections.Generic;
using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>
    /// Sfx from Resources/Audio/Sfx/&lt;name&gt;, music from Resources/Audio/Bgm/&lt;name&gt;.
    /// Missing clips are skipped silently (and only looked up once). Full list: docs/audio-list.md.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string KeyMusic = "co_vol_music";
        private const string KeySfx = "co_vol_sfx";

        private AudioSource sfxSource;
        private AudioSource bgmSource;
        private readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
        private readonly HashSet<string> missing = new HashSet<string>();
        private float musicVolume = 0.6f;
        private float sfxVolume = 0.8f;
        private string currentBgm = "";
        private float lastHoverTime;

        public float MusicVolume { get { return musicVolume; } }
        public float SfxVolume { get { return sfxVolume; } }

        private void Awake()
        {
            Instance = this;
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
            musicVolume = PlayerPrefs.GetFloat(KeyMusic, 0.6f);
            sfxVolume = PlayerPrefs.GetFloat(KeySfx, 0.8f);
            bgmSource.volume = musicVolume;
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
            AudioClip clip = Load("Audio/Sfx/" + name);
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        private void PlayBgmInternal(string name)
        {
            if (bgmSource == null) return;
            if (string.IsNullOrEmpty(name)) { bgmSource.Stop(); currentBgm = ""; return; }
            if (name == currentBgm && bgmSource.isPlaying) return;
            AudioClip clip = Load("Audio/Bgm/" + name);
            if (clip == null) return; // keep whatever is playing
            currentBgm = name;
            bgmSource.clip = clip;
            bgmSource.volume = musicVolume;
            bgmSource.Play();
        }

        public void SetMusicVolume(float v)
        {
            musicVolume = Mathf.Clamp01(v);
            if (bgmSource != null) bgmSource.volume = musicVolume;
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
