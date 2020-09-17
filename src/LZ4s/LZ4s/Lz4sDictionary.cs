using System;
using System.Collections.Generic;

namespace LZ4s
{
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
