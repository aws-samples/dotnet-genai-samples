using Amazon;
using Amazon.Bedrock;
using Amazon.BedrockRuntime;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Markdig;
using Markdown.ColorCode;
using MudBlazor.Services;

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

if (chain.TryGetAWSCredentials("Bedrock", out awsCredentials))
{
    AmazonBedrockClient client = new AmazonBedrockClient(awsCredentials);

    builder.Services.AddSingleton<AmazonBedrockRuntimeClient>(
    new AmazonBedrockRuntimeClient(awsCredentials,new AmazonBedrockRuntimeConfig()
    {
        RegionEndpoint = RegionEndpoint.USEast1
    }));

    builder.Services.AddSingleton<AmazonBedrockClient>(
    new AmazonBedrockClient(awsCredentials, new AmazonBedrockConfig()
    {
        RegionEndpoint = RegionEndpoint.USEast1
    }));
}
else
{
    builder.Services.AddSingleton<AmazonBedrockRuntimeClient>(
    new AmazonBedrockRuntimeClient(new AmazonBedrockRuntimeConfig()
    {
        RegionEndpoint = RegionEndpoint.USEast1
    }));
    builder.Services.AddSingleton<AmazonBedrockClient>(
        new AmazonBedrockClient(new AmazonBedrockConfig()
        {
            RegionEndpoint = RegionEndpoint.USEast1
        }));
}
//builder.Services.AddSingleton<AmazonBedrockAgentRuntimeClient>(
//    new AmazonBedrockAgentRuntimeClient()
//);

//builder.Services.AddSingleton<AmazonBedrockAgentClient>();

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
