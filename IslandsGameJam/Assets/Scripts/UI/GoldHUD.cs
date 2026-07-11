using TMPro;
using UnityEngine;

public class GoldHUD : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI goldText;

    Inventory inventory;

    public void Initialize(Inventory inv)
    {
        if (inventory != null)
            inventory.OnGoldChanged -= Refresh;

        inventory = inv;
        if (inventory == null)
            return;

        inventory.OnGoldChanged += Refresh;
        Refresh(inventory.gold);
    }

    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnGoldChanged -= Refresh;
    }

    void Refresh(int gold)
    {
        if (goldText != null)
            goldText.text = $"Gold: {gold}";
    }

#if UNITY_EDITOR
    public void EditorAssign(TextMeshProUGUI text) => goldText = text;
#endif
}
