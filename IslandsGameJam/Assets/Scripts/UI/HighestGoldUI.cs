using TMPro;
using UnityEngine;

public class HighestGoldUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI highestGoldText;

    Inventory inventory;

    void Start()
    {
        if (inventory == null)
            Initialize(GameManager.Main != null ? GameManager.Main.Inventory : FindFirstObjectByType<Inventory>());
    }

    public void Initialize(Inventory inv)
    {
        if (inventory != null)
            inventory.OnGoldChanged -= OnGoldChanged;

        inventory = inv;
        if (inventory == null)
        {
            Refresh(0);
            return;
        }

        inventory.OnGoldChanged += OnGoldChanged;
        Refresh(inventory.HighestGold);
    }

    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnGoldChanged -= OnGoldChanged;
    }

    void OnGoldChanged(int _)
    {
        if (inventory != null)
            Refresh(inventory.HighestGold);
    }

    void Refresh(int highestGold)
    {
        if (highestGoldText == null)
            highestGoldText = GetComponentInChildren<TextMeshProUGUI>(true);

        if (highestGoldText != null)
            highestGoldText.text = $"Highest: {highestGold}";
    }

#if UNITY_EDITOR
    public void EditorAssign(TextMeshProUGUI text) => highestGoldText = text;
#endif
}
