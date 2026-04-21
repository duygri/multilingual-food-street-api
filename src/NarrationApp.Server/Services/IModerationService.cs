using NarrationApp.Shared.DTOs.Moderation;

namespace NarrationApp.Server.Services;

public interface IModerationService
{
    Task<ModerationRequestDto> CreateAsync(Guid requestedBy, CreateModerationRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModerationRequestDto>> GetPendingAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModerationRequestDto>> GetByRequesterAsync(Guid requestedBy, CancellationToken cancellationToken = default);

    Task<ModerationRequestDto> ReviewAsync(int requestId, Guid reviewedBy, bool approved, string? reviewNote, CancellationToken cancellationToken = default);
}
