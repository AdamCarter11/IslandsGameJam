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
    [SerializeField] HighestGoldUI highestGoldUi;
    [SerializeField] TimerUI timerUi;
    [SerializeField] ShopPanelUI shopPanelUi;
    [SerializeField] RelicChoicePanelUI relicChoicePanelUi;
    [SerializeField] RelicInventoryPanelUI relicInventoryPanelUi;
    [SerializeField] Button relicsOpenButton;
    [SerializeField] OptionsPanelUI optionsPanel;
    [SerializeField] Button optionButton;

    public bool IsOpen { get; private set; }

    public bool IsOptionsOpen => optionsPanel != null && optionsPanel.IsOpen;

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

        if (relicsOpenButton != null)
        {
            relicsOpenButton.onClick.RemoveAllListeners();
            relicsOpenButton.onClick.AddListener(ToggleRelicInventory);
        }

        if (optionButton == null)
            optionButton = FindButtonByName("OptionButton");

        if (optionButton != null)
        {
            optionButton.onClick.RemoveAllListeners();
            optionButton.onClick.AddListener(OpenOptions);
        }

        goldHud?.Initialize(inventory);
        hotbarUi?.Initialize(inventory);

        if (highestGoldUi == null)
            highestGoldUi = GetComponentInChildren<HighestGoldUI>(true);
        if (timerUi == null)
            timerUi = GetComponentInChildren<TimerUI>(true);

        highestGoldUi?.Initialize(inventory);
        timerUi?.Initialize(GameManager.Main != null ? GameManager.Main : FindFirstObjectByType<GameManager>());

        if (relicChoicePanelUi == null && shopPanelUi != null)
            relicChoicePanelUi = shopPanelUi.GetComponentInChildren<RelicChoicePanelUI>(true);
        if (relicChoicePanelUi == null)
            relicChoicePanelUi = GetComponentInChildren<RelicChoicePanelUI>(true);

        if (relicInventoryPanelUi == null)
            relicInventoryPanelUi = GetComponentInChildren<RelicInventoryPanelUI>(true);

        if (optionsPanel == null)
            optionsPanel = GetComponentInChildren<OptionsPanelUI>(true);

        shopPanelUi?.Initialize(inventory, CloseShop, relicShop, relicChoicePanelUi);
        relicInventoryPanelUi?.Initialize(inventory);
        relicInventoryPanelUi?.Close();

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

        if (GameManager.Main != null && GameManager.Main.IsGameOver)
            return;

        // Options eats Escape (close) and blocks shop / relic shortcuts while open.
        if (IsOptionsOpen)
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
                CloseOptions();
            return;
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            ToggleShop();
            return;
        }

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            ToggleRelicInventory();
            return;
        }

        // Must pick a relic — Escape cannot dismiss the choice, shop, or relic inventory.
        if (IsRelicChoiceOpen)
            return;

        if (!Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        // Confirm dialog owns Escape while visible.
        if (GameManager.Main != null
            && GameManager.Main.ConfirmPanelUI != null
            && GameManager.Main.ConfirmPanelUI.IsVisible)
            return;

        // Relic inventory is independent of shop IsOpen; close it first if open.
        if (relicInventoryPanelUi != null && relicInventoryPanelUi.IsOpen)
        {
            relicInventoryPanelUi.Close();
            return;
        }

        if (IsOpen)
        {
            CloseShop();
            return;
        }

        OpenOptions();
    }

    public void ToggleShop()
    {
        if (IsGameOverBlocked() || IsRelicChoiceOpen || IsOptionsOpen)
            return;

        if (IsOpen)
            CloseShop();
        else
            OpenShop();
    }

    public void ToggleRelicInventory()
    {
        if (IsGameOverBlocked() || IsRelicChoiceOpen || IsOptionsOpen)
            return;

        relicInventoryPanelUi?.Toggle();
    }

    public void OpenShop()
    {
        if (IsGameOverBlocked() || IsOpen || IsOptionsOpen)
            return;

        if (ToolModeController.Main != null)
            ToolModeController.Main.ClearAllModes();

        IsOpen = true;
        if (shopPanelRoot != null)
            shopPanelRoot.SetActive(true);

        shopPanelUi?.RefreshRelicRollButton();
        GameManager.Main?.AudioService?.PlayShopOpen();
    }

    public void CloseShop()
    {
        if (!IsOpen || IsRelicChoiceOpen)
            return;
        IsOpen = false;
        if (shopPanelRoot != null)
            shopPanelRoot.SetActive(false);
        GameManager.Main?.AudioService?.PlayShopClose();
    }

    public void OpenOptions()
    {
        if (IsGameOverBlocked() || optionsPanel == null || IsOptionsOpen || IsRelicChoiceOpen)
            return;

        if (IsOpen)
            CloseShop();

        if (relicInventoryPanelUi != null && relicInventoryPanelUi.IsOpen)
            relicInventoryPanelUi.Close();

        if (ToolModeController.Main != null)
            ToolModeController.Main.ClearAllModes();

        GameManager.Main?.AudioService?.PlayUiClick();
        optionsPanel.Open();
    }

    public void CloseOptions()
    {
        if (!IsOptionsOpen)
            return;
        optionsPanel.Close();
    }

    static bool IsGameOverBlocked() =>
        GameManager.Main != null && GameManager.Main.IsGameOver;

    Button FindButtonByName(string buttonName)
    {
        foreach (var button in GetComponentsInChildren<Button>(true))
        {
            if (button.gameObject.name == buttonName)
                return button;
        }

        return null;
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
        RelicChoicePanelUI choicePanel = null,
        RelicInventoryPanelUI relicInventoryPanel = null,
        Button relicsButton = null,
        HighestGoldUI highestGold = null,
        TimerUI timer = null,
        OptionsPanelUI options = null,
        Button optionsButton = null)
    {
        shopPanelRoot = panelRoot;
        shopOpenButton = openButton;
        goldHud = gold;
        hotbarUi = hotbar;
        highestGoldUi = highestGold;
        timerUi = timer;
        shopPanelUi = shopPanel;
        relicChoicePanelUi = choicePanel;
        relicInventoryPanelUi = relicInventoryPanel;
        relicsOpenButton = relicsButton;
        optionsPanel = options;
        optionButton = optionsButton;
    }
#endif
}
