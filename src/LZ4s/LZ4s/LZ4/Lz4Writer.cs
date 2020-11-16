using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace LZ4s
{
    public class Lz4Writer : IByteWriter, IDisposable
    {
        private Stream _stream;
        private bool _closeStream;

        private FileBuffer _uncompressedBuffer;
        private FileBuffer _compressedBuffer;

        private MatchTable _matchTable;

        public Lz4Writer(Stream stream, bool closeStream = true)
        {
            _stream = stream;
            _closeStream = closeStream;

            _uncompressedBuffer = new FileBuffer();
            _compressedBuffer = new FileBuffer();

            _matchTable = new MatchTable();
        }

        public void Write(byte[] array, int index, int length)
        {
            int end = index + length;
            if (array == null) { throw new ArgumentNullException(nameof(array)); }
            if (index < 0 || index >= array.Length) { throw new ArgumentOutOfRangeException(nameof(index)); }
            if (length < 0 || end > array.Length) { throw new ArgumentOutOfRangeException(nameof(length)); }

            while (index < end)
            {
                // Fill uncompressed buffer from input
                index += _uncompressedBuffer.AppendFrom(array, index, end - index);

                // If uncompressed buffer full, compress, shift, clear
                if (index < end)
                {
                    CompressBuffer();
                }
            }
        }

        private void CompressBuffer()
        {
            // Identify and write tokens until near end of input buffer or at end of data
            int inputEnd = Math.Min(_uncompressedBuffer.Array.Length - Lz4Constants.MaximumTokenUncompressedLength, _uncompressedBuffer.End - Lz4Constants.MinimumCopyLength);

            byte[] array = _uncompressedBuffer.Array;
            int i = _uncompressedBuffer.Index;

            if (i >= inputEnd) { return; }

            while (i < inputEnd)
            {
                int tokenStart = i;
                int innerEnd = Math.Min(inputEnd, tokenStart + Lz4Constants.MaximumLiteralOrCopyLength);
                uint key = (uint)((array[i] << 16) + (array[i + 1] << 8) + (array[i + 2]));

                while (i < innerEnd)
                {
                    // Shift and add new byte to key (key is now first four bytes at tokenStart)
                    key = (uint)((key << 8) + array[i + 3]);

                    // Find the longest range matching array[tokenStart] we can copy from earlier
                    Match best = _matchTable.FindLongestMatch(_uncompressedBuffer, i, key);

                    if (best.Length >= Lz4Constants.MinimumCopyLength)
                    {
                        //// Add array[i + 1] .. array[i + (length-1)] to the Dictionary
                        //for (int j = i + 1; j < i + best.Length; ++j)
                        //{
                        //    _matchTable.Add(_uncompressedBuffer, j, key);
                        //}

                        long copyToPosition = _uncompressedBuffer.ArrayStartPosition + i;

                        WriteToken(new Token(i - tokenStart, best.Length, (ushort)(copyToPosition - best.Position)));
                        i += best.Length;
                        break;
                    }

                    i++;
                }

                if (_uncompressedBuffer.Index == tokenStart && i >= tokenStart + Lz4Constants.MaximumLiteralOrCopyLength)
                {
                    // If stopped at maximum literal length, write max length literal
                    WriteToken(new Token(i - tokenStart, 0, 0));
                }
            }

            // If out of input, shift the uncompressed buffer to make space to read more
            _uncompressedBuffer.Shift(Lz4Constants.MaximumCopyFromDistance);
        }

        private void WriteToken(Token token)
        {
            byte[] array = _uncompressedBuffer.Array;
            int index = _uncompressedBuffer.Index;

            // Flush buffer if too full for token
            if (token.CompressedLength > _compressedBuffer.RemainingSpace)
            {
                _compressedBuffer.WriteTo(_stream, Lz4Constants.MaximumCopyFromDistance);
                _compressedBuffer.Shift(Lz4Constants.MaximumCopyFromDistance);
            }

//#if DEBUG
//            string literal = Encoding.UTF8.GetString(array, index, token.LiteralLength);
//            string copy = Encoding.UTF8.GetString(array, index + token.LiteralLength, token.CopyLength);
//            string copySource = Encoding.UTF8.GetString(array, index + token.LiteralLength - token.CopyFromRelativeIndex, token.CopyLength);
//#endif

            _compressedBuffer.Append(token.LiteralLength);
            _compressedBuffer.Append(token.CopyLength);

            // Write literal bytes
            if (token.LiteralLength > 0)
            {
                _compressedBuffer.AppendFrom(array, index, token.LiteralLength);
            }

            // If copy bytes...
            if (token.CopyLength > 0)
            {
                // Write copy relative position
                _compressedBuffer.Append((byte)token.CopyFromRelativeIndex);
                _compressedBuffer.Append((byte)(token.CopyFromRelativeIndex >> 8));
            }

            // Mark the bytes of this token consumed
            _uncompressedBuffer.Index += token.LiteralLength + token.CopyLength;
        }

        private void Close()
        {
            // Compress remaining pending content
            CompressBuffer();

            // Write the last bytes of the compressed buffer as a literal
            if (_uncompressedBuffer.Length > 0)
            {
                WriteToken(new Token(_uncompressedBuffer.Length, 0, 0));
            }

            // Zero token to indicate end of content
            WriteToken(new Token(0, 0, 0));

            // Write everything
            _compressedBuffer.WriteTo(_stream);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_stream != null)
            {
                Close();
                if (_closeStream) { _stream.Dispose(); }
                _stream = null;
            }
        }
    }
}
