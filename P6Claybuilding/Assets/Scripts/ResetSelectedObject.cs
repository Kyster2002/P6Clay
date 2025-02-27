using UnityEngine;
using UnityEngine.UI;

public class ResetSelectedObject : MonoBehaviour
{
    public Button resetButton;

    void Start()
    {
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetLastSelected);
        }
    }

    void ResetLastSelected()
    {
        DripFillController selectedObject = SelectionManager.Instance.GetSelectedObject();

        if (selectedObject != null)
        {
            selectedObject.ResetDripEffect();
            Debug.Log($"🔄 Resetting {selectedObject.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("⚠ No object selected for reset!");
        }
    }
}
