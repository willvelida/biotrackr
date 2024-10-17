using Azure.Messaging.ServiceBus;
using Biotrackr.FitbitApi.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

public class QueueService : IQueueService
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<QueueService> _logger;

    public QueueService(ServiceBusClient serviceBusClient, ILogger<QueueService> logger)
    {
        _serviceBusClient = serviceBusClient;
        _logger = logger;
    }

    public async Task SendRecordToQueue<T>(T record, string queueName)
    {
        try
        {
            ServiceBusSender serviceBusSender = _serviceBusClient.CreateSender(queueName);
            var payload = JsonSerializer.Serialize(record);
            await serviceBusSender.SendMessageAsync(new ServiceBusMessage(payload));
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception thrown in {nameof(SendRecordToQueue)}: {ex.Message}");
            throw;
        }
    }
}
