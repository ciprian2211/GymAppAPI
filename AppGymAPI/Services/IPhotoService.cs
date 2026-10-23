namespace AppGymAPI.Services;

public interface IPhotoService
{
    Task<string?> AddPhotoAsync(IFormFile file);
    Task<string?> AddGifAsync(IFormFile file);
    Task<bool> DeletePhotoAsync(string publicId);
}
