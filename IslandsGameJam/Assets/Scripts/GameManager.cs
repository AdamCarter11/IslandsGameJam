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


    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        worldManager.Initialize();
    }
}
