using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.Tokenizers;
using Microsoft.CodeAnalysis.CSharp;
using Moq;

namespace De.Loooping.Templates.Core.Tests.Tokenizers;

public class TokenizerTests
{
    private static Mock<ITokenizerConfiguration> CreateConfigurationMock()
    {
        var configurationMock = new Mock<ITokenizerConfiguration>();
        
        configurationMock.Setup(configuration => configuration.LeftCommentDelimiter).Returns("{#");
        configurationMock.Setup(configuration => configuration.RightCommentDelimiter).Returns("#}");
        
        configurationMock.Setup(configuration => configuration.LeftContentDelimiter).Returns("{{");
        configurationMock.Setup(configuration => configuration.RightContentDelimiter).Returns("}}");
        configurationMock.Setup(configuration => configuration.ContentFormatDelimiter).Returns(":");
        
        configurationMock.Setup(configuration => configuration.LeftStatementDelimiter).Returns("{%");
        configurationMock.Setup(configuration => configuration.RightStatementDelimiter).Returns("%}");
        
        configurationMock.Setup(configuration => configuration.LeftCustomBlockDelimiter).Returns("{$");
        configurationMock.Setup(configuration => configuration.RightCustomBlockDelimiter).Returns("$}");
        configurationMock.Setup(configuration => configuration.CustomBlockIdentifierDelimiter).Returns(":");
        
        return configurationMock;
    }

    private static Mock<ITokenizerConfiguration> CreateSingleDelimiterConfigurationMock()
    {
        var configurationMock = new Mock<ITokenizerConfiguration>();
        
        configurationMock.Setup(configuration => configuration.LeftCommentDelimiter).Returns("{");
        configurationMock.Setup(configuration => configuration.RightCommentDelimiter).Returns("}");
        
        configurationMock.Setup(configuration => configuration.LeftContentDelimiter).Returns("<");
        configurationMock.Setup(configuration => configuration.RightContentDelimiter).Returns(">");
        configurationMock.Setup(configuration => configuration.ContentFormatDelimiter).Returns("%");
        
        configurationMock.Setup(configuration => configuration.LeftStatementDelimiter).Returns("[");
        configurationMock.Setup(configuration => configuration.RightStatementDelimiter).Returns("]");

        configurationMock.Setup(configuration => configuration.LeftCustomBlockDelimiter).Returns("$");
        configurationMock.Setup(configuration => configuration.RightCustomBlockDelimiter).Returns("$");
        configurationMock.Setup(configuration => configuration.CustomBlockIdentifierDelimiter).Returns(":");
        
        return configurationMock;
    }

    private static Tokenizer CreateTokenizer(ITokenizerConfiguration configuration)
    {
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp12);
        return new Tokenizer(configuration, options);
    }

    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts comments")]
    public void TokenizerExtractsComments()
    {
        // setup
        var configurationMock = CreateConfigurationMock();
        Tokenizer tokenizer = CreateTokenizer(configurationMock.Object);

        // act
        var result = tokenizer.Tokenize("{# Comment1 #}{#Comment2#}");

        // validate
        Assert.Collection(result,
            token =>
            {
                Assert.Equal(TokenType.LeftCommentDelimiter, token.TokenType);
                Assert.Equal("{#", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal(" Comment1 ", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightCommentDelimiter, token.TokenType);
                Assert.Equal("#}", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.LeftCommentDelimiter, token.TokenType);
                Assert.Equal("{#", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal("Comment2", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightCommentDelimiter, token.TokenType);
                Assert.Equal("#}", token.Value);
            }
        );
    }

    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts content")]
    public void TokenizerExtractsContent()
    {
        // setup
        var configurationMock = CreateConfigurationMock();
        Tokenizer tokenizer = CreateTokenizer(configurationMock.Object);
        
        // act
        var result = tokenizer.Tokenize("{{ Content1 }}{{Content2}}");
            
        // validate
        Assert.Collection(result,
            token =>
            {
                Assert.Equal(TokenType.LeftContentDelimiter, token.TokenType);
                Assert.Equal("{{", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.CSharp, token.TokenType);
                Assert.Equal(" Content1 ", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightContentDelimiter, token.TokenType);
                Assert.Equal("}}", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.LeftContentDelimiter, token.TokenType);
                Assert.Equal("{{", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.CSharp, token.TokenType);
                Assert.Equal("Content2", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightContentDelimiter, token.TokenType);
                Assert.Equal("}}", token.Value);
            }
        );
    }
    
    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts content with format")]
    public void TokenizerExtractsContentWithFormat()
    {
        // setup
        var configurationMock = CreateConfigurationMock();
        Tokenizer tokenizer = CreateTokenizer(configurationMock.Object);
        
        // act
        var result = tokenizer.Tokenize("{{ Content : Format }}");
            
        // validate
        Assert.Collection(result,
            token =>
            {
                Assert.Equal(TokenType.LeftContentDelimiter, token.TokenType);
                Assert.Equal("{{", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.CSharp, token.TokenType);
                Assert.Equal(" Content ", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.ContentFormatDelimiter, token.TokenType);
                Assert.Equal(":", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal(" Format ", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightContentDelimiter, token.TokenType);
                Assert.Equal("}}", token.Value);
            }
        );
    }
    
    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts Statements")]
    public void TokenizerExtractsStatements()
    {
        // setup
        var configurationMock = CreateConfigurationMock();
        Tokenizer tokenizer = CreateTokenizer(configurationMock.Object);
        
        // act
        var result = tokenizer.Tokenize("{% Statement1 %}{%Statement2%}");
            
        // validate
        Assert.Collection(result,
            token =>
            {
                Assert.Equal(TokenType.LeftStatementDelimiter, token.TokenType);
                Assert.Equal("{%", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.CSharp, token.TokenType);
                Assert.Equal(" Statement1 ", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightStatementDelimiter, token.TokenType);
                Assert.Equal("%}", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.LeftStatementDelimiter, token.TokenType);
                Assert.Equal("{%", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.CSharp, token.TokenType);
                Assert.Equal("Statement2", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightStatementDelimiter, token.TokenType);
                Assert.Equal("%}", token.Value);
            }
        );
    }
    
    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts Statements with right delimiter in double quotes")]
    public void TokenizerExtractsStatementsWithRightDelimiterInDoubleQuotes()
    {
        // setup
        var configurationMock = CreateConfigurationMock();
        Tokenizer tokenizer = CreateTokenizer(configurationMock.Object);
        
        // act
        var result = tokenizer.Tokenize("left literal{% \"%}\" %}right literal");
            
        // validate
        Assert.Collection(result,
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal("left literal", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.LeftStatementDelimiter, token.TokenType);
                Assert.Equal("{%", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.CSharp, token.TokenType);
                Assert.Equal(" \"%}\" ", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightStatementDelimiter, token.TokenType);
                Assert.Equal("%}", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal("right literal", token.Value);
            }
        );
    }
    
    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts Content with right delimiter in double quotes")]
    public void TokenizerExtractsContentWithRightDelimiterInDoubleQuotes()
    {
        // setup
        var configurationMock = CreateConfigurationMock();
        Tokenizer tokenizer = CreateTokenizer(configurationMock.Object);
        
        // act
        var result = tokenizer.Tokenize("left literal{{ \"}}\" }}right literal");
            
        // validate
        Assert.Collection(result,
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal("left literal", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.LeftContentDelimiter, token.TokenType);
                Assert.Equal("{{", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.CSharp, token.TokenType);
                Assert.Equal(" \"}}\" ", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightContentDelimiter, token.TokenType);
                Assert.Equal("}}", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal("right literal", token.Value);
            }
        );
    }
    
    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts Statements with right delimiter in single quotes")]
    public void TokenizerExtractsCSharpWithRightDelimiterInSingleQuotes()
    {
        // setup
        var configurationMock = CreateSingleDelimiterConfigurationMock();
        Tokenizer tokenizer = CreateTokenizer(configurationMock.Object);
        
        // act
        var result = tokenizer.Tokenize("left literal[ ']' ]right literal");
            
        // validate
        Assert.Collection(result,
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal("left literal", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.LeftStatementDelimiter, token.TokenType);
                Assert.Equal("[", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.CSharp, token.TokenType);
                Assert.Equal(" ']' ", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightStatementDelimiter, token.TokenType);
                Assert.Equal("]", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal("right literal", token.Value);
            }
        );
    }
    
    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts Content with right delimiter in single quotes")]
    public void TokenizerExtractsContentWithRightDelimiterInSingleQuotes()
    {
        // setup
        var configurationMock = CreateSingleDelimiterConfigurationMock();
        Tokenizer tokenizer = CreateTokenizer(configurationMock.Object);

        // act
        var result = tokenizer.Tokenize("left literal< '>' >right literal");
            
        // validate
        Assert.Collection(result,
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal("left literal", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.LeftContentDelimiter, token.TokenType);
                Assert.Equal("<", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.CSharp, token.TokenType);
                Assert.Equal(" '>' ", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightContentDelimiter, token.TokenType);
                Assert.Equal(">", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal("right literal", token.Value);
            }
        );
    }

    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts Content with format delimiter in single quotes")]
    public void TokenizerExtractsContentWithFormatDelimiterInSingleQuotes()
    {
        // setup
        var configurationMock = CreateSingleDelimiterConfigurationMock();
        Tokenizer tokenizer = CreateTokenizer(configurationMock.Object);

        // act
        var result = tokenizer.Tokenize("left literal< '%' >right literal");
            
        // validate
        Assert.Collection(result,
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal("left literal", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.LeftContentDelimiter, token.TokenType);
                Assert.Equal("<", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.CSharp, token.TokenType);
                Assert.Equal(" '%' ", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightContentDelimiter, token.TokenType);
                Assert.Equal(">", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal("right literal", token.Value);
            }
        );
    }

    [Fact(DisplayName = $"{nameof(Tokenizer)} extracts custom blocks")]
    public void TokenizerExtractsCustomBlocks()
    {
        // setup
        var configurationMock = CreateConfigurationMock();
        Tokenizer tokenizer = CreateTokenizer(configurationMock.Object);

        // act
        var result = tokenizer.Tokenize("left literal{$ Identifier : Content $}right literal");
            
        // validate
        Assert.Collection(result,
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal("left literal", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.LeftCustomBlockDelimiter, token.TokenType);
                Assert.Equal("{$", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.Identifier, token.TokenType);
                Assert.Equal("Identifier", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.CustomBlockIdentifierDelimiter, token.TokenType);
                Assert.Equal(":", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal(" Content ", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightCustomBlockDelimiter, token.TokenType);
                Assert.Equal("$}", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal("right literal", token.Value);
            }
        );
    }

    [Fact(DisplayName = $"{nameof(Tokenizer)} tokenizes complex template")]
    public void TokenizerTokenizesComplexTemplate()
    {
        // setup
        var configurationMock = CreateConfigurationMock();
        Tokenizer tokenizer = CreateTokenizer(configurationMock.Object);
        
        // act
        var result = tokenizer.Tokenize("{# A comment #}A literal\n{{ (\"{{\"+content+\"}}\").ToLower() }}{% var i = 42; %}");
            
        // validate
        Assert.Collection(result,
            token =>
            {
                Assert.Equal(TokenType.LeftCommentDelimiter, token.TokenType);
                Assert.Equal("{#", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal(" A comment ", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightCommentDelimiter, token.TokenType);
                Assert.Equal("#}", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.Literal, token.TokenType);
                Assert.Equal("A literal\n", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.LeftContentDelimiter, token.TokenType);
                Assert.Equal("{{", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.CSharp, token.TokenType);
                Assert.Equal(" (\"{{\"+content+\"}}\").ToLower() ", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightContentDelimiter, token.TokenType);
                Assert.Equal("}}", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.LeftStatementDelimiter, token.TokenType);
                Assert.Equal("{%", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.CSharp, token.TokenType);
                Assert.Equal(" var i = 42; ", token.Value);
            },
            token =>
            {
                Assert.Equal(TokenType.RightStatementDelimiter, token.TokenType);
                Assert.Equal("%}", token.Value);
            }
        );
    }
}