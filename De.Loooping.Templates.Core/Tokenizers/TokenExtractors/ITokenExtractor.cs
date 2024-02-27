namespace De.Loooping.Templates.Core.Tokenizers.TokenExtractors;

internal interface ITokenExtractor
{
    bool TryExtract(int startIndex, out Token? token);
}