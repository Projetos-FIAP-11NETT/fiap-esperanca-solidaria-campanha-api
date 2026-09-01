using Amazon.SimpleNotificationService;
using Amazon.SQS;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Notifications;
using FiapEsperancaSolidaria.Campanha.Queue.Configurations.MassTransit;
using FiapEsperancaSolidaria.Campanha.Queue.Notifications;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FiapEsperancaSolidaria.Campanha.Queue.Configurations.Sqs;

public static class SqsStartup
{
    public static void RegisterSqsStartup(this IServiceCollection services)
    {
        services.AddMassTransit<ISqsPublish>(x =>
        {
            //x.AddConsumers(GetConsumers());

            x.SetEndpointNameFormatter(
                new KebabCaseEndpointNameFormatter("catalog", false));

            x.UsingAmazonSqs((context, cfg) =>
            {
                var sqsSettings = context.GetRequiredService<IOptions<SqsSettings>>().Value;
                var massTransitSettings = context.GetRequiredService<IOptions<MassTransitSettings>>().Value;

                cfg.Host(sqsSettings.Region, h =>
                {
                    // ServiceUrl setado = LocalStack, precisa de credenciais explicitas.
                    // Sem ServiceUrl = AWS real: resolve as credenciais do credential
                    // chain padrao do SDK (IAM role do node via IMDS) UMA UNICA VEZ aqui
                    // e compartilha entre todos os receive endpoints. Deixar cada client
                    // (um por endpoint) resolver o chain default por conta propria causa
                    // corrida concorrente no IMDS quando ha mais de um endpoint no mesmo
                    // processo, falhando com "The security token included in the request
                    // is invalid" em um deles.
                    if (!string.IsNullOrWhiteSpace(sqsSettings.ServiceUrl))
                    {
                        h.AccessKey(sqsSettings.AccessKey);
                        h.SecretKey(sqsSettings.SecretKey);

                        h.Config(new AmazonSQSConfig
                        {
                            ServiceURL = sqsSettings.ServiceUrl,
                            AuthenticationRegion = sqsSettings.Region
                        });

                        h.Config(new AmazonSimpleNotificationServiceConfig
                        {
                            ServiceURL = sqsSettings.ServiceUrl,
                            AuthenticationRegion = sqsSettings.Region
                        });
                    }
                    else
                    {
                        h.Credentials(Amazon.Runtime.FallbackCredentialsFactory.GetCredentials());
                    }
                });

                cfg.UseMessageRetry(r => r.Interval(massTransitSettings.RetryCount, massTransitSettings.Interval));

                //cfg.UseConsumeFilter(typeof(NewRelicConsumeFilter<>), context);
                //cfg.UsePublishFilter(typeof(NewRelicPublishFilter<>), context);

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddSingleton<IAmazonSQS>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<SqsSettings>>().Value;

            if (!string.IsNullOrWhiteSpace(settings.ServiceUrl))
            {
                return new AmazonSQSClient(settings.AccessKey, settings.SecretKey, new AmazonSQSConfig
                {
                    ServiceURL = settings.ServiceUrl,
                    AuthenticationRegion = settings.Region
                });
            }

            // AWS real: sem credenciais explicitas, usa a IAM role do node (LabRole) via IMDS.
            return new AmazonSQSClient(new AmazonSQSConfig
            {
                AuthenticationRegion = settings.Region
            });
        });

        services.AddScoped<IDonationCreatedNotification, DonationCreatedNotification>();
    }

    //private static Type[] GetConsumers()
    //    => AppDomain.CurrentDomain
    //        .GetAssemblies()
    //        .SelectMany(a => a.GetTypes())
    //        .Where(p => typeof(IConsumer).IsAssignableFrom(p) &&
    //                    p.Namespace != null &&
    //                    p.Namespace.Contains("FiapCloudGames.Queue.Consumers.Sqs"))
    //        .ToArray();
}