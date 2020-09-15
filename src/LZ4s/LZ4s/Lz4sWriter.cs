using System;
using System.IO;

namespace LZ4s
{
    public class Lz4sWriter : IDisposable
    {
        private Stream _stream;

        // Buffer of *compressed* form of content, keeping enough context to find bytes to reuse (LZ4sConstants.MaximumCopyFromDistance)
        private byte[] _buffer;

        // First index in buffer which doesn't yet have any data
        private int _bufferEnd;

        public Lz4sWriter(Stream stream, byte[] buffer = null)
        {
            _stream = stream;
            _buffer = buffer ?? new byte[Lz4sConstants.BufferSize];

            Lz4sConstants.Preamble.CopyTo(_buffer, _bufferEnd);
            _bufferEnd += Lz4sConstants.Preamble.Length;
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
                Lz4sToken token = NextToken(array, nextTokenStart, end);
                WriteToken(array, nextTokenStart, token);
                nextTokenStart += token.LiteralLength + token.CopyLength;
            }
        }

        private Lz4sToken NextToken(byte[] array, int index, int end)
        {
            // Tokens have a limit of 'MaximumTokenLength'. After finding that many literal (non-copyable) bytes, stop and write a max-length, literals only token.
            // 'Stop' stops at the maximum token length. Since stop is passed as the array end to HasLongerMatch, it limits the literal+copy length to MaximumTokenLength.

            // Copying bytes isn't worth it unless there are at least 'MinimumCopyLength'. Require matches to be at least that long to track.
            // 'BestCopyLength' starts just under this minimum to ensure matches achieve it.

            int stop = Math.Min(index + Lz4sConstants.MaximumTokenLength, end);

            // Try to find bytes at 'current' to copy from earlier, within the length limits allowed
            int current;
            for (current = index; current < stop; ++current)
            {
                int bestCopyFrom = -1;
                int bestCopyLength = Lz4sConstants.MinimumCopyLength - 1;

                // Find the longest match with array[current] in already compressed content
                HasLongerMatch(array, current, stop, _buffer, Math.Max(0, _bufferEnd - Lz4sConstants.MaximumCopyFromDistance), _bufferEnd, ref bestCopyFrom, ref bestCopyLength);

                // If there's a longer match earlier within the current token...
                if (HasLongerMatch(array, current, stop, array, index, current, ref bestCopyFrom, ref bestCopyLength))
                {
                    // Write a literal-only token with the bytes we want to reference next
                    return new Lz4sToken(literalLength: bestCopyFrom + bestCopyLength, copyLength: 0, copyFromRelativeIndex: 0);
                }
                else if (bestCopyFrom > 0)
                {
                    // Write a token with the literal and copied bytes
                    return new Lz4sToken(literalLength: current - index, copyLength: bestCopyLength, copyFromRelativeIndex: _bufferEnd - bestCopyFrom);
                }
            }

            // If we hit the end of the array or had too many literal bytes, write a literal-only token
            return new Lz4sToken(literalLength: Math.Min(Lz4sConstants.MaximumTokenLength, end - index), copyLength: 0, copyFromRelativeIndex: 0);
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

        private void WriteToken(byte[] array, int index, Lz4sToken token)
        {
            // Flush buffer if too close to full
            if (_bufferEnd + 2 + token.LiteralLength + 2 > _buffer.Length)
            {
                Flush(false);
            }

            // Write literal length
            _buffer[_bufferEnd++] = token.LiteralLength;

            // Write copy length
            _buffer[_bufferEnd++] = token.CopyLength;

            // Write literal bytes
            if (token.LiteralLength > 0)
            {
                Buffer.BlockCopy(array, index, _buffer, _bufferEnd, token.LiteralLength);
                _bufferEnd += token.LiteralLength;
            }

            // Write copy relative position
            if (token.CopyLength > 0)
            {
                _buffer[_bufferEnd++] = (byte)token.CopyFromRelativeIndex;
                _buffer[_bufferEnd++] = (byte)(token.CopyFromRelativeIndex >> 8);
            }
        }

        private void Flush(bool everything)
        {
            int lengthToKeep = (everything ? 0 : Lz4sConstants.MaximumCopyFromDistance);
            int lengthToWrite = _bufferEnd - lengthToKeep;

            if (lengthToWrite > 0)
            {
                _stream.Write(_buffer, 0, lengthToWrite);
                _bufferEnd -= lengthToWrite;

                // Shift any remaining bytes to beginning of buffer
                if (lengthToKeep > 0)
                {
                    Buffer.BlockCopy(_buffer, lengthToWrite, _buffer, 0, lengthToKeep);
                }
            }
        }

        private void Close()
        {
            Flush(true);

            // Zero token to indicate end of content
            _buffer[_bufferEnd++] = (byte)0;
            _buffer[_bufferEnd++] = (byte)0;

            // TODO: Index entries

            // TODO: Uncompressed length

            Flush(true);
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

        private struct Lz4sToken
        {
            public byte LiteralLength;
            public byte CopyLength;
            public ushort CopyFromRelativeIndex;

            public Lz4sToken(int literalLength, int copyLength, int copyFromRelativeIndex)
            {
                this.LiteralLength = (byte)literalLength;
                this.CopyLength = (byte)copyLength;
                this.CopyFromRelativeIndex = (ushort)copyFromRelativeIndex;
            }
        }
    }
}
