using UnityEngine;

public class ToolTipObject : MonoBehaviour
{
    [SerializeField]
    private string toolTip;

    public string ToolTip => toolTip;
    public bool HasToolTip => !string.IsNullOrWhiteSpace(toolTip);
}
