using Microsoft.Extensions.Options;
using System.Drawing;
using System.Drawing.Imaging;

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

            // Save the file directly without optimization
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            _logger.LogInformation("Image saved successfully: {FilePath}", filePath);

            return $"/{folder}/{fileName}";
        }

        public async Task<string> OptimizeImageAsync(string imagePath, int maxWidth, int maxHeight, int quality)
        {
            try
            {
                await Task.Run(() =>
                {
                    using var image = Image.FromFile(imagePath);

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

                        var resizedImage = new Bitmap(newWidth, newHeight);
                        using (var graphics = Graphics.FromImage(resizedImage))
                        {
                            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                            graphics.DrawImage(image, 0, 0, newWidth, newHeight);
                        }

                        // Save with quality setting
                        var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                        var encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
                        resizedImage.Save(imagePath, jpegEncoder, encoderParams);
                        resizedImage.Dispose();
                    }
                    else
                    {
                        // Save with quality setting even if not resized
                        var jpegEncoder = GetEncoder(ImageFormat.Jpeg);
                        var encoderParams = new EncoderParameters(1);
                        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
                        image.Save(imagePath, jpegEncoder, encoderParams);
                    }

                    image.Dispose();
                });

                _logger.LogInformation("Image optimized successfully: {ImagePath}", imagePath);
                return imagePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing image: {ImagePath}", imagePath);
                throw;
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
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
            return "/images/default-book-cover.svg";
        }
    }

    public class ImageSettings
    {
        public int MaxFileSizeMB { get; set; } = 5;
        public List<string> AllowedExtensions { get; set; } = new List<string> { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
        public int MaxWidth { get; set; } = 1200;
        public int MaxHeight { get; set; } = 1600;
        public int Quality { get; set; } = 85;
    }
}
