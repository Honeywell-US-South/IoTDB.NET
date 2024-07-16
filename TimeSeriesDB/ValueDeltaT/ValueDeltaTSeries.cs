using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTDBdotNET.TimeSeriesDB.ValueDeltaT
{
    internal class ValueDeltaTSeries
    {
        public ConcurrentDictionary<string, DeltaTCompress> Values { get; set; } = new ConcurrentDictionary<string, DeltaTCompress>();
        public DateTime Start { get; set; }

        public void AddDataPoint(DateTime timestamp, string value)
        {
            if (Values.IsEmpty)
            {
                Start = timestamp;
            }

            int secondsSinceStart = (int)(timestamp - Start).TotalSeconds;

            var valueCompress = Values.GetOrAdd(value, v => new DeltaTCompress());
            valueCompress.AddTimestamp(secondsSinceStart);
        }

        public void CompressTimeDeltas()
        {
            Parallel.ForEach(Values.Values, valueCompress =>
            {
                var compressedDeltas = DeltaEncode(valueCompress.GetTimeDeltas());
                valueCompress.SetTimeDeltas(compressedDeltas);
            });
        }

        private List<int> DeltaEncode(List<int> deltas)
        {
            var encoded = new List<int>();
            if (deltas.Count == 0) return encoded;

            int previous = deltas[0];
            encoded.Add(previous);
            for (int i = 1; i < deltas.Count; i++)
            {
                encoded.Add(deltas[i] - previous);
                previous = deltas[i];
            }
            return encoded;
        }

        // Function to get data for a specified time frame
        public List<(DateTime, string)> GetDataForTimeFrame(DateTime start, DateTime end)
        {
            var result = new List<(DateTime, string)>();
            foreach (var valueCompress in Values)
            {
                var timeDeltas = valueCompress.Value.GetTimeDeltas();
                int cumulativeTime = 0;
                foreach (var delta in timeDeltas)
                {
                    cumulativeTime += delta;
                    var timestamp = Start.AddSeconds(cumulativeTime);
                    if (timestamp >= start && timestamp <= end)
                    {
                        result.Add((timestamp, valueCompress.Key));
                    }
                }
            }
            return result.OrderBy(t => t.Item1).ToList();
        }

        // Function to get data for a specified time frame with a set interval, filling missing data
        public List<(DateTime, string)> GetDataForTimeFrameWithInterval(DateTime start, DateTime end, TimeSpan interval)
        {
            var data = GetDataForTimeFrame(start, end);
            var result = new List<(DateTime, string)>();
            DateTime current = start;

            while (current <= end)
            {
                var nearestBefore = data.LastOrDefault(d => d.Item1 <= current);
                var nearestAfter = data.FirstOrDefault(d => d.Item1 >= current);

                if (nearestBefore != default && nearestAfter != default)
                {
                    if (nearestBefore.Item1 == nearestAfter.Item1)
                    {
                        result.Add(nearestBefore);
                    }
                    else
                    {
                        double t = (current - nearestBefore.Item1).TotalSeconds / (nearestAfter.Item1 - nearestBefore.Item1).TotalSeconds;
                        double interpolatedValue = double.Parse(nearestBefore.Item2) * (1 - t) + double.Parse(nearestAfter.Item2) * t;
                        result.Add((current, interpolatedValue.ToString("F2")));
                    }
                }
                else if (nearestBefore != default)
                {
                    result.Add(nearestBefore);
                }

                current = current.Add(interval);
            }

            return result;
        }

    }
}
