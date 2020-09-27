using System;

namespace LZ4s
{
    // Questions:
    //  - How long to hash and check each byte? [~200 MB/s]
    //  - How many false positives by bucket? Does keeping another hash byte sufficiently resolve?
    //  - How expensive to confirm bytes match at referenced position in array?

    public struct Match
    {
        public int Index;
        public ushort Length;
    }

    internal class DictionarySlice
    {
        public const int Size = 2 * Lz4Constants.MaximumCopyFromDistance;

        private long Start;
        private ushort[] Positions;

        public long SwapPosition => Start + Lz4Constants.MaximumCopyFromDistance;

        public DictionarySlice()
        {
            Positions = new ushort[Size];
            Start = -1;
        }

        public void Add(long position, uint bucket)
        {
            while (Positions[bucket] != 0)
            {
                bucket = (bucket + 1) & (Size - 1);
            }

            Positions[bucket] = (ushort)(position - Start);
        }

        public void FindLongestMatch(long position, Lz4sBuffer buffer, int index, uint bucket, ref Match match, bool add)
        {
            while (Positions[bucket] != 0)
            {
                long matchPosition = (Start + Positions[bucket]);
                long relativePosition = position - matchPosition;

                int matchIndex = (int)(index - relativePosition);
                int matchLength = Helpers.MatchLength(buffer.Array, index, buffer.Array, matchIndex, Math.Min(Math.Min(Lz4Constants.MaximumLiteralOrCopyLength, buffer.End - index), index - matchIndex));

                if (matchLength > match.Length)
                {
                    match.Index = matchIndex;
                    match.Length = (ushort)matchLength;
                }

                bucket = (bucket + 1) & (Size - 1);
            }

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

    public class Lz4sDictionary
    {
        private DictionarySlice _previous;
        private DictionarySlice _current;
        private long _nextSwap;

        public Lz4sDictionary()
        {
            _previous = new DictionarySlice();
            _current = new DictionarySlice();
            _nextSwap = _current.SwapPosition;
        }

        public Token NextMatch(Lz4sBuffer buffer, long bufferFilePosition)
        {
            // TEMP: Return literals only
            //return new Token(Math.Min(255, buffer.Length), 0, 0);

            // Hash and Check Dictionary only while 4+ bytes are left in array span
            int arrayCheckEnd = Math.Min(buffer.End - 3, buffer.Index + Lz4Constants.MaximumLiteralOrCopyLength);

            // Look for bytes to copy until first of:
            //  - Dictionary swap needed
            //  - Input buffer within MaxTokenLength of capacity (out-of-bounds risk)
            //  - Input buffer out of input (end of input)

            byte[] array = buffer.Array;
            int tokenStart = buffer.Index;
            int i = tokenStart;

            while (i < arrayCheckEnd)
            {
                uint key = (uint)((array[i] << 16) + (array[i + 1] << 8) + (array[i + 2]));

                int swapDictionaryIndex = (int)(_nextSwap - bufferFilePosition);
                int innerEnd = Math.Min(arrayCheckEnd, swapDictionaryIndex);

                while (i < innerEnd)
                {
                    // Shift and add new byte to key
                    key = (uint)((key << 8) + array[i + 3]);

                    // Determine dictionary bucket for these bytes
                    uint bucket = Hashing.Murmur3_Mix(key) & (DictionarySlice.Size - 1);

                    // Find the longest set of bytes we can copy matching array[i]
                    Match best = new Match();
                    _previous.FindLongestMatch(bufferFilePosition + 1, buffer, i, bucket, ref best, add: false);
                    _current.FindLongestMatch(bufferFilePosition + i, buffer, i, bucket, ref best, add: true);

                    if (best.Length >= Lz4Constants.MinimumCopyLength)
                    {
                        // Add array[i + 1] .. array[i + (length-1)] to the Dictionary
                        int j = i + 1;
                        int addToDictionaryEnd = Math.Min(arrayCheckEnd, i + best.Length);

                        while (j < addToDictionaryEnd)
                        {
                            int stop = Math.Min(addToDictionaryEnd, swapDictionaryIndex);
                            while (j < stop)
                            {
                                key = (uint)((key << 8) + array[j + 3]);
                                bucket = Hashing.Murmur3_Mix(key) & (DictionarySlice.Size - 1);
                                _current.Add(bufferFilePosition + j, bucket);
                                j++;
                            }

                            if (j >= swapDictionaryIndex)
                            {
                                SwapDictionaries(bufferFilePosition + j);
                                swapDictionaryIndex = (int)(_nextSwap - bufferFilePosition);
                            }
                        }

                        return new Token(i - tokenStart, best.Length, i - best.Index);
                    }

                    i++;
                }

                if (i >= swapDictionaryIndex)
                {
                    // If stopped at dictionary boundary, swap dictionaries
                    SwapDictionaries(bufferFilePosition + i);
                    swapDictionaryIndex = (int)(_nextSwap - bufferFilePosition);
                }
                else if (i < buffer.End - 3)
                {
                    // If stopped at maximum literal length, write max length literal
                    return new Token(i - tokenStart, 0, 0);
                }
            }

            // If stopped at end of source buffer, indicate via empty token
            return new Token();
        }

        private void SwapDictionaries(long newStartPosition)
        {
            DictionarySlice swap = _previous;
            _previous = _current;
            _current = swap;

            _current.Clear(newStartPosition);
            _nextSwap = _current.SwapPosition;
        }

        public void Clear()
        {
            _previous.Clear(0);
            _current.Clear(0);
            _nextSwap = _current.SwapPosition;
        }
    }
}
