namespace LibraryPro.Web.Services
{
    public interface IImageService
    {
        Task<string> SaveImageAsync(IFormFile imageFile, string folder);
        Task<string> OptimizeImageAsync(string imagePath, int maxWidth, int maxHeight, int quality);
        bool ValidateImage(IFormFile imageFile, out string errorMessage);
        void DeleteImage(string imagePath);
        string GetDefaultImagePath();
    }
}
