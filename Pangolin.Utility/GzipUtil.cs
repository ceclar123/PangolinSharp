using System.IO.Compression;
using System.Text;

namespace Pangolin.Utility
{
    public static class GzipUtil
    {
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
                }

                return compressStream.ToArray();
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
        /// 同步压缩+Base64 编码
        /// </summary>
        /// <param name="input">原始数据</param>
        /// <returns></returns>
        public static string CompressAndBase64Encode(string input)
        {
            byte[] data = Encoding.UTF8.GetBytes(input);
            byte[] compressed = Compress(data);
            return Convert.ToBase64String(compressed);
        }

        /// <summary>
        /// 同步解码+解压
        /// </summary>
        /// <param name="input">压缩数据</param>
        /// <returns></returns>
        public static string DecompressBase64Decode(string input)
        {
            byte[] compressedData = Convert.FromBase64String(input);
            byte[] decompressed = Decompress(compressedData);
            return Encoding.UTF8.GetString(decompressed);
        }


        /// <summary>
        /// 异步压缩
        /// </summary>
        /// <param name="input">原始数据</param>
        /// <returns></returns>
        public static async Task<byte[]> CompressAsync(byte[] input)
        {
            using var memoryStream = new MemoryStream();
            await using var zipStream = new GZipStream(memoryStream, CompressionMode.Compress);
            await zipStream.WriteAsync(input, 0, input.Length);
            return memoryStream.ToArray();
        }

        /// <summary>
        /// 异步解压缩
        /// </summary>
        /// <param name="input">压缩数据</param>
        /// <returns></returns>
        public static async Task<byte[]> DecompressAsync(byte[] input)
        {
            using var memoryStream = new MemoryStream(input);
            await using var zipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
            using var output = new MemoryStream();
            await zipStream.CopyToAsync(output);
            return output.ToArray();
        }

        /// <summary>
        /// 压缩
        /// </summary>
        /// <param name="inputFile">原始文件</param>
        /// <param name="outputFile">压缩文件</param>
        public static void Compress(string inputFile, string outputFile)
        {
            using FileStream inputStream = new FileStream(inputFile, FileMode.Open);
            using FileStream outputStream = File.Create(outputFile);
            using GZipStream compressionStream = new GZipStream(outputStream, CompressionMode.Compress);
            inputStream.CopyTo(compressionStream);
        }

        /// <summary>
        /// 解压缩
        /// </summary>
        /// <param name="inputFile">压缩文件</param>
        /// <param name="outputFile">原始文件</param>
        public static void Decompress(string inputFile, string outputFile)
        {
            using FileStream inputStream = new FileStream(inputFile, FileMode.Open);
            using FileStream outputStream = File.Create(outputFile);
            using GZipStream decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress);
            decompressionStream.CopyTo(outputStream);
        }
    }
}