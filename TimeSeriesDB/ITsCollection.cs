
namespace IoTDBdotNET.TimeSeriesDB
{
    internal interface ITsCollection
    {
        long Count();
        List<TsValue>? Get(DateTime startDate, DateTime endDate);
        List<TsValue>? Get(DateTime startDate, DateTime endDate, TimeSpan span);
    }
}