using BestStories.Domain;

namespace BestStories.Application
{
    public class GetTopStoriesUseCase(IHackerNewsService hackerNewsService)
    {
        private readonly IHackerNewsService _hackerNewsService = hackerNewsService;

        /// <summary>
        /// Gets the specified <paramref name="count"/> of Hacker News stories.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public async Task<List<BestHackerNewsStory>> ExecuteAsync(int count)
        {
            if (count <= 0)
                throw new ArgumentException("Count must be greater than zero.");

            List<BestHackerNewsStory> stories = await _hackerNewsService.GetTopStoriesAsync(count);

            return SortV2(stories);
        }

        /// <summary>
        /// Sort descending.
        /// </summary>
        private List<BestHackerNewsStory> Sort(List<BestHackerNewsStory> stories)
        {
            stories = [.. stories.OrderByDescending(s => s.Score)];
            return stories;
        }

        /// <summary>
        /// Sort descending. Less memory allocation.
        /// </summary>
        private List<BestHackerNewsStory> SortV2(List<BestHackerNewsStory> stories)
        {
            // Sort in place to avoid extra memory allocation
            stories.Sort((x, y) => y.Score.CompareTo(x.Score));

            return stories;
        }
    }
}
