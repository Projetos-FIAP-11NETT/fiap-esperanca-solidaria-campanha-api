using FiapEsperancaSolidaria.Campanha.Application.Jobs;
using Hangfire;

namespace FiapEsperancaSolidaria.Campanha.Api.Configurations.Jobs;

public static class JobsConfig
{
    public static WebApplication MapJobsConfiguration(this WebApplication app)
    {
        app.UseHangfireDashboard("/hangfire");

        RecurringJob.AddOrUpdate<UpdateCampaignStatusesJob>(
            "update-campaign-statuses",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily());

        return app;
    }
}
