using DogGo.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DogGo.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task EnviarCodigoConfirmacionAsync(string destino, string codigo)
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            mensaje.To.Add(MailboxAddress.Parse(destino));
            mensaje.Subject = "Código de confirmación - DogGo";

            mensaje.Body = new TextPart("plain")
            {
                Text = $"Tu código de confirmación es: {codigo}. Expira en 10 minutos."
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(mensaje);
            await client.DisconnectAsync(true);
        }
    }
}