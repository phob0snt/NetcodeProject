using System;
using System.Runtime.InteropServices;
#if UGS_QOS_SUPPORTED
using System.Text;
using Unity.Baselib.LowLevel;
using ErrorState = Unity.Baselib.LowLevel.Binding.Baselib_ErrorState;
#endif

namespace Unity.Networking.QoS
{
    /// <summary>
    ///     NetworkFamily indicates what type of underlying medium we are using.
    /// </summary>
    enum NetworkFamily
    {
        Invalid = 0,
        Ipv4 = 2,
        Ipv6 = 23
    }

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct NetworkEndPoint
    {
#if UGS_QOS_SUPPORTED
        internal Binding.Baselib_NetworkAddress rawNetworkAddress;

        ushort Port => (ushort) (rawNetworkAddress.port1 | (rawNetworkAddress.port0 << 8));

        NetworkFamily Family => FromBaselibFamily((Binding.Baselib_NetworkAddress_Family) rawNetworkAddress.family);

        internal string Address => AddressAsString();

        bool IsValid => Family != NetworkFamily.Invalid;

        internal static bool TryParse(string address, ushort port, out NetworkEndPoint endpoint,
            NetworkFamily family = NetworkFamily.Ipv4)
        {
            endpoint = default;
            const char nullTerminator = '\0';
            var errorState = default(ErrorState);
            var ipBytes = Encoding.UTF8.GetBytes(address + nullTerminator);
            fixed (byte* ipBytesPtr = ipBytes)
            fixed (Binding.Baselib_NetworkAddress* rawAddress = &endpoint.rawNetworkAddress)
            {
                Binding.Baselib_NetworkAddress_Encode(
                    rawAddress,
                    ToBaselibFamily(family),
                    ipBytesPtr,
                    port,
                    &errorState);
            }

            return errorState.code == Binding.Baselib_ErrorCode.Success && endpoint.IsValid;
        }

        string AddressAsString()
        {
            switch (Family)
            {
                case NetworkFamily.Ipv4:
                    return
                        $"{rawNetworkAddress.data0}.{rawNetworkAddress.data1}.{rawNetworkAddress.data2}.{rawNetworkAddress.data3}:{Port}";
                case NetworkFamily.Ipv6:
                    const string numberFormat = "[{0:x}:{1:x}:{2:x}:{3:x}:{4:x}:{5:x}:{6:x}:{7:x}]:{8}";
// TODO(steve): Include scope and handle leading zeros
// TODO(steve): Update to use ipv6_0 ... 15 when its available.
                    return string.Format(numberFormat,
                        rawNetworkAddress.data1 | (rawNetworkAddress.data0 << 8),
                        rawNetworkAddress.data3 | (rawNetworkAddress.data2 << 8),
                        rawNetworkAddress.data5 | (rawNetworkAddress.data4 << 8),
                        rawNetworkAddress.data7 | (rawNetworkAddress.data6 << 8),
                        rawNetworkAddress.data9 | (rawNetworkAddress.data8 << 8),
                        rawNetworkAddress.data11 | (rawNetworkAddress.data10 << 8),
                        rawNetworkAddress.data13 | (rawNetworkAddress.data12 << 8),
                        rawNetworkAddress.data15 | (rawNetworkAddress.data14 << 8),
                        Port
                    );
                default:
// TODO(steve): Throw an exception?
                    return string.Empty;
            }
        }

        static NetworkFamily FromBaselibFamily(Binding.Baselib_NetworkAddress_Family family)
        {
            return family switch
            {
                Binding.Baselib_NetworkAddress_Family.IPv4 => NetworkFamily.Ipv4,
                Binding.Baselib_NetworkAddress_Family.IPv6 => NetworkFamily.Ipv6,
                _ => NetworkFamily.Invalid
            };
        }

        static Binding.Baselib_NetworkAddress_Family ToBaselibFamily(NetworkFamily family)
        {
            return family switch
            {
                NetworkFamily.Ipv4 => Binding.Baselib_NetworkAddress_Family.IPv4,
                NetworkFamily.Ipv6 => Binding.Baselib_NetworkAddress_Family.IPv6,
                _ => Binding.Baselib_NetworkAddress_Family.Invalid
            };
        }
#else
        internal string Address => throw new NotImplementedException();
#endif
    }
}
