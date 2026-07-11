using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Wires the authored GameHUD prefab and tracks shop open state for world-input gating.
/// </summary>
public class ShopController : MonoBehaviour
{
    public static ShopController Main { get; private set; }

    [SerializeField] GameObject shopPanelRoot;
    [SerializeField] Button shopOpenButton;
    [SerializeField] GoldHUD goldHud;
    [SerializeField] HotbarUI hotbarUi;
    [SerializeField] ShopPanelUI shopPanelUi;

    public bool IsOpen { get; private set; }

    Inventory inventory;

    void Awake()
    {
        if (Main != null && Main != this)
        {
            Destroy(gameObject);
            return;
        }
        Main = this;
    }

    void Start()
    {
        EnsureEventSystem();

        inventory = GameManager.Main != null ? GameManager.Main.Inventory : FindFirstObjectByType<Inventory>();

        if (shopOpenButton != null)
        {
            shopOpenButton.onClick.RemoveAllListeners();
            shopOpenButton.onClick.AddListener(ToggleShop);
        }

        goldHud?.Initialize(inventory);
        hotbarUi?.Initialize(inventory);
        shopPanelUi?.Initialize(inventory, CloseShop);

        if (shopPanelRoot != null)
            shopPanelRoot.SetActive(false);
        IsOpen = false;
    }

    void OnDestroy()
    {
        if (Main == this)
            Main = null;
    }

    void Update()
    {
        if (Keyboard.current == null)
            return;

        if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseShop();
    }

    public void ToggleShop()
    {
        if (IsOpen)
            CloseShop();
        else
            OpenShop();
    }

    public void OpenShop()
    {
        if (IsOpen)
            return;

        if (ToolModeController.Main != null)
            ToolModeController.Main.ClearAllModes();

        IsOpen = true;
        if (shopPanelRoot != null)
            shopPanelRoot.SetActive(true);
    }

    public void CloseShop()
    {
        if (!IsOpen)
            return;
        IsOpen = false;
        if (shopPanelRoot != null)
            shopPanelRoot.SetActive(false);
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            if (EventSystem.current.GetComponent<InputSystemUIInputModule>() == null
                && EventSystem.current.GetComponent<BaseInputModule>() == null)
            {
                EventSystem.current.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            return;
        }

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

#if UNITY_EDITOR
    public void EditorAssign(
        GameObject panelRoot,
        Button openButton,
        GoldHUD gold,
        HotbarUI hotbar,
        ShopPanelUI shopPanel)
    {
        shopPanelRoot = panelRoot;
        shopOpenButton = openButton;
        goldHud = gold;
        hotbarUi = hotbar;
        shopPanelUi = shopPanel;
    }
#endif
}
