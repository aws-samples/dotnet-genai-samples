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

        builder.Services.AddSingleton(new AmazonBedrockClient(new AmazonBedrockConfig()));
        builder.Services.AddSingleton(new AmazonBedrockRuntimeClient(new AmazonBedrockRuntimeConfig()));
        builder.Services.AddSingleton(new AmazonBedrockAgentClient(new AmazonBedrockAgentConfig()));
        builder.Services.AddSingleton(new AmazonBedrockAgentRuntimeClient(new AmazonBedrockAgentRuntimeConfig()));
        builder.Services.AddSingleton(new AmazonS3Client(new AmazonS3Config()));

        ////      var chain = new CredentialProfileStoreChain();
        ////      if (chain.TryGetAWSCredentials("default", out var awsCredentials))
        ////{
        ////	builder.Services.AddSingleton(new AmazonBedrockClient(awsCredentials, new AmazonBedrockConfig()));
        ////	builder.Services.AddSingleton(new AmazonBedrockRuntimeClient(awsCredentials, new AmazonBedrockRuntimeConfig()));
        ////	builder.Services.AddSingleton(new AmazonBedrockAgentClient(awsCredentials, new AmazonBedrockAgentConfig()));
        ////	builder.Services.AddSingleton(new AmazonBedrockAgentRuntimeClient(awsCredentials, new AmazonBedrockAgentRuntimeConfig()));
        ////          builder.Services.AddSingleton(new AmazonS3Client(new AmazonS3Config()));
        ////      }
        ////      else
        ////{
        ////	builder.Services.AddSingleton(
        ////	new AmazonBedrockRuntimeClient(new AmazonBedrockRuntimeConfig()
        ////	{
        ////	//	RegionEndpoint = regionEndpoint
        ////	}));

        ////	builder.Services.AddSingleton(
        ////		new AmazonBedrockClient(new AmazonBedrockConfig()
        ////		{
        ////		//	RegionEndpoint = regionEndpoint
        ////		}));

        ////	builder.Services.AddSingleton(
        ////		new AmazonS3Client(new AmazonS3Config
        ////		{
        ////			//RegionEndpoint = regionEndpoint
        ////		}));

        ////          builder.Services.AddSingleton(
        ////              new AmazonBedrockAgentClient(awsCredentials, new AmazonBedrockAgentConfig()
        ////              {
        ////                  //RegionEndpoint = regionEndpoint
        ////              }));

        ////          builder.Services.AddSingleton(
        ////              new AmazonBedrockAgentRuntimeClient(new AmazonBedrockAgentRuntimeConfig
        ////              {
        ////                 // RegionEndpoint = regionEndpoint
        ////              }));
        //      }

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

public class AwsEnvironment
{
    public string? Account { get; set; }
    public string? Region { get; set; }
}