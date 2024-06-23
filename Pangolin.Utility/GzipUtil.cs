using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pangolin.Utility
{
    public class GzipUtil
    {
        private GzipUtil()
        { }

        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="input">原始数据</param>
        /// <returns></returns>
        public static byte[] Compress(byte[] input)
        {
            using (MemoryStream compressStream = new MemoryStream())
            {
                using (GZipStream zipStream = new GZipStream(compressStream, CompressionMode.Compress))
                {
                    zipStream.Write(input, 0, input.Length);
                    return compressStream.ToArray();
                }
            }
        }

        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="input">压缩数据</param>
        /// <returns></returns>
        public static byte[] Decompress(byte[] input)
        {
            using (MemoryStream compressStream = new MemoryStream(input))
            {
                using (GZipStream zipStream = new GZipStream(compressStream, CompressionMode.Decompress))
                {
                    using (MemoryStream resultStream = new MemoryStream())
                    {
                        zipStream.CopyTo(resultStream);
                        return resultStream.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="inputFile">原始文件</param>
        /// <param name="outputFile">压缩文件</param>
        public static void Compress(string inputFile, string outputFile)
        {
            using (FileStream inputStream = new FileStream(inputFile, FileMode.Open))
            {
                using (FileStream outputStream = File.Create(outputFile))
                {
                    using (GZipStream compressionStream = new GZipStream(outputStream, CompressionMode.Compress))
                    {
                        inputStream.CopyTo(compressionStream);
                    }
                }
            }
        }

        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="inputFile">压缩文件</param>
        /// <param name="outputFile">原始文件</param>
        public static void Decompress(string inputFile, string outputFile)
        {
            using (FileStream inputStream = new FileStream(inputFile, FileMode.Open))
            {
                using (FileStream outputStream = File.Create(outputFile))
                {
                    using (GZipStream decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress))
                    {
                        decompressionStream.CopyTo(outputStream);
                    }
                }
            }
        }
    }
}
