using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using Xunit;

namespace LZ4s.Test
{
    public class Lz4sDictionaryTests
    {
        [Fact]
        public void Lz4sDictionary_Basics()
        {
            Lz4sDictionary dictionary = new Lz4sDictionary();
            byte[] data = File.ReadAllBytes("Content/Tiny.json");

            for (int i = 0; i + 3 < data.Length; ++i)
            {
                uint key = (uint)((data[i] << 24) + (data[i + 1] << 16) + (data[i + 2] << 8) + data[i + 3]);
                dictionary.Add(key, i);
            }

            List<long> results = new List<long>();

            for (int i = 0; i + 3 < data.Length; ++i)
            {
                uint key = (uint)((data[i] << 24) + (data[i + 1] << 16) + (data[i + 2] << 8) + data[i + 3]);
                dictionary.Matches(key, results);

                Assert.Contains(i, results);

                if (results.Count > 1)
                {
                    Debugger.Break();
                }
            }
        }
    }
}
