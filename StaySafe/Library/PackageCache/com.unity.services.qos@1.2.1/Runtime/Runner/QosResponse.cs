using System;
#if UGS_QOS_SUPPORTED
using System.Runtime.InteropServices;
using Unity.Baselib.LowLevel;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace Unity.Networking.QoS
{
    internal enum FcType
    {
        None = 0,
        Throttle = 1,
        Ban = 2
    }

    internal class QosResponse
    {
        private const int MinPacketLen = 13;
        private const int MaxPacketLen = 1500;
        private const byte ResponseMagic = 0x95;
        private const byte ResponseVersion = 0;

        // Not making these public properties because we need to be able to take the address of them in Recv()
        private byte m_Magic;
        private byte m_VerAndFlow;
        private byte m_Sequence;
        private ushort m_Identifier;
        private ulong m_Timestamp;

        private int m_LatencyMs;

        private ushort m_PacketLength;

        // Can only read the response data.
        internal byte Magic => m_Magic;
        internal byte Version => (byte)((m_VerAndFlow >> 4) & 0x0F);
        internal byte FlowControl => (byte)(m_VerAndFlow & 0x0F);
        internal byte Sequence => m_Sequence;
        internal ushort Identifier => m_Identifier;
        internal ulong Timestamp => m_Timestamp;
        internal ushort Length => m_PacketLength;

        internal int LatencyMs => m_LatencyMs;

        /// <summary>
        /// Receive a QosResponse if one is available
        /// </summary>
        /// <param name="socketHandle">Native socket descriptor</param>
        /// <param name="expireTimeUtc">When to stop waiting for a response</param>
        /// <param name="endPoint">Remote endpoint from which to receive the response</param>
        /// <returns>
        /// (received, errorcode) where received is the number of bytes received and errorcode is the error code if any.
        /// 0 means no error.
        /// </returns>
        internal unsafe (int received, int errorCode) Recv(IntPtr socketHandle, bool wait, DateTime expireTimeUtc, ref NetworkEndPoint endPoint)
        {
#if UGS_QOS_SUPPORTED
            Binding.Baselib_Socket_Message message = new Binding.Baselib_Socket_Message();
            var buffer = new UnsafeAppendBuffer(2048, 16, Allocator.Persistent);
            var startTime = DateTime.UtcNow;
            fixed(Binding.Baselib_NetworkAddress* pEndpointAddress = &endPoint.rawNetworkAddress)
            {
                message.dataLen = (uint)buffer.Capacity;
                message.address = pEndpointAddress;
                message.data = new IntPtr(buffer.Ptr);

                var errorState = default(Binding.Baselib_ErrorState);
                var socket = new Binding.Baselib_Socket_Handle {handle = socketHandle};

                uint received = 0;
                var tries = 0;

                while (!QosHelper.ExpiredUtc(expireTimeUtc))
                {
                    errorState = default(Binding.Baselib_ErrorState);
                    tries++;

                    received = Binding.Baselib_Socket_UDP_Recv(socket, &message, 1, &errorState);
                    if (received == 0)
                    {
                        if (QosHelper.WouldBlock(errorState.nativeErrorCode))
                        {
                            if (!wait)
                            {
                                return (0, 0);
                            }

                            continue;
                        }
                    }
                    break; // Got a response, or a non-retryable error
                }

                if (received == 0)
                {
                    // Don't treat as an error as it's expected behaviour if we're seeing any level of packet loss.
                    buffer.Dispose();
                    return (0, (int)errorState.code);
                }

                endPoint.rawNetworkAddress = *message.address;
                m_PacketLength = (ushort)message.dataLen;
                Deserialize(message.data);

                m_LatencyMs = (Length >= MinPacketLen)
                    ? (int)((ulong)(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) - m_Timestamp)
                    : -1;
            }

            buffer.Dispose();
            return (Length, (int)Binding.Baselib_ErrorCode.Success);
#else
            throw new NotImplementedException();
#endif
        }

        internal void Deserialize(IntPtr msgData)
        {
            m_Magic = Marshal.ReadByte(msgData);
            m_VerAndFlow = Marshal.ReadByte(msgData, 1);
            m_Sequence = Marshal.ReadByte(msgData, 2);

            // m_Identifier = (ushort)Marshal.ReadInt16(msgData, 3);
            var id1 = (ushort)(Marshal.ReadByte(msgData, 3) << 0);
            var id2 = (ushort)(Marshal.ReadByte(msgData, 4) << 8);
            m_Identifier = (ushort)(id1 + id2);


            // m_Timestamp = (ulong)Marshal.ReadInt64(msgData, 5);
            var ts1 = ((ulong)Marshal.ReadByte(msgData,  5)) <<  0;
            var ts2 = ((ulong)Marshal.ReadByte(msgData,  6)) <<  8;
            var ts3 = ((ulong)Marshal.ReadByte(msgData,  7)) << 16;
            var ts4 = ((ulong)Marshal.ReadByte(msgData,  8)) << 24;
            var ts5 = ((ulong)Marshal.ReadByte(msgData,  9)) << 32;
            var ts6 = ((ulong)Marshal.ReadByte(msgData, 10)) << 40;
            var ts7 = ((ulong)Marshal.ReadByte(msgData, 11)) << 48;
            var ts8 = ((ulong)Marshal.ReadByte(msgData, 12)) << 56;
            m_Timestamp = ts1 + ts2 + ts3 + ts4 + ts5 + ts6 + ts7 + ts8;
        }

        /// <summary>
        /// Verifies the QosResponse contains valid required fields
        /// </summary>
        /// <param name="maxSequence">Maximum expected sequence value</param>
        /// <param name="error">Contains the description of the first validation failure if valiation failed</param>
        /// <returns>true if basic validation passes, false otherwise</returns>
        internal bool Verify(uint maxSequence, ref string error)
        {
            if (Length < MinPacketLen)
            {
                error = $"response is too small got {Length} bytes min expected {MinPacketLen} bytes";
                return false;
            }

            if (Magic != ResponseMagic)
            {
                error = $"response contains an invalid signature 0x{Magic:X} expected 0x{ResponseMagic:X}";
                return false;
            }

            if (Version != ResponseVersion)
            {
                error = $"response contains an invalid version {Version} expected {ResponseVersion}";
                return false;
            }

            if (Sequence > maxSequence)
            {
                error = $"response contains an invalid sequence {Sequence} max expected {maxSequence}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parses the FlowControl 4-bit value into the type and number of units (duration) that have been applied.
        /// </summary>
        /// <returns>
        /// (type, units) where type is the flow control type (FcType.None for no flow control), and units is the
        /// number of units of that type of flow control that the server has applied (and we should adhere to).
        /// </returns>
        internal (FcType type, byte units) ParseFlowControl()
        {
            if (FlowControl == 0)
            {
                return (FcType.None, 0);
            }

            FcType type = ((FlowControl & 0x8) != 0) ? FcType.Ban : FcType.Throttle;
            byte units = (byte)(FlowControl & 0x7);

            // Units are the lower 3 bits of the flow control nibble.  For throttles, the unit counts start at 1
            // (001b..111b). For bans, the unit count starts at 0 (000b..111b), so 0 is 1 unit, 1 is 2 units, etc.
            // So for bans, add 1 to get the number of units.
            if (type == FcType.Ban)
            {
                ++units;
            }

            return (type, units);
        }
    }
}
