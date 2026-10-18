// AudioManager.cs
// Centralized audio controller for background music and gameplay/UI sound effects.
// Exposes Inspector slots for all audio assets so custom music and SFX can be swapped in easily.

using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music & Ambience (Assign Custom Assets Here)")]
    public AudioClip backgroundMusic;
    [Range(0f, 1f)] public float musicVolume = 0.28f;
    public bool playMusicOnStart = true;

    [Header("Robot Action Sound Effects")]
    public AudioClip walkSfx;
    public AudioClip jumpSfx;
    public AudioClip forceSfx;
    public AudioClip bumpSfx;
    public AudioClip lavaFailSfx;
    public AudioClip winSfx;

    [Header("Node Editor UI Sound Effects")]
    public AudioClip nodePlaceSfx;
    public AudioClip nodeConnectSfx;

    [Header("Master Settings")]
    [Range(0f, 1f)] public float sfxVolume = 0.75f;
    public bool isMuted = false;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureSources();
        GameVolume.Changed += RefreshVolumes;
    }

    private void Start()
    {
        if (playMusicOnStart && backgroundMusic != null && !musicSource.isPlaying)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            RefreshVolumes();
            musicSource.Play();
        }
    }

    private void EnsureSources()
    {
        AudioSource[] sources = GetComponents<AudioSource>();
        if (sources.Length >= 2)
        {
            musicSource = sources[0];
            sfxSource = sources[1];
        }
        else
        {
            musicSource = gameObject.GetComponent<AudioSource>();
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
        RefreshVolumes();
    }

    public void RefreshVolumes()
    {
        if (musicSource != null) musicSource.volume = isMuted ? 0f : musicVolume * GameVolume.Music;
        if (sfxSource != null) sfxSource.volume = isMuted ? 0f : sfxVolume * GameVolume.Sfx;
    }

    public void ToggleMute()
    {
        isMuted = !isMuted;
        RefreshVolumes();
    }

    private void OnDestroy()
    {
        GameVolume.Changed -= RefreshVolumes;
        if (Instance == this) Instance = null;
    }

    public void PlaySfx(AudioClip clip, float pitch = 1f, float volumeScale = 1f)
    {
        if (isMuted || clip == null) return;
        EnsureSources();
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, volumeScale);
    }

    public void PlayWalk() => PlaySfx(walkSfx, Random.Range(0.95f, 1.05f), 0.85f);
    public void PlayJump() => PlaySfx(jumpSfx, Random.Range(0.96f, 1.04f), 0.95f);
    public void PlayForce() => PlaySfx(forceSfx, 1.0f, 1.0f);
    public void PlayBump() => PlaySfx(bumpSfx, Random.Range(0.92f, 1.04f), 0.9f);
    public void PlayLavaFail() => PlaySfx(lavaFailSfx, 1.0f, 1.0f);
    public void PlayWin() => PlaySfx(winSfx, 1.0f, 1.0f);
    public void PlayNodePlace() => PlaySfx(nodePlaceSfx, Random.Range(0.95f, 1.06f), 0.7f);
    public void PlayNodeConnect() => PlaySfx(nodeConnectSfx, Random.Range(0.98f, 1.05f), 0.8f);
}
