namespace Amazon.GenAI.Abstractions.OpenSearch;

public class OpenSearchVectorStoreOptions
{
    public string? IndexName { get; set; }
    public Uri? ConnectionUri { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int? Dimensions { get; set; }
    public RegionEndpoint? Region { get; set; } = RegionEndpoint.USWest2;
    public string? CollectionArn { get; set; }
}   