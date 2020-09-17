namespace LZ4s
{
    internal static class Constants
    {
        public static readonly byte[] Preamble = { (byte)'L', (byte)'Z', (byte)'4', (byte)'S', 0xFF };

        public const int MinimumCopyLength = 4;
        public const int MaximumTokenLength = 255;
        public const int MaximumCopyFromDistance = 8192;

        // Buffer lengths: Must be larger than MaxCopyDistance; larger means more bytes decoded per shift
        public const int BufferSize = 4 * MaximumCopyFromDistance;
    }
}
