using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class GameLogger : MonoBehaviour
{
    [Header("UI Components")]
    [Tooltip("Drag the TextMeshProUGUI component from your Scroll View Content here.")]
    public TextMeshProUGUI logText;
    
    [Tooltip("Optional: Drag the ScrollRect to auto-scroll to bottom.")]
    public ScrollRect scrollRect;

    [Header("Settings")]
    public int maxLogLines = 20;

    private List<string> logLines = new List<string>();

    void Awake()
    {
        if (logText == null)
        {
            Debug.LogWarning("GameLogger: logText not assigned!");
        }
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // 1. FILTER: Only show Debug.Log (LogType.Log)
        if (type != LogType.Log) return;
        
        string formattedLine = logString;
        
        // 2. FILTER: Ignore specific internal spam
        if (formattedLine.Contains("UnityEngine.GUIUtility")) return;
        if (formattedLine.Contains("Reduced")) return;

        // Add to list
        logLines.Add(formattedLine);

        // Trim list
        if (logLines.Count > maxLogLines)
        {
            logLines.RemoveAt(0);
        }

        // Update UI
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (logText != null)
        {
            logText.text = string.Join("\n", logLines);
            
            // Auto-scroll
            if (scrollRect != null)
            {
                // We use a small delay or canvas update to ensure the content size is updated before scrolling
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f; // Scroll to bottom
            }
        }
    }
}
