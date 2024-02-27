namespace De.Loooping.Templates.Core.Tokenizers.TokenExtractors;

internal abstract class AbstractTokenExtractor : ITokenExtractor
{
    protected string ToBeScanned { get; }

    protected AbstractTokenExtractor(string toBeScanned)
    {
        ToBeScanned = toBeScanned;
    }
    
    public abstract bool TryExtract(int startIndex, out Token? token);
}