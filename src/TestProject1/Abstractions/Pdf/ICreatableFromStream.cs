#if NET7_0_OR_GREATER
namespace TestProject1.Abstractions.Pdf;

public interface ICreatableFromStream<out TSelf> where TSelf : ICreatableFromStream<TSelf>
{
    public static abstract TSelf CreateFromStream(Stream stream);
}
#endif