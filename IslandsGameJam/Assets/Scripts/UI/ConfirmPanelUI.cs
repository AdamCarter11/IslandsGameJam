using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ConfirmPanelUI : MonoBehaviour
{
    [SerializeField]
    private GameObject panel;
    [SerializeField]
    private GameObject background;
    [SerializeField]
    private TMP_Text promptText;
    [SerializeField]
    private Button yesButton;
    [SerializeField]
    private Button noButton;

    public bool IsVisible => panel != null && panel.activeSelf;

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame &&
            panel.activeSelf)
        {
            Hide();
        }
    }

    public void Show(string prompt, Action onYes = null, Action onNo = null)
    {
        panel.SetActive(true);
        background.SetActive(true);
        promptText.text = prompt;

        yesButton.onClick.AddListener(() =>
        {
            onYes?.Invoke();
            Hide();
        });

        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(() =>
        {
            onNo?.Invoke();
            Hide();
        });
    }

    public void Hide()
    {
        panel.SetActive(false);
        background.SetActive(false);
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
    }
}
