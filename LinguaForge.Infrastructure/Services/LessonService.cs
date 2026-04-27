using LinguaForge.Application.DTOs;
using LinguaForge.Application.Interface;
using LinguaForge.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LinguaForge.Infrastructure.Services
{
    public class LessonService : ILessonService
    {
        private readonly LinguaForgeDbContext _dbContext;

        public LessonService(LinguaForgeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<LessonDto>> GetLessonsAsync(string level, CancellationToken cancellationToken = default)
        {
            var normalizedLevel = string.IsNullOrWhiteSpace(level) ? "A1" : level.ToUpperInvariant();

            var items = await _dbContext.VocabItems
                .Where(x => x.CefrLevel == normalizedLevel)
                .OrderBy(x => x.LessonKey)
                .ThenBy(x => x.German)
                .ToListAsync(cancellationToken);

            if (items.Count == 0)
            {
                return Array.Empty<LessonDto>();
            }

            return items.GroupBy(x => x.LessonKey)
                .Select(group => new LessonDto
                {
                    LessonKey = group.Key,
                    Level = normalizedLevel,
                    Title = group.Key.Replace("-", " "),
                    Description = "Vocabulary and grammar drill",
                    Vocabulary = group.Select(v => new LessonVocabDto
                    {
                        German = v.German,
                        English = v.English,
                        PartOfSpeech = v.PartOfSpeech,
                        AudioUrl = v.AudioUrl
                    }).ToList()
                })
                .ToList();
        }
    }
}