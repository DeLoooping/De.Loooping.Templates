using System.Text;
using De.Loooping.Templates.Core.Configuration;

namespace De.Loooping.Templates.Core.Tokenizers;

public class Tokenizer
{
    private readonly ITokenizerConfiguration _configuration;

    public Tokenizer(ITokenizerConfiguration configuration)
    {
        _configuration = configuration;
    }

    public List<Tuple<Token, string>> Tokenize(string template)
    {
        throw new NotImplementedException();
    }
}