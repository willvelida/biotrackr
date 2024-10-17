using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace Biotrackr.FitbitApi.UnitTests.ServiceTests
{
    public class QueueServiceShould
    {
        private readonly Mock<ServiceBusClient> _mockServiceBusClient;
        private readonly Mock<ILogger<QueueService>> _mockLogger;
        private readonly QueueService _queueService;

        public QueueServiceShould()
        {
            _mockServiceBusClient = new Mock<ServiceBusClient>();
            _mockLogger = new Mock<ILogger<QueueService>>();
            _queueService = new QueueService(_mockServiceBusClient.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task SendRecordToQueue_ShouldSendMessageToQueue()
        {
            // Arrange
            var queueName = "test-queue";
            var testRecord = new { Id = 1, Name = "Test" };
            var mockSender = new Mock<ServiceBusSender>();
            _mockServiceBusClient.Setup(client => client.CreateSender(queueName)).Returns(mockSender.Object);

            // Act      
            await _queueService.SendRecordToQueue(testRecord, queueName);

            // Assert
            mockSender.Verify(sender => sender.SendMessageAsync(It.Is<ServiceBusMessage>(msg =>
                msg.Body.ToString() == JsonSerializer.Serialize(testRecord, (JsonSerializerOptions)default)), default), Times.Once);
        }

        [Fact]
        public async Task SendRecordToQueue_ShouldLogError_WhenExceptionThrown()
        {
            // Arrange
            var queueName = "test-queue";
            var testRecord = new { Id = 1, Name = "Test" };
            var exceptionMessage = "Test exception";
            var mockSender = new Mock<ServiceBusSender>();
            _mockServiceBusClient.Setup(client => client.CreateSender(queueName)).Returns(mockSender.Object);
            mockSender.Setup(sender => sender.SendMessageAsync(It.IsAny<ServiceBusMessage>(), default))
                      .ThrowsAsync(new Exception(exceptionMessage));

            // Act
            Func<Task> act = async () => await _queueService.SendRecordToQueue(testRecord, queueName);

            // Assert
            await act.Should().ThrowAsync<Exception>();
            _mockLogger.VerifyLog(logger => logger.LogError($"Exception thrown in SendRecordToQueue: {exceptionMessage}"));
        }
    }
}
