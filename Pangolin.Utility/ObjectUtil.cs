using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pangolin.Utility
{
    public class ObjectUtil
    {
        private ObjectUtil() { }

        public static bool IsNull([AllowNull][NotNullWhen(false)] object val)
        {
            return val == null;
        }

        public static bool IsNotNull([AllowNull][NotNullWhen(true)] object val)
        {
            return val != null;
        }
    }
}
