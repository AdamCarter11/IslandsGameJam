using TMPro;
using UnityEngine;

public class Credits : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI namesText;
    private void OnEnable()
    {
        if(Random.Range(0,100) < 50)
            namesText.text = "Adam Carter\nKhoa Nguyen";
        else
            namesText.text = "Khoa Nguyen\nAdam Carter";
    }
}
