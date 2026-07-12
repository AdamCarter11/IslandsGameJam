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
        if (inventory != null)
        {
            inventory.ClearForNewGame();
            inventory.InitializeFromCatalog(seedShopCatalog);
        }

        if (relicShopService != null)
            relicShopService.ResetForNewGame();

        worldManager.Initialize();
    }

#if UNITY_EDITOR
    public void EditorAssign(RelicShopService shopService, RelicShopCatalog shopCatalog)
    {
        relicShopService = shopService;
        relicShopCatalog = shopCatalog;
    }
#endif
}
