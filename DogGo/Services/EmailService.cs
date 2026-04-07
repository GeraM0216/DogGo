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

        public async Task EnviarCorreoAsync(string destino, string asunto, string contenidoHtml)
        {
            var mensaje = new MimeMessage();
            mensaje.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            mensaje.To.Add(MailboxAddress.Parse(destino));
            mensaje.Subject = asunto;

            mensaje.Body = new TextPart("html")
            {
                Text = contenidoHtml
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.Username, _settings.Password);
            await client.SendAsync(mensaje);
            await client.DisconnectAsync(true);
        }

        public async Task EnviarCodigoConfirmacionAsync(string destino, string codigo)
        {
            var contenido = $@"
                <div style='font-family:Arial,sans-serif; padding:20px; color:#222;'>
                    <h2>Bienvenido a DogGo 🐾</h2>
                    <p>Gracias por registrarte.</p>
                    <p>Tu código de confirmación es:</p>
                    <div style='font-size:32px; font-weight:bold; letter-spacing:4px; margin:20px 0;'>
                        {codigo}
                    </div>
                    <p>Este código expira en 10 minutos.</p>
                    <p>Si tú no solicitaste este registro, puedes ignorar este correo.</p>
                    <hr />
                    <p style='color:#666;'>Equipo DogGo</p>
                </div>";

            await EnviarCorreoAsync(destino, "Código de confirmación - DogGo", contenido);
        }
    }
}