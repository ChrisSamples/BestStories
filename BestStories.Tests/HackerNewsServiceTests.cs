using System.Net;
using BestStories.Domain;
using BestStories.Infrastructure;
using BestStories.Infrastructure.Config;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace BestStories.Tests
{
    public class HackerNewsServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IMemoryCache> _memoryCacheMock;
        private readonly Mock<IBestStoriesConfig> _bestStoriesConfigMock;
        private readonly Mock<ILogger<HackerNewsService>> _loggerMock;
        private readonly HackerNewsService _hackerNewsService;

        public HackerNewsServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            IMemoryCache memoryCache = Mock.Of<IMemoryCache>();
            _memoryCacheMock = Mock.Get(memoryCache); //new Mock<IMemoryCache>();
            _bestStoriesConfigMock = new Mock<IBestStoriesConfig>();
            _bestStoriesConfigMock.SetupGet(c => c.EndpointBestStories).Returns("beststories.json");
            _bestStoriesConfigMock.SetupGet(c => c.EndpointStoryDetails).Returns("item/{0}.json");
            _bestStoriesConfigMock.SetupGet(c => c.CacheTopstoriesSlidingExpiration).Returns(10);
            _bestStoriesConfigMock.SetupGet(c => c.CacheTopstoriesAbsoluteExpiration).Returns(15);

            _loggerMock = new Mock<ILogger<HackerNewsService>>();

            _hackerNewsService = new HackerNewsService(_httpClientFactoryMock.Object, _memoryCacheMock.Object,
                _bestStoriesConfigMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task FetchBestStoriesV2_ShouldUseCache_WhenStoryIsCached()
        {
            // Arrange
            int storyId = 123;
            BestHackerNewsStory? storyDetails = new()
            {
                Title = "Some story",
                Uri = "http://example.com",
                PostedBy = "user",
                Time = DateTime.UtcNow,
                Score = 100,
                CommentCount = 50
            };

            ICacheEntry cacheEntry = Mock.Of<ICacheEntry>();
            _memoryCacheMock
                .Setup(m => m.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntry);

            List<int> storyIds = [storyId];
            int count = 1;

            // Act
            List<BestHackerNewsStory> result = await _hackerNewsService.FetchBestStoriesV2(count, storyIds);

            // Assert
            Assert.Single(result);
            Assert.Equal(storyDetails.Title, result[0].Title);

            _memoryCacheMock.Verify(m => m.TryGetValue(It.IsAny<object>(), out storyDetails), Times.Once);
        }

        [Fact]
        public async Task FetchBestStoriesV2_ShouldFetchFromApi_WhenStoryIsNotCached()
        {
            // Arrange
            int storyId = 123;
            HackerNewsStory fetchedStory = new()
            {
                By = "user",
                Descendants = 50,
                Id = storyId,
                Score = 100,
                Time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Title = "New Story",
                Type = "story",
                Url = "http://example.com"
            };

            BestHackerNewsStory? bestStory = new()
            {
                Title = "New Story",
                Uri = "http://example.com",
                PostedBy = "user",
                Time = DateTime.UtcNow,
                Score = 100,
                CommentCount = 50
            };

            ICacheEntry cacheEntry = Mock.Of<ICacheEntry>();
            _memoryCacheMock
                .Setup(m => m.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntry);

            Mock<HttpClient> httpClientMock = new();
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClientMock.Object);

            // Mocking API call
            httpClientMock.Setup(client => client.GetAsync(It.IsAny<string>()))
                          .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                          {
                              Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(fetchedStory))
                          });

            List<int> storyIds = [storyId];
            int count = 1;

            // Act
            var result = await _hackerNewsService.FetchBestStoriesV2(count, storyIds);

            // Assert
            result.Should().ContainSingle()
                  .Which.Title.Should().Be(fetchedStory.Title);

            _memoryCacheMock.Verify(m => m.Set(It.IsAny<object>(), It.IsAny<object>(), It.IsAny<MemoryCacheEntryOptions>()), Times.Once);
        }

        [Fact]
        public async Task FetchBestStoriesV2_ShouldLogError_WhenExceptionOccurs()
        {
            // Arrange
            int storyId = 123;
            List<int> storyIds = [storyId];
            int count = 1;

            ICacheEntry cacheEntry = Mock.Of<ICacheEntry>();
            _memoryCacheMock
                .Setup(m => m.CreateEntry(It.IsAny<object>()))
                .Returns(cacheEntry);

            Mock<HttpClient> httpClientMock = new();
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClientMock.Object);

            // Simulate API failure
            httpClientMock.Setup(client => client.GetAsync(It.IsAny<string>()))
                          .ThrowsAsync(new Exception("API error"));

            // Act
            var result = await _hackerNewsService.FetchBestStoriesV2(count, storyIds);

            // Assert
            result.Should().BeEmpty();
            _loggerMock.Verify(logger => logger.LogError(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<object>()), Times.Once);
        }
    }
}