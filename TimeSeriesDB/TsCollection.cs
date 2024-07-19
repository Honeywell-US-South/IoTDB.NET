using IoTDBdotNET.BlockDB;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoTDBdotNET.TimeSeriesDB
{
    internal class TsCollection : BaseDatabase, ITsCollection
    {
        #region Global Variables
        private readonly string _indexName = "Index";
        private readonly string _collectionName = "Collection";
        private bool _processingQueue = false;
        private ConcurrentQueue<TsValue> _entityQueue = new();
        private bool _queueProcessing = false;
        #endregion

        #region Constructors
        public TsCollection(string dbPath, string name, string password = "") : base(dbPath, name, password)
        {
            if (!HasIdProperty(typeof(TsItem)))
            {
                throw new KeyNotFoundException("Table missing Id property with int, long, or Guid data type.");
            }
        }
        #endregion

        #region Base Abstract
        protected override void InitializeDatabase()
        {

            var col = Database.GetCollection<TsItem>(_collectionName);
            // Ensure there is an index on the StartDatetime field to make the query efficient
            col.EnsureIndex(x => x.StartDateTime);

        }

        protected override void PerformBackgroundWork(CancellationToken cancellationToken)
        {
            if (_queueProcessing) return;
            else FlushQueue();
        }

        private void FlushQueue()
        {
            lock (SyncRoot)
            {
                _queueProcessing = true;
                try
                {

                    const int MaxItemsPerFlush = 5000; // Adjust this value as needed
                    int itemsProcessed = 0;

                    var collection = Database.GetCollection<TsItem>(_collectionName);
                    var tsItem = collection.Find(Query.All(Query.Descending), limit: 1).FirstOrDefault();
                    bool isNew = false;
                    if (tsItem == null)
                    {
                        tsItem = new TsItem();
                        isNew = true;
                    }
                    if (tsItem.Values.Count > 10000)
                    {
                        tsItem = new TsItem();
                        isNew = true;
                    }
                    while (_entityQueue.TryDequeue(out var item) && itemsProcessed <= MaxItemsPerFlush)
                    {
                        tsItem.Add(item.Value, item.Timestamp);
                        itemsProcessed++;
                    }
                    if (tsItem.Values.Count > 0)
                    {
                        if (isNew)
                        {
                            collection.Insert(tsItem);
                        }
                        else
                        {
                            collection.Update(tsItem);
                        }

                        //Database.Commit(); do not need to do IoTDBdotNET auto commit
                    }


                }
                catch (Exception ex) { OnExceptionOccurred(new(ex)); }
                _queueProcessing = false;
            }
        }
        #endregion

        #region C
        /// <summary>
        /// Get document count using property on collection.
        /// </summary>
        public long Count()
        {
            return Database.GetCollection<Block>(_collectionName).LongCount();
        }
        #endregion
        #region I
        /// <summary>
        /// Insert a new entity to this collection. Document Id must be a new value in collection - Returns document Id
        /// </summary>
        internal void Insert(BsonValue data, DateTime? ts)
        {
            TsValue value = new TsValue(data, ts);
            _entityQueue.Enqueue(value);
        }
        #endregion

        #region G

        public List<TsValue>? Get(DateTime startDate, DateTime endDate)
        {
            if (startDate.Kind != DateTimeKind.Utc) startDate = startDate.ToUniversalTime();
            if (endDate.Kind != DateTimeKind.Utc) endDate = endDate.ToUniversalTime();

            var col = Database.GetCollection<TsItem>(_collectionName);
            var list = col.Find(x => x.StartDateTime >= startDate && x.StartDateTime < endDate && x.Timestamp >= startDate).ToList();
            List<TsValue> values = new List<TsValue>();
            foreach (var item in list)
            {
                for (int i = 0; i < item.Values.Count; i++)
                {
                    for (ulong j = 0; j <= item.Counter[i]; j++)
                    {
                        TsValue tsValue = new(item.Values[i], item.StartDateTime.AddMilliseconds(j));
                        tsValue.IsIndex = tsValue.Timestamp == item.StartDateTime.AddMilliseconds(item.IndexMsTime[i]);
                        values.Add(tsValue);
                    }
                }
            }
            return values;

        }

        public List<TsValue>? Get(DateTime startDate, DateTime endDate, TimeSpan span)
        {
            var values = Get(startDate, endDate);
            if (values == null || !values.Any())
                return values;


            List<TsValue> result = new List<TsValue>();
            DateTime currentTimestamp = startDate;

            while (currentTimestamp <= endDate)
            {
                var closestBefore = values.LastOrDefault(v => v.Timestamp <= currentTimestamp);
                var closestAfter = values.FirstOrDefault(v => v.Timestamp > currentTimestamp);

                if (closestBefore != null && closestAfter != null)
                {
                    double timeDiff = (closestAfter.Timestamp - closestBefore.Timestamp).TotalMilliseconds;
                    double valueDiff = closestAfter.Value - closestBefore.Value;
                    double timeSpanRatio = (currentTimestamp - closestBefore.Timestamp).TotalMilliseconds / timeDiff;
                    var interpolatedValue = closestBefore.Value + (valueDiff * timeSpanRatio);
                    TsValue value = new TsValue(interpolatedValue, currentTimestamp);
                    value.IsIndex = (value.Timestamp == closestAfter.Timestamp && closestAfter.IsIndex) || (value.Timestamp == closestBefore.Timestamp && closestBefore.IsIndex);
                    result.Add(new TsValue(interpolatedValue, currentTimestamp));
                }
                else if (closestBefore != null)
                {
                    result.Add(new TsValue(closestBefore.Value, currentTimestamp));
                }

                currentTimestamp = currentTimestamp.Add(span);
            }

            return result;
        }
        #endregion

    }
}
