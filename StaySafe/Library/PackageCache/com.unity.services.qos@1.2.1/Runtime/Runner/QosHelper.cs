using System;

namespace Unity.Networking.QoS
{
    internal static class QosHelper
    {
        const ulong WSAEWOULDBLOCK = 10035;  // (windows)
        const ulong WSAETIMEDOUT = 10060;  // (windows)
        const ulong EAGAIN_EWOULDBLOCK_1 = 11; // (supported POSIX platforms, EWOULDBLOCK generally an alias for EAGAIN)(*)
        const ulong EAGAIN_EWOULDBLOCK_2 = 35; // (supported POSIX platforms, EWOULDBLOCK generally an alias for EAGAIN)(*)
        // (*)Could also be 54 on SUSv3, and 246 on AIX 4.3,5.1, but we don't support those platforms

        internal static bool WouldBlock(ulong errorcode)
        {
            return errorcode == WSAEWOULDBLOCK ||
                errorcode == WSAETIMEDOUT ||
                errorcode == EAGAIN_EWOULDBLOCK_1 ||
                errorcode == EAGAIN_EWOULDBLOCK_2;
        }

        internal static bool ExpiredUtc(DateTime timeUtc)
        {
            return DateTime.UtcNow > timeUtc;
        }

        internal static string Since(DateTime dt)
        {
            return $"{(DateTime.UtcNow - dt).TotalMilliseconds:F0}ms";
        }
    }
}
