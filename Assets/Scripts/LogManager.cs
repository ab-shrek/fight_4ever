using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

public class LogManager : MonoBehaviour
{
    private static LogManager instance;
    private Dictionary<string, StreamWriter> logFiles = new Dictionary<string, StreamWriter>();
    private string logDirectory = "Logs";

    public static LogManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("LogManager");
                instance = go.AddComponent<LogManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLogDirectory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeLogDirectory()
    {
        string fullPath = Path.Combine(Application.dataPath, "..", logDirectory);
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
    }

    public StreamWriter GetLogFile(string componentName)
    {
        if (!logFiles.ContainsKey(componentName))
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"{componentName}_{timestamp}.log";
            string fullPath = Path.Combine(Application.dataPath, "..", logDirectory, fileName);
            
            try
            {
                StreamWriter writer = new StreamWriter(fullPath, true);
                logFiles[componentName] = writer;
                Debug.Log($"[LogManager] Created new log file for {componentName}: {fileName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[LogManager] Failed to create log file for {componentName}: {e.Message}");
                return null;
            }
        }
        return logFiles[componentName];
    }

    public void Log(string componentName, string message)
    {
        StreamWriter logFile = GetLogFile(componentName);
        if (logFile != null)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                logFile.WriteLine($"[{timestamp}] {message}");
                logFile.Flush();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LogManager] Failed to write to log file for {componentName}: {e.Message}");
            }
        }
    }

    private void OnDestroy()
    {
        foreach (var writer in logFiles.Values)
        {
            try
            {
                writer.Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"[LogManager] Error closing log file: {e.Message}");
            }
        }
        logFiles.Clear();
    }
} 