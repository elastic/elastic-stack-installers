using System;
using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace Elastic.Installer
{
    public class Uuid5
    {
        // UUID v5 root namespace
        static readonly Lazy<byte[]> lazyRootNamespaceBytes = new Lazy<byte[]>(
            () => Guid.ParseExact("C10B7A26-0B46-11EA-A386-00155D03F864", "D")
                      .ToNetworkBytes());

        public static Guid FromString(string source)
        {
            using var sha1 = SHA1.Create();

            var poolBytes = ArrayPool<byte>.Shared.Rent(
                lazyRootNamespaceBytes.Value.Length + source.Length);

            try
            {
                Buffer.BlockCopy(
                    src: lazyRootNamespaceBytes.Value,
                    srcOffset: 0,
                    dst: poolBytes,
                    dstOffset: 0,
                    count: lazyRootNamespaceBytes.Value.Length);

                int sourceBytesLength = Encoding.ASCII.GetBytes(
                    s: source,
                    charIndex: 0,
                    charCount: source.Length,
                    bytes: poolBytes,
                    byteIndex: lazyRootNamespaceBytes.Value.Length);

                var hash = sha1
                    .ComputeHash(
                        poolBytes, 0,
                        lazyRootNamespaceBytes.Value.Length + sourceBytesLength)
                    .AsSpan(0, 16);

                // RFC 4122
                hash[6] = (byte) ((hash[6] & 0x0F) | 0x50);
                hash[8] = (byte) ((hash[8] & 0x3F) | 0x80);

                // .Empty is just a dummy for extension method
                return Guid.Empty.FromHostBytes(hash.ToArray());
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(poolBytes);
            }
        }
    }
}
