using UnityEngine;
using TMPro;
using System;
using System.IO;
using UnityEngine.SceneManagement;

public class FPSCounter : MonoBehaviour
{
    public TMP_Text fpsText;
    public float updateInterval = 1f;

    private int frameCount = 0;
    private float elapsed = 0f;
    private int lastDisplayedFPS = -1;

    private string filePath;
    private string sceneName;
    private string sceneFolderPath;

    void Start()
    {
        sceneName = SceneManager.GetActiveScene().name;

        // Create folder for scene
        sceneFolderPath = Path.Combine(Application.persistentDataPath, sceneName);
        Directory.CreateDirectory(sceneFolderPath); // safe even if it already exists

        // Create unique file path within scene folder
        filePath = GetUniqueFilePath("FPS_Log", ".csv", sceneFolderPath);

        // Write CSV header
        using (StreamWriter writer = new StreamWriter(filePath, false))
        {
            writer.WriteLine("Timestamp,SceneName,FPS");
        }

        Debug.Log("Logging FPS to: " + filePath);
    }

    void Update()
    {
        frameCount++;
        elapsed += Time.unscaledDeltaTime;

        if (elapsed >= updateInterval)
        {
            float fps = frameCount / elapsed;
            int roundedFPS = Mathf.RoundToInt(fps);

            if (roundedFPS != lastDisplayedFPS)
            {
                fpsText.text = $"FPS: {roundedFPS}";
                lastDisplayedFPS = roundedFPS;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            LogFPS(timestamp, sceneName, fps);

            frameCount = 0;
            elapsed = 0f;
        }
    }

    void LogFPS(string timestamp, string scene, float fps)
    {
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine($"{timestamp},{scene},{fps:F2}");
        }
    }

    string GetUniqueFilePath(string baseName, string extension, string directory)
    {
        string fullPath = Path.Combine(directory, baseName + extension);
        int counter = 1;

        while (File.Exists(fullPath))
        {
            fullPath = Path.Combine(directory, $"{baseName}_{counter}{extension}");
            counter++;
        }

        return fullPath;
    }
}