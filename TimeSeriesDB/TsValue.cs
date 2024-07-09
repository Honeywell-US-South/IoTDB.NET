using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTDBdotNET.TimeSeriesDB
{
    internal class TsValue
    {
        public BsonValue Value { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsIndex { get; set; } = false;

        public TsValue() { }
        public TsValue(BsonValue value) { Value = value; }
        public TsValue(BsonValue value, DateTime? timestamp) : this(value)
        {
            Timestamp = timestamp ?? DateTime.UtcNow;
            if (Timestamp.Kind != DateTimeKind.Utc) Timestamp = Timestamp.ToUniversalTime();
        }
    }
}
