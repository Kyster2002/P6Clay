using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// WallFillSlider: Bridges the UI Slider with the DripFillController on the
/// currently selected wall. Updates the slider to match fill level and
/// invokes fill or drip animations when buttons are pressed.
/// </summary>
public class WallFillSlider : MonoBehaviour
{
    [Header("UI References")]
    /// <summary>
    /// Reference to the UI Slider used for controlling the fill amount.
    /// </summary>
    public Slider fillSlider;

    /// <summary>
    /// (Unused directly) Can be set by other scripts to change the selection.
    /// </summary>
    public DripFillController selectedDripFillController;

    /// <summary>
    /// Called once when this component awakens. Sets up the slider callback.
    /// </summary>
    void Start()
    {
        if (fillSlider != null)
        {
            // Ensure no duplicate listeners, then hook our UpdateFillAmount method.
            fillSlider.onValueChanged.RemoveAllListeners();
            fillSlider.onValueChanged.AddListener(UpdateFillAmount);
        }
        else
        {
            Debug.LogWarning("⚠️ No fill slider assigned to WallFillSlider!");
        }
    }

    /// <summary>
    /// Called every frame. Keeps the UI slider in sync with the selected wall’s fill.
    /// </summary>
    void Update()
    {
        // If there’s a selected DripFillController, mirror its FillLevel to the slider.
        if (DripFillController.lastSelectedObject != null)
        {
            // Set the slider value silently (no callback) to avoid feedback loops.
            fillSlider.SetValueWithoutNotify(DripFillController.lastSelectedObject.FillLevel);
        }
        else
        {
            // No selection means slider resets to zero.
            fillSlider.SetValueWithoutNotify(0f);
        }
    }

    /// <summary>
    /// Callback invoked when the slider value changes. Updates the fill level on the wall.
    /// </summary>
    /// <param name="value">New slider value between 0 and 1.</param>
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

    /// <summary>
    /// Invoked by a UI Button to restart the dripping fill animation.
    /// </summary>
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

    /// <summary>
    /// Invoked by a UI Button to run the smooth (no-drip) fill animation.
    /// </summary>
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

    /// <summary>
    /// Allows external scripts (e.g., PrefabPlacer) to set which wall’s
    /// DripFillController the slider should drive.
    /// </summary>
    /// <param name="newController">The DripFillController to control.</param>
    public void SetDripFillController(DripFillController newController)
    {
        DripFillController.lastSelectedObject = newController;
    }
}
