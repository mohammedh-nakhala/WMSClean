namespace  WMSClean.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendProductReceivedEmailAsync(string to, string productName);
        Task SendProductExitedEmailAsync(string to, string productName);
        Task SendShipmentOnWayEmailAsync(string to, string productName, string trackingNumber);
    }
}