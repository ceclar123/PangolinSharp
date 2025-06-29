using System.IO.Compression;
using System.Text;

namespace Pangolin.Utility;

public static class ZipUtil
{
    /// <summary>
    /// 压缩文件夹
    /// </summary>
    /// <param name="sourceDirectoryName">c:/123</param>
    /// <param name="destinationArchiveFileName">c:/123.zip</param>
    /// <param name="compressionLevel">压缩等级</param>
    /// <param name="includeBaseDirectory">true-包含文件夹</param>
    public static void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, CompressionLevel compressionLevel, bool includeBaseDirectory)
    {
        ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, compressionLevel, includeBaseDirectory);
    }

    /// <summary>
    /// 解压缩
    /// </summary>
    /// <param name="sourceArchiveFileName">c:/123.zip</param>
    /// <param name="destinationDirectoryName">c:/123</param>
    /// <param name="entryNameEncoding">编码</param>
    /// <param name="overwriteFiles">true-覆盖已有文件</param>
    public static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, Encoding? entryNameEncoding, bool overwriteFiles)
    {
        ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName, entryNameEncoding, overwriteFiles);
    }


    public static byte[] CompressToZipMemory()
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                var file1 = archive.CreateEntry("file1.txt");
                using (var writer = new StreamWriter(file1.Open()))
                {
                    writer.Write("This is the content of file 1.");
                }

                var file2 = archive.CreateEntry("file2.txt");
                using (var writer = new StreamWriter(file2.Open()))
                {
                    writer.Write("Content of file 2.");
                }
            }

            return memoryStream.ToArray();
        }
    }

    public static void DecompressFromZipMemory(byte[] zipData)
    {
        using (var memoryStream = new MemoryStream(zipData))
        {
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    using (var reader = new StreamReader(entry.Open()))
                    {
                        Console.WriteLine(reader.ReadToEnd());
                    }
                }
            }
        }
    }
}