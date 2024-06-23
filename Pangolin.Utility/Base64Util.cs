namespace Pangolin.Utility
{
    public class Base64Util
    {
        private Base64Util()
        { }

        /// <summary>
        /// Base64编码
        /// </summary>
        /// <param name="input">原始字符串</param>
        /// <returns></returns>
        public static string Encode(byte[] input)
        {
            return System.Convert.ToBase64String(input);
        }

        /// <summary>
        /// Base64解码
        /// </summary>
        /// <param name="input">base64字符串</param>
        /// <returns></returns>
        public static byte[] Decode(string input)
        {
            return System.Convert.FromBase64String(input);
        }
    }
}
