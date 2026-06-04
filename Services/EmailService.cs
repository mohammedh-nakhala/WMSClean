using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace WMSClean.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");

                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(emailSettings["SenderEmail"]));
                email.To.Add(MailboxAddress.Parse(to));
                email.Subject = subject;
                email.Body = new TextPart("html") { Text = body };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(
                    emailSettings["SmtpServer"],
                    int.Parse(emailSettings["SmtpPort"]),
                    SecureSocketOptions.StartTls
                );
                await smtp.AuthenticateAsync(
                    emailSettings["SenderEmail"],
                    emailSettings["SenderPassword"]
                );
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                Console.WriteLine($"✅ Email sent successfully to {to}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Email error: {ex.Message}");
                throw;
            }
        }

        public async Task SendProductReceivedEmailAsync(string to, string productName)
        {
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                    <h2 style='color: #28a745;'>✓ Product Received</h2>
                    <p>Dear Customer,</p>
                    <p>Your product <strong>{productName}</strong> has been received at our warehouse.</p>
                    <p>The product is now safely stored and will be processed according to schedule.</p>
                    <hr>
                    <small>Warehouse Management System</small>
                </div>";
            await SendEmailAsync(to, $"Product '{productName}' - Received at Warehouse", body);
        }

        public async Task SendProductExitedEmailAsync(string to, string productName)
        {
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                    <h2 style='color: #dc3545;'>📦 Product Exited</h2>
                    <p>Dear Customer,</p>
                    <p>Your product <strong>{productName}</strong> has been removed from the warehouse.</p>
                    <p>The storage period has ended. Please check your invoice for final charges.</p>
                    <hr>
                    <small>Warehouse Management System</small>
                </div>";
            await SendEmailAsync(to, $"Product '{productName}' - Exited Warehouse", body);
        }

        public async Task SendShipmentOnWayEmailAsync(string to, string productName, string trackingNumber)
        {
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                    <h2 style='color: #007bff;'>🚚 Shipment On The Way!</h2>
                    <p>Dear Customer,</p>
                    <p>Your product <strong>{productName}</strong> has been shipped and is on its way to you.</p>
                    <p><strong>Tracking Number:</strong> {trackingNumber}</p>
                    <p>You can track your shipment using this number on our website.</p>
                    <hr>
                    <small>Warehouse Management System</small>
                </div>";
            await SendEmailAsync(to, $"Shipment '{productName}' - On The Way", body);
        }
    }
}