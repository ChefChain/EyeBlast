using System;
using Unity.Collections;

namespace Unity.PolySpatial.Networking
{
    internal struct ConnectionID
    {
        public FixedString128Bytes Address;
    };

    internal struct SocketAddress : IEquatable<SocketAddress>
    {
        public string Host;
        public int Port;
        public string Identifier => $"{Host}:{Port}";

        public bool Equals(SocketAddress other)
        {
            return string.Equals(Host, other.Host) && Port == other.Port;
        }

        public override bool Equals(object obj)
        {
            return obj is SocketAddress other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Host, Port);
        }

        public static bool ParseAddress(string address, int defaultPort, out SocketAddress socketAddress)
        {
            socketAddress = default;

            var parts = address.Split(':');
            if (parts.Length == 0)
                return false;

            var host = parts[0].ToLower().Trim();
            if (string.IsNullOrWhiteSpace(host))
                return false;

            var port = defaultPort;
            if (parts.Length > 1 && int.TryParse(parts[1], out var parsedPort))
                port = parsedPort;

            socketAddress = new SocketAddress
            {
                Host = host,
                Port = port
            };

            return true;
        }
    }
}
