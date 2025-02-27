using UnityEngine;
using UnityEngine.UI;

public class WallFillSlider : MonoBehaviour
{
    public Slider fillSlider;
    public DripFillController dripFillController;

    void Start()
    {
        if (fillSlider != null && dripFillController != null)
        {
            fillSlider.onValueChanged.AddListener(UpdateWallFill);
        }
    }

    void UpdateWallFill(float value)
    {
        if (dripFillController != null)
        {
            dripFillController.SetFillLevel(value);
        }
        else
        {
            Debug.LogWarning("⚠ No DripFillController assigned to the slider!");
        }
    }


    public void RestartDripFill()
    {
        fillSlider.value = 0; // Reset slider visually
        dripFillController.ResetDripEffect();
    }
}
