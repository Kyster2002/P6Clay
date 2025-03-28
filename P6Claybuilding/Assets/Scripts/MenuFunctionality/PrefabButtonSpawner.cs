using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PrefabButtonSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnableItem
    {
        public GameObject prefab;
        public Sprite icon;
    }

    [Header("UI References")]
    public GameObject buttonPrefab; // Your button (should just be an Image + Button)
    public Transform buttonPanelParent; // ScrollView Content object

    [Header("Spawnable Items")]
    public List<SpawnableItem> spawnableItems = new List<SpawnableItem>();

    // We'll use this later for placement
    [HideInInspector] public GameObject selectedPrefab;

    private void Start()
    {
        GenerateButtons();
    }

    void GenerateButtons()
    {
        foreach (SpawnableItem item in spawnableItems)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, buttonPanelParent);
            Button button = buttonObj.GetComponent<Button>();
            Image img = buttonObj.GetComponentInChildren<Image>();

            if (img != null && item.icon != null)
            {
                img.sprite = item.icon;

                // 🧠 Set button size to match sprite's aspect ratio
                float aspectRatio = item.icon.rect.width / item.icon.rect.height;
                float baseHeight = 100f;
                float width = baseHeight * aspectRatio;

                RectTransform rt = buttonObj.GetComponent<RectTransform>();
                if (rt != null)
                    rt.sizeDelta = new Vector2(width, baseHeight);
            }

            // 🖱 Hook up click event to select prefab
            button.onClick.AddListener(() =>
            {
                selectedPrefab = item.prefab;
                Debug.Log($"Selected prefab: {item.prefab.name}");
            });
        }
    }

}
