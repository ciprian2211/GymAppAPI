namespace AppGymAPI.Services;

public interface IPhotoService
{
    Task<string?> AddPhotoAsync(IFormFile file);
    Task<bool> DeletePhotoAsync(string publicId);
}
