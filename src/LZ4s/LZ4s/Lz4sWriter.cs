using System;
using System.IO;

namespace LZ4s
{
    public class Lz4sWriter : IDisposable
    {
        private Stream _stream;
        private Lz4sBuffer _compressedBuffer;

        public Lz4sWriter(Stream stream, byte[] buffer = null)
        {
            _stream = stream;
            _compressedBuffer = new Lz4sBuffer(buffer);
            _compressedBuffer.Append(Constants.Preamble, 0, Constants.Preamble.Length);
        }

        public void Write(byte[] array, int index, int length)
        {
            int end = index + length;
            if (array == null) { throw new ArgumentNullException(nameof(array)); }
            if (index < 0 || index >= array.Length) { throw new ArgumentOutOfRangeException(nameof(index)); }
            if (length < 0 || end > array.Length) { throw new ArgumentOutOfRangeException(nameof(length)); }

            int nextTokenStart = index;
            while (nextTokenStart < end)
            {
                Token token = NextToken(array, nextTokenStart, end);
                WriteToken(array, nextTokenStart, token);
                nextTokenStart += token.LiteralLength + token.CopyLength;
            }
        }

        private Token NextToken(byte[] array, int index, int end)
        {
            // Tokens have a limit of 'MaximumTokenLength'. After finding that many literal (non-copyable) bytes, stop and write a max-length, literals only token.
            // 'Stop' stops at the maximum token length. Since stop is passed as the array end to HasLongerMatch, it limits the literal+copy length to MaximumTokenLength.

            // Copying bytes isn't worth it unless there are at least 'MinimumCopyLength'. Require matches to be at least that long to track.
            // 'BestCopyLength' starts just under this minimum to ensure matches achieve it.

            int stop = Math.Min(index + Constants.MaximumTokenLength, end);

            // Try to find bytes at 'current' to copy from earlier, within the length limits allowed
            int current;
            for (current = index; current < stop; ++current)
            {
                int bestCopyFrom = -1;
                int bestCopyLength = Constants.MinimumCopyLength - 1;

                // Find the longest match with array[current] in already compressed content
                HasLongerMatch(array, current, stop, _compressedBuffer.Array, Math.Max(0, _compressedBuffer.End - Constants.MaximumCopyFromDistance), _compressedBuffer.End, ref bestCopyFrom, ref bestCopyLength);

                // If there's a longer match earlier within the current token...
                if (HasLongerMatch(array, current, stop, array, index, current, ref bestCopyFrom, ref bestCopyLength))
                {
                    // Write a literal-only token with the bytes we want to reference next
                    return new Token(literalLength: bestCopyFrom + bestCopyLength, copyLength: 0, copyFromRelativeIndex: 0);
                }
                else if (bestCopyFrom > 0)
                {
                    // Write a token with the literal and copied bytes
                    return new Token(literalLength: current - index, copyLength: bestCopyLength, copyFromRelativeIndex: _compressedBuffer.End - bestCopyFrom);
                }
            }

            // If we hit the end of the array or had too many literal bytes, write a literal-only token
            return new Token(literalLength: Math.Min(Constants.MaximumTokenLength, end - index), copyLength: 0, copyFromRelativeIndex: 0);
        }

        private bool HasLongerMatch(byte[] toMatch, int toMatchIndex, int toMatchEnd, byte[] withinArray, int withinIndex, int withinEnd, ref int longestMatchIndex, ref int longestMatchLength)
        {
            bool foundLonger = false;

            // Search 'withinArray' for the longest match to 'toMatch[toMatchIndex]'
            for (int current = withinIndex; current < withinEnd - longestMatchLength; ++current)
            {
                // How many bytes match starting at toMatch[toMatchIndex] and withinArray[current]?
                int matchLength = MatchLength(withinArray, current, toMatch, toMatchIndex, Math.Min(withinEnd - current, toMatchEnd - toMatchIndex));

                if (matchLength > longestMatchLength)
                {
                    longestMatchIndex = current;
                    longestMatchLength = matchLength;
                    foundLonger = true;
                }
            }

            return foundLonger;
        }

        private int MatchLength(byte[] left, int leftIndex, byte[] right, int rightIndex, int lengthLimit)
        {
            for (int length = 0; length < lengthLimit; ++length)
            {
                if (left[leftIndex + length] != right[rightIndex + length]) { return length; }
            }

            return lengthLimit;
        }

        private void WriteToken(byte[] array, int index, Token token)
        {
            // Flush buffer if too full for token
            if (token.CompressedLength > _compressedBuffer.RemainingSpace)
            {
                _compressedBuffer.Write(_stream, Constants.MaximumCopyFromDistance);
                _compressedBuffer.Shift(Constants.MaximumCopyFromDistance);
            }

            _compressedBuffer.Append(token.LiteralLength);
            _compressedBuffer.Append(token.CopyLength);

            // Write literal bytes
            if (token.LiteralLength > 0)
            {
                _compressedBuffer.Append(array, index, token.LiteralLength);
            }

            // Write copy relative position
            if (token.CopyLength > 0)
            {
                _compressedBuffer.Append((byte)token.CopyFromRelativeIndex);
                _compressedBuffer.Append((byte)(token.CopyFromRelativeIndex >> 8));
            }
        }

        private void Close()
        {
            // Zero token to indicate end of content
            WriteToken(null, 0, new Token(0, 0, 0));

            // TODO: Index entries

            // TODO: Uncompressed length

            // Write everything
            _compressedBuffer.Write(_stream);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            Close();
            _stream?.Dispose();
            _stream = null;
        }
    }
}
