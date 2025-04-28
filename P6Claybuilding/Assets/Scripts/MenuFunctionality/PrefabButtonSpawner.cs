using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// PrefabButtonSpawner: Dynamically configures a set of UI buttons so that
/// each button represents a spawnable prefab. Clicking a button sets the
/// selectedPrefab and refreshes the ghost preview in the scene.
/// </summary>
public class PrefabButtonSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnableItem
    {
        /// <summary>The prefab GameObject that will be instantiated when selected.</summary>
        public GameObject prefab;
        /// <summary>The icon to display on the corresponding UI button.</summary>
        public Sprite icon;
    }

    [Header("UI References")]
    /// <summary>
    /// Array of UI Buttons in the menu. Must match the length of spawnableItems.
    /// </summary>
    public Button[] menuButtons;

    [Header("Spawnable Items")]
    /// <summary>
    /// Array of SpawnableItem entries. Each entry pairs a prefab with its icon.
    /// </summary>
    public SpawnableItem[] spawnableItems;

    /// <summary>
    /// The prefab most recently chosen by clicking one of the buttons.
    /// Other scripts (e.g. PrefabPlacer) will read this to know what to spawn.
    /// </summary>
    [HideInInspector] public GameObject selectedPrefab;

    /// <summary>
    /// Unity Start callback: sets up each button’s image and click behavior.
    /// Ensures the arrays are the same length, then for each index:
    ///  1) assigns the icon to the button’s Image,
    ///  2) clears existing onClick listeners,
    ///  3) adds a new listener that sets selectedPrefab, refreshes the ghost,
    ///     and logs the selection.
    /// </summary>
    private void Start()
    {
        // Ensure number of buttons matches number of spawnable items
        if (menuButtons.Length != spawnableItems.Length)
        {
            Debug.LogError("Number of menu buttons and spawnable items must be equal.");
            return;
        }

        // Loop over each button/item pair
        for (int i = 0; i < menuButtons.Length; i++)
        {
            int index = i;  // Capture loop variable for the lambda
            Button button = menuButtons[index];
            SpawnableItem item = spawnableItems[index];

            // Set the button icon if available
            Image img = button.GetComponentInChildren<Image>();
            if (img != null && item.icon != null)
            {
                img.sprite = item.icon;

                // Optionally adjust button size to match icon aspect ratio
                float aspectRatio = item.icon.rect.width / item.icon.rect.height;
                float baseHeight = 100f;
                float width = baseHeight * aspectRatio;
                RectTransform rt = button.GetComponent<RectTransform>();
                if (rt != null)
                    rt.sizeDelta = new Vector2(width, baseHeight);
            }

            // Remove any old onClick handlers to avoid duplicates
            button.onClick.RemoveAllListeners();

            // Add a new onClick listener for this prefab
            button.onClick.AddListener(() =>
            {
                // Set the globally selected prefab
                selectedPrefab = item.prefab;
                // Find the PrefabPlacer instance in the scene and refresh the ghost preview
                FindObjectOfType<PrefabPlacer>()?.ForceRefreshGhost();
                Debug.Log($"Selected prefab: {item.prefab.name}");
            });
        }
    }
}
