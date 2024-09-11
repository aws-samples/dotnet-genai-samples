using Amazon;
using Amazon.Bedrock;
using Amazon.BedrockAgent;
using Amazon.BedrockAgentRuntime;
using Amazon.BedrockRuntime;
using Amazon.GenAI;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
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



		AWSCredentials awsCredentials;
		var chain = new CredentialProfileStoreChain();
        var regionEndpoint = RegionEndpoint.GetBySystemName(Constants.Region);

        if (chain.TryGetAWSCredentials("KBUser", out awsCredentials))
		{
			var client = new AmazonBedrockClient(awsCredentials);
			//var regionEndpoint = awsCredentials.GetCredentials().Token;
			builder.Services.AddSingleton(
			new AmazonBedrockRuntimeClient(awsCredentials, new AmazonBedrockRuntimeConfig()
			{
				RegionEndpoint = regionEndpoint
			}));

			builder.Services.AddSingleton(
			new AmazonBedrockClient(awsCredentials, new AmazonBedrockConfig()
			{
				//RegionEndpoint = regionEndpoint
			}));

			builder.Services.AddSingleton(
			new AmazonS3Client(new AmazonS3Config
			{
				//RegionEndpoint = regionEndpoint
			}));

			builder.Services.AddSingleton(
			new AmazonBedrockAgentClient(awsCredentials, new AmazonBedrockAgentConfig()
			{
				RegionEndpoint = regionEndpoint
			}));

			builder.Services.AddSingleton(
			new AmazonBedrockAgentRuntimeClient(awsCredentials, new AmazonBedrockAgentRuntimeConfig
			{
				RegionEndpoint = regionEndpoint
			}));
		}
		else
		{
			builder.Services.AddSingleton(
			new AmazonBedrockRuntimeClient(new AmazonBedrockRuntimeConfig()
			{
			//	RegionEndpoint = regionEndpoint
			}));
			builder.Services.AddSingleton(
				new AmazonBedrockClient(new AmazonBedrockConfig()
				{
				//	RegionEndpoint = regionEndpoint
				}));

			builder.Services.AddSingleton(
				new AmazonS3Client(new AmazonS3Config
				{
					//RegionEndpoint = regionEndpoint
				}));
		}

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

#if DEBUG

#endif
