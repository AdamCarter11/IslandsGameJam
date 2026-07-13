using TMPro;
using UnityEngine;

public class TimerUI : MonoBehaviour
{
    const float WarningSeconds = 60f;
    const float UrgentSeconds = 30f;
    const float WarningPulseAmount = 0.08f;
    const float UrgentPulseAmount = 0.16f;
    const float WarningPulseSpeed = 4f;
    const float UrgentPulseSpeed = 7.5f;

    [SerializeField] TextMeshProUGUI timerText;

    GameManager gameManager;
    int lastWholeSeconds = -1;
    Vector3 baseScale = Vector3.one;
    bool baseScaleCached;

    void Awake()
    {
        CacheBaseScale();
    }

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
        UpdatePulse();
    }

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        lastWholeSeconds = -1;
        CacheBaseScale();
        transform.localScale = baseScale;
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

    void UpdatePulse()
    {
        CacheBaseScale();

        if (gameManager == null || !gameManager.TimerStarted || gameManager.IsGameOver)
        {
            transform.localScale = baseScale;
            return;
        }

        float remaining = gameManager.GameDurationSeconds - gameManager.PlayTime;
        if (remaining > WarningSeconds || remaining <= 0f)
        {
            transform.localScale = baseScale;
            return;
        }

        bool urgent = remaining <= UrgentSeconds;
        float amount = urgent ? UrgentPulseAmount : WarningPulseAmount;
        float speed = urgent ? UrgentPulseSpeed : WarningPulseSpeed;
        float pulse = 1f + Mathf.Abs(Mathf.Sin(Time.time * speed)) * amount;
        transform.localScale = baseScale * pulse;
    }

    void CacheBaseScale()
    {
        if (baseScaleCached)
            return;

        baseScale = transform.localScale;
        if (baseScale.sqrMagnitude < 0.0001f)
            baseScale = Vector3.one;
        baseScaleCached = true;
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
