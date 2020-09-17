namespace LZ4s
{
    internal static class Helpers
    {
        public static void ArrayCopy(byte[] source, int sourceIndex, byte[] target, int targetIndex, int length)
        {
            // Use ArrayCopy for smaller copies (< 64b). Buffer.BlockCopy is faster for large copies.

            //Buffer.BlockCopy(source, sourceIndex, target, targetIndex, length); // 4.0s
            //Array.Copy(source, sourceIndex, target, targetIndex, length);       // 4.6s

            // 3.2s
            for (int i = 0; i < length; ++i)
            {
                target[targetIndex + i] = source[sourceIndex + i];
            }
        }
    }
}
