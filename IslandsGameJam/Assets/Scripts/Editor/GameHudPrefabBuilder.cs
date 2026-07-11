using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds/rewires GameHUD, HotbarSlot, and ShopRow prefabs and places GameHUD under MainGame's Game HUD.
/// </summary>
public static class GameHudPrefabBuilder
{
    const string PrefabFolder = "Assets/Prefabs/UI";
    const string HotbarSlotPath = PrefabFolder + "/HotbarSlot.prefab";
    const string ShopRowPath = PrefabFolder + "/ShopRow.prefab";
    const string GameHudPath = PrefabFolder + "/GameHUD.prefab";
    const string MainGamePath = "Assets/Scenes/MainGame.unity";

    const int SlotCount = 10;
    const float SlotSize = 56f;
    const float SlotGap = 6f;

    static readonly Color SlotColor = new(0.15f, 0.15f, 0.18f, 0.92f);
    static readonly Color PanelColor = new(0.08f, 0.09f, 0.12f, 0.96f);
    static readonly Color RowColor = new(0.16f, 0.17f, 0.22f, 1f);
    static readonly Color BuyColor = new(0.25f, 0.55f, 0.3f, 1f);

    [MenuItem("Tools/UI/Build Game HUD Prefabs")]
    public static void BuildAll()
    {
        EnsureFolder();

        var hotbarSlot = BuildHotbarSlotPrefab();
        var shopRow = BuildShopRowPrefab();
        BuildGameHudPrefab(hotbarSlot, shopRow);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GameHudPrefabBuilder] Wrote:\n  {HotbarSlotPath}\n  {ShopRowPath}\n  {GameHudPath}");
    }

    [MenuItem("Tools/UI/Wire Game HUD Into MainGame")]
    public static void WireMainGame()
    {
        BuildAll();

        var scene = EditorSceneManager.OpenScene(MainGamePath);
        var shopController = Object.FindFirstObjectByType<ShopController>();
        if (shopController == null)
        {
            Debug.LogError("[GameHudPrefabBuilder] No ShopController in MainGame.");
            return;
        }

        // Remove any previously spawned GameHUD children (runtime leftovers or prior wires).
        for (int i = shopController.transform.childCount - 1; i >= 0; i--)
        {
            var child = shopController.transform.GetChild(i);
            if (child.name is "GameHUD" or "GameCanvas")
                Object.DestroyImmediate(child.gameObject);
        }

        var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameHudPath);
        var hudInstance = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, shopController.transform);
        hudInstance.name = "GameHUD";

        var goldHud = hudInstance.GetComponentInChildren<GoldHUD>(true);
        var hotbarUi = hudInstance.GetComponentInChildren<HotbarUI>(true);
        var shopPanelUi = hudInstance.GetComponentInChildren<ShopPanelUI>(true);
        var shopPanelRoot = shopPanelUi != null ? shopPanelUi.gameObject : null;

        Button openButton = null;
        foreach (var btn in hudInstance.GetComponentsInChildren<Button>(true))
        {
            if (btn.gameObject.name == "ShopButton")
            {
                openButton = btn;
                break;
            }
        }

        shopController.EditorAssign(shopPanelRoot, openButton, goldHud, hotbarUi, shopPanelUi);
        EditorUtility.SetDirty(shopController);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[GameHudPrefabBuilder] Wired GameHUD into MainGame under Game HUD / ShopController.");
    }

    public static void BuildAllBatch()
    {
        try
        {
            WireMainGame();
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[GameHudPrefabBuilder] Failed: {ex}");
            EditorApplication.Exit(1);
        }
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
    }

    static GameObject BuildHotbarSlotPrefab()
    {
        var root = new GameObject("HotbarSlot", typeof(RectTransform), typeof(Image), typeof(Button));
        var rootRt = root.GetComponent<RectTransform>();
        SetAnchored(rootRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(SlotSize, SlotSize));

        var bg = root.GetComponent<Image>();
        bg.color = SlotColor;
        bg.raycastTarget = true;

        var button = root.GetComponent<Button>();
        button.targetGraphic = bg;
        ApplyButtonColors(button, SlotColor);

        var highlightRt = CreateRect("Highlight", rootRt);
        Stretch(highlightRt);
        var highlight = highlightRt.gameObject.AddComponent<Image>();
        highlight.color = Color.clear;
        highlight.raycastTarget = false;

        var iconRt = CreateRect("Icon", rootRt);
        SetAnchored(iconRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(40f, 40f));
        var icon = iconRt.gameObject.AddComponent<Image>();
        icon.color = Color.white;
        icon.raycastTarget = false;
        icon.enabled = false;
        icon.preserveAspect = true;

        var countRt = CreateRect("Count", rootRt);
        SetAnchored(countRt, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-4f, 2f), new Vector2(40f, 18f));
        var count = AddText(countRt, "", 14, TextAnchor.LowerRight, Color.white);

        var view = root.AddComponent<HotbarSlotView>();
        view.EditorAssign(bg, highlight, icon, count, button);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, HotbarSlotPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static ShopRowView BuildShopRowPrefab()
    {
        var root = new GameObject("ShopRow", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(0f, 64f);

        var le = root.GetComponent<LayoutElement>();
        le.minHeight = 64f;
        le.preferredHeight = 64f;

        var bg = root.GetComponent<Image>();
        bg.color = RowColor;
        bg.raycastTarget = true;

        var iconRt = CreateRect("Icon", rootRt);
        SetAnchored(iconRt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(8f, 0f), new Vector2(48f, 48f));
        var icon = iconRt.gameObject.AddComponent<Image>();
        icon.color = Color.white;
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        icon.enabled = false;

        var nameRt = CreateRect("Name", rootRt);
        SetAnchored(nameRt, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(64f, 8f), new Vector2(-180f, 28f));
        var nameText = AddText(nameRt, "Crop", 16, TextAnchor.MiddleLeft, Color.white);

        var priceRt = CreateRect("Price", rootRt);
        SetAnchored(priceRt, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(64f, -14f), new Vector2(-180f, 22f));
        var priceText = AddText(priceRt, "0 gold", 14, TextAnchor.MiddleLeft, new Color(1f, 0.9f, 0.5f));

        var buyRt = CreateRect("Buy", rootRt);
        SetAnchored(buyRt, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-10f, 0f), new Vector2(80f, 36f));
        var buyImg = buyRt.gameObject.AddComponent<Image>();
        buyImg.color = BuyColor;
        var buyBtn = buyRt.gameObject.AddComponent<Button>();
        buyBtn.targetGraphic = buyImg;
        ApplyButtonColors(buyBtn, BuyColor);
        var buyLabelRt = CreateRect("Label", buyRt);
        Stretch(buyLabelRt);
        AddText(buyLabelRt, "Buy", 16, TextAnchor.MiddleCenter, Color.white);

        var view = root.AddComponent<ShopRowView>();
        view.EditorAssign(icon, nameText, priceText, buyBtn);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, ShopRowPath);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<ShopRowView>();
    }

    static void BuildGameHudPrefab(GameObject hotbarSlotPrefab, ShopRowView shopRowPrefab)
    {
        var canvasGo = new GameObject("GameHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvasRt = (RectTransform)canvasGo.transform;
        Stretch(canvasRt);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Gold HUD
        var goldRoot = CreateRect("GoldHUD", canvasRt);
        SetAnchored(goldRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -16f), new Vector2(220f, 40f));
        var goldBg = goldRoot.gameObject.AddComponent<Image>();
        goldBg.color = new Color(0.05f, 0.05f, 0.08f, 0.75f);
        goldBg.raycastTarget = false;
        var goldHud = goldRoot.gameObject.AddComponent<GoldHUD>();

        var goldTextRt = CreateRect("Text", goldRoot);
        Stretch(goldTextRt);
        goldTextRt.offsetMin = new Vector2(12f, 0f);
        goldTextRt.offsetMax = new Vector2(-8f, 0f);
        var goldText = AddText(goldTextRt, "Gold: 0", 22, TextAnchor.MiddleLeft, new Color(1f, 0.92f, 0.45f));
        goldHud.EditorAssign(goldText);

        // Shop button
        var shopBtnRt = CreateRect("ShopButton", canvasRt);
        SetAnchored(shopBtnRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-16f, -16f), new Vector2(120f, 44f));
        var shopBtnColor = new Color(0.2f, 0.35f, 0.55f, 0.95f);
        var shopBtnImg = shopBtnRt.gameObject.AddComponent<Image>();
        shopBtnImg.color = shopBtnColor;
        var shopBtn = shopBtnRt.gameObject.AddComponent<Button>();
        shopBtn.targetGraphic = shopBtnImg;
        ApplyButtonColors(shopBtn, shopBtnColor);
        var shopLabelRt = CreateRect("Label", shopBtnRt);
        Stretch(shopLabelRt);
        AddText(shopLabelRt, "Shop", 20, TextAnchor.MiddleCenter, Color.white);

        // Tool buttons (top-right, left of Shop): Destroy, Harvest, Water
        var destroyBtnRt = CreateRect("DestroyButton", canvasRt);
        SetAnchored(destroyBtnRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-400f, -16f), new Vector2(120f, 44f));
        var destroyBtnColor = new Color(0.5f, 0.2f, 0.2f, 0.95f);
        var destroyBtnImg = destroyBtnRt.gameObject.AddComponent<Image>();
        destroyBtnImg.color = destroyBtnColor;
        var destroyBtn = destroyBtnRt.gameObject.AddComponent<Button>();
        destroyBtn.targetGraphic = destroyBtnImg;
        ApplyButtonColors(destroyBtn, destroyBtnColor);
        var destroyLabelRt = CreateRect("Label", destroyBtnRt);
        Stretch(destroyLabelRt);
        AddText(destroyLabelRt, "Destroy", 20, TextAnchor.MiddleCenter, Color.white);

        var harvestBtnRt = CreateRect("HarvestButton", canvasRt);
        SetAnchored(harvestBtnRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-272f, -16f), new Vector2(120f, 44f));
        var harvestBtnColor = new Color(0.2f, 0.45f, 0.25f, 0.95f);
        var harvestBtnImg = harvestBtnRt.gameObject.AddComponent<Image>();
        harvestBtnImg.color = harvestBtnColor;
        var harvestBtn = harvestBtnRt.gameObject.AddComponent<Button>();
        harvestBtn.targetGraphic = harvestBtnImg;
        ApplyButtonColors(harvestBtn, harvestBtnColor);
        var harvestLabelRt = CreateRect("Label", harvestBtnRt);
        Stretch(harvestLabelRt);
        AddText(harvestLabelRt, "Harvest", 20, TextAnchor.MiddleCenter, Color.white);

        var waterBtnRt = CreateRect("WaterButton", canvasRt);
        SetAnchored(waterBtnRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-144f, -16f), new Vector2(120f, 44f));
        var waterBtnColor = new Color(0.2f, 0.45f, 0.55f, 0.95f);
        var waterBtnImg = waterBtnRt.gameObject.AddComponent<Image>();
        waterBtnImg.color = waterBtnColor;
        var waterBtn = waterBtnRt.gameObject.AddComponent<Button>();
        waterBtn.targetGraphic = waterBtnImg;
        ApplyButtonColors(waterBtn, waterBtnColor);
        var waterLabelRt = CreateRect("Label", waterBtnRt);
        Stretch(waterLabelRt);
        AddText(waterLabelRt, "Water", 20, TextAnchor.MiddleCenter, Color.white);

        var toolMode = canvasGo.AddComponent<ToolModeController>();
        toolMode.EditorAssign(waterBtn, waterBtnImg, harvestBtn, harvestBtnImg, destroyBtn, destroyBtnImg);

        // Backdrop (visual dim + raycast absorb only — does not close the shop)
        var backdropRt = CreateRect("ShopBackdrop", canvasRt);
        Stretch(backdropRt);
        var backdropImage = backdropRt.gameObject.AddComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, 0.45f);
        backdropImage.raycastTarget = true;
        backdropRt.gameObject.SetActive(false);

        // Shop panel
        var shopPanelRt = CreateRect("ShopPanel", canvasRt);
        SetAnchored(shopPanelRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(420f, 420f));
        var panelImg = shopPanelRt.gameObject.AddComponent<Image>();
        panelImg.color = PanelColor;
        var shopPanel = shopPanelRt.gameObject.AddComponent<ShopPanelUI>();

        var titleRt = CreateRect("Title", shopPanelRt);
        SetAnchored(titleRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -12f), new Vector2(-24f, 36f));
        AddText(titleRt, "Seed Shop", 24, TextAnchor.MiddleCenter, Color.white);

        var closeRt = CreateRect("Close", shopPanelRt);
        SetAnchored(closeRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-10f, -10f), new Vector2(36f, 36f));
        var closeColor = new Color(0.45f, 0.2f, 0.2f, 1f);
        var closeImg = closeRt.gameObject.AddComponent<Image>();
        closeImg.color = closeColor;
        var closeBtn = closeRt.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        ApplyButtonColors(closeBtn, closeColor);
        var closeLabelRt = CreateRect("Label", closeRt);
        Stretch(closeLabelRt);
        AddText(closeLabelRt, "X", 18, TextAnchor.MiddleCenter, Color.white);

        var listRt = CreateRect("List", shopPanelRt);
        SetAnchored(listRt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -18f), new Vector2(-24f, -70f));
        var layout = listRt.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        var fitter = listRt.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        shopPanel.EditorAssign(closeBtn, listRt, shopRowPrefab);

        var sync = shopPanelRt.gameObject.AddComponent<ShopBackdropSync>();
        sync.SetBackdrop(backdropRt.gameObject);

        shopPanelRt.gameObject.SetActive(false);
        // Panel must render above the full-screen backdrop so buttons remain clickable.
        shopPanelRt.SetSiblingIndex(backdropRt.GetSiblingIndex() + 1);

        // Hotbar
        float totalWidth = SlotCount * SlotSize + (SlotCount - 1) * SlotGap;
        var hotbarRt = CreateRect("Hotbar", canvasRt);
        SetAnchored(hotbarRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 18f), new Vector2(totalWidth + 16f, SlotSize + 16f));
        var hotbarBg = hotbarRt.gameObject.AddComponent<Image>();
        hotbarBg.color = new Color(0.05f, 0.05f, 0.08f, 0.65f);
        hotbarBg.raycastTarget = false;
        var hotbar = hotbarRt.gameObject.AddComponent<HotbarUI>();

        var slotViews = new HotbarSlotView[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            var slotInstance = (GameObject)PrefabUtility.InstantiatePrefab(hotbarSlotPrefab, hotbarRt);
            slotInstance.name = $"Slot_{i}";
            var slotRt = (RectTransform)slotInstance.transform;
            float x = -totalWidth * 0.5f + SlotSize * 0.5f + i * (SlotSize + SlotGap);
            SetAnchored(slotRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(x, 0f), new Vector2(SlotSize, SlotSize));
            slotViews[i] = slotInstance.GetComponent<HotbarSlotView>();
        }
        hotbar.EditorAssignSlots(slotViews);

        PrefabUtility.SaveAsPrefabAsset(canvasGo, GameHudPath);
        Object.DestroyImmediate(canvasGo);
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static Text AddText(RectTransform rt, string content, int fontSize, TextAnchor alignment, Color color)
    {
        var text = rt.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static void ApplyButtonColors(Button button, Color normalColor)
    {
        var colors = button.colors;
        colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.15f);
        colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.4f);
        button.colors = colors;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }
}
