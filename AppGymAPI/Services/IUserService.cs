using AppGymAPI.DTOs;
using AppGymAPI.Models;

namespace AppGymAPI.Services;

public interface IUserService
{
    public Task<UserDto?> SearchUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    public Task<List<UserListDto>> SearchUsersByNameAsync(string name, CancellationToken cancellationToken = default);
    
    public Task<List<UserListDto>> SearchUsersByRoleAsync(Role role, CancellationToken cancellationToken = default);

    public Task<UserEditDto?> EditUserAsync(Guid id,UserEditDto dto, CancellationToken cancellationToken = default);

    public Task<bool> DeleteUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
}