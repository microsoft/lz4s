using System.Text;

namespace LZ4s
{
    internal static class Lz4sConstants
    {
        public static readonly byte[] Preamble = Encoding.UTF8.GetBytes("LZ4S");
        public const byte Separator = 0xFF;

        public const int MinimumCopyLength = 4;
        public const int MaximumTokenLength = 255;
        public const int MaximumCopyFromDistance = 8192;
    }
}
