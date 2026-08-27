namespace LibraryPro.Web.Services;

public interface IBarcodeService
{
    byte[] GenerateBarcode(string content);
    string GenerateBarcodeText(string content);
}
