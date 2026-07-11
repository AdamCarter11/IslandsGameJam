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
    [SerializeField] RelicChoicePanelUI relicChoicePanelUi;

    public bool IsOpen { get; private set; }

    public bool IsRelicChoiceOpen =>
        relicChoicePanelUi != null
            ? relicChoicePanelUi.IsOpen
            : shopPanelUi != null && shopPanelUi.IsRelicChoiceOpen;

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
        var relicShop = GameManager.Main != null ? GameManager.Main.RelicShopService : FindFirstObjectByType<RelicShopService>();

        if (shopOpenButton != null)
        {
            shopOpenButton.onClick.RemoveAllListeners();
            shopOpenButton.onClick.AddListener(ToggleShop);
        }

        goldHud?.Initialize(inventory);
        hotbarUi?.Initialize(inventory);

        if (relicChoicePanelUi == null && shopPanelUi != null)
            relicChoicePanelUi = shopPanelUi.GetComponentInChildren<RelicChoicePanelUI>(true);
        if (relicChoicePanelUi == null)
            relicChoicePanelUi = GetComponentInChildren<RelicChoicePanelUI>(true);

        shopPanelUi?.Initialize(inventory, CloseShop, relicShop, relicChoicePanelUi);

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

        // Must pick a relic — Escape cannot dismiss the choice or close the seed shop.
        if (IsRelicChoiceOpen)
            return;

        if (IsOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
            CloseShop();
    }

    public void ToggleShop()
    {
        if (IsRelicChoiceOpen)
            return;

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

        shopPanelUi?.RefreshRelicRollButton();
    }

    public void CloseShop()
    {
        if (!IsOpen || IsRelicChoiceOpen)
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
        ShopPanelUI shopPanel,
        RelicChoicePanelUI choicePanel = null)
    {
        shopPanelRoot = panelRoot;
        shopOpenButton = openButton;
        goldHud = gold;
        hotbarUi = hotbar;
        shopPanelUi = shopPanel;
        relicChoicePanelUi = choicePanel;
    }
#endif
}
