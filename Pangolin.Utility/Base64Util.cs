namespace Pangolin.Utility
{
    public static class Base64Util
    {
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
    }
}