using KundaliEngine.API.Services.ReportShare;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace KundaliEngine.API.Services.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly ReportShareSmtpOptions _options;

    public SmtpEmailSender(IOptions<ReportShareSmtpOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var smtpPassword = GetSmtpPassword();
        EnsureConfigured(smtpPassword);

        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(new MailboxAddress(_options.FromName, GetFromEmail()));
        mimeMessage.To.Add(MailboxAddress.Parse(message.ToEmail.Trim()));
        mimeMessage.Subject = message.Subject.Trim();
        var bodyBuilder = new BodyBuilder
        {
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody
        };
        //var body = new TextPart("plain")
        //{
        //    Text = message.TextBody.Trim()
        //};

        if (message.Attachments.Count == 0)
        {
            mimeMessage.Body = bodyBuilder.ToMessageBody();
        }
        else
        {
            var multipart = new Multipart("mixed") { bodyBuilder.ToMessageBody() };
            foreach (var attachment in message.Attachments)
            {
                if (attachment.Bytes.Length > _options.MaxAttachmentBytes)
                {
                    throw new InvalidOperationException("Email attachment is too large to send.");
                }

                multipart.Add(new MimePart(attachment.ContentType)
                {
                    Content = new MimeContent(new MemoryStream(attachment.Bytes)),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = SanitizeAttachmentFileName(attachment.FileName)
                });
            }

            mimeMessage.Body = multipart;
        }

        var timeoutSeconds = Math.Clamp(_options.TimeoutSeconds, 5, 120);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        using var client = new SmtpClient
        {
            Timeout = timeoutSeconds * 1000
        };

        await client.ConnectAsync(_options.Host, _options.Port, GetSecureSocketOptions(), timeoutCts.Token);
        await client.AuthenticateAsync(_options.UserName, smtpPassword, timeoutCts.Token);
        await client.SendAsync(mimeMessage, timeoutCts.Token);
        await client.DisconnectAsync(true, timeoutCts.Token);
    }

    private void EnsureConfigured(string smtpPassword)
    {
        if (string.IsNullOrWhiteSpace(_options.Host)
            || string.IsNullOrWhiteSpace(_options.UserName)
            || string.IsNullOrWhiteSpace(smtpPassword))
        {
            throw new ReportShareConfigurationException("Email SMTP settings are not configured.");
        }
    }

    private string GetSmtpPassword()
    {
        if (!string.IsNullOrWhiteSpace(_options.EncryptedPassword))
        {
            var key = Environment.GetEnvironmentVariable(_options.PasswordKeyEnvironmentVariable);
            return EncryptedSecretProtector.Decrypt(_options.EncryptedPassword, key ?? string.Empty);
        }

        return _options.Password;
    }

    private string GetFromEmail()
    {
        return string.IsNullOrWhiteSpace(_options.FromEmail)
            ? _options.UserName
            : _options.FromEmail;
    }

    private SecureSocketOptions GetSecureSocketOptions()
    {
        if (!_options.EnableSsl)
        {
            return SecureSocketOptions.None;
        }

        return _options.Port == 465
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
    }

    private static string SanitizeAttachmentFileName(string fileName)
    {
        var sanitized = string.Join("_", fileName.Trim().Split(Path.GetInvalidFileNameChars()));
        return string.IsNullOrWhiteSpace(sanitized) ? "attachment" : sanitized;
    }
}
