using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cortex.Modules.Search.UseCases;

namespace Cortex.Services
{
    public interface IInterestProfiler
    {
        Task<IList<string>> GetKeywordsAsync(Guid userId);
    }

    public class InterestProfiler : IInterestProfiler
    {
        private readonly GetInterestProfileUseCase _useCase;
        private readonly ILogger<InterestProfiler> _logger;

        public InterestProfiler(GetInterestProfileUseCase useCase, ILogger<InterestProfiler> logger)
        {
            _useCase = useCase;
            _logger = logger;
        }

        public async Task<IList<string>> GetKeywordsAsync(Guid userId)
        {
            var result = await _useCase.ExecuteAsync(userId);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Interest profile fetch failed for user {UserId}", userId);
                return new List<string>();
            }

            var keywords = new List<string>();
            foreach (var cat in result.Value.Categories ?? new List<Cortex.Modules.Search.UseCases.InterestCategoryResponse>())
            {
                if (!string.IsNullOrWhiteSpace(cat.Category))
                {
                    keywords.Add(cat.Category);
                }
            }
            return keywords;
        }
    }
}
