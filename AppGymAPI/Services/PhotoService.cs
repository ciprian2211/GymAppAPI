using AppGymAPI.Settings;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace AppGymAPI.Services;

public class PhotoService : IPhotoService
{
    private readonly Cloudinary _cloudinary;

    public PhotoService(IOptions<CloudinarySettings> config)
    {
        var acc = new Account(
            config.Value.CloudName,
            config.Value.ApiKey,
            config.Value.ApiSecret
        );

        _cloudinary = new Cloudinary(acc);
    }

    public async Task<string?> AddPhotoAsync(IFormFile file)
    {
        if (file.Length <= 0) return null;
        
        double sizeInMb = file.Length / (1024.0 * 1024.0);
        string contentType = file.ContentType;

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg" };

        if (!allowedTypes.Contains(contentType))
        {
            return null;
        }

        if (sizeInMb > 10)
        {
            return null;
        }
        
        await using var stream = file.OpenReadStream();
        
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face"),
            Folder = "appgymapi"
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            // Log the error if needed
            return null;
        }

        return uploadResult.SecureUrl.ToString();
    }

    public async Task<bool> DeletePhotoAsync(string publicId)
    {
        var deleteParams = new DeletionParams(publicId);
        var result = await _cloudinary.DestroyAsync(deleteParams);

        return result.Result == "ok";
    }
}
