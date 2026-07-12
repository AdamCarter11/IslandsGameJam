using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds/rewires GameHUD, HotbarSlot, ShopRow, RelicChoiceCard, RelicInventorySlot prefabs
/// and places GameHUD under MainGame's Game HUD.
/// </summary>
public static class GameHudPrefabBuilder
{
    const string PrefabFolder = "Assets/Prefabs/UI";
    const string HotbarSlotPath = PrefabFolder + "/HotbarSlot.prefab";
    const string ShopRowPath = PrefabFolder + "/ShopRow.prefab";
    const string RelicChoiceCardPath = PrefabFolder + "/RelicChoiceCard.prefab";
    const string RelicInventorySlotPath = PrefabFolder + "/RelicInventorySlot.prefab";
    const string GameHudPath = PrefabFolder + "/GameHUD.prefab";
    const string MainGamePath = "Assets/Scenes/MainGame.unity";
    const string DefaultTmpFontPath = "Assets/slapduck SDF.asset";

    const string RelicsFolder = "Assets/ScriptableObjects/Relics";
    const string RelicShopCatalogPath = RelicsFolder + "/RelicShopCatalog.asset";

    const int SlotCount = 10;
    const float SlotSize = 56f;
    const float SlotGap = 6f;

    static readonly Color SlotColor = new(0.15f, 0.15f, 0.18f, 0.92f);
    static readonly Color PanelColor = new(0.08f, 0.09f, 0.12f, 0.96f);
    static readonly Color RowColor = new(0.16f, 0.17f, 0.22f, 1f);
    static readonly Color BuyColor = new(0.25f, 0.55f, 0.3f, 1f);
    static readonly Color RelicRollColor = new(0.45f, 0.32f, 0.15f, 1f);
    static readonly Color RelicCardColor = new(0.14f, 0.15f, 0.2f, 1f);

    [MenuItem("Tools/UI/Build Game HUD Prefabs")]
    public static void BuildAll()
    {
        EnsureFolder();
        EnsureRelicShopCatalog();

        var hotbarSlot = BuildHotbarSlotPrefab();
        var shopRow = BuildShopRowPrefab();
        var relicCard = BuildRelicChoiceCardPrefab();
        var relicInvSlot = BuildRelicInventorySlotPrefab();
        BuildGameHudPrefab(hotbarSlot, shopRow, relicCard, relicInvSlot);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GameHudPrefabBuilder] Wrote:\n  {HotbarSlotPath}\n  {ShopRowPath}\n  {RelicChoiceCardPath}\n  {RelicInventorySlotPath}\n  {GameHudPath}\n  {RelicShopCatalogPath}");
    }

    [MenuItem("Tools/UI/Wire Game HUD Into MainGame")]
    public static void WireMainGame()
    {
        BuildAll();
        var catalog = EnsureRelicShopCatalog();

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
        var relicChoicePanelUi = hudInstance.GetComponentInChildren<RelicChoicePanelUI>(true);
        var relicInventoryPanelUi = hudInstance.GetComponentInChildren<RelicInventoryPanelUI>(true);
        var shopPanelRoot = shopPanelUi != null ? shopPanelUi.gameObject : null;

        Button openButton = null;
        Button relicsOpenButton = null;
        foreach (var btn in hudInstance.GetComponentsInChildren<Button>(true))
        {
            if (btn.gameObject.name == "ShopButton")
                openButton = btn;
            else if (btn.gameObject.name == "RelicsButton")
                relicsOpenButton = btn;
        }

        shopController.EditorAssign(
            shopPanelRoot,
            openButton,
            goldHud,
            hotbarUi,
            shopPanelUi,
            relicChoicePanelUi,
            relicInventoryPanelUi,
            relicsOpenButton);
        EditorUtility.SetDirty(shopController);

        WireRelicShopIntoGameManager(catalog);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[GameHudPrefabBuilder] Wired GameHUD + RelicShopCatalog into MainGame.");
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

    static void EnsureScriptableObjectsFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
            AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(RelicsFolder))
            AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Relics");
    }

    static RelicShopCatalog EnsureRelicShopCatalog()
    {
        EnsureScriptableObjectsFolders();

        var relics = new[]
        {
            EnsureRelicAsset(
                "Relic_GoldenTouch",
                "Golden Touch",
                "Harvested crops yield a bit more gold.",
                allowMultiple: false,
                RelicEffectType.ModifyGold,
                2f,
                multiplicative: false,
                RelicRarity.Rare),
            EnsureRelicAsset(
                "Relic_GrowthCharm",
                "Growth Charm",
                "Crops grow slightly faster. Stacks.",
                allowMultiple: true,
                RelicEffectType.ModifyGrowthTime,
                0.9f,
                multiplicative: true,
                RelicRarity.Common),
            EnsureRelicAsset(
                "Relic_BountifulYield",
                "Bountiful Yield",
                "Increases harvest multiplier. Stacks.",
                allowMultiple: true,
                RelicEffectType.ModifyMulti,
                0.15f,
                multiplicative: false,
                RelicRarity.Epic),
            EnsureRelicAsset(
                "Relic_DryEndurance",
                "Dry Endurance",
                "Crops survive dry soil longer.",
                allowMultiple: false,
                RelicEffectType.ModifyDryDeathTime,
                5f,
                multiplicative: false,
                RelicRarity.Common),
        };

        var catalog = AssetDatabase.LoadAssetAtPath<RelicShopCatalog>(RelicShopCatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<RelicShopCatalog>();
            AssetDatabase.CreateAsset(catalog, RelicShopCatalogPath);
        }

        catalog.allRelics = new System.Collections.Generic.List<RelicSO>(relics);
        catalog.baseRollCost = 25;
        catalog.costMultiplierPerPurchase = 1.5f;
        catalog.skipRefundBasePercent = 0.10f;
        catalog.skipRefundIncreasePerSkip = 0.05f;
        catalog.skipRefundMaxPercent = 0.50f;
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        return catalog;
    }

    static RelicSO EnsureRelicAsset(
        string fileName,
        string displayName,
        string description,
        bool allowMultiple,
        RelicEffectType effectType,
        float amount,
        bool multiplicative,
        RelicRarity rarity)
    {
        string path = $"{RelicsFolder}/{fileName}.asset";
        var relic = AssetDatabase.LoadAssetAtPath<RelicSO>(path);
        if (relic == null)
        {
            relic = ScriptableObject.CreateInstance<RelicSO>();
            AssetDatabase.CreateAsset(relic, path);
        }

        relic.relicName = displayName;
        relic.desc = description;
        relic.rarity = rarity;
        relic.allowMultiplePurchases = allowMultiple;
        relic.effects = new[]
        {
            new RelicEffect
            {
                type = effectType,
                amount = amount,
                multiplicative = multiplicative,
            }
        };
        EditorUtility.SetDirty(relic);
        return relic;
    }

    static void WireRelicShopIntoGameManager(RelicShopCatalog catalog)
    {
        var gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("[GameHudPrefabBuilder] No GameManager in MainGame.");
            return;
        }

        var relicShop = gameManager.GetComponent<RelicShopService>();
        if (relicShop == null)
            relicShop = Undo.AddComponent<RelicShopService>(gameManager.gameObject);

        var inventory = gameManager.Inventory ?? gameManager.GetComponent<Inventory>();
        relicShop.EditorAssign(inventory, catalog);
        gameManager.EditorAssign(relicShop, catalog);

        EditorUtility.SetDirty(relicShop);
        EditorUtility.SetDirty(gameManager);
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

    static RelicChoiceCardView BuildRelicChoiceCardPrefab()
    {
        var root = new GameObject("RelicChoiceCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(200f, 280f);

        var le = root.GetComponent<LayoutElement>();
        le.minWidth = 200f;
        le.preferredWidth = 200f;
        le.minHeight = 280f;
        le.preferredHeight = 280f;

        var bg = root.GetComponent<Image>();
        bg.color = RelicCardColor;
        bg.raycastTarget = true;

        var selectBtn = root.GetComponent<Button>();
        selectBtn.targetGraphic = bg;
        ApplyButtonColors(selectBtn, RelicCardColor);

        var iconRt = CreateRect("Icon", rootRt);
        SetAnchored(iconRt, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -16f), new Vector2(72f, 72f));
        var icon = iconRt.gameObject.AddComponent<Image>();
        icon.color = Color.white;
        icon.raycastTarget = false;
        icon.preserveAspect = true;
        icon.enabled = false;

        var nameRt = CreateRect("Name", rootRt);
        SetAnchored(nameRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -96f), new Vector2(-16f, 28f));
        var nameText = AddText(nameRt, "Relic", 18, TextAnchor.MiddleCenter, Color.white);

        var rarityRt = CreateRect("Rarity", rootRt);
        SetAnchored(rarityRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -122f), new Vector2(-16f, 22f));
        var rarityText = AddText(rarityRt, "Common", 14, TextAnchor.MiddleCenter, new Color(0.75f, 0.75f, 0.78f));

        var descRt = CreateRect("Desc", rootRt);
        SetAnchored(descRt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -28f), new Vector2(-20f, -170f));
        var descText = AddText(descRt, "Description", 13, TextAnchor.UpperCenter, new Color(0.85f, 0.85f, 0.9f));
        descText.textWrappingMode = TextWrappingModes.Normal;
        descText.overflowMode = TextOverflowModes.Truncate;

        var refundRt = CreateRect("Refund", rootRt);
        SetAnchored(refundRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 12f), new Vector2(-16f, 28f));
        var refundText = AddText(refundRt, "Refund: 0 gold", 14, TextAnchor.MiddleCenter, new Color(0.55f, 0.95f, 0.55f));
        refundText.gameObject.SetActive(false);

        var view = root.AddComponent<RelicChoiceCardView>();
        view.EditorAssign(icon, nameText, rarityText, descText, refundText, selectBtn);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, RelicChoiceCardPath);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<RelicChoiceCardView>();
    }

    static RelicInventorySlotView BuildRelicInventorySlotPrefab()
    {
        var root = new GameObject("RelicInventorySlot", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        var rootRt = root.GetComponent<RectTransform>();
        SetAnchored(rootRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(SlotSize, SlotSize));

        var le = root.GetComponent<LayoutElement>();
        le.minWidth = SlotSize;
        le.preferredWidth = SlotSize;
        le.minHeight = SlotSize;
        le.preferredHeight = SlotSize;

        var bg = root.GetComponent<Image>();
        bg.color = SlotColor;
        bg.raycastTarget = true;

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
        count.gameObject.SetActive(false);

        var view = root.AddComponent<RelicInventorySlotView>();
        view.EditorAssign(icon, count);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, RelicInventorySlotPath);
        Object.DestroyImmediate(root);
        return prefab.GetComponent<RelicInventorySlotView>();
    }

    static void BuildGameHudPrefab(
        GameObject hotbarSlotPrefab,
        ShopRowView shopRowPrefab,
        RelicChoiceCardView relicCardPrefab,
        RelicInventorySlotView relicInventorySlotPrefab)
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

        // Relics button (top-right, left of Shop)
        var relicsBtnRt = CreateRect("RelicsButton", canvasRt);
        SetAnchored(relicsBtnRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-144f, -16f), new Vector2(120f, 44f));
        var relicsBtnColor = new Color(0.4f, 0.28f, 0.5f, 0.95f);
        var relicsBtnImg = relicsBtnRt.gameObject.AddComponent<Image>();
        relicsBtnImg.color = relicsBtnColor;
        var relicsBtn = relicsBtnRt.gameObject.AddComponent<Button>();
        relicsBtn.targetGraphic = relicsBtnImg;
        ApplyButtonColors(relicsBtn, relicsBtnColor);
        var relicsLabelRt = CreateRect("Label", relicsBtnRt);
        Stretch(relicsLabelRt);
        AddText(relicsLabelRt, "Relics", 20, TextAnchor.MiddleCenter, Color.white);

        // Tool buttons (top-right, left of Relics): Destroy, Harvest, Water
        var destroyBtnRt = CreateRect("DestroyButton", canvasRt);
        SetAnchored(destroyBtnRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-528f, -16f), new Vector2(120f, 44f));
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
            new Vector2(-400f, -16f), new Vector2(120f, 44f));
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
            new Vector2(-272f, -16f), new Vector2(120f, 44f));
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
            Vector2.zero, new Vector2(420f, 480f));
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
            new Vector2(0f, 10f), new Vector2(-24f, -130f));
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

        var relicRollRt = CreateRect("RelicRollButton", shopPanelRt);
        SetAnchored(relicRollRt, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 12f), new Vector2(-24f, 44f));
        var relicRollImg = relicRollRt.gameObject.AddComponent<Image>();
        relicRollImg.color = RelicRollColor;
        var relicRollBtn = relicRollRt.gameObject.AddComponent<Button>();
        relicRollBtn.targetGraphic = relicRollImg;
        ApplyButtonColors(relicRollBtn, RelicRollColor);
        var relicRollLabelRt = CreateRect("Label", relicRollRt);
        Stretch(relicRollLabelRt);
        var relicRollLabel = AddText(relicRollLabelRt, "Buy Relic (25 gold)", 16, TextAnchor.MiddleCenter, Color.white);

        // Relic choice panel (must-pick overlay; no close button)
        var choiceRt = CreateRect("RelicChoicePanel", canvasRt);
        Stretch(choiceRt);
        var choiceBg = choiceRt.gameObject.AddComponent<Image>();
        choiceBg.color = new Color(0f, 0f, 0f, 0.55f);
        choiceBg.raycastTarget = true;
        var choicePanel = choiceRt.gameObject.AddComponent<RelicChoicePanelUI>();

        var choiceTitleRt = CreateRect("Title", choiceRt);
        SetAnchored(choiceTitleRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 180f), new Vector2(480f, 40f));
        AddText(choiceTitleRt, "Choose a Relic", 28, TextAnchor.MiddleCenter, Color.white);

        var cardsRt = CreateRect("Cards", choiceRt);
        SetAnchored(cardsRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -10f), new Vector2(680f, 300f));
        var cardsLayout = cardsRt.gameObject.AddComponent<HorizontalLayoutGroup>();
        cardsLayout.spacing = 20f;
        cardsLayout.padding = new RectOffset(10, 10, 10, 10);
        cardsLayout.childAlignment = TextAnchor.MiddleCenter;
        cardsLayout.childControlHeight = true;
        cardsLayout.childControlWidth = true;
        cardsLayout.childForceExpandHeight = true;
        cardsLayout.childForceExpandWidth = false;

        var choiceCards = new RelicChoiceCardView[3];
        for (int i = 0; i < 3; i++)
        {
            var cardInstance = (GameObject)PrefabUtility.InstantiatePrefab(relicCardPrefab.gameObject, cardsRt);
            cardInstance.name = $"RelicCard_{i}";
            choiceCards[i] = cardInstance.GetComponent<RelicChoiceCardView>();
        }
        choicePanel.EditorAssign(choiceCards);
        choiceRt.gameObject.SetActive(false);

        shopPanel.EditorAssign(closeBtn, listRt, shopRowPrefab, relicRollBtn, relicRollLabel, choicePanel);

        var sync = shopPanelRt.gameObject.AddComponent<ShopBackdropSync>();
        sync.SetBackdrop(backdropRt.gameObject);

        shopPanelRt.gameObject.SetActive(false);
        // Panel must render above the full-screen backdrop so buttons remain clickable.
        shopPanelRt.SetSiblingIndex(backdropRt.GetSiblingIndex() + 1);
        // Choice overlay sits above the seed shop.
        choiceRt.SetSiblingIndex(shopPanelRt.GetSiblingIndex() + 1);

        // Relic inventory panel (independent of seed shop)
        var relicInvRt = CreateRect("RelicInventoryPanel", canvasRt);
        SetAnchored(relicInvRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(220f, 0f), new Vector2(360f, 320f));
        var relicInvImg = relicInvRt.gameObject.AddComponent<Image>();
        relicInvImg.color = PanelColor;
        var relicInvPanel = relicInvRt.gameObject.AddComponent<RelicInventoryPanelUI>();

        var relicInvTitleRt = CreateRect("Title", relicInvRt);
        SetAnchored(relicInvTitleRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -12f), new Vector2(-24f, 36f));
        AddText(relicInvTitleRt, "Relics", 24, TextAnchor.MiddleCenter, Color.white);

        var relicInvCloseRt = CreateRect("Close", relicInvRt);
        SetAnchored(relicInvCloseRt, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-10f, -10f), new Vector2(36f, 36f));
        var relicInvCloseColor = new Color(0.45f, 0.2f, 0.2f, 1f);
        var relicInvCloseImg = relicInvCloseRt.gameObject.AddComponent<Image>();
        relicInvCloseImg.color = relicInvCloseColor;
        var relicInvCloseBtn = relicInvCloseRt.gameObject.AddComponent<Button>();
        relicInvCloseBtn.targetGraphic = relicInvCloseImg;
        ApplyButtonColors(relicInvCloseBtn, relicInvCloseColor);
        var relicInvCloseLabelRt = CreateRect("Label", relicInvCloseRt);
        Stretch(relicInvCloseLabelRt);
        AddText(relicInvCloseLabelRt, "X", 18, TextAnchor.MiddleCenter, Color.white);

        var relicInvContentRt = CreateRect("Content", relicInvRt);
        SetAnchored(relicInvContentRt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -8f), new Vector2(-24f, -64f));
        var relicInvGrid = relicInvContentRt.gameObject.AddComponent<GridLayoutGroup>();
        relicInvGrid.cellSize = new Vector2(SlotSize, SlotSize);
        relicInvGrid.spacing = new Vector2(SlotGap, SlotGap);
        relicInvGrid.padding = new RectOffset(8, 8, 8, 8);
        relicInvGrid.childAlignment = TextAnchor.UpperLeft;
        relicInvGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        relicInvGrid.constraintCount = 5;
        relicInvGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        relicInvGrid.startAxis = GridLayoutGroup.Axis.Horizontal;

        var relicInvEmptyRt = CreateRect("EmptyLabel", relicInvRt);
        SetAnchored(relicInvEmptyRt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -8f), new Vector2(-40f, -80f));
        var relicInvEmpty = AddText(relicInvEmptyRt, "No relics yet", 16, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.75f));

        var relicTooltipRt = CreateRect("Tooltip", relicInvRt);
        SetAnchored(relicTooltipRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 12f), new Vector2(280f, 88f));
        var relicTooltipBg = relicTooltipRt.gameObject.AddComponent<Image>();
        relicTooltipBg.color = new Color(0.05f, 0.06f, 0.09f, 0.96f);
        relicTooltipBg.raycastTarget = false;

        var relicTooltipNameRt = CreateRect("Name", relicTooltipRt);
        SetAnchored(relicTooltipNameRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -6f), new Vector2(-16f, 24f));
        var relicTooltipName = AddText(relicTooltipNameRt, "Relic", 16, TextAnchor.MiddleLeft, Color.white);

        var relicTooltipDescRt = CreateRect("Desc", relicTooltipRt);
        SetAnchored(relicTooltipDescRt, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -4f), new Vector2(-16f, -32f));
        var relicTooltipDesc = AddText(relicTooltipDescRt, "Description", 13, TextAnchor.UpperLeft, new Color(0.85f, 0.85f, 0.9f));
        relicTooltipDesc.textWrappingMode = TextWrappingModes.Normal;
        relicTooltipDesc.overflowMode = TextOverflowModes.Truncate;

        relicTooltipRt.gameObject.SetActive(false);

        relicInvPanel.EditorAssign(
            relicInvCloseBtn,
            relicInvContentRt,
            relicInventorySlotPrefab,
            relicInvEmpty,
            relicTooltipRt.gameObject,
            relicTooltipName,
            relicTooltipDesc,
            relicTooltipRt);
        relicInvRt.gameObject.SetActive(false);
        // Keep inventory panel above shop chrome but below must-pick choice overlay.
        relicInvRt.SetSiblingIndex(choiceRt.GetSiblingIndex());

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

    static TextMeshProUGUI AddText(RectTransform rt, string content, int fontSize, TextAnchor alignment, Color color)
    {
        var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultTmpFontPath);
        if (font == null)
            font = TMP_Settings.defaultFontAsset;
        if (font != null)
            text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = ToTmpAlignment(alignment);
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    static TextAlignmentOptions ToTmpAlignment(TextAnchor alignment) => alignment switch
    {
        TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
        TextAnchor.UpperCenter => TextAlignmentOptions.Top,
        TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
        TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
        TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
        TextAnchor.MiddleRight => TextAlignmentOptions.Right,
        TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
        TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
        TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
        _ => TextAlignmentOptions.Center
    };

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
