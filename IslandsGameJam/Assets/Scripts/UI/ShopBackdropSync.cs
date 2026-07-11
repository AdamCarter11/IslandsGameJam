using UnityEngine;

/// <summary>
/// Activates/deactivates the modal backdrop with the shop panel.
/// </summary>
public class ShopBackdropSync : MonoBehaviour
{
    [SerializeField] GameObject backdrop;

    public void SetBackdrop(GameObject backdropObject)
    {
        backdrop = backdropObject;
    }

    void OnEnable()
    {
        if (backdrop != null)
            backdrop.SetActive(true);
    }

    void OnDisable()
    {
        if (backdrop != null)
            backdrop.SetActive(false);
    }
}
