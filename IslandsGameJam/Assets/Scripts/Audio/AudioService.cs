using UnityEngine;

/// <summary>
/// Jam-style audio hub: one-shot SFX and looping music via separate AudioSources.
/// Access via GameManager.Main.AudioService or AudioService.Instance (e.g. Main Menu).
/// Missing clips/sources are no-ops. Scene-local — no DontDestroyOnLoad.
/// </summary>
public class AudioService : MonoBehaviour
{
    public static AudioService Instance { get; private set; }

    [Header("Sources")]
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource musicSource;

    [Header("Volumes")]
    [SerializeField] [Range(0f, 1f)] float sfxVolume = 1f;
    [SerializeField] [Range(0f, 1f)] float musicVolume = 1f;

    [Header("SFX Clips")]
    [SerializeField] AudioClip plantClip;
    [SerializeField] AudioClip waterClip;
    [SerializeField] AudioClip harvestClip;
    [SerializeField] AudioClip destroyClip;
    [SerializeField] AudioClip killClip;
    [SerializeField] AudioClip coinCollectClip;
    [SerializeField] AudioClip chunkUnlockClip;
    [SerializeField] AudioClip seedUnlockClip;
    [SerializeField] AudioClip shopOpenClip;
    [SerializeField] AudioClip shopCloseClip;
    [SerializeField] AudioClip buySeedClip;
    [SerializeField] AudioClip relicRollClip;
    [SerializeField] AudioClip relicSelectClip;
    [SerializeField] AudioClip uiClickClip;

    [Header("Music")]
    [SerializeField] AudioClip musicClip;

    [Header("Harvest Combo Pitch")]
    [SerializeField] float harvestPitchStep = 0.08f;
    [SerializeField] float harvestMaxPitch = 1.6f;

    private void Awake()
    {
        Instance = this;
        sfxVolume = AudioSettings.LoadSfxVolume();
        musicVolume = AudioSettings.LoadMusicVolume();
        ApplyVolumes();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnValidate()
    {
        ApplyVolumes();
    }

    private void OnEnable()
    {
        Coin.OnCoinCollected += HandleCoinCollected;
    }

    private void OnDisable()
    {
        Coin.OnCoinCollected -= HandleCoinCollected;
    }

    private void Start()
    {
        if (musicClip != null)
            PlayMusic();
    }

    public void PlayPlant() => PlaySfx(plantClip);
    public void PlayWater() => PlaySfx(waterClip);
    public void PlayHarvest(int comboIndex = 0)
    {
        float pitch = Mathf.Min(1f + comboIndex * harvestPitchStep, harvestMaxPitch);
        PlaySfxPitched(harvestClip, pitch);
    }
    public void PlayDestroy() => PlaySfx(destroyClip);
    public void PlayKill() => PlaySfx(killClip);
    public void PlayCoinCollect() => PlaySfx(coinCollectClip);
    public void PlayChunkUnlock() => PlaySfx(chunkUnlockClip);
    public void PlaySeedUnlock() => PlaySfx(seedUnlockClip);
    public void PlayShopOpen() => PlaySfx(shopOpenClip);
    public void PlayShopClose() => PlaySfx(shopCloseClip);
    public void PlayBuySeed() => PlaySfx(buySeedClip);
    public void PlayRelicRoll() => PlaySfx(relicRollClip);
    public void PlayRelicSelect() => PlaySfx(relicSelectClip);
    public void PlayUiClick() => PlaySfx(uiClickClip);

    public void PlayMusic()
    {
        if (musicSource == null || musicClip == null)
            return;

        musicSource.clip = musicClip;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null)
            return;

        musicSource.Stop();
    }

    public float GetSfxVolume() => sfxVolume;

    public float GetMusicVolume() => musicVolume;

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        AudioSettings.SaveSfxVolume(sfxVolume);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        AudioSettings.SaveMusicVolume(musicVolume);
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>
    /// Plays a one-shot at a custom pitch without changing the shared SFX source
    /// (PlayOneShot inherits AudioSource.pitch continuously).
    /// </summary>
    private void PlaySfxPitched(AudioClip clip, float pitch)
    {
        if (clip == null)
            return;

        var go = new GameObject("SFX_Pitched");
        go.transform.SetParent(transform);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.clip = clip;
        src.volume = sfxVolume;
        src.pitch = Mathf.Max(0.01f, pitch);
        src.Play();
        Destroy(go, clip.length / src.pitch + 0.05f);
    }

    private void HandleCoinCollected(int amount, Vector3 position)
    {
        PlayCoinCollect();
    }

    private void ApplyVolumes()
    {
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
        if (musicSource != null)
            musicSource.volume = musicVolume;
    }
}
