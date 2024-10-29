using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Nexus.Core.Services;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nexus.Timing
{
    [ServiceImplementation]
    public class NetworkTimeService : MonoBehaviour, INetworkTimeService
    {
#if UNITY_EDITOR
        [Header("[Unity Editor Only]")]
        [Tooltip("Show messages to indicate the status of the network time in the debug log.")]
        [SerializeField]
        private bool _showDebugMessages = true;

        [Tooltip("Show warnings to indicate an issue that could cause the app to not function correctly.")]
        [SerializeField]
        private bool _showDebugWarnings = true;

        private bool _applicationPaused;
#endif

        // Network communication and sync settings.
        private const int
            NtpServerCooldownSeconds = 64; // Time to wait before requesting time from same NTP server (64 to 1024).

        private const int NetworkTimeoutMilliseconds = 2000; // Time to wait on a NTP server response before canceling.

        private const int
            NtpRequestMaxFails = 2; // Allowed timeouts to a NTP server before excluding it as a request option.

        private const int
            AllowedOffSyncSeconds = 1; // Allowed time change between network time and system time each update frame.

        private const int
            AllowedPauseSeconds = 10; // Allowed time to be paused before requiring a resync to a NTP server.

        private const float
            WaitForNetworkMinSeconds =
                1f; // Minimum time to wait before trying to connect to a NTP server after all connections failed.

        private const float
            WaitForNetworkMaxSeconds =
                60f; // Maximum time to wait before trying to connect to a NTP server as each failed attempt increases the wait time by 1 second.

        private const string
            SaveNtpTimesName = "NextNtpTimes"; // PlayerPref name to save next request times for NTP servers.

        // NTP message data settings.
        private const int NtpUdpPort = 123; // Standard NTP port.
        private const int NtpMessageBytes = 48; // Standard NTP message size.
        private const byte NtpRequestHeader = 0x1B; // Standard NTP message header.
        private const int NtpSecondsOffsetByte = 40; // Standard byte position for current time seconds in NTP message.

        private const int
            NtpFractionOfSecondOffsetByte =
                44; // Standard byte position for current time fraction of seconds in NTP message.

        private readonly (string DomainNameAddress, DateTime NextRequestTime, int FailCount)[] _ntpServers =
            new (string, DateTime, int)[9]
            {
                ("time.cloudflare.com", DateTime.MinValue, 0),
                ("time.google.com", DateTime.MinValue, 0),
                ("pool.ntp.org", DateTime.MinValue, 0),
                ("time.aws.com", DateTime.MinValue, 0),
                ("time.windows.com", DateTime.MinValue, 0),
                ("time.apple.com", DateTime.MinValue, 0),
                ("time.nist.gov", DateTime.MinValue, 0),
                ("clock.isc.org", DateTime.MinValue, 0),
                ("ntp.ubuntu.com", DateTime.MinValue, 0)
            };

        // Network time management.
        private readonly DateTime
            _epochTime =
                new(1900, 1, 1, 0, 0, 0,
                    DateTimeKind.Utc); // Standard beginning of NTP time. New epoch on February 7, 2036.

        private DateTime _networkTimeUtc; // Current network time for the app to use.
        private DateTime _appPauseTime; // System time the app was paused at.
        private TimeSpan _timeDifferenceUtc; // Difference between the NTP time and system time.
        private bool _timeInSync; // To check if network time is correct before using it.
        private bool _currentlySyncingTime; // Prevent multiple simultaneous NTP server syncs.
        private TaskCompletionSource<bool> _initializationTask; // Task to track initialization status

        public DateTime DateTimeUtc
        {
            get
            {
                // Debug log messages.
#if UNITY_EDITOR
                if (_showDebugWarnings)
                {
                    if (_networkTimeUtc == DateTime.MinValue)
                        Debug.LogWarning("Network time has not been set. The value is DateTime.MinValue.");
                    if (!_timeInSync)
                        Debug.LogWarning(
                            "Network time is not in sync. Check IsTimeInSync before getting the network time.");
                }
#endif
                return _networkTimeUtc;
            }
        }

        public bool IsTimeInSync => _timeInSync;

        public bool IsInitialized { get; private set; }

        public void ForceTimeResync()
        {
            _timeInSync = false;
            if (_currentlySyncingTime) return;
            StartCoroutine(SyncNetworkTimeCoroutine(WaitForNetworkMinSeconds));
        }

        public async Task InitializeAsync()
        {
            if (IsInitialized) return;

            _initializationTask = new TaskCompletionSource<bool>();

            // Load the saved next request times for each NTP server
            LoadRequestTimesForNtpServers();

            // Start network time synchronization
            StartCoroutine(InitialSyncCoroutine());

            await _initializationTask.Task;
            IsInitialized = true;
        }

        public Task WaitForInitialization()
        {
            return IsInitialized ? Task.CompletedTask : _initializationTask?.Task ?? Task.CompletedTask;
        }

        private IEnumerator InitialSyncCoroutine()
        {
            var syncCoroutine = SyncNetworkTimeCoroutine(WaitForNetworkMinSeconds);
            yield return syncCoroutine;

            _initializationTask.SetResult(true);
        }

        private void OnEnable()
        {
            // Allow simulate application pausing when pause button is pressed inside Unity Editor.
#if UNITY_EDITOR
            EditorApplication.pauseStateChanged += OnPauseStateChanged;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.pauseStateChanged -= OnPauseStateChanged;
#endif
        }

        private void Update()
        {
            if (!IsInitialized) return;

            // Wait for network to be in sync.
            if (_currentlySyncingTime) return;

            // Calculate the next network time from the system time.
            DateTime systemTimeUtc = DateTime.UtcNow;
            DateTime nextNetworkTimeUtc = systemTimeUtc.Add(_timeDifferenceUtc);

            // Time has changed too much and is now out of sync.
            if (Mathf.Abs((float)(_networkTimeUtc - nextNetworkTimeUtc).TotalSeconds) > AllowedOffSyncSeconds)
            {
                _timeInSync = false;
                StartCoroutine(SyncNetworkTimeCoroutine(WaitForNetworkMinSeconds));
                return;
            }

            // Set network time value.
            _networkTimeUtc = nextNetworkTimeUtc;
        }

        private void OnApplicationQuit()
        {
            SaveRequestTimesForNtpServers();
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (!_timeInSync || _currentlySyncingTime)
                return;

            if (isPaused)
                _appPauseTime = DateTime.UtcNow;
            else
            {
                DateTime systemTime = DateTime.UtcNow;
                double appPauseForSeconds = (systemTime - _appPauseTime).TotalSeconds;
                if (appPauseForSeconds < 0 || appPauseForSeconds > AllowedPauseSeconds)
                    _timeInSync = false;
                else
                    _networkTimeUtc = systemTime.Add(_timeDifferenceUtc);
            }
        }

#if UNITY_EDITOR
        private void OnPauseStateChanged(PauseState pauseState)
        {
            _applicationPaused = pauseState == PauseState.Paused;
            OnApplicationPause(_applicationPaused);
        }
#endif

        private IEnumerator SyncNetworkTimeCoroutine(float waitSecondsBetweenFails)
        {
            LogSyncStartMessage(waitSecondsBetweenFails);

            _currentlySyncingTime = true;
            yield return null;

            while (true)
            {
                yield return new WaitForSecondsRealtime(waitSecondsBetweenFails);

                var syncResult = TryUpdateNetworkTime();
                bool success = false;

                while (syncResult.MoveNext())
                {
                    success = syncResult.Current;
                    yield return null;
                }

                if (success)
                {
                    yield break;
                }

                ResetNtpServerFailCounts();
                waitSecondsBetweenFails = UpdateWaitTime(waitSecondsBetweenFails);
                LogSyncFailureMessage(waitSecondsBetweenFails);
            }
        }

        private IEnumerator<bool> TryUpdateNetworkTime()
        {
            for (int i = 0; i < _ntpServers.Length; i++)
            {
                if (IsNetworkUnavailable())
                    break;

                if (ShouldSkipServer(i))
                    continue;

                UpdateServerRequestTime(i);

                var timeResult = TryGetTimeFromServer(i);
                while (timeResult.MoveNext())
                {
                    yield return false;
                }

                if (timeResult.Current)
                {
                    yield return true;
                    yield break;
                }
            }

            yield return false;
        }

        private bool IsNetworkUnavailable()
        {
            return Application.internetReachability == NetworkReachability.NotReachable;
        }

        private bool ShouldSkipServer(int serverIndex)
        {
            DateTime systemTimeUtc = DateTime.UtcNow;
            return systemTimeUtc < _ntpServers[serverIndex].NextRequestTime ||
                   _ntpServers[serverIndex].FailCount > NtpRequestMaxFails;
        }

        private void UpdateServerRequestTime(int serverIndex)
        {
            _ntpServers[serverIndex].NextRequestTime =
                DateTime.UtcNow.AddSeconds(NtpServerCooldownSeconds);
        }

        private IEnumerator<bool> TryGetTimeFromServer(int serverIndex)
        {
            Task<DateTime> getTimeTask = GetTimeFromNtpServerAsync(_ntpServers[serverIndex].DomainNameAddress);
            while (!getTimeTask.IsCompleted)
            {
                yield return false;
            }

            DateTime ntpTime = getTimeTask.Result;

            if (ntpTime == DateTime.MinValue)
            {
                HandleFailedTimeRequest(serverIndex);
                yield return false;
            }
            else
            {
                HandleSuccessfulTimeRequest(serverIndex, ntpTime);
                yield return true;
            }
        }

        private void HandleFailedTimeRequest(int serverIndex)
        {
            _ntpServers[serverIndex].FailCount++;
            LogFailedRequest(serverIndex);
        }

        private void HandleSuccessfulTimeRequest(int serverIndex, DateTime ntpTime)
        {
            DateTime systemTimeUtc = DateTime.UtcNow;
            _timeDifferenceUtc = systemTimeUtc - ntpTime;
            _ntpServers[serverIndex].FailCount = 0;

            LogSuccessfulRequest(serverIndex, ntpTime);

            _networkTimeUtc = systemTimeUtc.Add(_timeDifferenceUtc);
            _timeInSync = true;
            _currentlySyncingTime = false;
        }

        private void ResetNtpServerFailCounts()
        {
            for (int i = 0; i < _ntpServers.Length; i++)
            {
                _ntpServers[i].FailCount = 0;
            }
        }

        private float UpdateWaitTime(float currentWaitTime)
        {
            if (currentWaitTime < WaitForNetworkMaxSeconds)
            {
                return Math.Min(currentWaitTime + 1f, WaitForNetworkMaxSeconds);
            }

            return currentWaitTime;
        }

#if UNITY_EDITOR
        private void LogSyncStartMessage(float waitTime)
        {
            if (_showDebugMessages)
                Debug.Log("NetworkTimeManager: Getting time from a NTP server.");
            if (_showDebugWarnings && waitTime < 1f)
                Debug.LogWarning(
                    "NetworkTimeManager: The starting seconds to wait between sync attempts is less than 1.");
        }

        private void LogFailedRequest(int serverIndex)
        {
            if (_showDebugMessages)
                Debug.Log(
                    $"NetworkTimeManager: NTP request to \"{_ntpServers[serverIndex].DomainNameAddress}\" failed.");
        }

        private void LogSuccessfulRequest(int serverIndex, DateTime ntpTime)
        {
            if (_showDebugMessages)
                Debug.Log(
                    $"NetworkTimeManager: Received {ntpTime} from \"{_ntpServers[serverIndex].DomainNameAddress}\".");
        }

        private void LogSyncFailureMessage(float waitTime)
        {
            if (_showDebugWarnings)
                Debug.LogWarning("NetworkTimeManager: Failed to update network time from any NTP servers. "
                                 + $"Retry in {waitTime} {(waitTime == 1f ? "second" : "seconds")}.");
        }
#endif

        private async Task<DateTime> GetTimeFromNtpServerAsync(string ntpServerDnsAddress)
        {
            try
            {
                var ntpData = CreateNtpRequestData();
                var serverAddresses = await ResolveNtpServerAddresses(ntpServerDnsAddress);

                using var client = CreateUdpClient();
                client.Connect(new IPEndPoint(serverAddresses[0], NtpUdpPort));

                var (sendTime, receiveTime, response) = await SendAndReceiveNtpRequest(client, ntpData);

                ValidateNtpResponse(response, serverAddresses, ntpServerDnsAddress);
                ValidateRoundTripTime(sendTime, receiveTime);

                return CalculateNetworkTime(response.Buffer, sendTime, receiveTime);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return DateTime.MinValue;
            }
        }

        private byte[] CreateNtpRequestData()
        {
            var ntpData = new byte[NtpMessageBytes];
            ntpData[0] = NtpRequestHeader;
            return ntpData;
        }

        private async Task<IPAddress[]> ResolveNtpServerAddresses(string ntpServerDnsAddress)
        {
            var addresses = (await Dns.GetHostEntryAsync(ntpServerDnsAddress)).AddressList;

            if (addresses.Length == 0)
            {
                throw new InvalidOperationException(
                    $"NetworkTimeManager: No IP address found for \"{ntpServerDnsAddress}\".");
            }

            return addresses;
        }

        private UdpClient CreateUdpClient()
        {
            var client = new UdpClient();
            client.Client.ReceiveTimeout = NetworkTimeoutMilliseconds;
            return client;
        }

        private async Task<(DateTime sendTime, DateTime receiveTime, UdpReceiveResult response)>
            SendAndReceiveNtpRequest(UdpClient client, byte[] ntpData)
        {
            var sendTime = DateTime.UtcNow;
            await client.SendAsync(ntpData, ntpData.Length);

            var response = await client.ReceiveAsync();
            var receiveTime = DateTime.UtcNow;

            return (sendTime, receiveTime, response);
        }

        private void ValidateNtpResponse(UdpReceiveResult response, IPAddress[] serverAddresses,
            string ntpServerDnsAddress)
        {
            ValidateResponseSourceAddress(response, serverAddresses, ntpServerDnsAddress);
            ValidateNtpMessageFormat(response.Buffer);
            ValidateNtpServerSync(response.Buffer, ntpServerDnsAddress);
        }

        private void ValidateResponseSourceAddress(UdpReceiveResult response, IPAddress[] serverAddresses,
            string ntpServerDnsAddress)
        {
            if (!serverAddresses.Any(ipAddress => ipAddress.Equals(response.RemoteEndPoint.Address)))
            {
                throw new InvalidOperationException(
                    $"NetworkTimeManager: Received message IP address does not match \"{ntpServerDnsAddress}\".");
            }
        }

        private void ValidateNtpMessageFormat(byte[] ntpData)
        {
            bool isValidSize = ntpData.Length == NtpMessageBytes;
            bool isValidVersion = (ntpData[0] & 0x07) == 4;
            bool isValidMode = ((ntpData[0] >> 3) & 0x07) == 3 || ((ntpData[0] >> 3) & 0x07) == 4;

            if (!isValidSize || !isValidVersion || !isValidMode)
            {
                throw new InvalidOperationException(
                    "NetworkTimeManager: Received message is not a valid size, type, or version for the expected NTP data.");
            }
        }

        private void ValidateNtpServerSync(byte[] ntpData, string ntpServerDnsAddress)
        {
            if (((ntpData[0] >> 6) & 0x03) == 3)
            {
                throw new InvalidOperationException(
                    $"NetworkTimeManager: NTP server \"{ntpServerDnsAddress}\" is not synchronized.");
            }
        }

        private void ValidateRoundTripTime(DateTime sendTime, DateTime receiveTime)
        {
            TimeSpan roundTripTime = receiveTime - sendTime;
            if (roundTripTime.TotalMilliseconds > NetworkTimeoutMilliseconds || roundTripTime.TotalMilliseconds < 0)
            {
                throw new InvalidOperationException(
                    "NetworkTimeManager: The system time changed too much while waiting on a NTP response.");
            }
        }

        private DateTime CalculateNetworkTime(byte[] ntpData, DateTime sendTime, DateTime receiveTime)
        {
            var (ntpSeconds, ntpFractionOfSecond) = ExtractNtpTimeComponents(ntpData);

            double ntpMilliseconds = (ntpSeconds * 1000) + (ntpFractionOfSecond * 1000 / 0x100000000L);
            DateTime ntpTime = _epochTime.AddMilliseconds(ntpMilliseconds);

            TimeSpan roundTripTime = receiveTime - sendTime;
            return ntpTime.AddMilliseconds(-(roundTripTime.TotalMilliseconds / 2));
        }

        private (ulong seconds, ulong fractionOfSecond) ExtractNtpTimeComponents(byte[] ntpData)
        {
            ulong ntpSeconds = BitConverter.ToUInt32(ntpData, NtpSecondsOffsetByte);
            ulong ntpFractionOfSecond = BitConverter.ToUInt32(ntpData, NtpFractionOfSecondOffsetByte);

            if (BitConverter.IsLittleEndian)
            {
                ntpSeconds = SwapEndianness(ntpSeconds);
                ntpFractionOfSecond = SwapEndianness(ntpFractionOfSecond);
            }

            return (ntpSeconds, ntpFractionOfSecond);
        }

        /// <summary>
        /// Swap the endianness of <paramref name="dataToConvert"/>.
        /// </summary>
        private uint SwapEndianness(ulong dataToConvert)
        {
            return (uint)(((dataToConvert & 0x000000ff) << 24) + ((dataToConvert & 0x0000ff00) << 8)
                                                               + ((dataToConvert & 0x00ff0000) >> 8) +
                                                               ((dataToConvert & 0xff000000) >> 24));
        }

        /// <summary>
        /// Save the next request time for each NTP server.
        /// </summary>
        private void SaveRequestTimesForNtpServers()
        {
            // PlayerPref strings can be a max of 243 characters for Windows registry (255 total - 12 Unity suffix).
            int ntpServerCount = _ntpServers.Length;
            if (ntpServerCount > 12)
            {
                ntpServerCount = 12;

                // Debug log messages.
#if UNITY_EDITOR
                if (_showDebugWarnings)
                    Debug.LogWarning(
                        $"NetworkTimeManager: Only 12 of the request times for {_ntpServers.Length} NTP servers were saved. "
                        + "PlayerPref strings can be a max of 243 characters for Windows registry.");
#endif
            }

            // Add the request time for each NTP server to a string.
            StringBuilder requestTimesToSave = new();
            for (int i = 0; i < ntpServerCount; i++)
                requestTimesToSave.Append(_ntpServers[i].NextRequestTime.Ticks.ToString("D20"));

            // Save the next request times.
            PlayerPrefs.SetString(SaveNtpTimesName, requestTimesToSave.ToString());
            PlayerPrefs.Save();

            // Debug log messages.
#if UNITY_EDITOR
            if (_showDebugMessages)
            {
                StringBuilder debugMessage = new();
                debugMessage.Append($"NetworkTimeManager: Saved request times for NTP servers: {requestTimesToSave}");
                for (int i = 0; i < ntpServerCount; i++)
                    debugMessage.Append($"\n{_ntpServers[i].DomainNameAddress} : {_ntpServers[i].NextRequestTime}");
                Debug.Log(debugMessage);
            }
#endif
        }

        private void LoadRequestTimesForNtpServers()
        {
            string savedRequestTimes = LoadSavedRequestTimes();
            if (string.IsNullOrEmpty(savedRequestTimes))
                return;

            int ntpServerCount = GetValidServerCount();

            if (!ValidateSavedDataFormat(savedRequestTimes))
                return;

            var requestTimeTicks = ParseRequestTimes(savedRequestTimes, ntpServerCount);
            AssignRequestTimesToServers(requestTimeTicks);

            LogLoadedRequestTimes(savedRequestTimes, ntpServerCount);
        }

        private string LoadSavedRequestTimes()
        {
            return PlayerPrefs.GetString(SaveNtpTimesName, string.Empty);
        }

        private int GetValidServerCount()
        {
            const int MaxServerCount = 12;
            return Math.Min(_ntpServers.Length, MaxServerCount);
        }

        private bool ValidateSavedDataFormat(string savedRequestTimes)
        {
            const int TimestampLength = 20;

            if (savedRequestTimes.Length % TimestampLength == 0)
                return true;

            LogValidationError();
            return false;
        }

        private void LogValidationError()
        {
            string errorMessage = "NetworkTimeManager: Failed to load NTP server request times.";
#if UNITY_EDITOR
            errorMessage += " The read string value is not the correct length.";
#endif
            Debug.LogError(errorMessage);
        }

        private long[] ParseRequestTimes(string savedRequestTimes, int serverCount)
        {
            const int TimestampLength = 20;
            var requestTimeTicks = new long[serverCount];

            for (int i = 0; i < serverCount; i++)
            {
                string parsedRequestTime = savedRequestTimes.Substring(i * TimestampLength, TimestampLength);
                requestTimeTicks[i] = long.Parse(parsedRequestTime);
            }

            return requestTimeTicks;
        }

        private void AssignRequestTimesToServers(long[] requestTimeTicks)
        {
            for (int i = 0; i < requestTimeTicks.Length; i++)
            {
                var nextRequestTime = new DateTime(requestTimeTicks[i]);

                if (!IsRequestTimeValid(nextRequestTime))
                {
                    LogInvalidRequestTimeError();
                    continue;
                }

                _ntpServers[i].NextRequestTime = nextRequestTime;
            }
        }

        private bool IsRequestTimeValid(DateTime requestTime)
        {
            var timeUntilRequest = (requestTime - DateTime.UtcNow).TotalSeconds;
            return timeUntilRequest <= NtpServerCooldownSeconds;
        }

        private void LogInvalidRequestTimeError()
        {
            string errorMessage = "NetworkTimeManager: Failed to load a NTP server request time.";
#if UNITY_EDITOR
            errorMessage += " The read time is greater than \"NtpServerCooldownSeconds\".";
#endif
            Debug.LogError(errorMessage);
        }

#if UNITY_EDITOR
        private void LogLoadedRequestTimes(string savedRequestTimes, int serverCount)
        {
            if (!_showDebugMessages)
                return;

            var debugMessage = CreateDebugMessage(savedRequestTimes, serverCount);
            Debug.Log(debugMessage);
        }

        private string CreateDebugMessage(string savedRequestTimes, int serverCount)
        {
            var debugMessage = new StringBuilder();
            debugMessage.Append($"NetworkTimeManager: Read request times for NTP servers: {savedRequestTimes}");

            for (int i = 0; i < serverCount; i++)
            {
                debugMessage.Append($"\n{_ntpServers[i].DomainNameAddress} : {_ntpServers[i].NextRequestTime}");
            }

            return debugMessage.ToString();
        }
#endif
    }
}