using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.Tokenizers.TokenExtractors;
using Microsoft.CodeAnalysis.CSharp;

namespace De.Loooping.Templates.Core.Tokenizers;

internal class Tokenizer
{
    private enum State
    {
        Literal,
        
        Content,
        ContentFormat,
        
        Statement,
        
        Comment,
        
        CustomBlockIdentifier,
        CustomBlockContent,
    }

    private class PotentialNextState
    {
        public required ITokenExtractor Test { get; init; }
        public required State NextState { get; init; }
    }
    
    private readonly ITokenizerConfiguration _configuration;
    private readonly CSharpParseOptions _parseOptions;

    public Tokenizer(ITokenizerConfiguration configuration, CSharpParseOptions parseOptions)
    {
        _configuration = configuration;
        _parseOptions = parseOptions;
    }

    public List<Token> Tokenize(string template)
    {
        State currentState = State.Literal;
        int currentIndex = 0;

        // content
        var leftContentDelimiterExtractor = new DelimiterExtractor(template, _configuration.LeftContentDelimiter, TokenType.LeftContentDelimiter);
        var rightContentDelimiterExtractor = new DelimiterExtractor(template, _configuration.RightContentDelimiter, TokenType.RightContentDelimiter);
        var contentFormatDelimiterExtractor = new DelimiterExtractor(template, _configuration.ContentFormatDelimiter, TokenType.ContentFormatDelimiter);
        var contentCSharpExtractor = new CSharpExtractor(template, new[] { _configuration.RightContentDelimiter, _configuration.ContentFormatDelimiter }, _parseOptions);
        var contentFormatExtractor = new LiteralExtractor(template, new[] { _configuration.RightContentDelimiter });

        // statement
        var leftStatementDelimiterExtractor = new DelimiterExtractor(template, _configuration.LeftStatementDelimiter, TokenType.LeftStatementDelimiter);
        var rightStatementDelimiterExtractor = new DelimiterExtractor(template, _configuration.RightStatementDelimiter, TokenType.RightStatementDelimiter);
        var statementCSharpExtractor = new CSharpExtractor(template, new[] { _configuration.RightStatementDelimiter }, _parseOptions);

        // comment
        var leftCommentDelimiterExtractor = new DelimiterExtractor(template, _configuration.LeftCommentDelimiter, TokenType.LeftCommentDelimiter);
        var rightCommentDelimiterExtractor = new DelimiterExtractor(template, _configuration.RightCommentDelimiter, TokenType.RightCommentDelimiter);
        var commentExtractor = new LiteralExtractor(template, new[]
        {
            _configuration.RightCommentDelimiter
        });

        // custom blocks
        var leftCustomBlockDelimiterExtractor = new DelimiterExtractor(template, _configuration.LeftCustomBlockDelimiter, TokenType.LeftCustomBlockDelimiter);
        var rightCustomBlockDelimiterExtractor = new DelimiterExtractor(template, _configuration.RightCustomBlockDelimiter, TokenType.RightCustomBlockDelimiter);
        var customBlockIdentifierDelimiterExtractor = new DelimiterExtractor(template, _configuration.CustomBlockIdentifierDelimiter, TokenType.CustomBlockIdentifierDelimiter);
        var customBlockIdentifierExtractor = new LiteralExtractor(template, new[] { _configuration.CustomBlockIdentifierDelimiter },
            TokenType.Identifier, value => value.Trim());
        var customBlockContentExtractor = new LiteralExtractor(template, new[] { _configuration.RightCustomBlockDelimiter });
        
        // literals
        var literalExtractor = new LiteralExtractor(template, new[]
        {
            _configuration.LeftContentDelimiter,
            _configuration.LeftStatementDelimiter,
            _configuration.LeftCommentDelimiter,
            _configuration.LeftCustomBlockDelimiter
        });

        // transitions of the state machine
        Dictionary<State, List<PotentialNextState>> stateTransitions = new()
        {
            {
                State.Literal, new List<PotentialNextState> {
                    new PotentialNextState() { Test = leftContentDelimiterExtractor, NextState = State.Content },
                    new PotentialNextState() { Test = leftStatementDelimiterExtractor, NextState = State.Statement },
                    new PotentialNextState() { Test = leftCommentDelimiterExtractor, NextState = State.Comment },
                    new PotentialNextState() { Test = leftCustomBlockDelimiterExtractor, NextState = State.CustomBlockIdentifier },
                    new PotentialNextState() { Test = literalExtractor, NextState = State.Literal }
                }
            },
            {
                State.Content, new List<PotentialNextState> {
                    new PotentialNextState() { Test = contentFormatDelimiterExtractor, NextState = State.ContentFormat },
                    new PotentialNextState() { Test = rightContentDelimiterExtractor, NextState = State.Literal },
                    new PotentialNextState() { Test = contentCSharpExtractor, NextState = State.Content },
                }
            },
            {
                State.ContentFormat, new List<PotentialNextState> {
                    new PotentialNextState() { Test = rightContentDelimiterExtractor, NextState = State.Literal },
                    new PotentialNextState() { Test = contentFormatExtractor, NextState = State.ContentFormat },
                }
            },
            {
                State.Statement, new List<PotentialNextState> {
                    new PotentialNextState() { Test = rightStatementDelimiterExtractor, NextState = State.Literal },
                    new PotentialNextState() { Test = statementCSharpExtractor, NextState = State.Statement },
                }
            },
            {
                State.Comment, new List<PotentialNextState> {
                    new PotentialNextState() { Test = rightCommentDelimiterExtractor, NextState = State.Literal },
                    new PotentialNextState() { Test = commentExtractor, NextState = State.Comment },
                }
            },
            {
                State.CustomBlockIdentifier, new List<PotentialNextState> {
                    new PotentialNextState() { Test = customBlockIdentifierExtractor, NextState = State.CustomBlockIdentifier },
                    new PotentialNextState() { Test = customBlockIdentifierDelimiterExtractor, NextState = State.CustomBlockContent }
                }
            },
            {
                State.CustomBlockContent, new List<PotentialNextState> {
                    new PotentialNextState() { Test = customBlockContentExtractor, NextState = State.CustomBlockContent },
                    new PotentialNextState() { Test = rightCustomBlockDelimiterExtractor, NextState = State.Literal }
                }
            }
        };
        
        List<Token> result = new();
        while (currentIndex < template.Length)
        {
            if (!stateTransitions.TryGetValue(currentState, out var potentialNextStates))
            {
                throw new IndexOutOfRangeException($"Unexpected state {currentState}");
            }

            Token? nextToken = null;
            foreach (var potentialNextState in potentialNextStates)
            {
                if (potentialNextState.Test.TryExtract(currentIndex, out nextToken))
                {
                    currentState = potentialNextState.NextState;
                    break;
                }
            }

            if (nextToken == null && (currentIndex != template.Length || currentState != State.Literal))
            {
                // TODO: more information about where the syntax error is
                throw new Exception("Syntax error");
            }

            if (nextToken != null)
            {
                result.Add(nextToken);
                currentIndex += nextToken.CharactersConsumed;
            }
        }

        return result;
    }
}