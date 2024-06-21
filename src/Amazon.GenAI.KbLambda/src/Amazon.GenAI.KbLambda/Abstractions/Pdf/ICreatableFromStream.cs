#if NET7_0_OR_GREATER
namespace Amazon.GenAI.KbLambda.Abstractions.Pdf;

public interface ICreatableFromStream<out TSelf> where TSelf : ICreatableFromStream<TSelf>
{
    public static abstract TSelf CreateFromStream(Stream stream);
}
#endif