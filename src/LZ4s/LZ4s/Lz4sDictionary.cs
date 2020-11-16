using System;
using System.Diagnostics;

namespace LZ4s
{
    // Questions:
    //  - How long to hash and check each byte? [~200 MB/s]
    //  - How many false positives by bucket? Does keeping another hash byte sufficiently resolve?
    //  - How expensive to confirm bytes match at referenced position in array?

    public struct Match
    {
        public long Position;
        public int Length;
    }

    public class MatchTable
    {
        private MatchTableSlice Previous;
        private MatchTableSlice Current;
        private long NextSwap;
        public static long CheckTotal;

        public MatchTable()
        {
            Previous = new MatchTableSlice(2 * Lz4Constants.MaximumCopyFromDistance);
            Current = new MatchTableSlice(2 * Lz4Constants.MaximumCopyFromDistance);
            NextSwap = ushort.MaxValue;
        }

        public void Add(FileBuffer buffer, int index, uint key)
        {
            long position = buffer.ArrayStartPosition + index;
            if (position >= NextSwap) { Swap(); }

            uint hash = Hashing.Murmur3_Mix(key);

            Current.Add(position, buffer, index, hash);
        }

        public Match FindLongestMatch(FileBuffer buffer, int index, uint key)
        {
            long position = buffer.ArrayStartPosition + index;
            if (position >= NextSwap) { Swap(); }

            uint hash = Hashing.Murmur3_Mix(key);

            Match best = new Match();
            Previous.FindLongestMatch(position, buffer, index, hash, ref best, add: false);
            Current.FindLongestMatch(position, buffer, index, hash, ref best, add: true);

            return best;
        }

        private void Swap()
        {
            MatchTableSlice newCurrent = Previous;

            Previous = Current;
            Current = newCurrent;

            Current.Clear(NextSwap);
            NextSwap += ushort.MaxValue;
        }

        public void Clear()
        {
            Previous.Clear(0);
            Current.Clear(0);
            NextSwap = ushort.MaxValue;
        }

        private struct MatchTableSlice
        {
            private long Start;
            private ushort[] Positions;

            public MatchTableSlice(int length)
            {
                Start = -1;
                Positions = new ushort[length];
            }

            public void Add(long position, FileBuffer buffer, int index, uint hash)
            {
                uint checkCount = 0;
                uint bucket = hash % (uint)Positions.Length;

                // First byte in array not in key/hash
                int nextByteIndex = index + 4;

                while (Positions[bucket] != 0)
                {
                    checkCount++;

                    if (nextByteIndex < buffer.End)
                    {
                        hash = Hashing.Murmur3_Mix(1 + hash + buffer.Array[nextByteIndex++]);
                    }
                    else
                    {
                        hash = Hashing.Murmur3_Mix(1 + hash);
                    }

                    bucket = hash % (uint)Positions.Length;

                    //bucket++;
                    //if (bucket == Positions.Length) { bucket = 0; }
                }

                MatchTable.CheckTotal += checkCount;
                Positions[bucket] = (ushort)(position - Start);
            }

            public void FindLongestMatch(long position, FileBuffer buffer, int index, uint hash, ref Match match, bool add)
            {
                uint checkCount = 0;
                uint bucket = hash % (uint)Positions.Length;

                // First byte in array not in key/hash
                int nextByteIndex = index + 4;

                int maxMatchLength = Math.Min(Lz4Constants.MaximumLiteralOrCopyLength, buffer.End - index);

                while (Positions[bucket] != 0)
                {
                    checkCount++;

                    long matchPosition = (Start + Positions[bucket]);
                    long relativePosition = position - matchPosition;

                    if (relativePosition < Lz4Constants.MaximumCopyFromDistance && match.Length < maxMatchLength)
                    {
                        int matchIndex = (int)(index - relativePosition);
                        int matchLength = Helpers.MatchLength(buffer.Array, index, buffer.Array, matchIndex, Math.Min(maxMatchLength, index - matchIndex));

                        if (matchLength > match.Length)
                        {
                            match.Position = matchPosition;
                            match.Length = matchLength;
                        }
                    }

                    if (nextByteIndex < buffer.End)
                    {
                        hash = Hashing.Murmur3_Mix(1 + hash + buffer.Array[nextByteIndex++]);
                    }
                    else
                    {
                        hash = Hashing.Murmur3_Mix(1 + hash);
                    }

                    bucket = hash % (uint)Positions.Length;

                    //bucket++;
                    //if (bucket == Positions.Length) { bucket = 0; }
                }

                MatchTable.CheckTotal += checkCount;

                if (add)
                {
                    Positions[bucket] = (ushort)(position - Start);
                }
            }

            public void Clear(long newStartPosition)
            {
                Array.Clear(Positions, 0, Positions.Length);
                Start = newStartPosition - 1;
            }
        }
    }
}
