using UnityEngine;
using FishNet.Connection;
using FishNet.Object;
using System;
using System.IO;

public class NetworkedLogger : NetworkBehaviour
{
    public static NetworkedLogger Instance { get; private set; }

    // The path where the log file will be written.
    private string filePath;

    private void Awake()
    {
        // Establish a singleton instance (note: this instance won't persist across scenes)
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Set the file path to persistentDataPath
        String filename = $"game_log_{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.txt";
        filePath = Path.Combine(Application.persistentDataPath, filename);
        Debug.Log("[NetworkedLogger] Started - logging to: " + Application.persistentDataPath);
    }

    /// <summary>
    /// Logs a message. If called from a client, the message is sent to the server via ServerRpc.
    /// </summary>
    public void Log(string message)
    {
        // If this is a client, send the log message to the server.
        //if (IsClient)
        //{
        LogServerRpc(message);
        //}
        //// If this is the server, log directly.
        //else if (IsServer)
        //{
        //    WriteLog(message);
        //}
    }

    /// <summary>
    /// A ServerRpc method that writes the log on the server.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void LogServerRpc(string message, NetworkConnection networkConnection = null)
    {
        WriteLog(message, networkConnection);
    }

    /// <summary>
    /// Writes the log message to the console and appends it to a file.
    /// </summary>
    private void WriteLog(string message, NetworkConnection connection)
    {
        // Log to the Unity console.
        Debug.Log("[NetworkedLogger] " + connection + " - " + message);
        try
        {
            // Prepare the log entry with a timestamp.
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {connection} - {message}{Environment.NewLine}";
            File.AppendAllText(filePath, logEntry);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to write log to file: " + ex.Message);
        }
    }
}
