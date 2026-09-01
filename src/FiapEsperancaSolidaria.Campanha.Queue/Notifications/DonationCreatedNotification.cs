using Amazon.SQS;
using Amazon.SQS.Model;
using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Notifications;
using FiapEsperancaSolidaria.Campanha.Queue.Configurations.Sqs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FiapEsperancaSolidaria.Campanha.Queue.Notifications;

public class DonationCreatedNotification(
        IAmazonSQS sqsClient,
        IOptions<SqsSettings> sqsSettings,
        ILogger<DonationCreatedNotification> logger
    )
    : IDonationCreatedNotification
{
    public async Task PublishAsync(Guid donationId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[FiapEsperancaSolidaria.Campanha] Publishing donation notification to SQS: DonationId={DonationId}",
            donationId);

        try
        {
            await sqsClient.SendMessageAsync(new SendMessageRequest
            {
                QueueUrl = sqsSettings.Value.EmailQueueUrl,
                MessageBody = donationId.ToString()
            },
            cancellationToken);

            logger.LogInformation(
                "[FiapEsperancaSolidaria.Campanha] Successfully published donation notification to SQS: DonationId={DonationId}",
                donationId);

        }
        catch (Exception e)
        {
            logger.LogError(
                "[FiapEsperancaSolidaria.Campanha] Failed to publish donation notification to SQS: DonationId={DonationId} | Error: {Error}",
                donationId, e);
            throw;
        }
    }
}