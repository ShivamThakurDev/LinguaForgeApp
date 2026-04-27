using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;

namespace LinguaForge.Application.UseCaseServices
{
    public class RecommendationAppService
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationAppService(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        public Task<RecommendationDto> GetNextAsync(Guid userId, CancellationToken cancellationToken = default)
            => _recommendationService.GetNextRecommendationAsync(userId, cancellationToken);
    }
}
