using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace WeatherMCPServer
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
                var client = new HttpClient() { BaseAddress = new Uri("https://api.weather.gov") };
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("weather-tool", "1.0"));
                return client;
            });

            var app = builder.Build();
            
            // Get the HttpClient from the dependency injection container
            var client = app.Services.GetRequiredService<HttpClient>();

            
            await app.RunAsync();
        }
    }
}
