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

        public static uint Murmur1_Mix(uint h)
        {
            h *= 0xc6a4a793;
            h ^= h >> 10;
            h *= 0xc6a4a793;
            h ^= h >> 17;

            return h;
        }

        public static uint Murmur2_Mix(uint h)
        {
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;

            return h;
        }

        // More options from:
        // https://nullprogram.com/blog/2018/07/31/; https://github.com/skeeto/hash-prospector
        // "All information on this blog, unless otherwise noted, is hereby released into the public domain, with no rights reserved."
        public static uint LowBias32(uint x)
        {
            x ^= x >> 16;
            x *= 0x7feb352d;
            x ^= x >> 15;
            x *= 0x846ca68b;
            x ^= x >> 16;
            return x;
        }

        public static uint Triple32(uint x)
        {
            x ^= x >> 17;
            x *= 0xed5ad4bb;
            x ^= x >> 11;
            x *= 0xac4c1b51;
            x ^= x >> 15;
            x *= 0x31848bab;
            x ^= x >> 14;
            return x;
        }
    }
}
