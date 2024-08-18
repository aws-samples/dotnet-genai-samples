using Amazon.BedrockAgent;
using Amazon.BedrockAgent.Model;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.DynamoDBv2;
using Amazon.Lambda;
using Amazon.OpenSearchServerless;
using Amazon.OpenSearchServerless.Model;
using Amazon.S3;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Amazon.CloudWatchLogs;
using Amazon.CloudWatchLogs.Model;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;

namespace TestProject1;

public class Cleanup
{
	private const string NamePrefix = "dotnet-genai";

	[Test]
	public async Task DeleteCloudFormationStacks()
	{
		Console.WriteLine("Deleting CloudFormation Stacks");

		try
		{
			var client = new AmazonCloudFormationClient();

			var listResponse = await client.ListStacksAsync();
			var list = listResponse.StackSummaries.Where(x => x.StackName.StartsWith(NamePrefix)).ToList();

			foreach (var item in list)
			{
				var request = new DeleteStackRequest { StackName = item.StackName };
				await client.DeleteStackAsync(request);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	[Test]
	public async Task DeleteCollections()
	{
		try
		{
			var client = new AmazonOpenSearchServerlessClient();

			var listRequest = new ListCollectionsRequest();
			var listResponse = await client.ListCollectionsAsync(listRequest);
			var list = listResponse.CollectionSummaries.Where(x => x.Name.StartsWith(NamePrefix)).ToList();

			foreach (var item in list)
			{
				var request = new DeleteCollectionRequest { Id = item.Id };
				await client.DeleteCollectionAsync(request);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	[Test]
	public async Task DeleteDynamoDb()
	{
		try
		{
			var client = new AmazonDynamoDBClient();
			var list = (await client.ListTablesAsync()).TableNames.Where(x => x.StartsWith(NamePrefix)).ToList();

			foreach (var item in list)
			{
				var deleteResponse = await client.DeleteTableAsync(item);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	[Test]
	public async Task DeleteStateMachines()
	{
		var client = new AmazonStepFunctionsClient();

		try
		{
			var listResponse = await client.ListStateMachinesAsync(new ListStateMachinesRequest());
			var list = listResponse.StateMachines.Where(x => x.Name.StartsWith(NamePrefix)).ToList();

			foreach (var item in list)
			{
				var deleteRequest = new DeleteStateMachineRequest
				{
					StateMachineArn = item.StateMachineArn
				};

				var deleteResponse = await client.DeleteStateMachineAsync(deleteRequest);
				Console.WriteLine($"State machine '{item.Name}' deleted successfully.");
			}
		}
		catch (AmazonStepFunctionsException e)
		{
			Console.WriteLine($"Error: {e.Message}");
		}
	}

	[Test]
	public async Task DeleteLambdas()
	{
		try
		{
			var client = new AmazonLambdaClient();
			var list = (await client.ListFunctionsAsync()).Functions.Where(x => x.FunctionName.StartsWith(NamePrefix)).ToList();

			foreach (var item in list)
			{
				var deleteResponse = await client.DeleteFunctionAsync(item.FunctionName);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	[Test]
	public async Task DeleteS3Buckets()
	{
		try
		{
			var client = new AmazonS3Client();
			var list = (await client.ListBucketsAsync()).Buckets.Where(x => x.BucketName.StartsWith(NamePrefix)).ToList();

			foreach (var item in list)
			{
				var deleteResponse = await client.DeleteBucketAsync(item.BucketName);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	[Test]
	public async Task DeleteEventBridgeRules()
	{
		const string eventBusName = "default"; // Use "default" for the default event bus, or specify your custom event bus name

		var client = new AmazonEventBridgeClient();

		try
		{
			var list = (await client.ListRulesAsync(new ListRulesRequest())).Rules.Where(x => x.Name.StartsWith(NamePrefix)).ToList();

			foreach (var item in list)
			{
				// First, list targets for the rule
				var listTargetsRequest = new ListTargetsByRuleRequest
				{
					Rule = item.Name,
					EventBusName = eventBusName
				};
				var listTargetsResponse = await client.ListTargetsByRuleAsync(listTargetsRequest);

				// If there are targets, remove them
				if (listTargetsResponse.Targets.Count > 0)
				{
					var targetIds = listTargetsResponse.Targets.Select(t => t.Id).ToList();
					var removeTargetsRequest = new RemoveTargetsRequest
					{
						Rule = item.Name,
						EventBusName = eventBusName,
						Ids = targetIds
					};
					var removeTargetsResponse = await client.RemoveTargetsAsync(removeTargetsRequest);

					if (removeTargetsResponse.FailedEntryCount > 0)
					{
						Console.WriteLine("Failed to remove some targets. Please check and retry.");
						return;
					}
				}

				var deleteRuleRequest = new DeleteRuleRequest
				{
					Name = item.Name,
					EventBusName = eventBusName
				};
				var deleteRuleResponse = await client.DeleteRuleAsync(deleteRuleRequest);

				Console.WriteLine($"EventBridge rule '{item.Name}' deleted successfully.");
			}

		}
		catch (AmazonEventBridgeException e)
		{
			Console.WriteLine($"Error: {e.Message}");
		}
	}


	[Test]
	public async Task DeleteAccessPolicies()
	{
		try
		{
			var client = new AmazonOpenSearchServerlessClient();

			var listRequest = new ListAccessPoliciesRequest { Type = AccessPolicyType.Data };
			var listResponse = await client.ListAccessPoliciesAsync(listRequest);
			var summaries = listResponse.AccessPolicySummaries.Where(x => x.Name.StartsWith(NamePrefix)).ToList();

			foreach (var item in summaries)
			{
				var request = new DeleteAccessPolicyRequest { Name = item.Name, Type = item.Type };
				await client.DeleteAccessPolicyAsync(request);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	[Test]
	public async Task DeleteEncryptionPolicies()
	{
		try
		{
			var client = new AmazonOpenSearchServerlessClient();

			var listRequest = new ListSecurityPoliciesRequest { Type = SecurityPolicyType.Encryption };
			var listResponse = await client.ListSecurityPoliciesAsync(listRequest);
			var summaries = listResponse.SecurityPolicySummaries.Where(x => x.Name.StartsWith(NamePrefix)).ToList();

			foreach (var item in summaries)
			{
				var request = new DeleteSecurityPolicyRequest() { Name = item.Name, Type = item.Type };
				await client.DeleteSecurityPolicyAsync(request);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	[Test]
	public async Task DeleteNetworkPolicies()
	{
		try
		{
			var client = new AmazonOpenSearchServerlessClient();

			var listRequest = new ListSecurityPoliciesRequest { Type = SecurityPolicyType.Network };
			var listResponse = await client.ListSecurityPoliciesAsync(listRequest);
			var summaries = listResponse.SecurityPolicySummaries.Where(x => x.Name.StartsWith(NamePrefix)).ToList();

			foreach (var item in summaries)
			{
				var request = new DeleteSecurityPolicyRequest() { Name = item.Name, Type = item.Type };
				await client.DeleteSecurityPolicyAsync(request);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	[Test]
	public async Task DeleteLogs()
	{
		var client = new AmazonCloudWatchLogsClient();

		string nextToken = null;
		do
		{
			var request = new DescribeLogGroupsRequest
			{
				NextToken = nextToken
			};

			var response = await client.DescribeLogGroupsAsync(request);

			foreach (var logGroup in response.LogGroups)
			{
				Console.WriteLine($"Deleting log group: {logGroup.LogGroupName}");

				try
				{
					await client.DeleteLogGroupAsync(new DeleteLogGroupRequest
					{
						LogGroupName = logGroup.LogGroupName
					});
					Console.WriteLine($"Successfully deleted log group: {logGroup.LogGroupName}");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to delete log group {logGroup.LogGroupName}: {ex.Message}");
				}
			}

			nextToken = response.NextToken;
		} while (nextToken != null);

		Console.WriteLine("Finished deleting all log groups.");
	}

	[Test]
	public async Task DeleteKnowledgeBases()
	{
		try
		{
			var client = new AmazonBedrockAgentClient();

			var listRequest = new ListKnowledgeBasesRequest();
			var listResponse = await client.ListKnowledgeBasesAsync(listRequest);
			var summaries = listResponse.KnowledgeBaseSummaries.Where(x => x.Name.StartsWith(NamePrefix)).ToList();

			foreach (var request in summaries.Select(item => new DeleteKnowledgeBaseRequest { KnowledgeBaseId = item.KnowledgeBaseId }))
			{
				await client.DeleteKnowledgeBaseAsync(request);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	[Test]
	public async Task DeleteSystemManagerParameters()
	{
		try
		{
			var client = new AmazonSimpleSystemsManagementClient();

			var listRequest = new GetParametersRequest { Names = [@"name:*"] };
			var listResponse = await client.GetParametersAsync(listRequest);
			var summaries = listResponse.Parameters.ToList();

			//foreach (var request in summaries.Select(item => new DeleteKnowledgeBaseRequest { KnowledgeBaseId = item.KnowledgeBaseId }))
			//{
			//    await client.DeleteKnowledgeBaseAsync(request);
			//}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}

	[Test]
	public async Task DeleteRoles()
	{
		try
		{
			var client = new AmazonIdentityManagementServiceClient();

			var listRolesRequest = new ListRolesRequest() { MaxItems = 300 };
			var listRolesResponse = await client.ListRolesAsync(listRolesRequest);
			var list = listRolesResponse.Roles.Where(x => x.RoleName.StartsWith(NamePrefix)).ToList();
			Console.WriteLine($"Count: {list.Count}");

			foreach (Role? item in list)
			{
				// List attached role policies
				var listAttachedRolePoliciesRequest = new ListAttachedRolePoliciesRequest { RoleName = item.RoleName };
				var listAttachedRolePoliciesResponse = await client.ListAttachedRolePoliciesAsync(listAttachedRolePoliciesRequest);

				// Detach each policy
				foreach (var attachedPolicy in listAttachedRolePoliciesResponse.AttachedPolicies)
				{
					var detachRolePolicyRequest = new DetachRolePolicyRequest
					{
						RoleName = item.RoleName,
						PolicyArn = attachedPolicy.PolicyArn
					};
					await client.DetachRolePolicyAsync(detachRolePolicyRequest);
					Console.WriteLine($"Detached policy {attachedPolicy.PolicyName} from role {item.RoleName}");
				}

				// 2. Delete inline policies
				var listRolePoliciesRequest = new ListRolePoliciesRequest { RoleName = item.RoleName };
				var listRolePoliciesResponse = await client.ListRolePoliciesAsync(listRolePoliciesRequest);

				foreach (var policyName in listRolePoliciesResponse.PolicyNames)
				{
					var deleteRolePolicyRequest = new DeleteRolePolicyRequest
					{
						RoleName = item.RoleName,
						PolicyName = policyName
					};
					await client.DeleteRolePolicyAsync(deleteRolePolicyRequest);
					Console.WriteLine($"Deleted inline policy {policyName} from role {item.RoleName}");
				}

				var deleteRequest = new DeleteRoleRequest { RoleName = item.RoleName };
				await client.DeleteRoleAsync(deleteRequest);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			throw;
		}
	}
}