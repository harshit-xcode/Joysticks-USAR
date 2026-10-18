using System;
using UnityEngine;

// User gains multiply the scene's authored music/SFX balance independently.
public static class GameVolume
{
    public const string PreferenceKey = "Joysticks.MasterVolume"; // Previous releases; migration only.
    public const string MusicPreferenceKey = "Joysticks.MusicVolume";
    public const string SfxPreferenceKey = "Joysticks.SfxVolume";
    public static float Music { get; private set; } = 1f;
    public static float Sfx { get; private set; } = 1f;
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Restore()
    {
        float legacy = Mathf.Clamp01(PlayerPrefs.GetFloat(PreferenceKey, 1f));
        Music = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicPreferenceKey, legacy));
        Sfx = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxPreferenceKey, legacy));
        // The former master gain is now represented by the two category settings.
        AudioListener.volume = 1f;
        if (!PlayerPrefs.HasKey(MusicPreferenceKey) || !PlayerPrefs.HasKey(SfxPreferenceKey)) Save();
        Changed?.Invoke();
    }

    public static void SetMusic(float value)
    {
        Music = Mathf.Clamp01(value);
        Changed?.Invoke();
        Save();
    }

    public static void SetSfx(float value)
    {
        Sfx = Mathf.Clamp01(value);
        Changed?.Invoke();
        Save();
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(MusicPreferenceKey, Music);
        PlayerPrefs.SetFloat(SfxPreferenceKey, Sfx);
        PlayerPrefs.Save();
    }
}
