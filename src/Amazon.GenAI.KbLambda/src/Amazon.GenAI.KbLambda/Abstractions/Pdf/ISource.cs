namespace Amazon.GenAI.KbLambda.Abstractions.Pdf;

/// <summary>
/// 
/// </summary>
public interface ISource
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancellationToken"></param>
    Task<IReadOnlyCollection<Document>> LoadAsync(CancellationToken cancellationToken = default);
}