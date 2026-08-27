using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace LibraryPro.Web.Services;

public class QRCodeService : IQRCodeService
{
    private readonly ILogger<QRCodeService> _logger;

    public QRCodeService(ILogger<QRCodeService> logger)
    {
        _logger = logger;
    }

    public byte[] GenerateQRCode(string content)
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);
            
            return qrCodeBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code for content: {Content}", content);
            throw;
        }
    }
}
