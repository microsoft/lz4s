using System;
using System.Collections.Generic;

namespace LZ4s
{
    // Questions:
    //  - How long to hash and check each byte? [~200 MB/s]
    //  - How many false positives by bucket? Does keeping another hash byte sufficiently resolve?
    //  - How expensive to confirm bytes match at referenced position in array?

    public class Lz4sDictionary2
    {
        private const int Size = 2 * Constants.MaximumCopyFromDistance;

        private long _newStart;
        private uint[] _newKeys;
        private ushort[] _newPositions;

        private long _oldStart;
        private uint[] _oldKeys;
        private ushort[] _oldPositions;

        public Lz4sDictionary2()
        {
            _oldPositions = new ushort[Size];
            _oldKeys = new uint[Size];

            _newPositions = new ushort[Size];
            _newKeys = new uint[Size];
        }

        public int Scan(byte[] array, int index, int end, long arrayStartFilePosition)
        {
            int matchCount = 0;

            // Hash and Check Dictionary only while 4+ bytes are left in array span
            int arrayCheckEnd = end - 3;

            int i = index;
            while (i < arrayCheckEnd)
            {
                uint key = (uint)((array[i] << 16) + (array[i + 1] << 8) + (array[i + 2]));

                int arrayNewRelativePosition = (int)(arrayStartFilePosition - _newStart);
                int dictSwapEnd = Constants.MaximumCopyFromDistance - arrayNewRelativePosition;
                int innerEnd = Math.Min(dictSwapEnd, arrayCheckEnd);

                while (i < innerEnd)
                {
                    // Shift and add new byte to key
                    key = (uint)((key << 8) + array[i + 3]);

                    // Hash value
                    uint hash = Hashing.Murmur3_Mix(key);

                    // Find bucket
                    uint bucket = hash & (Size - 1);

                    // Look for match
                    bool found = false;

                    while (_newPositions[bucket] != 0)
                    {
                        if (_newKeys[bucket] == key)
                        {
                            long matchAbsolutePosition = _newStart + _newPositions[bucket];
                            int matchIndex = (int)(matchAbsolutePosition - arrayNewRelativePosition);

                            // Match found
                            matchCount++;
                            found = true;
                            break;
                        }

                        bucket = (bucket + 1) & (Size - 1);
                    }

                    // Add this position and key (over match or in first opening)
                    _newKeys[bucket] = key;
                    _newPositions[bucket] = (ushort)(arrayNewRelativePosition + i);

                    if (!found)
                    {
                        // If no match in new bucket, check old bucket
                        while (_oldPositions[bucket] != 0)
                        {
                            if (_oldKeys[bucket] == key)
                            {
                                matchCount++;
                                found = true;
                                break;
                            }

                            bucket = (bucket + 1) & (Size - 1);
                        }
                    }

                    i++;
                }

                // Swap Dictionaries if we stopped due to Dictionary boundary
                if (i < arrayCheckEnd)
                {
                    SwapDictionaries(arrayStartFilePosition + i);
                }
            }

            return matchCount;
        }

        private void SwapDictionaries(long newStartPosition)
        {
            ushort[] donePositions = _oldPositions;
            uint[] doneKeys = _oldKeys;

            // Move '_new' data to '_old'
            _oldStart = _newStart;
            _oldPositions = _newPositions;
            _oldKeys = _newKeys;

            // Reuse previous '_old' arrays for new Dictionary
            _newStart = newStartPosition;
            _newPositions = donePositions;
            _newKeys = doneKeys;

            // Clear Dictionary (positions only are sufficient)
            Array.Clear(donePositions, 0, Size);
        }

        public void Clear()
        {
            _newStart = 0;
            _oldStart = 0;

            Array.Clear(_oldPositions, 0, Size);
            Array.Clear(_newPositions, 0, Size);
        }
    }

    /// <summary>
    ///  Lz4sDictionary tracks each position in the last 'MaximumCopyFromLength' in which
    ///  four bytes in order are found. It is used to quickly find bytes which can be reused
    ///  to compress the current data from the compressed output so far.
    /// </summary>
    public class Lz4sDictionary
    {
        private const int Size = 2 * Constants.MaximumCopyFromDistance;

        private long _newStart;
        private ushort[] _newPositions;
        private long _oldStart;
        private ushort[] _oldPositions;

        public Lz4sDictionary()
        {
            _oldPositions = new ushort[Size];
            _newPositions = new ushort[Size];
        }

        public void Add(byte[] array, int index, long position)
        {
            uint key = (uint)((array[index] << 24) + (array[index + 1] << 16) + (array[index + 2] << 8) + array[index + 3]);
            uint bucket = Hashing.Murmur3_Mix(key) & (Size - 1);

            // On each boundary, swap index parts and clear the current index
            ushort relativePosition = (ushort)(position - _newStart);
            if (relativePosition >= Constants.MaximumCopyFromDistance)
            {
                ushort[] old = _oldPositions;
                _oldPositions = _newPositions;
                _oldStart = _newStart;

                _newPositions = old;
                _newStart = position;
                relativePosition = 0;
                Array.Clear(_newPositions, 0, _newPositions.Length);
            }

            while (_newPositions[bucket] != 0)
            {
                bucket = (bucket + 1) & (Size - 1);
            }

            _newPositions[bucket] = (ushort)(relativePosition + 1);
        }

        public void Matches(byte[] array, int index, List<long> result)
        {
            result.Clear();
            uint key = (uint)((array[index] << 24) + (array[index + 1] << 16) + (array[index + 2] << 8) + array[index + 3]);
            uint bucket = Hashing.Murmur3_Mix(key) & (Size - 1);

            uint oldBucket = bucket;
            while (_oldPositions[oldBucket] != 0)
            {
                result.Add(_oldStart + _oldPositions[oldBucket] - 1);
                oldBucket = (oldBucket + 1) & (Size - 1);
            }

            while (_newPositions[bucket] != 0)
            {
                result.Add(_newStart + _newPositions[bucket] - 1);
                bucket = (bucket + 1) & (Size - 1);
            }
        }
    }
}
