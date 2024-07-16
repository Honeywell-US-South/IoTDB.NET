using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTDBdotNET.TimeSeriesDB.ValueDeltaT
{
    public class DeltaTCompress
    {
        public List<int> TimeDeltas { get; set; } = new List<int>();
        private int lastTimestamp = -1;

        public void AddTimestamp(int timestamp)
        {
            if (lastTimestamp == -1)
            {
                TimeDeltas.Add(0); // First timestamp, delta is 0
            }
            else
            {
                int delta = timestamp - lastTimestamp;
                TimeDeltas.Add(delta);
            }
            lastTimestamp = timestamp;
        }

        public List<int> GetTimeDeltas()
        {
            return TimeDeltas.ToList();
        }

        public void SetTimeDeltas(List<int> deltas)
        {
            TimeDeltas = new List<int>(deltas);
        }
    }
}
