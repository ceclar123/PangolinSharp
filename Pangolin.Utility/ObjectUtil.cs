namespace Pangolin.Utility
{
    public static class ObjectUtil
    {
        public static bool IsNull(object? val)
        {
            return val == null;
        }

        public static bool IsNotNull(object? val)
        {
            return val != null;
        }
    }
}