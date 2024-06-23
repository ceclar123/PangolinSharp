namespace Pangolin.Utility
{
    public class Base64Util
    {
        private Base64Util()
        { }

        public static string Encode(byte[] input)
        {
            return System.Convert.ToBase64String(input);
        }

        public static byte[] Decode(string input)
        {
            return System.Convert.FromBase64String(input);
        }
    }
}
