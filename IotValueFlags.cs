using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTDBdotNET
{
    [Flags]
    public enum IotValueFlags
    {
        None = 0,
        AllowManualOperator = 1 << 0,
        TimeSeries = 1 << 1,
        BlockChain = 1 << 2,
        PasswordValue = 1 << 3,
        LogChange = 1 << 4
    }
}
