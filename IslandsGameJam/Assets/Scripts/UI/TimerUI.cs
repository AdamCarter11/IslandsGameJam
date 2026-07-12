using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI timerText;

    GameManager gameManager;
    int lastWholeSeconds = -1;

    void Start()
    {
        if (gameManager == null)
            Initialize(GameManager.Main != null ? GameManager.Main : FindFirstObjectByType<GameManager>());
    }

    void Update()
    {
        if (gameManager == null)
            gameManager = GameManager.Main != null ? GameManager.Main : FindFirstObjectByType<GameManager>();

        Refresh();
    }

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        lastWholeSeconds = -1;
        Refresh();
    }

    void Refresh()
    {
        if (timerText == null)
            timerText = GetComponentInChildren<TextMeshProUGUI>(true);

        int seconds = gameManager != null
            ? Mathf.FloorToInt(Mathf.Max(0f, gameManager.PlayTime))
            : 0;

        if (seconds == lastWholeSeconds)
            return;

        lastWholeSeconds = seconds;
        if (timerText != null)
            timerText.text = $"Time: {FormatTime(seconds)}";
    }

    static string FormatTime(int totalSeconds)
    {
        int seconds = totalSeconds % 60;
        int totalMinutes = totalSeconds / 60;

        if (totalMinutes < 100)
            return $"{totalMinutes:00}m{seconds:00}s";

        int hours = totalSeconds / 3600;
        int minutes = totalMinutes % 60;
        return $"{hours:0}h{minutes:00}m{seconds:00}s";
    }

#if UNITY_EDITOR
    public void EditorAssign(TextMeshProUGUI text) => timerText = text;
#endif
}
