using System;
using UnityEngine;

/// <summary>
/// Holds all sound/music clips as key-based entries. Teammates add new
/// sounds by editing THIS ASSET in the Inspector (dragging in a clip and
/// giving it a key) -- not by editing AudioManager.cs. This is what keeps
/// multiple people adding sounds from constantly merge-conflicting on the
/// same script file in git.
///
/// Create one via: Right-click in Project window > Create > Audio > Sound Library
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    public class SoundEntry
    {
        [Tooltip("The string key other scripts use to play this sound, e.g. jump, button_press, level1_theme.")]
        public string key;

        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Tooltip("Randomizes pitch slightly each time this plays, so repeated sounds like footsteps don't sound robotic. Set both to 1 for no variation.")]
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    }

    [Header("Sound Effects")]
    public SoundEntry[] soundEffects;

    [Header("Music Tracks")]
    public SoundEntry[] musicTracks;

    private System.Collections.Generic.Dictionary<string, SoundEntry> sfxLookup;
    private System.Collections.Generic.Dictionary<string, SoundEntry> musicLookup;

    /// <summary>Call once at startup (AudioManager does this automatically) to build fast lookups.</summary>
    public void BuildLookups()
    {
        sfxLookup = new System.Collections.Generic.Dictionary<string, SoundEntry>();
        foreach (SoundEntry entry in soundEffects)
        {
            if (string.IsNullOrEmpty(entry.key))
            {
                Debug.LogWarning($"SoundLibrary '{name}': a sound effect entry has no key set and will be skipped.");
                continue;
            }

            if (sfxLookup.ContainsKey(entry.key))
            {
                Debug.LogWarning($"SoundLibrary '{name}': duplicate SFX key '{entry.key}'. Only the first will be used.");
                continue;
            }

            sfxLookup[entry.key] = entry;
        }

        musicLookup = new System.Collections.Generic.Dictionary<string, SoundEntry>();
        foreach (SoundEntry entry in musicTracks)
        {
            if (string.IsNullOrEmpty(entry.key))
            {
                Debug.LogWarning($"SoundLibrary '{name}': a music entry has no key set and will be skipped.");
                continue;
            }

            if (musicLookup.ContainsKey(entry.key))
            {
                Debug.LogWarning($"SoundLibrary '{name}': duplicate music key '{entry.key}'. Only the first will be used.");
                continue;
            }

            musicLookup[entry.key] = entry;
        }
    }

    public bool TryGetSfx(string key, out SoundEntry entry)
    {
        if (sfxLookup == null) BuildLookups();
        return sfxLookup.TryGetValue(key, out entry);
    }

    public bool TryGetMusic(string key, out SoundEntry entry)
    {
        if (musicLookup == null) BuildLookups();
        return musicLookup.TryGetValue(key, out entry);
    }
}