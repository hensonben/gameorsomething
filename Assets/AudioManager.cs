using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Central audio manager. Persists across scene loads. Every other script
/// in the game should call into this via AudioManager.Instance rather than
/// holding its own AudioSource -- e.g.:
///
///     AudioManager.Instance.PlaySFX("jump");
///     AudioManager.Instance.PlayMusic("level1_theme");
///
/// To add a new sound: don't edit this script. Instead, open the
/// SoundLibrary asset and add a new entry with a key and clip. This is
/// what keeps multiple people adding sounds from merge-conflicting on the
/// same file in git.
///
/// Setup:
/// 1. Create an empty GameObject in your first/bootstrap scene, name it
///    "AudioManager", attach this script.
/// 2. Assign a SoundLibrary asset to the library field.
/// 3. (Optional) Assign an AudioMixer and its exposed parameter names if
///    you want volume sliders in a settings menu.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Sound Data")]
    [SerializeField] private SoundLibrary library;

    [Header("SFX Pool")]
    [Tooltip("Number of simultaneous SFX AudioSources. If more sounds than " +
             "this play at once, the oldest-playing one gets reused/cut off.")]
    [SerializeField] private int sfxPoolSize = 8;

    [Header("Music")]
    [Tooltip("Default crossfade duration in seconds when switching tracks via PlayMusic.")]
    [SerializeField] private float defaultMusicFadeDuration = 1.0f;

    [Header("Mixer (optional)")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private string musicVolumeParam = "MusicVolume";
    [SerializeField] private string sfxVolumeParam = "SFXVolume";

    private List<AudioSource> sfxPool;
    private int nextPoolIndex;

    // Two sources so music can crossfade between tracks instead of hard-cutting.
    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private AudioSource activeMusicSource;
    private Coroutine musicFadeCoroutine;
    private string currentMusicKey;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Enforce singleton: if one already exists (e.g. from a
            // previous scene that carried over), destroy this duplicate.
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (library != null)
        {
            library.BuildLookups();
        }
        else
        {
            Debug.LogError($"{name}: AudioManager has no SoundLibrary assigned. No sounds will play.");
        }

        BuildSfxPool();
        BuildMusicSources();
    }

    private void BuildSfxPool()
    {
        sfxPool = new List<AudioSource>(sfxPoolSize);
        for (int i = 0; i < sfxPoolSize; i++)
        {
            GameObject sourceObj = new GameObject($"SFX_Source_{i}");
            sourceObj.transform.SetParent(transform);
            AudioSource source = sourceObj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            if (sfxMixerGroup != null) source.outputAudioMixerGroup = sfxMixerGroup;
            sfxPool.Add(source);
        }
    }

    private void BuildMusicSources()
    {
        musicSourceA = CreateMusicSource("Music_Source_A");
        musicSourceB = CreateMusicSource("Music_Source_B");
        activeMusicSource = musicSourceA;
    }

    private AudioSource CreateMusicSource(string sourceName)
    {
        GameObject sourceObj = new GameObject(sourceName);
        sourceObj.transform.SetParent(transform);
        AudioSource source = sourceObj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;
        if (musicMixerGroup != null) source.outputAudioMixerGroup = musicMixerGroup;
        return source;
    }

    /// <summary>Plays a one-shot sound effect by key. Overlapping calls are fine.</summary>
    public void PlaySFX(string key)
    {
        if (library == null) return;

        if (!library.TryGetSfx(key, out SoundLibrary.SoundEntry entry))
        {
            Debug.LogWarning($"AudioManager: no SFX found for key '{key}'.");
            return;
        }

        if (entry.clip == null)
        {
            Debug.LogWarning($"AudioManager: SFX entry '{key}' has no clip assigned.");
            return;
        }

        AudioSource source = GetNextSfxSource();
        source.clip = entry.clip;
        source.volume = entry.volume;
        source.pitch = Random.Range(entry.pitchRange.x, entry.pitchRange.y);
        source.Play();
    }

    private AudioSource GetNextSfxSource()
    {
        // First preference: a source that's currently free (not playing).
        for (int i = 0; i < sfxPool.Count; i++)
        {
            if (!sfxPool[i].isPlaying)
            {
                return sfxPool[i];
            }
        }

        // All busy: round-robin reuse. This may cut off whichever sound
        // was occupying that slot -- acceptable for a fixed-size pool
        // under heavy simultaneous SFX load. Raise sfxPoolSize if this
        // happens often in your game.
        AudioSource source = sfxPool[nextPoolIndex];
        nextPoolIndex = (nextPoolIndex + 1) % sfxPool.Count;
        return source;
    }

    /// <summary>
    /// Crossfades to a music track by key. Calling this with the track
    /// already playing does nothing (avoids restarting the same song).
    /// </summary>
    public void PlayMusic(string key, float fadeDuration = -1f)
    {
        if (library == null) return;
        if (key == currentMusicKey) return;

        if (!library.TryGetMusic(key, out SoundLibrary.SoundEntry entry))
        {
            Debug.LogWarning($"AudioManager: no music track found for key '{key}'.");
            return;
        }

        if (entry.clip == null)
        {
            Debug.LogWarning($"AudioManager: music entry '{key}' has no clip assigned.");
            return;
        }

        float duration = fadeDuration >= 0f ? fadeDuration : defaultMusicFadeDuration;

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        currentMusicKey = key;
        musicFadeCoroutine = StartCoroutine(CrossfadeMusic(entry, duration));
    }

    /// <summary>Fades music out to silence without starting a new track.</summary>
    public void StopMusic(float fadeDuration = -1f)
    {
        float duration = fadeDuration >= 0f ? fadeDuration : defaultMusicFadeDuration;

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        currentMusicKey = null;
        musicFadeCoroutine = StartCoroutine(FadeOutMusic(duration));
    }

    private IEnumerator CrossfadeMusic(SoundLibrary.SoundEntry entry, float duration)
    {
        AudioSource incoming = (activeMusicSource == musicSourceA) ? musicSourceB : musicSourceA;
        AudioSource outgoing = activeMusicSource;

        incoming.clip = entry.clip;
        incoming.volume = 0f;
        incoming.Play();

        float targetVolume = entry.volume;
        float elapsed = 0f;

        // Fading over unscaled time so music doesn't stall if the game is paused via Time.timeScale.
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? elapsed / duration : 1f;

            incoming.volume = Mathf.Lerp(0f, targetVolume, t);
            outgoing.volume = Mathf.Lerp(outgoing.volume, 0f, t);

            yield return null;
        }

        incoming.volume = targetVolume;
        outgoing.volume = 0f;
        outgoing.Stop();

        activeMusicSource = incoming;
        musicFadeCoroutine = null;
    }

    private IEnumerator FadeOutMusic(float duration)
    {
        AudioSource source = activeMusicSource;
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration > 0f ? elapsed / duration : 1f;
            source.volume = Mathf.Lerp(startVolume, 0f, t);
            yield return null;
        }

        source.volume = 0f;
        source.Stop();
        musicFadeCoroutine = null;
    }

    /// <summary>Set music bus volume, 0-1. Requires audioMixer and musicVolumeParam to be set up.</summary>
    public void SetMusicVolume(float linearVolume)
    {
        SetMixerVolume(musicVolumeParam, linearVolume);
    }

    /// <summary>Set SFX bus volume, 0-1. Requires audioMixer and sfxVolumeParam to be set up.</summary>
    public void SetSFXVolume(float linearVolume)
    {
        SetMixerVolume(sfxVolumeParam, linearVolume);
    }

    private void SetMixerVolume(string paramName, float linearVolume)
    {
        if (audioMixer == null || string.IsNullOrEmpty(paramName)) return;

        // AudioMixer volume parameters are in decibels, not linear 0-1.
        // This converts a normal 0-1 slider value into the right dB range.
        float clamped = Mathf.Clamp(linearVolume, 0.0001f, 1f);
        float dB = Mathf.Log10(clamped) * 20f;
        audioMixer.SetFloat(paramName, dB);
    }
}