using System.Text;

namespace LZ4s
{
    internal static class Lz4sConstants
    {
        public static readonly byte[] Preamble = Encoding.UTF8.GetBytes("LZ4S1");
        public const byte Separator = 0xFF;

        public const int MaximumTokenLength = 255;
        public const int MaximumCopyFromLength = 8192;
    }
}
