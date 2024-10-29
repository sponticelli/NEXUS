using System;
using Nexus.Core.Services;

namespace Nexus.Timing
{
    [ServiceInterface]
    public interface INetworkTimeService : IInitiable
    {
        /// <summary>
        /// The current network time in UTC.
        /// Check IsTimeInSync to make sure the time is in sync before using this value.
        /// </summary>
        DateTime DateTimeUtc { get; }

        /// <summary>
        /// Indicates if the network time is in sync with the device system time.
        /// </summary>
        bool IsTimeInSync { get; }

        /// <summary>
        /// Set network time as off sync and resync to a NTP server.
        /// </summary>
        void ForceTimeResync();
    }
}