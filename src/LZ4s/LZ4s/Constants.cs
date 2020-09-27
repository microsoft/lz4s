namespace LZ4s
{
    public static class Lz4Constants
    {
        public const int MinimumCopyLength = 5;
        public const int MaximumLiteralOrCopyLength = 255;
        public const int MaximumTokenUncompressedLength = 2 * MaximumLiteralOrCopyLength;
        public const int MaximumCopyFromDistance = 65536;

        // Buffer lengths: Must be larger than MaxCopyDistance; larger means more bytes decoded per shift
        public const int BufferSize = 4 * MaximumCopyFromDistance;
    }

    public static class Constants
    {
        public static readonly byte[] Preamble = { (byte)'L', (byte)'Z', (byte)'4', (byte)'S', 0xFF };

        public const int MinimumCopyLength = 5;
        public const int MaximumTokenLength = 255;
        public const int MaximumCopyFromDistance = 8192;

        // Buffer lengths: Must be larger than MaxCopyDistance; larger means more bytes decoded per shift
        public const int BufferSize = 4 * MaximumCopyFromDistance;
    }
}
