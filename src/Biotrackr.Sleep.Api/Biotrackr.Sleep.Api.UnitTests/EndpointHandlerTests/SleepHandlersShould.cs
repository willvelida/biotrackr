using AutoFixture;
using Biotrackr.Sleep.Api.EndpointHandlers;
using Biotrackr.Sleep.Api.Models;
using Biotrackr.Sleep.Api.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Biotrackr.Sleep.Api.UnitTests.EndpointHandlerTests
{
    public class SleepHandlersShould
    {
        private readonly Mock<ICosmosRepository> _cosmosRepositoryMock;

        public SleepHandlersShould()
        {
            _cosmosRepositoryMock = new Mock<ICosmosRepository>();
        }

        [Fact]
        public async Task GetSleepByDate_ShouldReturnOk_WhenSleepDocumentIsFound()
        {
            // Arrange
            var date = "2022-01-01";
            var fixture = new Fixture();
            var sleepDocument = fixture.Create<SleepDocument>();
            sleepDocument.Date = date;

            _cosmosRepositoryMock.Setup(x => x.GetSleepSummaryByDate(date)).ReturnsAsync(sleepDocument);

            // Act
            var result = await SleepHandlers.GetSleepByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            result.Result.Should().BeOfType<Ok<SleepDocument>>();
        }

        [Fact]
        public async Task GetSleepByDate_ShouldReturnNotFound_WhenSleepDocumentIsNotFound()
        {
            // Arrange
            var date = "2022-01-01";
            _cosmosRepositoryMock.Setup(x => x.GetSleepSummaryByDate(date)).ReturnsAsync((SleepDocument)null);

            // Act
            var result = await SleepHandlers.GetSleepByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            result.Result.Should().BeOfType<NotFound>();
        }

        [Fact]
        public async Task GetAllSleeps_ShouldReturnListOfSleepDocuments()
        {
            // Arrange
            var fixture = new Fixture();
            var sleepDocuments = fixture.CreateMany<SleepDocument>().ToList();
            _cosmosRepositoryMock.Setup(x => x.GetAllSleepDocuments()).ReturnsAsync(sleepDocuments);
            // Act
            var result = await SleepHandlers.GetAllSleeps(_cosmosRepositoryMock.Object);
            // Assert
            result.Should().BeOfType<Ok<List<SleepDocument>>>();
        }
    }
}
