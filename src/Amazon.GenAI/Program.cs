using Amazon.Bedrock;
using Amazon.BedrockAgent;
using Amazon.BedrockAgentRuntime;
using Amazon.BedrockRuntime;
using Amazon.GenAI.Abstractions;
using Amazon.S3;
using Markdig;
using Markdown.ColorCode;
using MudBlazor.Services;

internal class Program
{
	private static void Main(string[] args)
    {
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddRazorPages();
		builder.Services.AddServerSideBlazor();

		builder.Services.AddMudServices();

		var pipeline = new MarkdownPipelineBuilder()
			.UseAdvancedExtensions()
			.UseColorCode()
			.Build();

		builder.Services.AddSingleton(pipeline);

        builder.Services.AddSingleton(new AmazonBedrockClient());
        builder.Services.AddSingleton(new AmazonBedrockRuntimeClient());
        builder.Services.AddSingleton(new AmazonBedrockAgentClient());
        builder.Services.AddSingleton(new AmazonBedrockAgentRuntimeClient());
        builder.Services.AddSingleton(new AmazonS3Client());

        builder.Services.AddSingleton<BrowserService>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Configure the HTTP request pipeline.
		if (!app.Environment.IsDevelopment())
		{
			app.UseExceptionHandler("/Error");
			// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
			app.UseHsts();
		}

		app.UseHttpsRedirection();

		app.UseStaticFiles();

		app.UseRouting();

		app.MapBlazorHub();
		app.MapFallbackToPage("/_Host");

		app.Run();
	}
}