using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Runtime;
using BestStories.Application;
using BestStories.Domain;
using BestStories.Infrastructure.Config;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BestStories.Infrastructure
{
    public class HackerNewsService : IHackerNewsService
    {
        public const string HACKER_NEWS_HTTPCLIENT_NAME = "HackerNewsClient";

        private const string CACHE_TOPSTORIES_KEY = "TopStories";
        private const string CACHE_STORY_DETAILS_KEY = "StoryDetails_";

        private static string? endpointBestStories;
        private static string? endpointStoryDetails;
        private static int cacheTopstoriesSlidingExpiration;
        private static int cacheTopstoriesAbsoluteExpiration;
        private static int cacheStorySlidingExpiration;
        private static int cacheStoryAbsoluteExpiration;

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private readonly IBestStoriesConfig _bestStoriesConfig;
        private readonly ILogger<HackerNewsService> _logger;

        public HackerNewsService(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache,
            IBestStoriesConfig bestStoriesConfig, ILogger<HackerNewsService> logger)
        {
            _httpClient = httpClientFactory.CreateClient(HACKER_NEWS_HTTPCLIENT_NAME);
            _memoryCache = memoryCache;
            _bestStoriesConfig = bestStoriesConfig;
            _logger = logger;

            endpointBestStories ??= _bestStoriesConfig.EndpointBestStories;
            endpointStoryDetails ??= _bestStoriesConfig.EndpointStoryDetails;

            if (cacheTopstoriesSlidingExpiration == 0) cacheTopstoriesSlidingExpiration = _bestStoriesConfig.CacheTopstoriesSlidingExpiration;
            if (cacheTopstoriesAbsoluteExpiration == 0) cacheTopstoriesAbsoluteExpiration = _bestStoriesConfig.CacheTopstoriesAbsoluteExpiration;
            if (cacheStorySlidingExpiration == 0) cacheStorySlidingExpiration = _bestStoriesConfig.CacheStorySlidingExpiration;
            if (cacheStoryAbsoluteExpiration == 0) cacheStoryAbsoluteExpiration = _bestStoriesConfig.CacheStoryAbsoluteExpiration;
        }

        public async Task<List<BestHackerNewsStory>> GetTopStoriesAsync(int count)
        {
            if (string.IsNullOrEmpty(endpointBestStories))
                throw new ArgumentException("EndpointBestStories is not specified in config.", "EndpointBestStories");
            if (string.IsNullOrEmpty(endpointStoryDetails))
                throw new ArgumentException("EndpointStoryDetails is not specified in config.", "EndpointStoryDetails");

            // Try to get the cached stories first
            if (_memoryCache.TryGetValue(CACHE_TOPSTORIES_KEY, out List<BestHackerNewsStory>? cachedStories) && cachedStories != null)
            {
                return cachedStories;
            }

            HttpResponseMessage response = await _httpClient.GetAsync(endpointBestStories);
            response.EnsureSuccessStatusCode();

            List<int>? storyIds = await response.Content.ReadFromJsonAsync<List<int>>();
            if (storyIds == null || storyIds.Count == 0)
                return [];

            List<BestHackerNewsStory> result;
            //result = await FetchBestStories(count, storyIds);
            result = await FetchBestStoriesV2(count, storyIds);

            // Cache the result with a sliding expiration of 1 hour
            _memoryCache.Set(CACHE_TOPSTORIES_KEY, result, new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(cacheTopstoriesSlidingExpiration),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheTopstoriesAbsoluteExpiration),
                Size = 1
            });

            return result;
        }

        private async Task<List<BestHackerNewsStory>> FetchBestStories(int count, List<int> storyIds)
        {
            if (endpointStoryDetails == null)
                return [];

            // Fetch stories concurrently
            IEnumerable<Task<HackerNewsStory?>> stories = storyIds.Take(count).Select(async id =>
            {
                HttpResponseMessage storyResponse = await _httpClient.GetAsync(string.Format(endpointStoryDetails, id));
                storyResponse.EnsureSuccessStatusCode();
                HackerNewsStory? story = await storyResponse.Content.ReadFromJsonAsync<HackerNewsStory>();
                return story;
            });

            return (await Task.WhenAll(stories)).Where(s => s != null).Select(s => HackerNewsStoryMapping.MapToBestStory(s!)).ToList();
        }

        /// <summary>
        /// Fetchs Best Stories. Less memory allocation.
        /// 
        /// NOTE: To reduce memory allocation, uses manual task and semaphore management (SemaphoreSlim), instead of Task Parallel Library (TPL).
        /// </summary>
        public async Task<List<BestHackerNewsStory>> FetchBestStoriesV2(int count, List<int> storyIds)
        {
            if (endpointStoryDetails == null)
                return [];

            // Prepare a buffer for results
            BestHackerNewsStory[] stories = new BestHackerNewsStory[Math.Min(count, storyIds.Count)];
            int index = 0;

            // Fetch story details in parallel, capped to a reasonable degree of parallelism
            SemaphoreSlim throttler = new(10); // Limit concurrent requests to avoid overwhelming the API
            List<Task> tasks = [];

            ConcurrentBag<Exception> exceptions = [];

            foreach (int id in storyIds.Take(count))
            {
                await throttler.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        int position = Interlocked.Increment(ref index) - 1;

                        BestHackerNewsStory? bestStory = null;

                        if (!_memoryCache.TryGetValue(CACHE_STORY_DETAILS_KEY + id, out bestStory))
                        {
                            using HttpResponseMessage storyResponse = await _httpClient.GetAsync(string.Format(endpointStoryDetails, id));
                            storyResponse.EnsureSuccessStatusCode();

                            HackerNewsStory? story = await storyResponse.Content.ReadFromJsonAsync<HackerNewsStory>();
                            if (story != null)
                            {
                                bestStory = HackerNewsStoryMapping.MapToBestStory(story);

                                _memoryCache.Set(CACHE_STORY_DETAILS_KEY + id, bestStory, new MemoryCacheEntryOptions
                                {
                                    SlidingExpiration = TimeSpan.FromMinutes(cacheStorySlidingExpiration),
                                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheStoryAbsoluteExpiration),
                                    Size = 1
                                });
                            }
                        }

                        if (bestStory != null)
                            stories[position] = bestStory;

                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                        _logger.LogError(ex, "Could not retrieve Best Story '{Id}'", id);
                    }
                    finally
                    {
                        throttler.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);

            if (!exceptions.IsEmpty)
                throw new AggregateException(exceptions);

            return stories.Where(s => s != null).ToList();
        }
    }
}
