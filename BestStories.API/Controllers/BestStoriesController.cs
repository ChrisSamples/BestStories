using Asp.Versioning;
using BestStories.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BestStories.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{v:apiVersion}/beststories")]
    public class BestStoriesV1Controller(GetTopStoriesUseCase getTopStoriesUseCase, ILogger<BestStoriesV1Controller> logger) : ControllerBase
    {
        private readonly GetTopStoriesUseCase _getTopStoriesUseCase = getTopStoriesUseCase;
        private readonly ILogger<BestStoriesV1Controller> _logger = logger;

        //[HttpGet]
        //public IActionResult Get()
        //{
        //    return new OkObjectResult("beststories from v1 controller");
        //}

        [HttpGet]
        [EnableRateLimiting("Fixed")]
        public async Task<IActionResult> GetBestStories([FromQuery] int count = 10)
        {
            try
            {
                List<Domain.BestHackerNewsStory> stories = await _getTopStoriesUseCase.ExecuteAsync(count);
                _logger.LogInformation("Fetching {Count} best stories at {UtcNow}", stories.Count, DateTime.UtcNow);
                return Ok(stories);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }

    //[ApiController]
    //[ApiVersion("2.0")]
    //[Route("api/v{v:apiVersion}/beststories")]
    //public class BestStoriesV2Controller : ControllerBase
    //{
    //    [HttpGet]
    //    public IActionResult Get()
    //    {
    //        return new OkObjectResult("beststories from v2 controller");
    //    }
    //}
}
