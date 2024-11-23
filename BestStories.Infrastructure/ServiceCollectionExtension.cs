using BestStories.Application;
using BestStories.Infrastructure.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace BestStories.Infrastructure
{
    public static class ServiceCollectionExtension
    {
        /// <summary>
        /// Sets up logic related to BestStories Infrastructure.
        /// </summary>
        public static IServiceCollection AddBestStoriesServiceInfrastructure(this IServiceCollection services, BestStoriesConfig bestStoriesConfig)
        {
            string? hackerNewsBaseUrl = bestStoriesConfig.HackerNewsBaseUrl; // "https://hacker-news.firebaseio.com"
            string? hackerNewsApiVersion = bestStoriesConfig.HackerNewsApiVersion; // "v0"
            if (string.IsNullOrEmpty(hackerNewsBaseUrl))
                throw new ArgumentException("Parameter not specified in the config", "HackerNewsBaseUrl");
            if (string.IsNullOrEmpty(hackerNewsApiVersion))
                throw new ArgumentException("Parameter not specified in the config", "HackerNewsApiVersion");

            services
                .AddHttpClient(HackerNewsService.HACKER_NEWS_HTTPCLIENT_NAME, client =>
                {
                    client.BaseAddress = new Uri($"{hackerNewsBaseUrl}/{hackerNewsApiVersion}/");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                })
                .AddTransientHttpErrorPolicy(policyBuilder =>
                    policyBuilder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            services.AddSingleton<IBestStoriesConfig>(bestStoriesConfig);
            services.AddScoped<IHackerNewsService, HackerNewsService>();
            services.AddScoped<GetTopStoriesUseCase>();

            return services;
        }
    }
}
