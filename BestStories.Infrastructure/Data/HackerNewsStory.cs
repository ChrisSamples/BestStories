namespace BestStories.Infrastructure
{
    public class HackerNewsStory
    {
        public string By { get; set; } = string.Empty;
        public int Descendants { get; set; }
        public int Id { get; set; }
        //public List<int> Kids { get; set; }
        public int Score { get; set; }
        // time in unix epoch (utc, seconds)
        public long Time { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }
}
