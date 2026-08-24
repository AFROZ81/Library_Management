using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace LibraryPro.Web.Services
{
    public class ImageService : IImageService
    {
        private readonly ImageSettings _imageSettings;
        private readonly ILogger<ImageService> _logger;
        private readonly string _webRootPath;

        public ImageService(
            IOptions<ImageSettings> imageSettings,
            ILogger<ImageService> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _imageSettings = imageSettings.Value;
            _logger = logger;
            _webRootPath = webHostEnvironment.WebRootPath;
        }

        public bool ValidateImage(IFormFile imageFile, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (imageFile == null || imageFile.Length == 0)
            {
                errorMessage = "No file selected.";
                return false;
            }

            // Check file size
            if (imageFile.Length > _imageSettings.MaxFileSizeMB * 1024 * 1024)
            {
                errorMessage = $"File size exceeds maximum allowed size of {_imageSettings.MaxFileSizeMB}MB.";
                return false;
            }

            // Check file extension
            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!_imageSettings.AllowedExtensions.Contains(extension))
            {
                errorMessage = $"File type not allowed. Allowed types: {string.Join(", ", _imageSettings.AllowedExtensions)}";
                return false;
            }

            // Check if it's actually an image
            if (!imageFile.ContentType.StartsWith("image/"))
            {
                errorMessage = "File is not a valid image.";
                return false;
            }

            return true;
        }

        public async Task<string> SaveImageAsync(IFormFile imageFile, string folder)
        {
            if (!ValidateImage(imageFile, out var errorMessage))
            {
                throw new ArgumentException(errorMessage);
            }

            // Create folder if it doesn't exist
            var uploadPath = Path.Combine(_webRootPath, folder);
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            var filePath = Path.Combine(uploadPath, fileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            _logger.LogInformation("Image saved successfully: {FilePath}", filePath);

            // Optimize the image
            await OptimizeImageAsync(filePath, _imageSettings.MaxWidth, _imageSettings.MaxHeight, _imageSettings.Quality);

            return $"/{folder}/{fileName}";
        }

        public async Task<string> OptimizeImageAsync(string imagePath, int maxWidth, int maxHeight, int quality)
        {
            try
            {
                using var image = await Image.LoadAsync(imagePath);

                // Calculate new dimensions maintaining aspect ratio
                int newWidth, newHeight;
                double aspectRatio = (double)image.Width / image.Height;

                if (image.Width > maxWidth || image.Height > maxHeight)
                {
                    if (aspectRatio > 1)
                    {
                        newWidth = maxWidth;
                        newHeight = (int)(maxWidth / aspectRatio);
                    }
                    else
                    {
                        newHeight = maxHeight;
                        newWidth = (int)(maxHeight * aspectRatio);
                    }

                    image.Mutate(x => x.Resize(new ResizeOptions
                    {
                        Size = new Size(newWidth, newHeight),
                        Mode = ResizeMode.Max
                    }));
                }

                // Save with quality setting
                var encoder = new JpegEncoder { Quality = quality };
                await image.SaveAsync(imagePath, encoder);

                _logger.LogInformation("Image optimized successfully: {ImagePath}", imagePath);
                return imagePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing image: {ImagePath}", imagePath);
                throw;
            }
        }

        public void DeleteImage(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return;

            var fullPath = Path.Combine(_webRootPath, imagePath.TrimStart('/'));
            
            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    System.IO.File.Delete(fullPath);
                    _logger.LogInformation("Image deleted successfully: {ImagePath}", fullPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting image: {ImagePath}", fullPath);
                }
            }
        }

        public string GetDefaultImagePath()
        {
            return "/images/default-book-cover.jpg";
        }
    }

    public class ImageSettings
    {
        public int MaxFileSizeMB { get; set; } = 5;
        public List<string> AllowedExtensions { get; set; } = new List<string> { ".jpg", ".jpeg", ".png", ".webp" };
        public int MaxWidth { get; set; } = 1200;
        public int MaxHeight { get; set; } = 1600;
        public int Quality { get; set; } = 85;
    }
}
