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
    [SerializeField]
    private WorldManager worldManager;
    public WorldManager WorldManager => worldManager;

    [SerializeField] private Inventory inventory;
    public Inventory Inventory => inventory;

    [SerializeField] private CropStateResolver cropStateResolver;
    public CropStateResolver CropStateResolver => cropStateResolver;

    [SerializeField] private CropSystem cropSystem;
    public CropSystem CropSystem => cropSystem;

    [Header("Shop")]
    [SerializeField] private SeedShopCatalog seedShopCatalog;
    public SeedShopCatalog SeedShopCatalog => seedShopCatalog;

    [Header("Runtime")]
    [SerializeField]
    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;


    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (inventory != null)
            inventory.InitializeFromCatalog(seedShopCatalog);

        worldManager.Initialize();
        isInitialized = true;
    }
}
