using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Main { get; private set; }

    private void Awake()
    {
        if (Main == null) Main = this;
        else Destroy(gameObject);
    }

    [Header("Components")]
    [SerializeField] private WorldManager worldManager;
    public WorldManager WorldManager => worldManager;

    [SerializeField] private Inventory inventory;
    public Inventory Inventory => inventory;

    [SerializeField] private CropStateResolver cropStateResolver;
    public CropStateResolver CropStateResolver => cropStateResolver;

    [SerializeField] private CropSystem cropSystem;
    public CropSystem CropSystem => cropSystem;

    [SerializeField] private RelicShopService relicShopService;
    public RelicShopService RelicShopService => relicShopService;

    [SerializeField] private AudioService audioService;
    public AudioService AudioService => audioService;

    [SerializeField] private LandUnlockSystem landUnlockSystem;
    public LandUnlockSystem LandUnlockSystem => landUnlockSystem;

    [Header("UI")]
    [SerializeField] private ConfirmPanelUI confirmPanelUI;
    public ConfirmPanelUI ConfirmPanelUI => confirmPanelUI;
    [SerializeField] private LandCostUI landCostUI;
    public LandCostUI LandCostUI => landCostUI;
    [SerializeField] private GameOverUI gameOverUI;
    public GameOverUI GameOverUI => gameOverUI;

    [Header("Shop")]
    [SerializeField] private SeedShopCatalog seedShopCatalog;
    public SeedShopCatalog SeedShopCatalog => seedShopCatalog;

    [SerializeField] private RelicShopCatalog relicShopCatalog;
    public RelicShopCatalog RelicShopCatalog => relicShopCatalog;


    [Header("Timed Run")]
    [SerializeField] private float gameDurationSeconds = 20f * 60f; // 20 minutes
    public float GameDurationSeconds => gameDurationSeconds;

    [Header("Runtime")]
    [SerializeField]
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;
    [SerializeField]
    private bool timerStarted = false;
    public bool TimerStarted => timerStarted;
    [SerializeField]
    private float playTime = 0f;
    public float PlayTime => playTime;

    private bool isGameOver;
    public bool IsGameOver => isGameOver;

    private void Start()
    {
        if (SaveGameService.BootMode == BootMode.Load && SaveGameService.HasSave)
            SaveGameService.Load(this);
        else
            Initialize();

        SaveGameService.BootMode = BootMode.None;
        isInitialized = true;
        SaveGameService.BindAutosave(this);

        if (timerStarted && playTime >= gameDurationSeconds)
            EndGame();
    }

    private void OnDestroy()
    {
        if (Main == this)
        {
            Main = null;
            Time.timeScale = 1f;
        }
    }

    private void Initialize()
    {
        ResetTimer();
        isGameOver = false;
        Time.timeScale = 1f;

        if (inventory != null)
        {
            inventory.ClearForNewGame();
            inventory.InitializeFromCatalog(seedShopCatalog);
        }

        if (relicShopService != null)
            relicShopService.ResetForNewGame();

        cropSystem?.ResetRelicRuntimeState();

        worldManager.Initialize();
    }

    // Start timer on the first crop planted
    public void StartTimer()
    {
        if (timerStarted || isGameOver)
            return;

        timerStarted = true;
        SaveGameService.NotifyChanged();
    }

    public void ResetTimer()
    {
        timerStarted = false;
        playTime = 0f;
    }

    private void Update()
    {
        if (!timerStarted || isGameOver)
            return;

        playTime += Time.deltaTime;
        if (playTime >= gameDurationSeconds)
            EndGame();
    }

    /// <summary>
    /// Ends the timed run: freezes gameplay, updates highscore, deletes the mid-run save, shows UI.
    /// Idempotent.
    /// </summary>
    public void EndGame()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        int score = inventory != null ? inventory.HighestGold : 0;
        bool isNewRecord = HighscoreSettings.TrySetIfHigher(score);
        int highscore = HighscoreSettings.Get();

        SaveGameService.DeleteSave();
        Time.timeScale = 0f;

        EnsureGameOverUI();
        gameOverUI?.Show(score, highscore, isNewRecord);
    }

    public void PlayAgain()
    {
        GameOverUI.ReloadNewGame();
    }

    public void ReturnToMainMenu()
    {
        GameOverUI.LoadMainMenu();
    }

    void EnsureGameOverUI()
    {
        if (gameOverUI != null)
            return;

        gameOverUI = FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
        if (gameOverUI != null)
            return;

        var go = new GameObject("GameOverUI");
        gameOverUI = go.AddComponent<GameOverUI>();
    }

    public void CaptureTo(GameSaveData data)
    {
        if (data == null)
            return;

        data.timerStarted = timerStarted;
        data.playTime = playTime;
    }

    public void ApplyFrom(GameSaveData data)
    {
        if (data == null)
            return;

        timerStarted = data.timerStarted;
        playTime = Mathf.Max(0f, data.playTime);

        if (timerStarted && playTime >= gameDurationSeconds)
            EndGame();
    }

#if UNITY_EDITOR
    public void EditorAssign(RelicShopService shopService, RelicShopCatalog shopCatalog)
    {
        relicShopService = shopService;
        relicShopCatalog = shopCatalog;
    }
#endif
}
