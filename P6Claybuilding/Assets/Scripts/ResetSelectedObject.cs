using UnityEngine;
using UnityEngine.UI;

public class ResetSelectedObject : MonoBehaviour
{
    public Button resetButton; // Button that triggers reset

    void Start()
    {
        // Ensure the reset button is assigned and adds the reset function
        if (resetButton != null)
            resetButton.onClick.AddListener(ResetLastSelected);
        else
            Debug.LogWarning("⚠ Reset button is not assigned!");
    }

    void ResetLastSelected()
    {
        // Get the currently selected object from SelectionManager
        DripFillController selectedObject = SelectionManager.Instance.GetSelectedObject();

        if (selectedObject != null)
        {
            selectedObject.ResetDripEffect();
            Debug.Log($"🔄 Resetting: {selectedObject.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("⚠ No object selected to reset!");
        }
    }
}
