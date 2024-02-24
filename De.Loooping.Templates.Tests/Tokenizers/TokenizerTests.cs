using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.Tokenizers;
using Moq;

namespace De.Loooping.Templates.Tests.Tokenizers;

public class TokenizerTests
{
    private static Mock<ITokenizerConfiguration> CreateConfigurationMock()
    {
        var configurationMock = new Mock<ITokenizerConfiguration>();
        configurationMock.SetupProperty(configuration => configuration.LeftCommentDelimiter, "{#");
        configurationMock.SetupProperty(configuration => configuration.RightCommentDelimiter, "#}");
        configurationMock.SetupProperty(configuration => configuration.LeftContentDelimiter, "{{");
        configurationMock.SetupProperty(configuration => configuration.RightContentDelimiter, "}}");
        configurationMock.SetupProperty(configuration => configuration.LeftStatementDelimiter, "{%");
        configurationMock.SetupProperty(configuration => configuration.RightStatementDelimiter, "%}");
        return configurationMock;
    }

    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts {nameof(Token.Comment)}")]
    public void TokenizerExtractsComments()
    {
        // setup
        var configurationMock = CreateConfigurationMock();
        Tokenizer tokenizer = new Tokenizer(configurationMock.Object);

        // act
        var result = tokenizer.Tokenize("{# Comment1 #}{#Comment2#}");

        // validate
        Assert.Collection(result,
            tuple =>
            {
                Assert.Equal(Token.Comment, tuple.Item1);
                Assert.Equal(" Comment1 ", tuple.Item2);
            },
            tuple =>
            {
                Assert.Equal(Token.Comment, tuple.Item1);
                Assert.Equal("Comment2", tuple.Item2);
            }
        );
    }

    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts {nameof(Token.Content)}")]
    public void TokenizerExtractsContent()
    {
        // setup
        var configurationMock = CreateConfigurationMock();
        Tokenizer tokenizer = new Tokenizer(configurationMock.Object);
        
        // act
        var result = tokenizer.Tokenize("{{ Content1 }}{{Content2}}");
            
        // validate
        Assert.Collection(result,
            tuple =>
            {
                Assert.Equal(Token.Content, tuple.Item1);
                Assert.Equal(" Content1 ", tuple.Item2);
            },
            tuple =>
            {
                Assert.Equal(Token.Content, tuple.Item1);
                Assert.Equal("Content2", tuple.Item2);
            }
        );
    }
    
    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts {nameof(Token.Statement)}")]
    public void TokenizerExtractsStatements()
    {
        // setup
        var configurationMock = CreateConfigurationMock();
        Tokenizer tokenizer = new Tokenizer(configurationMock.Object);
        
        // act
        var result = tokenizer.Tokenize("{% Statement1 %}{%Statement2%}");
            
        // validate
        Assert.Collection(result,
            tuple =>
            {
                Assert.Equal(Token.Content, tuple.Item1);
                Assert.Equal(" Statement1 ", tuple.Item2);
            },
            tuple =>
            {
                Assert.Equal(Token.Content, tuple.Item1);
                Assert.Equal("Statement2", tuple.Item2);
            }
        );
    }
    
    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts {nameof(Token.Statement)} with right delimiter in double quotes")]
    public void TokenizerExtractsStatementsWithRightDelimiterInDoubleQuotes()
    {
        // setup
        var configurationMock = CreateConfigurationMock();
        Tokenizer tokenizer = new Tokenizer(configurationMock.Object);
        
        // act
        var result = tokenizer.Tokenize("left literal{% \"%}\" %}right literal");
            
        // validate
        Assert.Collection(result,
            tuple =>
            {
                Assert.Equal(Token.Literal, tuple.Item1);
                Assert.Equal("left literal", tuple.Item2);
            },
            tuple =>
            {
                Assert.Equal(Token.Statement, tuple.Item1);
                Assert.Equal(" \"%}\" ", tuple.Item2);
            },
            tuple =>
            {
                Assert.Equal(Token.Literal, tuple.Item1);
                Assert.Equal("right literal", tuple.Item2);
            }
        );
    }
    
    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts {nameof(Token.Content)} with right delimiter in double quotes")]
    public void TokenizerExtractsContentWithRightDelimiterInDoubleQuotes()
    {
        // setup
        var configurationMock = CreateConfigurationMock();
        Tokenizer tokenizer = new Tokenizer(configurationMock.Object);
        
        // act
        var result = tokenizer.Tokenize("left literal{{ \"%}\" }}right literal");
            
        // validate
        Assert.Collection(result,
            tuple =>
            {
                Assert.Equal(Token.Literal, tuple.Item1);
                Assert.Equal("left literal", tuple.Item2);
            },
            tuple =>
            {
                Assert.Equal(Token.Content, tuple.Item1);
                Assert.Equal(" \"%}\" ", tuple.Item2);
            },
            tuple =>
            {
                Assert.Equal(Token.Literal, tuple.Item1);
                Assert.Equal("right literal", tuple.Item2);
            }
        );
    }
    
    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts {nameof(Token.Statement)} with right delimiter in single quotes")]
    public void TokenizerExtractsStatementsWithRightDelimiterInSigngleQuotes()
    {
        // setup
        var configurationMock = new Mock<ITokenizerConfiguration>();
        configurationMock.SetupProperty(configuration => configuration.LeftCommentDelimiter, "{");
        configurationMock.SetupProperty(configuration => configuration.RightCommentDelimiter, "}");
        configurationMock.SetupProperty(configuration => configuration.LeftContentDelimiter, "<");
        configurationMock.SetupProperty(configuration => configuration.RightContentDelimiter, ">");
        configurationMock.SetupProperty(configuration => configuration.LeftStatementDelimiter, "[");
        configurationMock.SetupProperty(configuration => configuration.RightStatementDelimiter, "]");
        Tokenizer tokenizer = new Tokenizer(configurationMock.Object);
        
        // act
        var result = tokenizer.Tokenize("left literal[ ']' ]right literal");
            
        // validate
        Assert.Collection(result,
            tuple =>
            {
                Assert.Equal(Token.Literal, tuple.Item1);
                Assert.Equal("left literal", tuple.Item2);
            },
            tuple =>
            {
                Assert.Equal(Token.Statement, tuple.Item1);
                Assert.Equal(" ']' ", tuple.Item2);
            },
            tuple =>
            {
                Assert.Equal(Token.Literal, tuple.Item1);
                Assert.Equal("right literal", tuple.Item2);
            }
        );
    }
    
    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts {nameof(Token.Content)} with right delimiter in single quotes")]
    public void TokenizerExtractsContentWithRightDelimiterInSingleQuotes()
    {
        // setup
        var configurationMock = new Mock<ITokenizerConfiguration>();
        configurationMock.SetupProperty(configuration => configuration.LeftCommentDelimiter, "{");
        configurationMock.SetupProperty(configuration => configuration.RightCommentDelimiter, "}");
        configurationMock.SetupProperty(configuration => configuration.LeftContentDelimiter, "<");
        configurationMock.SetupProperty(configuration => configuration.RightContentDelimiter, ">");
        configurationMock.SetupProperty(configuration => configuration.LeftStatementDelimiter, "[");
        configurationMock.SetupProperty(configuration => configuration.RightStatementDelimiter, "]");
        Tokenizer tokenizer = new Tokenizer(configurationMock.Object);
        
        // act
        var result = tokenizer.Tokenize("left literal< '>' >right literal");
            
        // validate
        Assert.Collection(result,
            tuple =>
            {
                Assert.Equal(Token.Literal, tuple.Item1);
                Assert.Equal("left literal", tuple.Item2);
            },
            tuple =>
            {
                Assert.Equal(Token.Content, tuple.Item1);
                Assert.Equal(" '>' ", tuple.Item2);
            },
            tuple =>
            {
                Assert.Equal(Token.Literal, tuple.Item1);
                Assert.Equal("right literal", tuple.Item2);
            }
        );
    }
    
    [Fact(DisplayName = $"{nameof(Tokenizer)} tokenizes complex template")]
    public void TokenizerTokenizesComplexTemplate()
    {
        // setup
        var configurationMock = CreateConfigurationMock();
        Tokenizer tokenizer = new Tokenizer(configurationMock.Object);
        
        // act
        var result = tokenizer.Tokenize("{# A comment #}A literal\n{{ A content }}{% A statement %}");
            
        // validate
        Assert.Collection(result,
            tuple =>
            {
                Assert.Equal(Token.Comment, tuple.Item1);
                Assert.Equal(" Comment1 ", tuple.Item2);
            },
            tuple =>
            {
                Assert.Equal(Token.Literal, tuple.Item1);
                Assert.Equal("A literal\n", tuple.Item2);
            },
            tuple =>
            {
                Assert.Equal(Token.Content, tuple.Item1);
                Assert.Equal(" A content ", tuple.Item2);
            },
            tuple =>
            {
                Assert.Equal(Token.Statement, tuple.Item1);
                Assert.Equal(" A statement ", tuple.Item2);
            }
        );
    }
}