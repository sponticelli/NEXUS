using UnityEngine;

namespace Nexus.Timing
{
    [CreateAssetMenu(fileName = "NetworkTimeServiceConfig", menuName = "Nexus/Timing/Network Time Config")]
    public class NetworkTimeServiceConfig : ScriptableObject
    {
        
        [Header("NTP Server Settings")]
        [Tooltip("Time to wait before requesting time from same NTP server (64 to 1024 seconds)")]
        [Range(64, 1024)]
        public int ntpServerCooldownSeconds = 64;

        [Tooltip("Time to wait on a NTP server response before canceling")] 
        [Range(500, 5000)]
        public int networkTimeoutMilliseconds = 2000;

        [Tooltip("Allowed timeouts to a NTP server before excluding it as a request option")] 
        [Range(1, 5)]
        public int ntpRequestMaxFails = 2;

        [Header("Time Sync Settings")]
        [Tooltip("Allowed time change between network time and system time each update frame")]
        [Range(0.1f, 5f)]
        public float allowedOffSyncSeconds = 1f;

        [Tooltip("Allowed time to be paused before requiring a resync to a NTP server")] 
        [Range(1, 30)]
        public int allowedPauseSeconds = 10;

        [Header("Network Retry Settings")]
        [Tooltip("Minimum time to wait before trying to connect to a NTP server after all connections failed")]
        [Range(0.5f, 5f)]
        public float waitForNetworkMinSeconds = 1f;

        [Tooltip(
            "Maximum time to wait before trying to connect to a NTP server as each failed attempt increases the wait time")]
        [Range(30f, 300f)]
        public float waitForNetworkMaxSeconds = 60f;

        [Header("Debug Settings")]
        [Tooltip("Show messages to indicate the status of the network time in the debug log")]
        public bool showDebugMessages = true;

        [Tooltip("Show warnings to indicate an issue that could cause the app to not function correctly")]
        public bool showDebugWarnings = true;
    }
}