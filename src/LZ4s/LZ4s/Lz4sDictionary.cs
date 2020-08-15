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
        private long _newStart;
        private ushort[] _newPositions;
        private long _oldStart;
        private ushort[] _oldPositions;

        public Lz4sDictionary()
        {
            int size = Lz4sConstants.MaximumCopyFromLength * 2;
            _oldPositions = new ushort[size];
            _newPositions = new ushort[size];
        }

        public void Add(uint key, long position)
        {
            uint bucket = Hashing.Murmur3_Mix(key) & (Lz4sConstants.MaximumCopyFromLength * 2 - 1);

            // On each boundary, swap index parts and clear the current index
            ushort relativePosition = (ushort)(position - _newStart);
            if (relativePosition >= Lz4sConstants.MaximumCopyFromLength)
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
                bucket = (bucket + 1) & (Lz4sConstants.MaximumCopyFromLength * 2 - 1);
            }

            _newPositions[bucket] = (ushort)(relativePosition + 1);
        }

        public void Matches(uint key, List<long> result)
        {
            result.Clear();
            uint bucket = Hashing.Murmur3_Mix(key) & (Lz4sConstants.MaximumCopyFromLength * 2 - 1);

            uint oldBucket = bucket;
            while (_oldPositions[oldBucket] != 0)
            {
                result.Add(_oldStart + _oldPositions[oldBucket] - 1);
                oldBucket = (oldBucket + 1) & (Lz4sConstants.MaximumCopyFromLength * 2 - 1);
            }

            while (_newPositions[bucket] != 0)
            {
                result.Add(_newStart + _newPositions[bucket] - 1);
                bucket = (bucket + 1) & (Lz4sConstants.MaximumCopyFromLength * 2 - 1);
            }
        }
    }
}
