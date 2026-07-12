using System.Text;
using UnityEngine;

[CreateAssetMenu(fileName = "CropGrowth", menuName = "Scriptable Objects/CropGrowthGroup")]
public class CropGrowthSO : ScriptableObject
{
    public string cropName;
    [TextArea(3, 10)] public string desc;
    public CropPropertiesSO[] stages;

    [Header("Drought")]
    [Tooltip("Seconds of continuous dry time before the crop dies.")]
    public float dryDeathTime = 30f;
    [Tooltip("Base gold dropped when the crop dies from drought.")]
    public int deathGold = 1;

    [Header("Shop")]
    [Tooltip("Shown in the shop/hotbar. Falls back to the first stage visual if unset.")]
    public Sprite shopIcon;
    public int seedPrice = 10;
    public bool unlockedByDefault;

    [Header("Harvest")]
    [Tooltip("Sprite used by the harvest hop flyer. Falls back to the ready (final) stage visual if unset.")]
    public Sprite harvestBounceVisual;

    public Sprite GetShopIcon()
    {
        if (shopIcon != null)
            return shopIcon;
        if (stages != null && stages.Length > 0 && stages[0] != null)
            return stages[0].cropVisual;
        return null;
    }

    /// <summary>
    /// Visual for the bouncing harvest flyer (not the planted growth sprite).
    /// </summary>
    public Sprite GetHarvestBounceVisual()
    {
        if (harvestBounceVisual != null)
            return harvestBounceVisual;
        if (stages != null && stages.Length > 0)
        {
            CropPropertiesSO ready = stages[stages.Length - 1];
            if (ready != null)
                return ready.cropVisual;
        }
        return null;
    }

    /// <summary>
    /// Multiline tooltip body from authored base values (not relic-resolved):
    /// optional desc, growth time, ready-stage gold/multi/dry growth, harvest pattern, drought.
    /// </summary>
    public string BuildTooltipBody()
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(desc))
        {
            sb.Append(desc.Trim());
            sb.AppendLine();
            sb.AppendLine();
        }

        float totalGrowth = 0f;
        if (stages != null)
        {
            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i] != null)
                    totalGrowth += stages[i].growthTime;
            }
        }

        CropPropertiesSO ready = GetReadyStage();
        sb.Append("Growth: ").Append(FormatSeconds(totalGrowth)).AppendLine();

        if (ready != null)
        {
            sb.Append("Gold: ").Append(ready.goldGain).AppendLine();
            sb.Append("Multi: ").Append(FormatNumber(ready.multiBonus)).AppendLine();
            sb.Append("Dry growth: ").Append(FormatNumber(ready.dryGrowthMultiplier)).Append('x').AppendLine();
            //sb.Append("Harvest: ").AppendLine(FormatHarvestPattern(ready.harvestPattern));
        }

        sb.Append("Drought: dies in ").Append(FormatSeconds(dryDeathTime))
            .Append(" (").Append(deathGold).Append(" gold)");

        return sb.ToString();
    }

    CropPropertiesSO GetReadyStage()
    {
        if (stages == null || stages.Length == 0)
            return null;
        return stages[stages.Length - 1];
    }

    static string FormatHarvestPattern(HarvestPattern pattern)
    {
        if (pattern == null)
            return "none";

        switch (pattern.kind)
        {
            case HarvestPatternKind.Offsets:
            {
                int count = pattern.offsets != null ? pattern.offsets.Length : 0;
                return count == 1 ? "1 offset" : $"{count} offsets";
            }
            case HarvestPatternKind.Ray:
            {
                Vector2Int dir = pattern.direction;
                string steps = pattern.maxSteps < 0
                    ? "until blocked"
                    : $"{pattern.maxSteps} steps";
                return $"ray ({dir.x},{dir.y}), {steps}";
            }
            default:
                return pattern.kind.ToString();
        }
    }

    static string FormatSeconds(float seconds)
    {
        if (Mathf.Approximately(seconds, Mathf.Round(seconds)))
            return $"{Mathf.RoundToInt(seconds)}s";
        return $"{seconds:0.##}s";
    }

    static string FormatNumber(float value)
    {
        if (Mathf.Approximately(value, Mathf.Round(value)))
            return Mathf.RoundToInt(value).ToString();
        return value.ToString("0.##");
    }
}
