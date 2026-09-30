using System.Security.Claims;
using System.Text.Json;
using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;
using KundaliEngine.API.Services.Entitlements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KundaliEngine.API.Controllers;

public sealed class PaidPlanAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userIdValue = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var dbContext = context.HttpContext.RequestServices.GetRequiredService<KundaliDbContext>();
        var entitlements = context.HttpContext.RequestServices.GetRequiredService<IEntitlementService>();
        var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId);
        if (user == null || !user.IsActive)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!entitlements.IsPaid(user))
        {
            context.Result = new ObjectResult(new { message = "Notes are available on Premium." })
            {
                StatusCode = StatusCodes.Status402PaymentRequired
            };
            return;
        }

        await next();
    }
}

[Authorize]
[ApiController]
[PaidPlan]
[Route("api/[controller]")]
public class PortalNotesController : ControllerBase
{
    private static readonly HashSet<string> AllowedVoiceContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "audio/webm",
        "audio/ogg",
        "audio/mp4",
        "audio/mpeg",
        "audio/wav",
        "audio/x-wav"
    };

    private const int MaxContentLength = 20000;
    private const long MaxVoiceFileBytes = 12 * 1024 * 1024;
    private readonly KundaliDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public PortalNotesController(
        KundaliDbContext dbContext,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _environment = environment;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<PortalNoteDto>> Get([FromQuery] string contextType, [FromQuery] Guid? contextId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var normalizedContextType = NormalizeContextType(contextType);
        if (string.IsNullOrWhiteSpace(normalizedContextType))
        {
            return BadRequest("Valid note context is required.");
        }

        var normalizedContextId = NormalizeContextId(contextId);
        if (!await CanAccessContextAsync(userId.Value, normalizedContextType, normalizedContextId))
        {
            return NotFound();
        }

        var note = await _dbContext.PortalNotes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId.Value
                && x.ContextType == normalizedContextType
                && x.ContextId == normalizedContextId);

        return Ok(note == null
            ? new PortalNoteDto { ContextType = normalizedContextType, ContextId = DenormalizeContextId(normalizedContextId) }
            : Map(note));
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<PortalNoteDto>>> GetAll()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var notes = await _dbContext.PortalNotes
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value)
            .OrderByDescending(x => x.UpdatedAt)
            .ToListAsync();

        var voiceCounts = await _dbContext.PortalNoteVoiceRecordings
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value)
            .GroupBy(x => new { x.ContextType, x.ContextId })
            .Select(group => new
            {
                group.Key.ContextType,
                group.Key.ContextId,
                Count = group.Count()
            })
            .ToListAsync();

        var noteDtos = notes.Select(Map).ToList();
        foreach (var note in noteDtos)
        {
            var normalizedContextId = NormalizeContextId(note.ContextId);
            note.VoiceRecordingCount = voiceCounts
                .FirstOrDefault(item => item.ContextType == note.ContextType && item.ContextId == normalizedContextId)
                ?.Count ?? 0;
        }

        return Ok(noteDtos);
    }

    [HttpPut]
    public async Task<ActionResult<PortalNoteDto>> Upsert([FromBody] UpsertPortalNoteRequest request)
    {
        if (request == null)
        {
            return BadRequest("Request is required.");
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var normalizedContextType = NormalizeContextType(request.ContextType);
        if (string.IsNullOrWhiteSpace(normalizedContextType))
        {
            return BadRequest("Valid note context is required.");
        }

        if (request.Content.Length > MaxContentLength)
        {
            return BadRequest($"Notes can be up to {MaxContentLength} characters.");
        }

        var normalizedContextId = NormalizeContextId(request.ContextId);
        if (!await CanAccessContextAsync(userId.Value, normalizedContextType, normalizedContextId))
        {
            return NotFound();
        }

        var contextSnapshot = await ResolveContextSnapshotAsync(userId.Value, normalizedContextType, normalizedContextId);

        var note = await _dbContext.PortalNotes
            .FirstOrDefaultAsync(x => x.UserId == userId.Value
                && x.ContextType == normalizedContextType
                && x.ContextId == normalizedContextId);

        var now = DateTime.UtcNow;
        if (note == null)
        {
            note = new PortalNote
            {
                UserId = userId.Value,
                ContextType = normalizedContextType,
                ContextId = normalizedContextId,
                Content = request.Content.Trim(),
                ContextTitle = contextSnapshot.Title,
                ContextDetailsJson = SerializeContextDetails(contextSnapshot.Details),
                CreatedAt = now,
                UpdatedAt = now
            };
            _dbContext.PortalNotes.Add(note);
        }
        else
        {
            note.Content = request.Content.Trim();
            note.ContextTitle = contextSnapshot.Title;
            note.ContextDetailsJson = SerializeContextDetails(contextSnapshot.Details);
            note.UpdatedAt = now;
        }

        await _dbContext.SaveChangesAsync();

        return Ok(Map(note));
    }

    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] string contextType, [FromQuery] Guid? contextId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var normalizedContextType = NormalizeContextType(contextType);
        if (string.IsNullOrWhiteSpace(normalizedContextType))
        {
            return BadRequest("Valid note context is required.");
        }

        var normalizedContextId = NormalizeContextId(contextId);
        var note = await _dbContext.PortalNotes
            .FirstOrDefaultAsync(x => x.UserId == userId.Value
                && x.ContextType == normalizedContextType
                && x.ContextId == normalizedContextId);

        var voiceRecordings = await _dbContext.PortalNoteVoiceRecordings
            .Where(x => x.UserId == userId.Value
                && x.ContextType == normalizedContextType
                && x.ContextId == normalizedContextId)
            .ToListAsync();

        var storagePaths = voiceRecordings.Select(x => x.StoragePath).ToList();

        if (note != null)
        {
            _dbContext.PortalNotes.Remove(note);
        }

        if (voiceRecordings.Count > 0)
        {
            _dbContext.PortalNoteVoiceRecordings.RemoveRange(voiceRecordings);
        }

        if (note != null || voiceRecordings.Count > 0)
        {
            await _dbContext.SaveChangesAsync();
        }

        foreach (var storagePath in storagePaths)
        {
            if (System.IO.File.Exists(storagePath))
            {
                System.IO.File.Delete(storagePath);
            }
        }

        return NoContent();
    }
    [HttpGet("voice/options")]
    public ActionResult<object> GetVoiceOptions()
    {
        return Ok(new
        {
            maxDurationSeconds = GetMaxVoiceDurationSeconds(),
            maxFileBytes = MaxVoiceFileBytes
        });
    }

    [HttpGet("voice")]
    public async Task<ActionResult<List<PortalNoteVoiceRecordingDto>>> GetVoiceRecordings(
        [FromQuery] string contextType,
        [FromQuery] Guid? contextId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var normalizedContextType = NormalizeContextType(contextType);
        if (string.IsNullOrWhiteSpace(normalizedContextType))
        {
            return BadRequest("Valid note context is required.");
        }

        var normalizedContextId = NormalizeContextId(contextId);
        if (!await CanAccessContextAsync(userId.Value, normalizedContextType, normalizedContextId))
        {
            return NotFound();
        }

        var recordings = await _dbContext.PortalNoteVoiceRecordings
            .AsNoTracking()
            .Where(x => x.UserId == userId.Value
                && x.ContextType == normalizedContextType
                && x.ContextId == normalizedContextId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => MapVoiceRecording(x))
            .ToListAsync();

        return Ok(recordings);
    }
    [HttpGet("voice/{id:guid}")]
    public async Task<IActionResult> DownloadVoiceRecording(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var recording = await _dbContext.PortalNoteVoiceRecordings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId.Value);

        if (recording == null || !System.IO.File.Exists(recording.StoragePath))
        {
            return NotFound();
        }

        return PhysicalFile(recording.StoragePath, recording.ContentType, recording.FileName);
    }

    [HttpPost("voice")]
    [RequestSizeLimit(MaxVoiceFileBytes)]
    public async Task<ActionResult<PortalNoteVoiceRecordingDto>> UploadVoiceRecording(
        [FromForm] string contextType,
        [FromForm] Guid? contextId,
        [FromForm] int? durationSeconds,
        [FromForm] IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var normalizedContextType = NormalizeContextType(contextType);
        if (string.IsNullOrWhiteSpace(normalizedContextType))
        {
            return BadRequest("Valid note context is required.");
        }

        if (file == null || file.Length == 0)
        {
            return BadRequest("Voice recording is required.");
        }

        if (file.Length > MaxVoiceFileBytes)
        {
            return BadRequest($"Voice recording can be up to {MaxVoiceFileBytes} bytes.");
        }

        var contentType = NormalizeVoiceContentType(file.ContentType);
        if (!AllowedVoiceContentTypes.Contains(contentType))
        {
            return BadRequest("Unsupported voice recording format.");
        }

        var maxDurationSeconds = GetMaxVoiceDurationSeconds();
        if (durationSeconds.HasValue && durationSeconds.Value > maxDurationSeconds)
        {
            return BadRequest($"Voice recording can be up to {maxDurationSeconds} seconds.");
        }

        var normalizedContextId = NormalizeContextId(contextId);
        if (!await CanAccessContextAsync(userId.Value, normalizedContextType, normalizedContextId))
        {
            return NotFound();
        }

        var extension = GetAudioExtension(contentType, file.FileName);
        var storageDirectory = GetVoiceStorageDirectory(userId.Value, normalizedContextType, normalizedContextId);
        Directory.CreateDirectory(storageDirectory);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var storagePath = Path.Combine(storageDirectory, storedFileName);

        await using (var stream = System.IO.File.Create(storagePath))
        {
            await file.CopyToAsync(stream);
        }

        var recording = new PortalNoteVoiceRecording
        {
            UserId = userId.Value,
            ContextType = normalizedContextType,
            ContextId = normalizedContextId,
            FileName = string.IsNullOrWhiteSpace(file.FileName) ? storedFileName : Path.GetFileName(file.FileName),
            ContentType = contentType,
            StoragePath = storagePath,
            DurationSeconds = durationSeconds,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.PortalNoteVoiceRecordings.Add(recording);
        await _dbContext.SaveChangesAsync();

        return Ok(MapVoiceRecording(recording));
    }
    [HttpDelete("voice/{id:guid}")]
    public async Task<IActionResult> DeleteVoiceRecording(Guid id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized();
        }

        var recording = await _dbContext.PortalNoteVoiceRecordings
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId.Value);

        if (recording == null)
        {
            return NoContent();
        }

        var storagePath = recording.StoragePath;
        _dbContext.PortalNoteVoiceRecordings.Remove(recording);
        await _dbContext.SaveChangesAsync();

        if (System.IO.File.Exists(storagePath))
        {
            System.IO.File.Delete(storagePath);
        }

        return NoContent();
    }

    private async Task<bool> CanAccessContextAsync(Guid userId, string contextType, Guid contextId)
    {
        if (contextId == Guid.Empty)
        {
            return true;
        }

        if (string.Equals(contextType, "Kundali", StringComparison.OrdinalIgnoreCase))
        {
            return await _dbContext.Kundalis
                .AsNoTracking()
                .AnyAsync(k => k.Id == contextId && k.UserId == userId);
        }

        if (string.Equals(contextType, "Compatibility", StringComparison.OrdinalIgnoreCase))
        {
            return await _dbContext.SavedCompatibilities
                .AsNoTracking()
                .AnyAsync(item => item.Id == contextId && item.UserId == userId);
        }

        return false;
    }

    private int GetMaxVoiceDurationSeconds()
    {
        var configuredValue = _configuration.GetValue<int?>("PortalNotes:VoiceMaxDurationSeconds");
        return Math.Clamp(configuredValue.GetValueOrDefault(120), 1, 3600);
    }

    private string GetVoiceStorageDirectory(Guid userId, string contextType, Guid contextId)
    {
        var safeContextType = string.Concat(contextType.Where(char.IsLetterOrDigit));
        return Path.Combine(
            _environment.ContentRootPath,
            "App_Data",
            "PortalNoteVoiceRecordings",
            userId.ToString("N"),
            safeContextType,
            contextId.ToString("N"));
    }

    private Guid? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }

    private static string NormalizeContextType(string? contextType)
    {
        return string.IsNullOrWhiteSpace(contextType) ? string.Empty : contextType.Trim();
    }

    private static Guid NormalizeContextId(Guid? contextId)
    {
        return contextId.GetValueOrDefault(Guid.Empty);
    }

    private static Guid? DenormalizeContextId(Guid? contextId)
    {
        var value = contextId.GetValueOrDefault(Guid.Empty);
        return value == Guid.Empty ? null : value;
    }

    private async Task<(string? Title, List<string> Details)> ResolveContextSnapshotAsync(Guid userId, string contextType, Guid contextId)
    {
        if (contextId == Guid.Empty)
        {
            return ($"{contextType} page", new List<string>());
        }

        if (string.Equals(contextType, "Kundali", StringComparison.OrdinalIgnoreCase))
        {
            var kundali = await _dbContext.Kundalis
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == contextId && item.UserId == userId);

            if (kundali != null)
            {
                return (
                    kundali.FullName,
                    new List<string>
                    {
                        $"DOB {kundali.DateOfBirth:dd MMM yyyy}",
                        $"Time {kundali.TimeOfBirth:hh\\:mm}",
                        kundali.BirthPlace
                    }.Where(item => !string.IsNullOrWhiteSpace(item)).ToList());
            }
        }

        if (string.Equals(contextType, "Compatibility", StringComparison.OrdinalIgnoreCase))
        {
            var compatibility = await _dbContext.SavedCompatibilities
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.Id == contextId && item.UserId == userId);

            if (compatibility != null)
            {
                return (
                    $"{compatibility.PersonAName} & {compatibility.PersonBName}",
                    new List<string>
                    {
                        $"{compatibility.PersonAName}: {compatibility.PersonADateOfBirth:dd MMM yyyy}, {compatibility.PersonATimeOfBirth:hh\\:mm}",
                        $"{compatibility.PersonBName}: {compatibility.PersonBDateOfBirth:dd MMM yyyy}, {compatibility.PersonBTimeOfBirth:hh\\:mm}"
                    });
            }
        }

        return ($"{contextType} note", new List<string>());
    }

    private static string? SerializeContextDetails(List<string> details)
    {
        return details.Count == 0 ? null : JsonSerializer.Serialize(details);
    }

    private static List<string> DeserializeContextDetails(string? detailsJson)
    {
        if (string.IsNullOrWhiteSpace(detailsJson))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(detailsJson) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
    private static string NormalizeVoiceContentType(string? contentType)
    {
        var value = (contentType ?? string.Empty).Split(';')[0].Trim();
        return value.Equals("video/webm", StringComparison.OrdinalIgnoreCase) ? "audio/webm" : value;
    }

    private static string GetAudioExtension(string contentType, string fileName)
    {
        var extension = Path.GetExtension(fileName);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension;
        }

        return contentType.ToLowerInvariant() switch
        {
            "audio/ogg" => ".ogg",
            "audio/mp4" => ".m4a",
            "audio/mpeg" => ".mp3",
            "audio/wav" or "audio/x-wav" => ".wav",
            _ => ".webm"
        };
    }

    private static PortalNoteDto Map(PortalNote note)
    {
        return new PortalNoteDto
        {
            Id = note.Id,
            ContextType = note.ContextType,
            ContextId = DenormalizeContextId(note.ContextId),
            Content = note.Content,
            ContextTitle = note.ContextTitle,
            ContextDetails = DeserializeContextDetails(note.ContextDetailsJson),
            CreatedAt = note.CreatedAt,
            UpdatedAt = note.UpdatedAt
        };
    }

    private static PortalNoteVoiceRecordingDto MapVoiceRecording(PortalNoteVoiceRecording recording)
    {
        return new PortalNoteVoiceRecordingDto
        {
            Id = recording.Id,
            ContextType = recording.ContextType,
            ContextId = DenormalizeContextId(recording.ContextId),
            FileName = recording.FileName,
            ContentType = recording.ContentType,
            DurationSeconds = recording.DurationSeconds,
            CreatedAt = recording.CreatedAt
        };
    }
}




