using System;
using System.IO;

namespace LZ4s
{
    public class Lz4sBuffer
    {
        public byte[] Array;
        public int Index;
        public int End;

        public int Length => End - Index;
        public int RemainingSpace => Array.Length - End;

        public Lz4sBuffer(byte[] array = null, int index = 0, int end = 0)
        {
            Array = array ?? new byte[Lz4Constants.BufferSize];
            Index = index;
            End = end;
        }

        public void Append(byte[] array, int index, int length)
        {
            Helpers.ArrayCopy(array, index, Array, End, length);
            End += length;
        }

        public void Append(byte value)
        {
            Array[End++] = value;
        }

        public int Read(Stream stream)
        {
            int lengthRead = stream.Read(Array, End, Array.Length - End);
            End += lengthRead;

            return lengthRead;
        }

        public int Read(byte[] array, int index, int length)
        {
            int lengthToRead = Math.Min(Array.Length - End, length);

            Buffer.BlockCopy(array, index, Array, End, lengthToRead);
            End += lengthToRead;

            return lengthToRead;
        }


        public int Write(Stream stream, int bytesToKeep = 0)
        {
            int lengthToWrite = Math.Max(0, Length - bytesToKeep);

            stream.Write(Array, Index, lengthToWrite);
            Index += lengthToWrite;

            return lengthToWrite;
        }

        public int Write(byte[] array, int index, int length)
        {
            int lengthToWrite = Math.Min(Length, length);

            Buffer.BlockCopy(Array, Index, array, index, lengthToWrite);
            Index += lengthToWrite;

            return lengthToWrite;
        }

        public void Shift(int bytesBeforeIndexToKeep = 0)
        {
            // When unable to decode more, shift bytes and read to refill buffer.
            // Must keep bytes between _bufferIndex and _bufferEnd, plus MaxCopyFromDistance before _bufferIndex.

            // Buffer:
            // 0        keepFromIndex                  _bufferIndex        _bufferEnd    _buffer.Length
            // |        | <- bytesBeforeIndexToKeep -> |                   |             |
            // |        | <-             keepLength                     -> |             |

            int keepFromIndex = Index - bytesBeforeIndexToKeep;
            int keepLength = End - keepFromIndex;

            if (keepFromIndex > 0)
            {
                // Shift by keepFromIndex to keep to beginning of buffer
                Buffer.BlockCopy(Array, keepFromIndex, Array, 0, keepLength);
                Index -= keepFromIndex;
                End = keepLength;
            }
        }
    }
}
