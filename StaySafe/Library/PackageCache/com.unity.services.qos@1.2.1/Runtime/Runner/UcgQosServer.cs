using System;
using UnityEngine;

namespace Unity.Networking.QoS
{
    /// <summary>
    /// Definition of QoS server returned from the Discovery Service.
    /// </summary>
    /// <remarks>
    /// Note Unity's JSON deserializer is case-sensitive.  The fields must match exactly.
    /// </remarks>
    [Serializable]
    internal struct UcgQosServer
    {
        // Multiplay-specific region ID of the server
        internal string regionid;
        // Dotted-quad IPv4 address of QoS Server
        internal string ipv4;

        // Dotted-quad IPv6 address of QoS Server
        internal string ipv6;

        // Port of QoS Server. Must be [1..65535]
        internal ushort port;

        [HideInInspector]
        [NonSerialized]
        // May be set as a result of Flow Control.  If set, when to allow this QosServer to be used again.
        internal DateTime BackoffUntilUtc;

        public override string ToString()
        {
            // Prefer IPv6 address with fallback to IPv4
            if (!string.IsNullOrEmpty(ipv6))
                return $"[{ipv6}]:{port}, {regionid}, {BackoffUntilUtc}";

            return string.IsNullOrEmpty(ipv4) ? "" : $"{ipv4}:{port}, {regionid}, {BackoffUntilUtc}";
        }
    }
}
