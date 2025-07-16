using System;
using System.IO;
using UnityEngine;
using FishNet.Managing;
using FishNet.Managing.Statistic;      // StatisticsManager, NetworkTrafficArgs

[DisallowMultipleComponent]
public class NetStatsLogger : MonoBehaviour
{
    [Header("Output")]
    [Tooltip("Blank = Application.persistentDataPath")]
    [SerializeField] string directory = "";
    [SerializeField] string filenameBase = "net_metrics";

    [Header("Sampling")]
    [SerializeField] float sampleInterval = 0.2f;   // seconds

    [Header("Debug")]
    [SerializeField] bool echoToConsole = true;     // live feed in Console

    /* ───────────────────────────────────────────── */
    StreamWriter _csv;
    float _nextSample;
    ulong _bytesIn, _bytesOut;                      // accumulated since last sample
    NetworkManager _nm;

    void Awake()
    {
        _nm = FindObjectOfType<NetworkManager>();
        if (_nm == null) { Debug.LogError("No NetworkManager found"); enabled = false; return; }

        // make sure StatisticsManager is present
        var stats = _nm.GetComponent<StatisticsManager>() ??
                    _nm.gameObject.AddComponent<StatisticsManager>();

        // hook the traffic events
        var nt = stats.NetworkTraffic;
        nt.OnClientNetworkTraffic += OnClientTraffic;
        nt.OnServerNetworkTraffic += OnServerTraffic;

        // resolve path and open CSV
        if (string.IsNullOrWhiteSpace(directory))
            directory = Application.persistentDataPath;
        Directory.CreateDirectory(directory = Path.GetFullPath(directory));

        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var path = Path.Combine(directory, $"{filenameBase}_{stamp}.csv");

        _csv = new StreamWriter(path);
        _csv.WriteLine("t_s,rtt_ms,bytesIn,bytesOut");
        Debug.Log($"[NetStatsLogger] Writing to: {path}");
    }

    void Update()
    {
        if (Time.time < _nextSample || _csv == null) return;
        _nextSample = Time.time + sampleInterval;

        // snapshot + reset accumulators
        ulong bi = _bytesIn; _bytesIn = 0;
        ulong bo = _bytesOut; _bytesOut = 0;

        long rtt = _nm.TimeManager.RoundTripTime;  // ms

        string row = $"{Time.time:F3},{rtt},{bi},{bo}";
        _csv.WriteLine(row);
        if (echoToConsole) Debug.Log("[NetStats] " + row);
    }

    /* traffic event handlers --------------------------------------------- */
    void OnClientTraffic(NetworkTrafficArgs a)
    {
        // FromServerBytes == received, ToServerBytes == sent
        _bytesIn += a.FromServerBytes;
        _bytesOut += a.ToServerBytes;
    }
    void OnServerTraffic(NetworkTrafficArgs a)
    {
        // opposite perspective
        _bytesIn += a.ToServerBytes;
        _bytesOut += a.FromServerBytes;
    }

    /* clean-up ------------------------------------------------------------ */
    void OnApplicationQuit() => Close();
    void OnDestroy() => Close();
    void Close()
    {
        if (_csv == null) return;
        _csv.Flush(); _csv.Dispose(); _csv = null;
        Debug.Log("[NetStatsLogger] CSV closed.");
    }
}
