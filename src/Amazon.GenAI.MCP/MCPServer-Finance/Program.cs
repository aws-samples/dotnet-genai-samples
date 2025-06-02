using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;

namespace FinanceMCPServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateEmptyApplicationBuilder(settings: null);

            builder.Services.AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

            builder.Services.AddSingleton(_ =>
            {
                var client = new HttpClient() { BaseAddress = new Uri("https://api.finance.com") };
                return client;
            });

            var app = builder.Build();
            
            // Get the HttpClient from the dependency injection container
            var client = app.Services.GetRequiredService<HttpClient>();

            await FinanceTools.GetStockHistory(client, "AAPL", DateTime.Now.AddYears(-5), DateTime.Now);



            // Call GetAlerts with the required parameters
            await FinanceTools.GetStockPrice(client, new string[] { "AAPL", "MSFT", "GOOG" });

            
            await app.RunAsync();
        }
    }
}
