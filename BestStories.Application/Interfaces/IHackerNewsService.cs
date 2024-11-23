using BestStories.Domain;

namespace BestStories.Application
{
    public interface IHackerNewsService
    {
        Task<List<BestHackerNewsStory>> GetTopStoriesAsync(int count);
    }
}
