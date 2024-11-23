namespace BestStories.Infrastructure.Config
{
    public interface IBestStoriesConfig
    {
        string? LogSeqUrl { get; set; }
        string? HackerNewsBaseUrl { get; set; }
        string? HackerNewsApiVersion { get; set; }
        string? EndpointBestStories { get; set; }
        string? EndpointStoryDetails { get; set; }
        int CacheTopstoriesSlidingExpiration { get; set; }
        int CacheTopstoriesAbsoluteExpiration { get; set; }
        int CacheStorySlidingExpiration { get; set; }
        int CacheStoryAbsoluteExpiration { get; set; }
    }

    public class BestStoriesConfig : IBestStoriesConfig
    {
        public string? LogSeqUrl { get; set; }
        public string? HackerNewsBaseUrl { get; set; }
        public string? HackerNewsApiVersion { get; set; }
        public string? EndpointBestStories { get; set; }
        public string? EndpointStoryDetails { get; set; }
        /// <summary>
        /// In minutes.
        /// </summary>
        public int CacheTopstoriesSlidingExpiration { get; set; }
        /// <summary>
        /// In minutes.
        /// </summary>
        public int CacheTopstoriesAbsoluteExpiration { get; set; }
        /// <summary>
        /// In minutes.
        /// </summary>
        public int CacheStorySlidingExpiration { get; set; }
        /// <summary>
        /// In minutes.
        /// </summary>
        public int CacheStoryAbsoluteExpiration { get; set; }
    }
}
