using UnityEngine;
using UnityEngine.UI;

public class WallFillSlider : MonoBehaviour
{
    public Slider fillSlider;  // Assign your global slider in the Inspector.
    public DripFillController selectedDripFillController;

    void Start()
    {
        if (fillSlider != null)
        {
            fillSlider.onValueChanged.RemoveAllListeners();
            fillSlider.onValueChanged.AddListener(UpdateFillAmount);
        }
        else
        {
            Debug.LogWarning("⚠️ No fill slider assigned to WallFillSlider!");
        }
    }

    void Update()
    {
        // Only update the slider's value if an object is selected.
        if (DripFillController.lastSelectedObject != null)
        {
            // Update the slider's value without notifying its listeners.
            fillSlider.SetValueWithoutNotify(DripFillController.lastSelectedObject.FillLevel);
        }
        else
        {
            fillSlider.SetValueWithoutNotify(0f);
        }
    }

    public void UpdateFillAmount(float value)
    {
        if (DripFillController.lastSelectedObject != null)
        {
            DripFillController.lastSelectedObject.SetFillLevel(value);
        }
        else
        {
            Debug.LogWarning("⚠️ No DripFillController selected to update!");
        }
    }

    public void StartDripFill()
    {
        if (DripFillController.lastSelectedObject != null)
        {
            DripFillController.lastSelectedObject.ResetDripEffect();
        }
        else
        {
            Debug.LogWarning("⚠️ No DripFillController selected to start drip fill!");
        }
    }

    public void StartSmoothFill()
    {
        if (DripFillController.lastSelectedObject != null)
        {
            DripFillController.lastSelectedObject.StartSmoothFillWithoutDrip();
        }
        else
        {
            Debug.LogWarning("⚠️ No DripFillController selected to start smooth fill!");
        }
    }

    // This method is provided so other scripts can set the global selection.
    public void SetDripFillController(DripFillController newController)
    {
        DripFillController.lastSelectedObject = newController;
    }
}
