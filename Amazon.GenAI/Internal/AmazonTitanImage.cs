using System.Text.Json.Nodes;

namespace Amazon.GenAI.Internal;

public static class AmazonTitanText
{
	public static JsonObject CreateBodyJson(string prompt)
	{
		var bodyJson = new JsonObject
		{
			["inputText"] = prompt,
			["textGenerationConfig"] = new JsonObject
			{
				["maxTokenCount"] = 4096,
				["temperature"] = 0,
				["topP"] = 1,
				["stopSequences"] = Array.Empty<string>().AsArray()
			}
		};
		return bodyJson;
	}
}