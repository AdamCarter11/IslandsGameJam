using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prefab-authored hotbar slot visuals. HotbarUI binds clicks and refreshes data.
/// </summary>
public class HotbarSlotView : MonoBehaviour
{
    [SerializeField] Image background;
    [SerializeField] Image highlight;
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI count;
    [SerializeField] Button button;

    public Image Background => background;
    public Image Highlight => highlight;
    public Image Icon => icon;
    public TextMeshProUGUI Count => count;
    public Button Button => button;

#if UNITY_EDITOR
    public void EditorAssign(Image bg, Image hl, Image ic, TextMeshProUGUI ct, Button btn)
    {
        background = bg;
        highlight = hl;
        icon = ic;
        count = ct;
        button = btn;
    }
#endif
}
