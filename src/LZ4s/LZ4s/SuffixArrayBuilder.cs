using System;

namespace LZ4s
{
    /// <summary>
    ///  SuffixArrayBuilder computes and returns an array of ushorts pointing to each
    ///  position in the array, sorted in lexicographic order.
    /// </summary>
    /// <remarks>
    ///  This class implements the algorithm in "Suffix Arrays: A New Method for On-Line String Searches"
    ///  by Manber and Myers, 1990.
    ///  
    ///  To improve performance, consider a more complex algorithm (see SA-IS, 'saislite', SACA-K).
    /// </remarks>
    public class SuffixArrayBuilder
    {
        private const int AlphabetSize = 256;

        // Variables use the names from the whitepaper to minimize confusion comparing to the paper description.

        // 'Pos' is the Suffix Array; an array with the index of each position in the text, sorted in Ordinal order.
        private ushort[] Pos;

        // 'Prm' is the 'Rank' of each position; for a file position, Rank identifies the sort order (or the index in Pos) for that text position.
        private ushort[] Prm;

        // 'Count' tracks the number of suffixes in each bucket during sorting. The bucket count changes with each pass, up to one bucket per position.
        private ushort[] Count;

        // 'BH[i]' is true when 'Pos[i]' is the first position in a different bucket (with a new distinct prefix of the current sorted length)
        private bool[] BH;

        // 'B2H[i]' is true when 'Pos[i]' will be the first position in a different bucket sorted by the next pass length (double the characters of the previous pass)
        private bool[] B2H;

        // 'Next[bucketStartIndex]' provides the first index of the next bucket. Computing this is faster than looking for the next bucket start twice in the main algorithm.
        private int[] Next;

        public SuffixArrayBuilder(int textLength)
        {
            if (textLength - 1 > ushort.MaxValue) { throw new ArgumentOutOfRangeException("SuffixArrayBuilder can only sort ranges of 64KB and under."); }

            Pos = new ushort[textLength];
            Prm = new ushort[textLength];
            Count = new ushort[Math.Max(AlphabetSize, textLength)];
            BH = new bool[textLength];
            B2H = new bool[textLength];
            Next = new int[textLength];
        }

        public ushort[] SuffixArray(Span<byte> text)
        {
            // Reset secondary result arrays (to fill out as we go with primary purpose here)
            Array.Clear(Prm, 0, Prm.Length);
            Array.Clear(Count, 0, Count.Length);
            Array.Clear(BH, 0, BH.Length);
            Array.Clear(B2H, 0, B2H.Length);

            // 1. Sort each position in text by the first byte there
            BucketByFirstByte(text);

            // 2. Reorder items within each bucket to be sorted based on how the suffix N characters later sorted
            bool sortFinished = false;
            for (int sortLength = 1; sortLength <= 256 && !sortFinished; sortLength *= 2)
            {
                sortFinished = BucketToLength(text, sortLength);
            }

            return Pos;
        }

        private void BucketByFirstByte(Span<byte> text)
        {
            // Count number of suffixes per first byte
            ushort[] countPerBucket = Count;

            Array.Clear(countPerBucket, 0, AlphabetSize);
            for (int i = 0; i < text.Length; ++i)
            {
                countPerBucket[text[i]]++;
            }

            // Sum to find the first index per bucket
            ushort[] nextIndexForBucket = Count;

            int sumBeforeBucket = 0;
            for (int i = 0; i < AlphabetSize && sumBeforeBucket < text.Length; ++i)
            {
                BH[sumBeforeBucket] = true;

                int countInBucket = countPerBucket[i];
                nextIndexForBucket[i] = (ushort)sumBeforeBucket;
                sumBeforeBucket += countInBucket;
            }

            // Put the suffixes into buckets by first byte
            ushort[] suffixesSortedByFirstByte = Pos;

            Array.Clear(suffixesSortedByFirstByte, 0, suffixesSortedByFirstByte.Length);
            for (int i = 0; i < text.Length; ++i)
            {
                int bucket = text[i];
                int index = nextIndexForBucket[bucket]++;
                suffixesSortedByFirstByte[index] = (ushort)i;
            }
        }

        private bool BucketToLength(Span<byte> text, int phaseLength)
        {
            // In a previous pass, each suffix was sorted by the first 'phaseLength' characters.
            // This logic sorts each suffix by (2 * phaseLength) characters.
            // It does this by re-ordering the suffixes in each bucket based on how the suffix 'phaseLength' characters later was sorted.
            // Each bucket is then split into one bucket for each different 'next suffix' found.
            // This continues until every suffix is in a separate bucket (meaning every suffix was distinct within 'phaseLength' characters).

            // Reset Prm[i] to point to the first cell of the bucket
            int bucketCount = 0;
            int currentBucketStart = 0;
            for (int i = 0; i < text.Length; ++i)
            {
                if (BH[i])
                {
                    Next[currentBucketStart] = i;
                    currentBucketStart = i;
                    bucketCount++;
                }

                Prm[Pos[i]] = (ushort)currentBucketStart;
            }
            Next[currentBucketStart] = text.Length;

            // If there is already one bucket per position, we're done sorting
            if (bucketCount == text.Length)
            {
                return true;
            }

            // Clear counts
            Array.Clear(Count, 0, Count.Length);

            Count[Prm[text.Length - phaseLength]] = 1;
            B2H[Prm[text.Length - phaseLength]] = true;

            // Walk the buckets in order (sorted by the first 'phaseLength' bytes)
            currentBucketStart = 0;
            int currentBucketEnd;
            for (; currentBucketStart < text.Length; currentBucketStart = currentBucketEnd)
            {
                currentBucketEnd = Next[currentBucketStart];

                // For each item in this bucket...
                for (int i = currentBucketStart; i < currentBucketEnd; ++i)
                {
                    // Find the suffix 'phaseLength' bytes earlier (so, Pos[i] is phaseLength bytes after the start of Ti)
                    int Ti = Pos[i] - phaseLength;
                    if (Ti >= 0)
                    {
                        // Reorder Ti in the bucket it was in (when done, these suffixes will then be sorted by 2*phaseLength bytes)
                        int bucket = Prm[Ti];
                        int newBucketPosition = bucket + Count[bucket]++;
                        Prm[Ti] = (ushort)newBucketPosition;

                        // Mark the item moved (so we can identify how many buckets Ti's bucket was split into for the next pass)
                        B2H[Prm[Ti]] = true;
                    }
                }

                // Ensure only the first start of each newly split bucket is marked
                for (int i = currentBucketStart; i < currentBucketEnd; ++i)
                {
                    int Ti = Pos[i] - phaseLength;
                    if (Ti >= 0)
                    {
                        if (B2H[Prm[Ti]])
                        {
                            for (int k = Prm[Ti] + 1; k < text.Length && !BH[k] && B2H[k]; k++)
                            {
                                B2H[k] = false;
                            }
                        }
                    }
                }
            }

            // Update 'Pos' to have the new sort orders from the updated 'Prm',
            for (int i = 0; i < text.Length; ++i)
            {
                Pos[Prm[i]] = (ushort)i;
            }

            // Update 'BH' with the larger set of buckets from B2H.
            Array.Copy(B2H, BH, BH.Length);

            // Return sort not finished
            return false;
        }
    }
}
