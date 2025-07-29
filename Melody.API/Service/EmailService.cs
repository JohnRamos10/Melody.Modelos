using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Melody.API.Service
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task EnviarEmailAsync(string destinatario, string asunto, string contenido)
        {
            var smtpHost = _configuration["Email:SmtpHost"];
            var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            var username = _configuration["Email:Username"];
            var password = _configuration["Email:Password"];
            var fromEmail = _configuration["Email:FromEmail"];
            var fromName = _configuration["Email:FromName"];

            var smtpClient = new SmtpClient(smtpHost)
            {
                Port = smtpPort,
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = asunto,
                Body = contenido,
                IsBodyHtml = false,
            };

            mailMessage.To.Add(destinatario);

            try
            {
                _logger.LogInformation("📧 Enviando correo a: {Destinatario}", destinatario);
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("✅ Correo enviado exitosamente a: {Destinatario}", destinatario);
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "❌ Error SMTP al enviar correo a: {Destinatario}", destinatario);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error general al enviar correo a: {Destinatario}", destinatario);
                throw;
            }
        }

        public async Task EnviarEmailForgotPasswordAsync(string destinatario, string nombreUsuario, string resetLink)
        {
            var asunto = "Restablecer Contraseña - Melody Stream";
            var contenido = $@"Hola {nombreUsuario},

Recibimos una solicitud para restablecer la contraseña de tu cuenta en Melody Stream.
Para restablecer tu contraseña, haz clic en el siguiente enlace:

{resetLink}

Este enlace expirará en 30 minutos por motivos de seguridad.

Si no solicitaste este cambio, simplemente ignora este correo.

Saludos,
El equipo de Melody Stream";

            await EnviarEmailAsync(destinatario, asunto, contenido);
        }

        public async Task EnviarEmailPasswordResetConfirmationAsync(string destinatario, string nombreUsuario)
        {
            var asunto = "Contraseña Restablecida - Melody Stream";
            var contenido = $@"Hola {nombreUsuario},

Tu contraseña ha sido restablecida exitosamente.
Ya puedes iniciar sesión en Melody Stream con tu nueva contraseña.

IMPORTANTE: Si no realizaste este cambio, contacta inmediatamente con nuestro soporte.

Saludos,
El equipo de Melody Stream";

            await EnviarEmailAsync(destinatario, asunto, contenido);
        }
    }
}

