using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTDBdotNET.Extensions
{
    public static class IotValueFlagsExtensions
    {
        public static IotValueFlags Enable(this IotValueFlags flags, IotValueFlags flagToEnable)
        {
            return flags | flagToEnable;
        }

        public static IotValueFlags Disable(this IotValueFlags flags, IotValueFlags flagToDisable)
        {
            return flags & ~flagToDisable;
        }

        public static bool IsEnabled(this IotValueFlags flags, IotValueFlags flagToCheck)
        {
            return (flags & flagToCheck) == flagToCheck;
        }
    }
}
