namespace Amazon.GenAI.KbLambda;

public class ResourceProperties
{
    public string NameSuffix { get; set; } = "";
    public string NamePrefix { get; set; } = "";
    public string KnowledgeBaseRoleArn { get; set; } = "";
    public string KnowledgeBaseCustomResourceRole { get; set; } = "";
    public string AccessPolicyArns { get; set; } = "";
    public string KnowledgeBaseEmbeddingModelArn { get; set; } = "";
    public string KnowledgeBaseBucketArn { get; set; } = "";
}