using System;
using System.IO;

namespace LZ4s
{
    /// <summary>
    ///  FileBuffer holds a buffer of a portion of a file, and tracks
    ///  data read (added) and used (consumed) from the buffer.
    /// </summary>
    public class FileBuffer
    {
        // Array with a portion of file contents
        public byte[] Array;

        // Next index to be consumed in array (valid but not used up)
        public int Index;

        // First index not valid in array (no data read into here yet)
        public int End;

        // Absolute file position corresponding to Array[0].
        public long ArrayStartPosition;

        public long Position => ArrayStartPosition + Index;
        
        public int Length => End - Index;
        public int RemainingSpace => Array.Length - End;
        public int Capacity => Array.Length;

        public FileBuffer(byte[] array = null)
        {
            Array = array ?? new byte[Lz4Constants.BufferSize];
        }

        public int AppendFrom(Stream stream)
        {
            int lengthRead = stream.Read(Array, End, Array.Length - End);
            End += lengthRead;

            return lengthRead;
        }

        public int AppendFrom(byte[] array, int index, int length)
        {
            int lengthToRead = Math.Min(Array.Length - End, length);

            Buffer.BlockCopy(array, index, Array, End, lengthToRead);
            End += lengthToRead;

            return lengthToRead;
        }

        public void Append(byte value)
        {
            Array[End++] = value;
        }

        public int WriteTo(Stream stream, int bytesToKeep = 0)
        {
            int lengthToWrite = Math.Max(0, Length - bytesToKeep);

            stream.Write(Array, Index, lengthToWrite);
            Index += lengthToWrite;

            return lengthToWrite;
        }

        public int WriteTo(byte[] array, int index, int length)
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

                // Track the absolute position of the array start (the sum of all shifting we've done)
                ArrayStartPosition += keepFromIndex;
            }
        }
    }
}
