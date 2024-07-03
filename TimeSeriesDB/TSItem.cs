using IoTDBdotNET.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTDBdotNET.TimeSeriesDB
{
    internal class TsItem
    {
        public Guid Id { get; set; }
        public DateTime StartDateTime { get; set; } = DateTime.UtcNow;
        public List<ulong> IndexMsTime { get; set; } = new();
        public List<BsonValue> Values { get; set; } = new();
        public List<ulong> Counter { get; set; } = new();
        public DateTime Timestamp {  get; set; } = DateTime.MinValue;


        public TsItem() { }
        public TsItem(BsonValue value, DateTime ts)
        {
            Add(value, ts);
        }

        public void Add(BsonValue value)
        {
            Add(value, DateTime.UtcNow);
        }
        public void Add(BsonValue value, DateTime ts)
        {
            if (ts.Kind != DateTimeKind.Utc) ts = ts.ToUniversalTime();

            if (ts < Timestamp) throw new InvalidOperationException($"Cannot insert time series item older or equal to last timestamp [{Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff")}]");
            bool newIndex = false;
            if (Values.Count == 0)
            {
                StartDateTime = ts;
                newIndex = true;
            }
            else
            {
                var lastIndex = Counter.Count - 1;
                var lastValue = Values[lastIndex];
                if (lastValue == value)
                {
                    Counter[lastIndex] += Timestamp.MsTimeDiff(ts);
                } else
                {
                    newIndex = true;
                }
            }

            if (newIndex)
            {
                Values.Add(value);
                Counter.Add(0);
                IndexMsTime.Add(StartDateTime.MsTimeDiff(ts));
            }

            Timestamp = ts;
        }
    }
}
