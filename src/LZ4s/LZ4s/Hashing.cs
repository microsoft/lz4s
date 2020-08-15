namespace LZ4s
{
    internal static class Hashing
    {
        /// <summary>
        ///  Murmur3 32-bit Final Mix
        /// </summary>
        /// <remarks>
        ///   https://github.com/aappleby/smhasher/blob/master/src/MurmurHash3.cpp
        ///   "MurmurHash3 was written by Austin Appleby, and is placed in the public
        ///   domain. The author hereby disclaims copyright to this source code."
        /// </remarks>
        /// <param name="h">32-bits to mix</param>
        /// <returns>32-bit result</returns>
        public static uint Murmur3_Mix(uint h)
        {
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;

            return h;
        }
    }
}
