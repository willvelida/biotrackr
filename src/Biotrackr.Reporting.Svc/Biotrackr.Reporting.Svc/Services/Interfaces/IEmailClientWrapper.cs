using Azure.Communication.Email;

namespace Biotrackr.Reporting.Svc.Services.Interfaces;

public interface IEmailClientWrapper
{
    Task<EmailSendStatus> SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
