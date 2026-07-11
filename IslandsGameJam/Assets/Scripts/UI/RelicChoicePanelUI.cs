using UnityEngine;

/// <summary>
/// Must-pick relic offer UI. No close control — player selects one of the three cards.
/// </summary>
public class RelicChoicePanelUI : MonoBehaviour
{
    [SerializeField] RelicChoiceCardView[] cards = new RelicChoiceCardView[3];

    RelicShopService relicShop;
    System.Action onChoiceComplete;

    public bool IsOpen => gameObject.activeSelf;

    public void Initialize(RelicShopService shopService, System.Action choiceComplete = null)
    {
        relicShop = shopService;
        onChoiceComplete = choiceComplete;
        Hide();
    }

    public void ShowOffers()
    {
        if (relicShop == null || !relicShop.HasActiveRoll)
            return;

        var offers = relicShop.CurrentOffers;
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
                continue;

            RelicSO relic = i < offers.Count ? offers[i] : null;
            if (relic == null)
            {
                cards[i].Clear();
                continue;
            }

            int index = i;
            int refund = relicShop.GetPreviewRefund(relic);
            cards[i].Bind(relic, refund, () => OnSelect(index));
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        for (int i = 0; i < cards.Length; i++)
            cards[i]?.Clear();
        gameObject.SetActive(false);
    }

    void OnSelect(int index)
    {
        if (relicShop == null || !relicShop.TrySelectOffer(index))
            return;

        Hide();
        onChoiceComplete?.Invoke();
    }

#if UNITY_EDITOR
    public void EditorAssign(RelicChoiceCardView[] cardViews)
    {
        cards = cardViews;
    }
#endif
}
