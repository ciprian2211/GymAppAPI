using AppGymAPI.DTOs;
using AppGymAPI.Models;

namespace AppGymAPI.Services;

public interface IUserService
{
    public Task<UserDTO?> SearchUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    public Task<List<UserListDTO>> SearchUsersByNameAsync(string name, CancellationToken cancellationToken = default);
    
    public Task<List<UserListDTO>> SearchUsersByRoleAsync(Role role, CancellationToken cancellationToken = default);

    public Task<UserEditDTO?> EditUserAsync(Guid id,UserEditDTO dto, CancellationToken cancellationToken = default);

    public Task<bool> DeleteUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
}