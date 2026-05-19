using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneAtelier.Audio
{
    public sealed class AudioManager : MonoBehaviour
    {
        private const float DefaultMusicVolume = 0.6f;
        private const float DefaultSFXVolume = 0.8f;
        private const float DefaultFadeDuration = 0.6f;

        // ---------------------------------------------------------------------------
        // Resources filename map — files live in Assets/.../Audio/Resources/
        // Add/rename entries here when new clips are placed in the Resources folder.
        // ---------------------------------------------------------------------------
        private static readonly Dictionary<MusicTrack, string> MusicFiles = new()
        {
            { MusicTrack.MainMenu,     "menu_theme" },
            { MusicTrack.Prologue,     "prologue_theme" },
            { MusicTrack.Workshop,     "workshop_scene_loop" },
            { MusicTrack.Battle,       "battle_scene_loop" },
            { MusicTrack.VictorySting, "victory_sting" },
            { MusicTrack.DefeatSting,  "defeat_sting" },
        };

        private static readonly Dictionary<SFXType, string> SFXFiles = new()
        {
            { SFXType.NodePlacement,               "sfx_node_placement" },
            { SFXType.NodeRotation,                "sfx_node_rotation" },
            { SFXType.NodeRemoval,                 "sfx_node_removal" },
            { SFXType.ElementProductionTick,       "sfx_element_tick" },
            { SFXType.SpellCardOutputBasic,        "sfx_card_output_basic" },
            { SFXType.SpellCardOutputIntermediate, "sfx_card_output_intermediate" },
            { SFXType.SpellCardOutputAdvanced,     "sfx_card_output_advanced" },
            { SFXType.PayloadCommit,               "sfx_payload_commit" },
            { SFXType.ButtonClick,                 "sfx_button_click" },
            { SFXType.ButtonHover,                 "sfx_button_hover" },
            { SFXType.BoonDrawerOpen,              "sfx_drawer_open" },
            { SFXType.BoonDrawerClose,             "sfx_drawer_close" },
            { SFXType.UnlockNotification,          "sfx_unlock" },
            { SFXType.ErrorBuzz,                   "sfx_error" },
            { SFXType.CardDraw,                    "sfx_card_draw" },
            { SFXType.CardPlayWhoosh,              "sfx_card_play" },
            { SFXType.AttackHitGeneric,            "sfx_hit_generic" },
            { SFXType.FireHit,                     "sfx_hit_fire" },
            { SFXType.WaterHit,                    "sfx_hit_water" },
            { SFXType.WindHit,                     "sfx_hit_wind" },
            { SFXType.EarthHit,                    "sfx_hit_earth" },
            { SFXType.IceHit,                      "sfx_hit_ice" },
            { SFXType.ThunderHit,                  "sfx_hit_thunder" },
            { SFXType.LightHit,                    "sfx_hit_light" },
            { SFXType.DarkHit,                     "sfx_hit_dark" },
            { SFXType.HealRestore,                 "sfx_heal" },
            { SFXType.ShieldBlock,                 "sfx_shield" },
            { SFXType.PlayerHurt,                  "sfx_player_hurt" },
            { SFXType.EnemyHurt,                   "sfx_enemy_hurt" },
            { SFXType.EnemyDefeat,                 "sfx_enemy_defeat" },
            { SFXType.PlayerDefeat,                "sfx_player_defeat" },
            { SFXType.EndTurnConfirm,              "sfx_end_turn" },
        };

        // Runtime cache — clips loaded on first use, never reloaded
        private readonly Dictionary<MusicTrack, AudioClip> musicCache = new();
        private readonly Dictionary<SFXType, AudioClip> sfxCache = new();

        [Header("Settings")]
        [SerializeField] private float musicVolume = DefaultMusicVolume;
        [SerializeField] private float sfxVolume = DefaultSFXVolume;
        [SerializeField] private float fadeDuration = DefaultFadeDuration;

        private AudioSource musicSource;
        private AudioSource sfxSource;
        private Coroutine fadeCoroutine;
        private MusicTrack currentTrack = MusicTrack.None;

        private static AudioManager instance;

        public static AudioManager Instance
        {
            get
            {
                if (instance != null) return instance;

                instance = FindFirstObjectByType<AudioManager>();
                if (instance != null) return instance;

                var go = new GameObject("AudioManager");
                instance = go.AddComponent<AudioManager>();
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            gameObject.AddComponent<AudioListener>();
            SceneManager.sceneLoaded += OnSceneLoaded;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0f;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
        }

        // ----------------------------------------------------------------
        // Public API — Music
        // ----------------------------------------------------------------

        public static void PlayMusic(MusicTrack track)
        {
            if (Instance.currentTrack == track) return;
            var clip = Instance.GetMusicClip(track);
            if (clip == null) return;
            Instance.currentTrack = track;
            Instance.StartFade(clip, Instance.musicVolume);
        }

        public static void StopMusic()
        {
            Instance.currentTrack = MusicTrack.None;
            Instance.StartFade(null, 0f);
        }

        public static void SetMusicVolume(float volume)
        {
            Instance.musicVolume = Mathf.Clamp01(volume);
            if (Instance.musicSource != null)
                Instance.musicSource.volume = Instance.musicVolume;
        }

        // ----------------------------------------------------------------
        // Public API — SFX
        // ----------------------------------------------------------------

        public static void PlaySFX(SFXType type)
        {
            var clip = Instance.GetSFXClip(type);
            if (clip == null) return;
            Instance.sfxSource.PlayOneShot(clip, Instance.sfxVolume);
        }

        public static void SetSFXVolume(float volume)
        {
            Instance.sfxVolume = Mathf.Clamp01(volume);
            if (Instance.sfxSource != null)
                Instance.sfxSource.volume = Instance.sfxVolume;
        }

        // ----------------------------------------------------------------
        // Public API — Stings (victory / defeat, one-shot, not looped)
        // ----------------------------------------------------------------

        public static void PlaySting(MusicTrack track)
        {
            var clip = Instance.GetMusicClip(track);
            if (clip == null) return;
            Instance.sfxSource.PlayOneShot(clip, Instance.musicVolume * 0.3f);
        }

        // ----------------------------------------------------------------
        // Internal
        // ----------------------------------------------------------------

        private AudioClip GetMusicClip(MusicTrack track)
        {
            if (track == MusicTrack.None) return null;
            if (musicCache.TryGetValue(track, out var cached)) return cached;

            if (!MusicFiles.TryGetValue(track, out var filename)) return null;
            var clip = Resources.Load<AudioClip>(filename);
            if (clip == null)
                Debug.LogWarning($"[AudioManager] Music clip not found in Resources: '{filename}'");

            musicCache[track] = clip;
            return clip;
        }

        private AudioClip GetSFXClip(SFXType type)
        {
            if (type == SFXType.None) return null;
            if (sfxCache.TryGetValue(type, out var cached)) return cached;

            if (!SFXFiles.TryGetValue(type, out var filename)) return null;
            var clip = Resources.Load<AudioClip>(filename);
            if (clip == null)
                Debug.LogWarning($"[AudioManager] SFX clip not found in Resources: '{filename}'");

            sfxCache[type] = clip;
            return clip;
        }

        private void StartFade(AudioClip newClip, float targetVolume)
        {
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeMusic(newClip, targetVolume));
        }

        private System.Collections.IEnumerator FadeMusic(AudioClip newClip, float targetVolume)
        {
            // Fade out current
            if (musicSource.isPlaying)
            {
                float start = musicSource.volume;
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    musicSource.volume = Mathf.Lerp(start, 0f, elapsed / fadeDuration);
                    yield return null;
                }
                musicSource.Stop();
                musicSource.volume = 0f;
            }

            // Fade in new
            if (newClip != null)
            {
                musicSource.clip = newClip;
                musicSource.Play();
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeDuration);
                    yield return null;
                }
                musicSource.volume = targetVolume;
            }

            fadeCoroutine = null;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (instance == this) instance = null;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Disable any AudioListeners in the newly loaded scene —
            // we have our own persistent one on AudioManager.
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            {
                if (listener.gameObject != Instance.gameObject)
                    listener.enabled = false;
            }
        }
    }
}
