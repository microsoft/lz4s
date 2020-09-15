namespace LZ4s
{
    internal struct Lz4sToken
    {
        public byte LiteralLength;
        public byte CopyLength;
        public ushort CopyFromRelativeIndex;

        public int CompressedLength => 2 + LiteralLength + (CopyLength > 0 ? 2 : 0);
        public int DecompressedLength => LiteralLength + CopyLength;

        public Lz4sToken(int literalLength, int copyLength, int copyFromRelativeIndex)
        {
            this.LiteralLength = (byte)literalLength;
            this.CopyLength = (byte)copyLength;
            this.CopyFromRelativeIndex = (ushort)copyFromRelativeIndex;
        }
    }
}
