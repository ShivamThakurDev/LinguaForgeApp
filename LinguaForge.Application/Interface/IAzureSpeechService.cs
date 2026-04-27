using LinguaForge.Application.DTOs;

namespace LinguaForge.Application.Interface
{
    public interface IAzureSpeechService
    {
        Task<PronunciationResultDto> AssessPronunciationAsync(byte[] wavAudio, string referenceText, string locale, CancellationToken cancellationToken = default);
    }
}