using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarUI : MonoBehaviour
{
    const int SlotCount = Inventory.HotbarSlotCount;

    [SerializeField] HotbarSlotView[] slots = new HotbarSlotView[SlotCount];
    [SerializeField] SeedTooltipUI seedTooltip;

    Inventory inventory;

    static readonly Color SlotColor = new(0.15f, 0.15f, 0.18f, 0.92f);
    static readonly Color SelectedColor = new(0.95f, 0.85f, 0.35f, 1f);

    public void Initialize(Inventory inv)
    {
        if (inventory != null)
        {
            inventory.OnHotbarChanged -= Refresh;
            inventory.OnSelectedSlotChanged -= OnSelectedChanged;
        }

        inventory = inv;
        WireSlotClicks();
        WireSlotHover();

        if (inventory == null)
            return;

        inventory.OnHotbarChanged += Refresh;
        inventory.OnSelectedSlotChanged += OnSelectedChanged;
        Refresh();
        OnSelectedChanged(inventory.SelectedSlot);
    }

    void WireSlotClicks()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;
            var slot = slots[i];
            if (slot == null || slot.Button == null)
                continue;

            slot.Button.onClick.RemoveAllListeners();
            slot.Button.onClick.AddListener(() =>
            {
                ClearAllToolModes();
                inventory?.SelectSlot(index);
            });
        }
    }

    void WireSlotHover()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null)
                continue;
            slot.SetHoverHandlers(ShowTooltipForSlot, HideSeedTooltip);
        }
    }

    static void ClearAllToolModes()
    {
        if (ToolModeController.Main != null)
            ToolModeController.Main.ClearAllModes();
    }

    void OnDestroy()
    {
        HideSeedTooltip();
        if (inventory == null)
            return;
        inventory.OnHotbarChanged -= Refresh;
        inventory.OnSelectedSlotChanged -= OnSelectedChanged;
    }

    void Update()
    {
        if (inventory == null)
            return;

        if (ShopController.Main != null && ShopController.Main.IsOptionsOpen)
            return;

        if (TrySelectFromScroll())
            return;

        if (Keyboard.current == null)
            return;

        for (int digit = 1; digit <= 9; digit++)
        {
            if (WasDigitPressed(digit))
            {
                inventory.SelectSlot(digit - 1);
                return;
            }
        }

        if (WasDigitPressed(0))
            inventory.SelectSlot(9);
    }

    bool TrySelectFromScroll()
    {
        if (Mouse.current == null)
            return false;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Approximately(scroll, 0f))
            return false;

        int delta = scroll > 0f ? -1 : 1;
        int next = (inventory.SelectedSlot + delta + SlotCount) % SlotCount;
        inventory.SelectSlot(next);
        return true;
    }

    static bool WasDigitPressed(int digit)
    {
        var kb = Keyboard.current;
        var wasDigitPressed = digit switch
        {
            0 => kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame,
            1 => kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame,
            2 => kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame,
            3 => kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame,
            4 => kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame,
            5 => kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame,
            6 => kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame,
            7 => kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame,
            8 => kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame,
            9 => kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame,
            _ => false
        };
        if (wasDigitPressed) ToolModeController.Main.SetMode(ToolMode.None);
        return wasDigitPressed;
    }

    void OnSelectedChanged(int index)
    {
        ClearAllToolModes();

        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null)
                continue;

            bool selected = i == index;
            if (slot.Highlight != null)
                slot.Highlight.color = selected ? SelectedColor : Color.clear;
            if (slot.Background != null)
            {
                slot.Background.color = selected
                    ? Color.Lerp(SlotColor, SelectedColor, 0.25f)
                    : SlotColor;
            }
            if (slot.Count != null)
                slot.Count.color = selected ? Color.black : Color.white;
        }
    }

    void Refresh()
    {
        if (inventory == null || slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null)
                continue;

            var stack = inventory.GetSlot(i);
            bool empty = stack == null || stack.IsEmpty;
            if (empty)
            {
                if (slot.BoundCrop != null)
                    HideSeedTooltip();
                slot.BindCrop(null);
                if (slot.Icon != null)
                {
                    slot.Icon.enabled = false;
                    slot.Icon.sprite = null;
                }
                if (slot.Count != null)
                    slot.Count.text = string.Empty;
                continue;
            }

            slot.BindCrop(stack.crop);
            var icon = stack.crop.GetShopIcon();
            if (slot.Icon != null)
            {
                slot.Icon.sprite = icon;
                slot.Icon.enabled = icon != null;
            }
            if (slot.Count != null)
                slot.Count.text = stack.count > 1 ? stack.count.ToString() : string.Empty;
        }
    }

    void ShowTooltipForSlot(HotbarSlotView slot, CropGrowthSO crop)
    {
        if (seedTooltip == null || crop == null || slot == null)
        {
            HideSeedTooltip();
            return;
        }

        seedTooltip.Show(crop, slot.transform as RectTransform);
    }

    void HideSeedTooltip()
    {
        seedTooltip?.Hide();
    }

#if UNITY_EDITOR
    public void EditorAssignSlots(HotbarSlotView[] slotViews, SeedTooltipUI tooltip = null)
    {
        slots = slotViews;
        if (tooltip != null)
            seedTooltip = tooltip;
    }
#endif
}
