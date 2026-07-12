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

    [Header("Shop")]
    [SerializeField] private SeedShopCatalog seedShopCatalog;
    public SeedShopCatalog SeedShopCatalog => seedShopCatalog;

    [SerializeField] private RelicShopCatalog relicShopCatalog;
    public RelicShopCatalog RelicShopCatalog => relicShopCatalog;


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

    private void Start()
    {
        if (SaveGameService.BootMode == BootMode.Load && SaveGameService.HasSave)
            SaveGameService.Load(this);
        else
            Initialize();

        SaveGameService.BootMode = BootMode.None;
        isInitialized = true;
        SaveGameService.BindAutosave(this);
    }

    private void Initialize()
    {
        ResetTimer();

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
        if (timerStarted)
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
        if (timerStarted) playTime += Time.deltaTime;
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
    }

#if UNITY_EDITOR
    public void EditorAssign(RelicShopService shopService, RelicShopCatalog shopCatalog)
    {
        relicShopService = shopService;
        relicShopCatalog = shopCatalog;
    }
#endif
}
