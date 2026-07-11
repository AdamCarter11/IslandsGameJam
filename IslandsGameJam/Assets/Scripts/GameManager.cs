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

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        worldManager.Initialize();
    }
}
