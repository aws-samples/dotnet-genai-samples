namespace Amazon.GenAI.Cdk;

public interface IStackConfiguration
{
    string NamePrefix { get; }
    string NameSuffix { get; }
}