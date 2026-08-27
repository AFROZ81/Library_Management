using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using System.Drawing;
using System.Drawing.Imaging;

namespace LibraryPro.Web.Services;

public class BarcodeService : IBarcodeService
{
    private readonly ILogger<BarcodeService> _logger;

    public BarcodeService(ILogger<BarcodeService> logger)
    {
        _logger = logger;
    }

    public byte[] GenerateBarcode(string content)
    {
        try
        {
            var writer = new BarcodeWriter<Bitmap>
            {
                Format = BarcodeFormat.CODE_128,
                Options = new EncodingOptions
                {
                    Width = 300,
                    Height = 100,
                    Margin = 10
                }
            };

            using var bitmap = writer.Write(content);
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating barcode for content: {Content}", content);
            throw;
        }
    }

    public string GenerateBarcodeText(string content)
    {
        // Generate a unique barcode based on content
        // For books, we'll use ISBN or a generated ID
        if (string.IsNullOrEmpty(content))
        {
            content = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
        }
        return content.ToUpper();
    }
}
