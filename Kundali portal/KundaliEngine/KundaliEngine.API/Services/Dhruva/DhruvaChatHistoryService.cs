using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Services.Dhruva;

public interface IDhruvaChatHistoryService
{
    Task SaveAsync(Guid userId, string? kundaliId, string? question, string? response, CancellationToken cancellationToken);

    Task<IReadOnlyList<DhruvaChatChartDto>> ListChartsAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DhruvaChatMessageDto>> ListMessagesAsync(Guid userId, Guid kundaliId, CancellationToken cancellationToken);
}

public sealed class DhruvaChatHistoryService : IDhruvaChatHistoryService
{
    public const int QuestionMaxLength = 2000;
    public const int ResponseMaxLength = 16000;

    private readonly KundaliDbContext _dbContext;

    public DhruvaChatHistoryService(KundaliDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SaveAsync(Guid userId, string? kundaliId, string? question, string? response, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(kundaliId, out var parsedKundaliId))
        {
            return;
        }

        var storedQuestion = Trim(question, QuestionMaxLength);
        var storedResponse = Trim(response, ResponseMaxLength);
        if (storedQuestion.Length == 0 || storedResponse.Length == 0)
        {
            return;
        }

        var ownsChart = await _dbContext.Kundalis
            .AsNoTracking()
            .AnyAsync(item => item.Id == parsedKundaliId && item.UserId == userId, cancellationToken);
        if (!ownsChart)
        {
            return;
        }

        _dbContext.DhruvaChatMessages.Add(new DhruvaChatMessage
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            KundaliId = parsedKundaliId,
            Question = storedQuestion,
            Response = storedResponse,
            AskedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DhruvaChatChartDto>> ListChartsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rows = await (
            from message in _dbContext.DhruvaChatMessages.AsNoTracking()
            join kundali in _dbContext.Kundalis.AsNoTracking() on message.KundaliId equals kundali.Id
            where message.UserId == userId && kundali.UserId == userId
            group message by new { kundali.Id, kundali.FullName, kundali.DateOfBirth, kundali.BirthPlace }
            into chart
            select new DhruvaChatChartDto
            {
                KundaliId = chart.Key.Id,
                FullName = chart.Key.FullName,
                DateOfBirth = chart.Key.DateOfBirth,
                BirthPlace = chart.Key.BirthPlace,
                MessageCount = chart.Count(),
                LastAskedAt = chart.Max(item => item.AskedAt),
            })
            .ToListAsync(cancellationToken);

        return rows
            .OrderByDescending(item => item.LastAskedAt)
            .ToList();
    }

    public async Task<IReadOnlyList<DhruvaChatMessageDto>> ListMessagesAsync(Guid userId, Guid kundaliId, CancellationToken cancellationToken)
    {
        var ownsChart = await _dbContext.Kundalis
            .AsNoTracking()
            .AnyAsync(item => item.Id == kundaliId && item.UserId == userId, cancellationToken);
        if (!ownsChart)
        {
            return Array.Empty<DhruvaChatMessageDto>();
        }

        return await _dbContext.DhruvaChatMessages
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.KundaliId == kundaliId)
            .OrderBy(item => item.AskedAt)
            .Select(item => new DhruvaChatMessageDto
            {
                Id = item.Id,
                Question = item.Question,
                Response = item.Response,
                AskedAt = item.AskedAt,
            })
            .ToListAsync(cancellationToken);
    }

    private static string Trim(string? value, int maxLength)
    {
        var text = (value ?? string.Empty).Trim();
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}

public sealed class DhruvaChatChartDto
{
    public Guid KundaliId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public DateOnly DateOfBirth { get; set; }

    public string BirthPlace { get; set; } = string.Empty;

    public int MessageCount { get; set; }

    public DateTimeOffset LastAskedAt { get; set; }
}

public sealed class DhruvaChatMessageDto
{
    public Guid Id { get; set; }

    public string Question { get; set; } = string.Empty;

    public string Response { get; set; } = string.Empty;

    public DateTimeOffset AskedAt { get; set; }
}

public sealed class SaveDhruvaChatRequest
{
    public string? KundaliId { get; set; }

    public string? Question { get; set; }

    public string? Response { get; set; }
}
