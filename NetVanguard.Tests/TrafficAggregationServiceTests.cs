using Moq;
using NetVanguard.Core.Models;
using NetVanguard.Core.Services;

namespace NetVanguard.Tests
{
    public class TrafficAggregationServiceTests
    {
        [Fact]
        public void Service_Aggregates_Traffic_Correctly()
        {
            // Arrange
            var mockEtwMonitor = new Mock<IEtwMonitorService>();
            var mockProcessMapper = new Mock<IProcessMapperService>();
            var mockStatsService = new Mock<IStatisticsService>();

            mockProcessMapper.Setup(m => m.GetOrResolveApplication(1234))
                .Returns(new NetworkApplication { ProcessId = 1234, ProcessName = "chrome" });

            var service = new TrafficAggregationService(mockEtwMonitor.Object, mockProcessMapper.Object, mockStatsService.Object);

            TrafficUpdateMessage capturedMessage = null;
            service.OnTrafficUpdated += (s, msg) => capturedMessage = msg;

            // Start so we can capture things
            var cts = new CancellationTokenSource();
            service.StartAggregating();

            // Act
            mockEtwMonitor.Raise(m => m.OnTrafficCaptured += null, new NetworkTrafficEventArgs
            {
                ProcessId = 1234,
                Size = 1000,
                IsReceive = true,
                SourceIp = "1.1.1.1",
                DestinationIp = "192.168.1.10"
            });

            mockEtwMonitor.Raise(m => m.OnTrafficCaptured += null, new NetworkTrafficEventArgs
            {
                ProcessId = 1234,
                Size = 500,
                IsReceive = false,
                SourceIp = "192.168.1.10",
                DestinationIp = "1.1.1.1"
            });

            // Need to force drain buffer since it's a background loop
            var drainMethod = typeof(TrafficAggregationService).GetMethod("DrainBufferAndPublish", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            drainMethod.Invoke(service, null);

            // Assert
            Assert.NotNull(capturedMessage);
            Assert.Single(capturedMessage.Applications);
            var app = capturedMessage.Applications[0];
            Assert.Equal(1234, app.ProcessId);
            Assert.Equal(1000, app.BytesReceived);
            Assert.Equal(500, app.BytesSent);

            service.Dispose();
        }
    }
}
