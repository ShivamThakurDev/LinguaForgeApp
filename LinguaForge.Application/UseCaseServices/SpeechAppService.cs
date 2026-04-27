using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;

namespace LinguaForge.Application.UseCaseServices
{
    public class SpeechAppService
    {
        private readonly IAzureSpeechService _speechService;

        public SpeechAppService(IAzureSpeechService speechService)
        {
            _speechService = speechService;
        }

        public Task<PronunciationResultDto> AssessAsync(byte[] wavAudio, SpeechAssessmentRequestDto request, CancellationToken cancellationToken = default)
            => _speechService.AssessPronunciationAsync(wavAudio, request.ReferenceText, request.Locale, cancellationToken);
    }
}