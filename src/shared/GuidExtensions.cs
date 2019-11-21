using System;

namespace Elastic.Installer
{
    // Guid generation on windows puts words in little-endian format.
    // Idealy we want to generate cross-platform v5 UUIDs
    public static class GuidExtensions
    {
        public static byte[] ToNetworkBytes(this Guid uid)
        {
            var bytes = uid.ToByteArray();

            if (BitConverter.IsLittleEndian)
                ReverseGuidEndianness(ref bytes);

            return bytes;
        }

        public static Guid FromHostBytes(this Guid _, byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
                ReverseGuidEndianness(ref bytes);

            return new Guid(bytes);
        }

        static void ReverseGuidEndianness(ref byte[] bytes)
        {
            // dword
            Swap(ref bytes[0], ref bytes[3]);
            Swap(ref bytes[1], ref bytes[2]);

            // word
            Swap(ref bytes[4], ref bytes[5]);

            // word
            Swap(ref bytes[6], ref bytes[7]);
        }

        static void Swap(ref byte b1, ref byte b2)
        {
            byte temp = b1;
            b1 = b2;
            b2 = temp;
        }
    }
}
