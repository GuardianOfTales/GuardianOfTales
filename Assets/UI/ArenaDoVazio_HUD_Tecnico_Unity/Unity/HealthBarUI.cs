using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    public void SetValue(float current, float maximum)
    {
        if (fillImage == null || maximum <= 0f) return;
        fillImage.fillAmount = Mathf.Clamp01(current / maximum);
    }
}
