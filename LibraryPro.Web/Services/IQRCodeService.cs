namespace LibraryPro.Web.Services;

public interface IQRCodeService
{
    byte[] GenerateQRCode(string content);
}
