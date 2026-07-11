using UnityEngine;

/// <summary>
/// Runtime state for a planted crop on the grid (not a ScriptableObject).
/// </summary>
public class CropCell
{
    public CropGrowthSO crop;
    public int stageIndex;
    public float stageElapsed;
    public CropView view;

    /// <summary>
    /// True once the crop has entered its final stage (harvestable).
    /// </summary>
    public bool IsReady =>
        crop != null &&
        crop.stages != null &&
        crop.stages.Length > 0 &&
        stageIndex >= crop.stages.Length - 1;

    public CropPropertiesSO CurrentStage
    {
        get
        {
            if (crop == null || crop.stages == null)
                return null;
            if (stageIndex < 0 || stageIndex >= crop.stages.Length)
                return null;
            return crop.stages[stageIndex];
        }
    }

    public void AdvanceStage()
    {
        if (crop == null || crop.stages == null)
            return;
        if (stageIndex >= crop.stages.Length - 1)
            return;

        stageIndex++;
        stageElapsed = 0f;
    }
}
