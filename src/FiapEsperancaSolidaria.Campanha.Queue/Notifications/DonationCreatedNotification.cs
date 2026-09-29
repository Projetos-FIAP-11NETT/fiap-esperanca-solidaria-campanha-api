using Amazon.SQS;
using Amazon.SQS.Model;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Notifications;
using FiapEsperancaSolidaria.Campanha.Observability.Correlation;
using FiapEsperancaSolidaria.Campanha.Queue.Configurations.Sqs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FiapEsperancaSolidaria.Campanha.Queue.Notifications;

public class DonationCreatedNotification(
        IAmazonSQS sqsClient,
        IOptions<SqsSettings> sqsSettings,
        ILogger<DonationCreatedNotification> logger,
        ICorrelationIdAccessor correlation
    )
    : IDonationCreatedNotification
{
    public async Task PublishAsync(Guid donationId, CancellationToken cancellationToken = default)
    {
        string messageBody;

        try
        {
            messageBody = JsonSerializer.Serialize(new
            {
                DonationId = donationId,
                CorrelationId = correlation.CorrelationId.ToString()
            });
        }
        catch (Exception e)
        {
            logger.LogInformation(
                "[FiapEsperancaSolidaria.Campanha] Failed to serialize donation notification: DonationId={DonationId} | Error: {Error}",
                donationId, e);
            throw;
        }

        logger.LogInformation(
            "[FiapEsperancaSolidaria.Campanha] Publishing donation notification to SQS: MessageBody={MessageBody}",
            messageBody);

        try
        {
            await sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = sqsSettings.Value.EmailQueueUrl,
                MessageBody = messageBody
            },
            cancellationToken);

            logger.LogInformation(
                "[FiapEsperancaSolidaria.Campanha] Successfully published donation notification to SQS: MessageBody={MessageBody}",
                messageBody);

        }
        catch (Exception e)
        {
            logger.LogError(
                "[FiapEsperancaSolidaria.Campanha] Failed to publish donation notification to SQS: MessageBody={MessageBody} | Error: {Error}",
                messageBody, e);
            throw;
        }
    }
}