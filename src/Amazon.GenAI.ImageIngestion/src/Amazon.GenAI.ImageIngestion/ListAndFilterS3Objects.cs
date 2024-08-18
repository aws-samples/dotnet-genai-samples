using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.GenAI.ImageIngestion.Abstractions;
using Amazon.S3;
using Amazon.S3.Model;
using S3Object = Amazon.GenAI.ImageIngestion.Abstractions.S3Object;

namespace Amazon.GenAI.ImageIngestion;

public class ListAndFilterS3Objects
{
	private readonly string? _stateMachineArn = Environment.GetEnvironmentVariable("STATE_MACHINE_ARN");
	private readonly string? _tableName = Environment.GetEnvironmentVariable("DYNAMODB_TABLE_NAME");
	private readonly string? _destinationBucket = Environment.GetEnvironmentVariable("DESTINATION_BUCKET");
	private readonly IAmazonStepFunctions _stepFunctionsClient = new AmazonStepFunctionsClient();
	private readonly AmazonDynamoDBClient _dynamoDbClient = new();
	private readonly AmazonS3Client _s3Client = new();
	private ILambdaContext? _context;

	public async Task FunctionHandler(CloudWatchEvent<S3Detail> input, ILambdaContext context)
	{
		_context = context;

		_context.Logger.LogInformation($"####  in ListAndFilterS3Objects ------------");

		_context.Logger.LogInformation($"input.Detail.BucketName: {input.Detail?.BucketName}");
		_context.Logger.LogInformation($"_destinationBucket: {_destinationBucket}");

		string? bucketName = input.Detail?.BucketName ?? _destinationBucket;
		_context.Logger.LogInformation($"bucketName: {bucketName}");

		var key = input.Detail?.Object?.Key;
		_context.Logger.LogInformation($"key: {key}");

		try
		{
			if (key != null && (key.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
								key.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
			{
				var exists = await DoesKeyExistInTable(key, bucketName);
				if (exists == false)
				{
					_context.Logger.LogError($" --- starting State Machine");
					await StartStateMachineExecution(bucketName, key);
				}
			}
			else
			{
				var request = new ListObjectsV2Request { BucketName = bucketName };
				var response = await _s3Client.ListObjectsV2Async(request);

				do
				{
					foreach (var item in response.S3Objects)
					{
						if (item.Key.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
						    item.Key.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
						    item.Key.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
						{
							var exists = await DoesKeyExistInTable(item.Key, _destinationBucket);
							if (exists) continue;

							_context.Logger.LogError($" --- starting State Machine");
							await StartStateMachineExecution(bucketName, item.Key);
						}
					}

					request.ContinuationToken = response.NextContinuationToken;
				} while (!string.IsNullOrEmpty(request.ContinuationToken));
			}
		}
		catch (Exception e)
		{
			_context.Logger.LogError($"Error getting inference: {e.Message}");
			throw;
		}
	}

	private async Task<bool> DoesKeyExistInTable(string? key, string? bucketName)
	{
		_context?.Logger.LogInformation($"searching for... {key} / {bucketName}");

		var table = Table.LoadTable(_dynamoDbClient, _tableName);

		var find = new Dictionary<string, DynamoDBEntry>
		{
			{ "key", key },
			{ "bucketName", bucketName }
		};

		var document = await table.GetItemAsync(find);
		_context?.Logger.LogInformation($"document found: {document != null}");

		return document != null;
	}

	private async Task StartStateMachineExecution(string? bucketName, string key)
	{
		var input = new S3Object
		{
			Detail = new S3Detail
			{
				Bucket = new S3DetailBucket { Name = bucketName },
				Object = new S3DetailObject { Key = key }
			},
		};

		var request = new StartExecutionRequest
		{
			StateMachineArn = _stateMachineArn,
			Input = JsonSerializer.Serialize(input)
		};

		await _stepFunctionsClient.StartExecutionAsync(request);
	}
}