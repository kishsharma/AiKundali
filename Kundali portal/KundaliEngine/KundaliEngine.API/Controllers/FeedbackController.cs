using System.Security.Claims;
using KundaliEngine.API.DTOs.Feedback;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Authorize]
[Route("api/feedback")]
public class FeedbackController : ControllerBase
{
    private const int NoteMaxLength = 400;

    private static readonly Dictionary<string, string[]> Topics = new(StringComparer.Ordinal)
    {
        ["dhruva"] = [],
        ["kundali"] = ["chart", "prediction", "layout"],
        ["compatibility"] = ["score", "explanation", "layout"],
        ["report"] = ["content", "delivery"],
        ["general"] = ["chart", "match", "dhruva", "other"],
    };

    private readonly KundaliDbContext _dbContext;
    private readonly IEntitlementService _entitlementService;

    public FeedbackController(KundaliDbContext dbContext, IEntitlementService entitlementService)
    {
        _dbContext = dbContext;
        _entitlementService = entitlementService;
    }

    [HttpGet("prompt")]
    public async Task<IActionResult> Prompt([FromQuery] string surface, [FromQuery] string contextKey)
    {
        if (!TryNormalize(surface, contextKey, "useful", null, null, out var prompt, out var error))
        {
            return BadRequest(new { message = error });
        }

        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized();
        }

        var alreadyGiven = await AlreadyGivenAsync(user.Id, prompt.Surface, prompt.ContextKey);
        return Ok(new { show = !alreadyGiven });
    }

    [HttpPost]
    public async Task<IActionResult> Submit(SubmitProductFeedbackRequest request)
    {
        if (!TryNormalize(request.Surface, request.ContextKey, request.Sentiment, request.Topic, request.Note, out var prompt, out var error))
        {
            return BadRequest(new { message = error });
        }

        var user = await GetCurrentUserAsync();
        if (user is null)
        {
            return Unauthorized();
        }

        if (await AlreadyGivenAsync(user.Id, prompt.Surface, prompt.ContextKey))
        {
            return Ok(new { saved = false, alreadyGiven = true });
        }

        var snapshot = _entitlementService.GetSnapshot(user);
        _dbContext.ProductFeedback.Add(new ProductFeedback
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Surface = prompt.Surface,
            ContextKey = prompt.ContextKey,
            Sentiment = prompt.Sentiment,
            Topic = prompt.Topic,
            Note = prompt.Note,
            PlanCode = snapshot.PlanCode,
            FeedbackDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CreatedAt = DateTime.UtcNow
        });

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Ok(new { saved = false, alreadyGiven = true });
        }

        return Ok(new { saved = true, alreadyGiven = false });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var rows = await (
            from feedback in _dbContext.ProductFeedback.AsNoTracking()
            join user in _dbContext.Users.AsNoTracking() on feedback.UserId equals user.Id
            orderby feedback.CreatedAt descending
            select new
            {
                id = feedback.Id,
                fullName = user.FullName,
                email = user.Email,
                surface = feedback.Surface,
                contextKey = feedback.ContextKey,
                sentiment = feedback.Sentiment,
                topic = feedback.Topic,
                note = feedback.Note,
                planCode = feedback.PlanCode,
                createdAt = feedback.CreatedAt
            })
            .Take(100)
            .ToListAsync();

        return Ok(rows);
    }

    private async Task<bool> AlreadyGivenAsync(Guid userId, string surface, string contextKey)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await _dbContext.ProductFeedback.AnyAsync(item =>
            item.UserId == userId &&
            item.Surface == surface &&
            item.ContextKey == contextKey &&
            item.FeedbackDate == today);
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return null;
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == parsedUserId);
        return user is null || !user.IsActive ? null : user;
    }

    private static bool TryNormalize(
        string? surface,
        string? contextKey,
        string? sentiment,
        string? topic,
        string? note,
        out NormalizedFeedback prompt,
        out string error)
    {
        prompt = new NormalizedFeedback(string.Empty, string.Empty, string.Empty, null, null);
        var normalizedSurface = (surface ?? string.Empty).Trim().ToLowerInvariant();
        if (!Topics.ContainsKey(normalizedSurface))
        {
            error = "Choose a feedback screen.";
            return false;
        }

        var normalizedContext = (contextKey ?? string.Empty).Trim();
        if (normalizedContext.Length is 0 or > 200)
        {
            error = "This result cannot take feedback yet.";
            return false;
        }

        var normalizedSentiment = (sentiment ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedSentiment is not ("useful" or "needs_work"))
        {
            error = "Choose useful or needs work.";
            return false;
        }

        string? normalizedTopic = string.IsNullOrWhiteSpace(topic) ? null : topic.Trim().ToLowerInvariant();
        if (normalizedTopic is not null && !Topics[normalizedSurface].Contains(normalizedTopic, StringComparer.Ordinal))
        {
            normalizedTopic = null;
        }

        var normalizedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (normalizedNote is { Length: > NoteMaxLength })
        {
            error = "Keep the note under 400 characters.";
            return false;
        }

        prompt = new NormalizedFeedback(normalizedSurface, normalizedContext, normalizedSentiment, normalizedTopic, normalizedNote);
        error = string.Empty;
        return true;
    }

    private sealed record NormalizedFeedback(string Surface, string ContextKey, string Sentiment, string? Topic, string? Note);
}
