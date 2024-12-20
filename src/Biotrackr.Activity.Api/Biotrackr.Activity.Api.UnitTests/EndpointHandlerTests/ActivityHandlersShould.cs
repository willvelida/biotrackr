﻿using AutoFixture;
using Biotrackr.Activity.Api.EndpointHandlers;
using Biotrackr.Activity.Api.Models;
using Biotrackr.Activity.Api.Repositories.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace Biotrackr.Activity.Api.UnitTests.EndpointHandlerTests
{
    public class ActivityHandlersShould
    {
        private readonly Mock<ICosmosRepository> _cosmosRepositoryMock;

        public ActivityHandlersShould()
        {
            _cosmosRepositoryMock = new Mock<ICosmosRepository>();
        }

        [Fact]
        public async Task GetActivityByDate_ShouldReturnOk_WhenActivityIsFound()
        {
            // Arrange
            var date = "2022-01-01";
            var fixture = new Fixture();
            var activityDocument = fixture.Create<ActivityDocument>();
            activityDocument.Date = date;

            _cosmosRepositoryMock.Setup(x => x.GetActivitySummaryByDate(date)).ReturnsAsync(activityDocument);

            // Act
            var result = await ActivityHandlers.GetActivityByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            result.Result.Should().BeOfType<Ok<ActivityDocument>>();
        }

        [Fact]
        public async Task GetActivityByDate_ShouldReturnNotFound_WhenActivityIsNotFound()
        {
            // Arrange
            var date = "2022-01-01";
            _cosmosRepositoryMock.Setup(x => x.GetActivitySummaryByDate(date))
                .ReturnsAsync((ActivityDocument)null);

            // Act
            var result = await ActivityHandlers.GetActivityByDate(_cosmosRepositoryMock.Object, date);

            // Assert
            result.Result.Should().BeOfType<NotFound>();
        }
    }
}
