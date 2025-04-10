using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PrefabButtonSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnableItem
    {
        public GameObject prefab; // The prefab to be selected
        public Sprite icon;       // The sprite to assign to the button
    }

    [Header("UI References")]
    public Button[] menuButtons; // Set these in the inspector (four buttons in your case)

    [Header("Spawnable Items")]
    public SpawnableItem[] spawnableItems; // Should have the same number of elements as menuButtons

    // This will hold the prefab you choose
    [HideInInspector] public GameObject selectedPrefab;

    private void Start()
    {
        // Check if both arrays have the same length
        if (menuButtons.Length != spawnableItems.Length)
        {
            Debug.LogError("Number of menu buttons and spawnable items must be equal.");
            return;
        }

        // Iterate over each button and assign the correct listener and icon
        for (int i = 0; i < menuButtons.Length; i++)
        {
            int index = i; // Capture index for the lambda expression

            // Get the button reference
            Button button = menuButtons[i];
            // Get the corresponding spawnable item
            SpawnableItem item = spawnableItems[i];

            // Set the button's image to match the spawnable item's icon
            Image img = button.GetComponentInChildren<Image>();
            if (img != null && item.icon != null)
            {
                img.sprite = item.icon;

                // Optionally adjust aspect ratio, size, etc.
                float aspectRatio = item.icon.rect.width / item.icon.rect.height;
                float baseHeight = 100f;
                float width = baseHeight * aspectRatio;
                RectTransform rt = button.GetComponent<RectTransform>();
                if (rt != null)
                    rt.sizeDelta = new Vector2(width, baseHeight);
            }

            // Remove any pre-existing listeners (optional, but good for hygiene)
            button.onClick.RemoveAllListeners();

            // Add the click event listener
            button.onClick.AddListener(() =>
            {
                // When the button is clicked, set the selected prefab
                selectedPrefab = item.prefab;
                // Optionally, update any preview or placement logic
                FindObjectOfType<PrefabPlacer>()?.ForceRefreshGhost();
                Debug.Log($"Selected prefab: {item.prefab.name}");
            });
        }
    }
}