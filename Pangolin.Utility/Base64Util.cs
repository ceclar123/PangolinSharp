using System.Text;

namespace Pangolin.Utility
{
    public static class Base64Util
    {
        private const int MimeLineMax = 76;

        /// <summary>
        /// Base64编码
        /// </summary>
        /// <param name="input">原始字符串</param>
        /// <returns></returns>
        public static string Encode(byte[] input)
        {
            return Convert.ToBase64String(input);
        }

        /// <summary>
        /// Base64解码
        /// </summary>
        /// <param name="input">base64字符串</param>
        /// <returns></returns>
        public static byte[] Decode(string input)
        {
            return Convert.FromBase64String(input);
        }

        /// <summary>
        /// Base64编码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string UrlSafeEncode(byte[] input)
        {
            string base64 = Convert.ToBase64String(input);
            return base64.Replace('+', '-').Replace('/', '_').Replace("=", "");
        }

        /// <summary>
        /// Base64解码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static byte[] UrlSafeDecode(string input)
        {
            string base64 = input.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            return Convert.FromBase64String(base64);
        }

        /// <summary>
        /// Base64编码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string MimeEncode(byte[] input)
        {
            string base64 = Convert.ToBase64String(input);

            // 插入每76字符换行
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < base64.Length; i += MimeLineMax)
            {
                int length = Math.Min(MimeLineMax, base64.Length - i);
                sb.AppendLine(base64.Substring(i, length));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Base64解码
        /// </summary>
        /// <param name="input">base64字符串</param>
        /// <returns></returns>
        public static byte[] MimeDecode(string input)
        {
            return Convert.FromBase64String(input);
        }
    }
}