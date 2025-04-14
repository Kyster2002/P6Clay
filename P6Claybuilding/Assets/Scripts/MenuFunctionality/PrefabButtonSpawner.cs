using UnityEngine;
using UnityEngine.UI;

public class PrefabButtonSpawner : MonoBehaviour
{
    [System.Serializable]
    public class SpawnableItem
    {
        public GameObject prefab;
        public Sprite icon;
    }

    [Header("Spawnable Items")]
    public SpawnableItem[] spawnableItems;

    private Button[] menuButtons;
    [HideInInspector] public GameObject selectedPrefab;

    public void Initialize()
    {
        // 🧠 Try to use self if already Content
        Transform contentTransform = transform.name == "Content" ? transform : FindDeepChild(transform, "Content");

        if (contentTransform == null)
        {
            Debug.LogError("❌ Content not found inside spawned menu!");
            return;
        }

        menuButtons = contentTransform.GetComponentsInChildren<Button>();
        if (menuButtons.Length == 0)
        {
            Debug.LogError("❌ No buttons found under Content!");
            return;
        }

        if (menuButtons.Length != spawnableItems.Length)
        {
            Debug.LogError($"❌ Number of buttons ({menuButtons.Length}) and spawnable items ({spawnableItems.Length}) does not match!");
            return;
        }

        for (int i = 0; i < menuButtons.Length; i++)
        {
            int index = i;
            Image img = menuButtons[i].GetComponentInChildren<Image>();
            if (img != null && spawnableItems[i].icon != null)
            {
                img.sprite = spawnableItems[i].icon;
                img.preserveAspect = true;
            }

            menuButtons[i].onClick.RemoveAllListeners();
            menuButtons[i].onClick.AddListener(() =>
            {
                selectedPrefab = spawnableItems[index].prefab;
                Debug.Log($"✅ Selected prefab: {selectedPrefab.name}");
            });
        }
    }


    // Deep search helper
    private Transform FindDeepChild(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
                return child;

            Transform found = FindDeepChild(child, targetName);
            if (found != null)
                return found;
        }
        return null;
    }

}
