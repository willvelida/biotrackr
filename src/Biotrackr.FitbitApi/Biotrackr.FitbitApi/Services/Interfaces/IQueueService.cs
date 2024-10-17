namespace Biotrackr.FitbitApi.Services.Interfaces
{
    public interface IQueueService
    {
        Task SendRecordToQueue<T>(T record, string queueName);
    }
}
