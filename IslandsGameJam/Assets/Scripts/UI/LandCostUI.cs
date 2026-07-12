using TMPro;
using UnityEngine;

public class LandCostUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private Canvas canvas;
    [SerializeField] private TMP_Text costText;

    [Header("Position")]
    [SerializeField] private Vector3 worldOffset;

    private RectTransform textRectTransform;
    private RectTransform canvasRectTransform;
    private bool isVisible = true;

    private void Awake()
    {
        textRectTransform = costText.rectTransform;
        canvasRectTransform = canvas.transform as RectTransform;
    }

    private void LateUpdate()
    {
        if (!isVisible || target == null)
            return;

        FollowTarget();
    }

    private void FollowTarget()
    {
        Vector3 targetPosition = target.position + worldOffset;
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(targetPosition);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                screenPosition,
                null,
                out Vector2 localPosition))
        {
            textRectTransform.anchoredPosition = localPosition;
        }
    }

    public void UpdateText(string text)
    {
        costText.text = text;
    }

    public void Show()
    {
        isVisible = true;
        costText.gameObject.SetActive(true);
    }

    public void Hide()
    {
        isVisible = false;
        costText.gameObject.SetActive(false);
    }
}