using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net.Http.Headers;

namespace WeatherMCPServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateEmptyApplicationBuilder(settings: null);

            // WithToolsFromAssembly() will scan the assembly for classes with the McpServerToolType attribute and register all methods with the McpServerTool attribute
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