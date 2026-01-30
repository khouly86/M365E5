using Cloudativ.Assessment.Application.DTOs;

namespace Cloudativ.Assessment.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<AppUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task<AppUserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AppUserDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default);
    Task<AppUserDto> UpdateUserAsync(UpdateUserDto dto, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<bool> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<bool> ToggleUserStatusAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
}
