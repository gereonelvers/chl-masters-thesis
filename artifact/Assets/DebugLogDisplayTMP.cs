using UnityEngine;
using TMPro;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class DebugLogDisplayTMP : MonoBehaviour
{
    public TextMeshProUGUI logText;

    // Option: if true, new logs are inserted at the top; if false, they're appended at the bottom.
    public bool prependNewLogs = false;

    private StringBuilder logBuilder = new StringBuilder();
    private const int maxLogCount = 1000;

    // Buffer to store incoming log messages
    private List<string> logBuffer = new List<string>();

    // Coroutine update interval in seconds
    public float updateInterval = 1f;

    private Coroutine updateCoroutine;

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        updateCoroutine = StartCoroutine(UpdateLogText());
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        StringBuilder singleLog = new StringBuilder();
        singleLog.AppendFormat("[{0}] {1}\n", type, logString);

        if (type == LogType.Error || type == LogType.Exception)
        {
            singleLog.AppendLine(stackTrace);
        }

        // Add the single log to the buffer
        lock (logBuffer)
        {
            logBuffer.Add(singleLog.ToString());
        }
    }

    IEnumerator UpdateLogText()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            // Retrieve and clear the buffer
            List<string> logsToAdd;
            lock (logBuffer)
            {
                if (logBuffer.Count == 0)
                    continue;

                logsToAdd = new List<string>(logBuffer);
                logBuffer.Clear();
            }

            // Insert new logs at the top or append at the bottom based on the option.
            foreach (string log in logsToAdd)
            {
                if (prependNewLogs)
                {
                    logBuilder.Insert(0, log);
                }
                else
                {
                    logBuilder.Append(log);
                }
            }

            // Split current log into lines for trimming
            string[] logs = logBuilder.ToString().Split('\n');
            // Remove possible empty last element due to trailing newline.
            if (logs.Length > 0 && string.IsNullOrEmpty(logs[logs.Length - 1]))
            {
                logs = logs.Take(logs.Length - 1).ToArray();
            }

            // If we exceed max log count, remove the oldest logs.
            if (logs.Length > maxLogCount)
            {
                int excess = logs.Length - maxLogCount;
                if (prependNewLogs)
                {
                    // When new logs are at the top, the oldest logs are at the bottom.
                    // Keep only the first maxLogCount lines.
                    var newLogs = logs.Take(maxLogCount).ToArray();
                    logBuilder.Clear();
                    logBuilder.Append(string.Join("\n", newLogs) + "\n");
                }
                else
                {
                    // When new logs are appended at the bottom, remove from the start.
                    int currentIndex = 0;
                    int newLineCount = 0;
                    for (int i = 0; i < logBuilder.Length; i++)
                    {
                        if (logBuilder[i] == '\n')
                        {
                            newLineCount++;
                            if (newLineCount >= excess)
                            {
                                currentIndex = i + 1;
                                break;
                            }
                        }
                    }
                    if (currentIndex > 0)
                    {
                        logBuilder.Remove(0, currentIndex);
                    }
                    else
                    {
                        logBuilder.Clear();
                    }
                }
            }

            // Update the UI with the latest log content
            if (logText != null)
            {
                logText.text = logBuilder.ToString();
            }
        }
    }

    public void ClearLogs()
    {
        lock (logBuffer)
        {
            logBuffer.Clear();
        }

        logBuilder.Clear();
        if (logText != null)
        {
            logText.text = "";
        }
    }
}
