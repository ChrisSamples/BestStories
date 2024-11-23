using BestStories.Domain;

namespace BestStories.Infrastructure
{
    /// <summary>
    /// Maps between HackerNewsStory and BestHackerNewsStory.
    /// 
    /// NOTE: I did not use AutoMapper as we want to reduce memory allocation and improve performance
    /// (make manual mappings instead of using AutoMapper reflection or expression tree compilation).
    /// </summary>
    public static class HackerNewsStoryMapping
    {
        public static BestHackerNewsStory MapToBestStory(HackerNewsStory story)
        {
            if (story == null)
                throw new ArgumentNullException(nameof(story), "HackerNewsStory cannot be null.");

            return new BestHackerNewsStory
            {
                Title = story.Title ?? "Untitled",
                Uri = story.Url ?? "No URL",
                PostedBy = story.By ?? "Anonymous",
                Time = DateTimeOffset.FromUnixTimeSeconds(story.Time).UtcDateTime,
                Score = story.Score,
                CommentCount = story.Descendants
            };
        }
    }
}
