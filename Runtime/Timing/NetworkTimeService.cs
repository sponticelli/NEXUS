using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Nexus.Core.ServiceLocation;
using UnityEngine;
using Nexus.Core.Services;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nexus.Timing
{
    [ServiceImplementation]
    public class NetworkTimeService : MonoBehaviour, INetworkTimeService, IConfigurable<NetworkTimeServiceConfig>
    {
        private NetworkTimeServiceConfig _config;
        
#if UNITY_EDITOR
        private bool _applicationPaused;
#endif

        public string NtpTimesKey { get; set; } = "NEXUS_NTP_TIMES";

        // NTP message data settings.
        private const int NtpUdpPort = 123; // Standard NTP port.
        private const int NtpMessageBytes = 48; // Standard NTP message size.
        private const byte NtpRequestHeader = 0x1B; // Standard NTP message header.
        private const int NtpSecondsOffsetByte = 40; // Standard byte position for current time seconds in NTP message.
        private const int NtpFractionOfSecondOffsetByte = 44; // Standard byte position for current time fraction of seconds in NTP message.

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

        // Standard beginning of NTP time. New epoch on February 7, 2036.
        private readonly DateTime _epochTime = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc); 
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
#if UNITY_EDITOR
                if (_config.showDebugWarnings)
                {
                    if (_networkTimeUtc == DateTime.MinValue)
                        Debug.LogWarning("Network time has not been set. The value is DateTime.MinValue.");
                    if (!_timeInSync)
                        Debug.LogWarning("Network time is not in sync. Check IsTimeInSync before getting the network time.");
                }
#endif
                return _networkTimeUtc;
            }
        }

        public bool IsTimeInSync => _timeInSync;

        public bool IsInitialized { get; private set; }
        
        public void Configure(NetworkTimeServiceConfig configuration)
        {
            _config = configuration;
            Debug.Log("NetworkTimeService configured with custom settings");
        }


        public void ForceTimeResync()
        {
            _timeInSync = false;
            if (_currentlySyncingTime) return;
            StartCoroutine(SyncNetworkTimeCoroutine(_config.waitForNetworkMinSeconds));
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
            var syncCoroutine = SyncNetworkTimeCoroutine(_config.waitForNetworkMinSeconds);
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
            if (Mathf.Abs((float)(_networkTimeUtc - nextNetworkTimeUtc).TotalSeconds) > _config.allowedOffSyncSeconds)
            {
                _timeInSync = false;
                StartCoroutine(SyncNetworkTimeCoroutine(_config.waitForNetworkMinSeconds));
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
                if (appPauseForSeconds < 0 || appPauseForSeconds > _config.allowedPauseSeconds)
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
                   _ntpServers[serverIndex].FailCount > _config.ntpRequestMaxFails;
        }

        private void UpdateServerRequestTime(int serverIndex)
        {
            _ntpServers[serverIndex].NextRequestTime =
                DateTime.UtcNow.AddSeconds(_config.ntpServerCooldownSeconds);
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
            if (currentWaitTime < _config.waitForNetworkMaxSeconds)
            {
                return Math.Min(currentWaitTime + 1f, _config.waitForNetworkMaxSeconds);
            }

            return currentWaitTime;
        }


        private void LogSyncStartMessage(float waitTime)
        {
            if (_config.showDebugMessages)
                Debug.Log("NetworkTimeService: Getting time from a NTP server.");
            if (_config.showDebugWarnings && waitTime < 1f)
                Debug.LogWarning(
                    "NetworkTimeService: The starting seconds to wait between sync attempts is less than 1.");
        }

        private void LogFailedRequest(int serverIndex)
        {
            if (_config.showDebugMessages)
                Debug.Log(
                    $"NetworkTimeService: NTP request to \"{_ntpServers[serverIndex].DomainNameAddress}\" failed.");
        }

        private void LogSuccessfulRequest(int serverIndex, DateTime ntpTime)
        {
            if (_config.showDebugMessages)
                Debug.Log(
                    $"NetworkTimeService: Received {ntpTime} from \"{_ntpServers[serverIndex].DomainNameAddress}\".");
        }

        private void LogSyncFailureMessage(float waitTime)
        {
            if (_config.showDebugWarnings)
                Debug.LogWarning("NetworkTimeService: Failed to update network time from any NTP servers. "
                                 + $"Retry in {waitTime} {(waitTime == 1f ? "second" : "seconds")}.");
        }

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
                    $"NetworkTimeService: No IP address found for \"{ntpServerDnsAddress}\".");
            }

            return addresses;
        }

        private UdpClient CreateUdpClient()
        {
            var client = new UdpClient();
            client.Client.ReceiveTimeout = _config.networkTimeoutMilliseconds;
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
                    $"NetworkTimeService: Received message IP address does not match \"{ntpServerDnsAddress}\".");
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
                    "NetworkTimeService: Received message is not a valid size, type, or version for the expected NTP data.");
            }
        }

        private void ValidateNtpServerSync(byte[] ntpData, string ntpServerDnsAddress)
        {
            if (((ntpData[0] >> 6) & 0x03) == 3)
            {
                throw new InvalidOperationException(
                    $"NetworkTimeService: NTP server \"{ntpServerDnsAddress}\" is not synchronized.");
            }
        }

        private void ValidateRoundTripTime(DateTime sendTime, DateTime receiveTime)
        {
            TimeSpan roundTripTime = receiveTime - sendTime;
            if (roundTripTime.TotalMilliseconds > _config.networkTimeoutMilliseconds || roundTripTime.TotalMilliseconds < 0)
            {
                throw new InvalidOperationException(
                    "NetworkTimeService: The system time changed too much while waiting on a NTP response.");
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
                if (_config.showDebugWarnings)
                    Debug.LogWarning(
                        $"NetworkTimeService: Only 12 of the request times for {_ntpServers.Length} NTP servers were saved. "
                        + "PlayerPref strings can be a max of 243 characters for Windows registry.");
#endif
            }

            // Add the request time for each NTP server to a string.
            StringBuilder requestTimesToSave = new();
            for (int i = 0; i < ntpServerCount; i++)
                requestTimesToSave.Append(_ntpServers[i].NextRequestTime.Ticks.ToString("D20"));

            // Save the next request times.
            PlayerPrefs.SetString(NtpTimesKey, requestTimesToSave.ToString());
            PlayerPrefs.Save();

            // Debug log messages.
#if UNITY_EDITOR
            if (_config.showDebugMessages)
            {
                StringBuilder debugMessage = new();
                debugMessage.Append($"NetworkTimeService: Saved request times for NTP servers: {requestTimesToSave}");
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
            return PlayerPrefs.GetString(NtpTimesKey, string.Empty);
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
            string errorMessage = "NetworkTimeService: Failed to load NTP server request times.";
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
            return timeUntilRequest <= _config.ntpServerCooldownSeconds;
        }

        private void LogInvalidRequestTimeError()
        {
            string errorMessage = "NetworkTimeService: Failed to load a NTP server request time.";
#if UNITY_EDITOR
            errorMessage += $" The read time is greater than {_config.ntpServerCooldownSeconds}.";
#endif
            Debug.LogError(errorMessage);
        }

#if UNITY_EDITOR
        private void LogLoadedRequestTimes(string savedRequestTimes, int serverCount)
        {
            if (!_config.showDebugMessages)
                return;

            var debugMessage = CreateDebugMessage(savedRequestTimes, serverCount);
            Debug.Log(debugMessage);
        }

        private string CreateDebugMessage(string savedRequestTimes, int serverCount)
        {
            var debugMessage = new StringBuilder();
            debugMessage.Append($"NetworkTimeService: Read request times for NTP servers: {savedRequestTimes}");

            for (int i = 0; i < serverCount; i++)
            {
                debugMessage.Append($"\n{_ntpServers[i].DomainNameAddress} : {_ntpServers[i].NextRequestTime}");
            }

            return debugMessage.ToString();
        }
#endif
    }
}