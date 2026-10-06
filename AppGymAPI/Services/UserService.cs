using AppGymAPI.Data;
using AppGymAPI.DTOs;
using AppGymAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AppGymAPI.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _appDbContext;
    private readonly IPhotoService _photoService;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext appDbContext, IPhotoService photoService, ILogger<UserService> logger)
    {
        _appDbContext = appDbContext;
        _photoService = photoService;
        _logger = logger;
    }

    public async Task<UserDTO?> SearchUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _appDbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            
        if (user is null) return null;

        return new UserDTO
        {
            Id = user.Id,
            Name = user.Name,
            Surname = user.Surname,
            Email = user.Email ?? string.Empty,
            Role = user.Role
        };
    }

    public async Task<List<UserListDTO>> SearchUsersByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Users
            .AsNoTracking()
            .Where(u => u.Name.Contains(name))
            .Select(u => new UserListDTO
            {
                Name = u.Name,
                Surname = u.Surname,
                Email = u.Email ?? string.Empty,
                ReviewStars = u.ReviewStars
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<UserListDTO>> SearchUsersByRoleAsync(Role role, CancellationToken cancellationToken = default)
    {
        return await _appDbContext.Users
            .AsNoTracking()
            .Where(u => u.Role == role)
            .Select(u => new UserListDTO
            {
                Name = u.Name,
                Surname = u.Surname,
                Email = u.Email ?? string.Empty,
                ReviewStars = u.ReviewStars
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<UserEditDTO?> EditUserAsync(Guid id,UserEditDTO dto, CancellationToken cancellationToken = default)
    {
        var user = await _appDbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        
        if (user is null) return null;
        
        user.Name = dto.Name;
        user.Surname = dto.Surname;
        user.Description = dto.Description;
        
        if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
        {
         
            if (!string.IsNullOrEmpty(user.ImageUrl))
            {
                try 
                {
                   
                    var uri = new Uri(user.ImageUrl);
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(uri.LocalPath);
                    var publicId = $"appgymapi/{fileName}";
                    await _photoService.DeletePhotoAsync(publicId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old image {ImageUrl} for user {UserId}", user.ImageUrl, id);
                }
            }

       
            var imageUrl = await _photoService.AddPhotoAsync(dto.ProfileImage);
            if (!string.IsNullOrEmpty(imageUrl))
            {
                user.ImageUrl = imageUrl;
            }
        }
        
        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error while updating user {UserId}", id);
            throw;
        }
        
        return dto;
    }

    public async Task<bool> DeleteUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _appDbContext.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            
        if (user is null) return false;

        _appDbContext.Users.Remove(user);
        
        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error while deleting user {UserId}", id);
            return false; 
        }
        
        return true;
    }
}