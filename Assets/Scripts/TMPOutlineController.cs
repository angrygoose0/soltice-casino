using UnityEngine;
using TMPro;

public class TMPOutlineController : MonoBehaviour
{
    [SerializeField] private float outlineThickness = 0f;

    private void Awake()
    {
        var tmp = GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.fontMaterial = new Material(tmp.fontMaterial);
            tmp.fontMaterial.SetFloat("_OutlineThickness", outlineThickness);
        }
    }
}

