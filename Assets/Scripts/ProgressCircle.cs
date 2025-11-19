using UnityEngine;
using UnityEngine.UI;

public class ProgressCircle : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    private void Start()
    {
        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }
        
        if (fillImage != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Radial360;
            fillImage.fillOrigin = (int)Image.Origin360.Top;
        }
    }

    public void SetProgress(float value)
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(value / 100f);
        }
    }
}

