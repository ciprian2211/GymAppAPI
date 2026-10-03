using AppGymAPI.Data;
using AppGymAPI.DTOs;
using AppGymAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AppGymAPI.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _appDbContext;
    private readonly IPhotoService _photoService;

    public UserService(AppDbContext appDbContext, IPhotoService photoService)
    {
        _appDbContext = appDbContext;
        _photoService = photoService;
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

    public async Task<List<UserListDTO>> SearchUserByNameAsync(string name, CancellationToken cancellationToken = default)
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

    public async Task<UserEditDTO?> EditUserAsync(UserEditDTO dto, CancellationToken cancellationToken = default)
    {
        var user = await _appDbContext.Users
            .FirstOrDefaultAsync(u => u.Id == dto.Id, cancellationToken);
        
        if (user is null) return null;

        // NOTE: Handled race condition: if you use raw RowVersion or Concurrency Tokens, 
        // a DbUpdateConcurrencyException might be thrown here if another request updated the user simultaneously.
        user.Name = dto.Name;
        user.Surname = dto.Surname;
        user.Description = dto.Description;
        
        if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
        {
            // 1. Delete the old photo if it exists
            if (!string.IsNullOrEmpty(user.ImageUrl))
            {
                try 
                {
                    // Extract PublicId from the Cloudinary URL (assuming folder 'appgymapi')
                    var uri = new Uri(user.ImageUrl);
                    var fileName = System.IO.Path.GetFileNameWithoutExtension(uri.LocalPath);
                    var publicId = $"appgymapi/{fileName}";
                    await _photoService.DeletePhotoAsync(publicId);
                }
                catch (Exception)
                {
                    // Log error if deletion fails, but don't stop the upload process
                }
            }

            // 2. Upload the new photo
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
        catch (DbUpdateConcurrencyException)
        {
            // Handle concurrency (e.g. log, throw custom error, or retry)
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
        catch (DbUpdateConcurrencyException)
        {
            // Handle concurrency exception: The user might have been deleted by another thread simultaneously.
            return false; 
        }
        
        return true;
    }
}