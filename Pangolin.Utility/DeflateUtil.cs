using System.IO.Compression;
using System.Text;

namespace Pangolin.Utility;

public static class DeflateUtil
{
    public static byte[] Compress(byte[] input)
    {
        using (var output = new MemoryStream())
        {
            using (var deflateStream = new DeflateStream(output, CompressionMode.Compress, true))
            {
                deflateStream.Write(input, 0, input.Length);
            }

            return output.ToArray();
        }
    }

    public static byte[] Compress(string input)
    {
        return Compress(Encoding.UTF8.GetBytes(input));
    }

    public static string Decompress(string compressedString)
    {
        byte[] compressedData = Encoding.UTF8.GetBytes(compressedString);
        return Decompress(compressedData);
    }

    public static string Decompress(byte[] compressedData)
    {
        using var inputStream = new MemoryStream(compressedData);
        using var outputStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
        {
            deflateStream.CopyTo(outputStream);
        }

        return Encoding.UTF8.GetString(outputStream.ToArray());
    }

    public static void CompressFile(string sourceFile, string compressedFile)
    {
        using FileStream originalFileStream = File.OpenRead(sourceFile);
        using FileStream compressedFileStream = File.Create(compressedFile);
        using DeflateStream compressionStream = new DeflateStream(compressedFileStream, CompressionMode.Compress);
        originalFileStream.CopyTo(compressionStream);
    }

    public static void DecompressFile(string compressedFile, string targetFile)
    {
        using FileStream compressedFileStream = File.OpenRead(compressedFile);
        using FileStream outputFileStream = File.Create(targetFile);
        using DeflateStream decompressionStream = new DeflateStream(compressedFileStream, CompressionMode.Decompress);
        decompressionStream.CopyTo(outputFileStream);
    }
}