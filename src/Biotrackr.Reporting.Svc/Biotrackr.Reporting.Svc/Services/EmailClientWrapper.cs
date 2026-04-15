using System.Diagnostics.CodeAnalysis;
using Azure;
using Azure.Communication.Email;
using Biotrackr.Reporting.Svc.Services.Interfaces;

namespace Biotrackr.Reporting.Svc.Services;

[ExcludeFromCodeCoverage]
public class EmailClientWrapper : IEmailClientWrapper
{
    private readonly EmailClient _emailClient;

    public EmailClientWrapper(EmailClient emailClient)
    {
        _emailClient = emailClient;
    }

    public async Task<EmailSendStatus> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var operation = await _emailClient.SendAsync(WaitUntil.Completed, message, cancellationToken);
        return operation.Value.Status;
    }
}
