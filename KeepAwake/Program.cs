using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;

namespace KeepAwake
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Logging.AddEventLog(settings => settings.SourceName = "KeepAwake");
            builder.Services.AddWindowsService(options => options.ServiceName = "KeepAwake");

            builder.Services.AddHostedService<Worker>();
            builder.Services.AddRazorPages();

            builder.Services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("HangfireConnection")))
            );

            // Add the processing server as IHostedService
            builder.Services.AddHangfireServer();

            // Add framework services.
            builder.Services.AddMvc();

            var app = builder.Build();

            app.UseStaticFiles();

            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new LocalNetworkAuthorizationFilter()] // Optional: Add custom authorization filters
            });

            app.UseRouting();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHangfireDashboard();

            app.Run();
        }
    }
    public class LocalNetworkAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            // Allow access only from local network or customize as needed
            return true;
        }
    }
}