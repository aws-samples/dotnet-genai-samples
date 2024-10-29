namespace Amazon.GenAI.Abstractions.OpenSearch;

public class OpenSearchVectorStoreOptions
{
    public string? ImageIndexName { get; set; }
    public string? TextIndexName { get; set; }
    public Uri? ConnectionUri { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public int? Dimensions { get; set; }
    public RegionEndpoint? Region { get; set; } = RegionEndpoint.USEast1;
    public string? CollectionArn { get; set; }
}   