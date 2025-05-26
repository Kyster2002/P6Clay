using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

public class TechnicalTestLogger : MonoBehaviour
{
    private string filePath;
    private float globalTimeCounter;
    private float timeCounter;
    private int frameCounter;
    private float lastFramerate;

    // Maximum number of placed objects during the session
    private int maxPlacedObjects = 0;

    // Dictionary to track placed walls and their types
    private Dictionary<string, int> wallTypeCount;

    // Layer Mask for Walls
    private int wallLayer;

    // Buffer for logs
    private List<string> logBuffer = new List<string>();
    private float logInterval = 0.5f; // Log every second
    private float nextLogTime = 0f;

    void Start()
    {
        timeCounter = 0.0f;
        frameCounter = 0;
        lastFramerate = 0.0f;
        wallTypeCount = new Dictionary<string, int>();

        // Get the layer index for Walls
        wallLayer = LayerMask.NameToLayer("Walls");

        // Generate an indexed filename
        int fileIndex = 0;
        do
        {
            filePath = Application.persistentDataPath + $"/PerformanceLog_{fileIndex}.csv";
            fileIndex++;
        } while (File.Exists(filePath));

        Debug.Log($"CSV File path: {filePath}");

        // Add headers to the buffer (not written yet)
        StringBuilder header = new StringBuilder();
        header.Append("Time (s),FPS,Max Placed Objects");

        // Get all wall types and write headers dynamically
        var initialWallTypes = GetAllCurrentWallTypes();
        foreach (var type in initialWallTypes)
        {
            header.Append($",{type}");
            wallTypeCount[type] = 0;
        }

        // Add the header to the buffer
        logBuffer.Add(header.ToString());
        Debug.Log("Logging started. Listening for Walls.");
    }

    void Update()
    {
        float deltaTime = Time.deltaTime;

        // ✅ Global time is incremented independently of Start()
        globalTimeCounter += deltaTime;

        // FPS calculation
        timeCounter += deltaTime;
        frameCounter++;

        if (timeCounter >= 1.0f)
        {
            lastFramerate = frameCounter / timeCounter;
            frameCounter = 0;
            timeCounter = 0.0f;
        }

        // ** Find all objects in the "Walls" layer **
        GameObject[] walls = FindObjectsOfType<GameObject>()
            .Where(go => go.layer == wallLayer)
            .ToArray();

        Debug.Log($"Found {walls.Length} objects on the 'Walls' layer");

        // ** Reset the counter for this frame **
        foreach (var key in wallTypeCount.Keys.ToList())
        {
            wallTypeCount[key] = 0;
        }

        // ** Count the objects by type and exclude Ghosts **
        foreach (GameObject wall in walls)
        {
            if (wall.name.Contains("Ghost")) continue; // Ignore ghosts

            string wallName = NormalizeName(wall.name);

            if (!wallTypeCount.ContainsKey(wallName))
            {
                wallTypeCount[wallName] = 0;
                AddColumnToBuffer(wallName);  // Add the new type to the buffer header
            }

            // ✅ Increment the count live
            wallTypeCount[wallName]++;
        }

        // ✅ Get the current total count
        int totalPlaced = wallTypeCount.Values.Sum();
        maxPlacedObjects = Mathf.Max(maxPlacedObjects, totalPlaced);

        // ✅ Only log if enough time has passed (1 second)
        if (Time.time >= nextLogTime)
        {
            nextLogTime = Time.time + logInterval;
            BufferLog(globalTimeCounter, lastFramerate);
        }
    }

    // ✅ This runs when Unity stops play mode
    void OnApplicationQuit()
    {
        SaveLogToFile();
        Debug.Log($"CSV written to: {filePath}");
    }

    private void BufferLog(float time, float fps)
    {
        StringBuilder line = new StringBuilder();
        line.Append($"{time:F2},{fps:F1},{maxPlacedObjects}");

        // Write the count for each wall type in the order of the header
        foreach (var type in wallTypeCount.Values)
        {
            line.Append($",{type}");
        }

        // Add to buffer
        logBuffer.Add(line.ToString());
    }

    private void SaveLogToFile()
    {
        Debug.Log("Flushing logs to file...");
        using (StreamWriter writer = new StreamWriter(filePath, false, Encoding.UTF8))
        {
            foreach (string line in logBuffer)
            {
                writer.WriteLine(line);
            }
        }
        logBuffer.Clear();
    }

    private List<string> GetAllCurrentWallTypes()
    {
        // Find all objects in the "Walls" layer and get their unique names
        GameObject[] walls = FindObjectsOfType<GameObject>()
            .Where(go => go.layer == wallLayer)
            .ToArray();

        return walls.Select(wall => NormalizeName(wall.name))
                    .Where(name => !name.Contains("Ghost")) // Ignore ghosts
                    .Distinct()
                    .ToList();
    }

    private void AddColumnToBuffer(string wallType)
    {
        // Add a new column in the header if a new wall type is found
        logBuffer[0] += $",{wallType}";
    }

    // Normalizes names by removing (Clone) and any numeric suffixes
    private string NormalizeName(string originalName)
    {
        // Remove (Clone) and trailing numbers
        string normalized = Regex.Replace(originalName, @" \(Clone\)$", "");
        normalized = Regex.Replace(normalized, @"\s*\d*$", "");
        return normalized;
    }
}
