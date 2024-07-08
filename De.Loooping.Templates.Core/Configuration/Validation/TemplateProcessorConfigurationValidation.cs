using System.Linq.Expressions;
using Microsoft.Extensions.Options;

namespace De.Loooping.Templates.Core.Configuration.Validation;

public class TemplateProcessorConfigurationValidation: IValidateOptions<TemplateProcessorConfiguration>
{
    public bool CheckCustomBlockConfiguration { get; set; } = true;
    
    private void CheckAndAddDelimiterFailure(List<string> failures,
        TemplateProcessorConfiguration options,
        Expression<Func<TemplateProcessorConfiguration, string>> left,
        Expression<Func<TemplateProcessorConfiguration, string>> right)
    {
        string leftString = left.Compile().Invoke(options);
        string rightString = right.Compile().Invoke(options);

        if (leftString.Contains(rightString))
        {
            failures.Add($"{GetPropertyName(left)} must not contain {GetPropertyName(right)}");
        }
    }

    private void CheckForEmptyDelimitersAndAddFailure(List<string> failures,
        TemplateProcessorConfiguration options,
        Expression<Func<TemplateProcessorConfiguration, string>> delimiter)
    {
        string delimiterString = delimiter.Compile().Invoke(options);

        if (string.IsNullOrEmpty(delimiterString))
        {
            failures.Add($"{GetPropertyName(delimiter)} must not be empty");
        }
    }

    private string GetPropertyName(Expression<Func<TemplateProcessorConfiguration, string>> expression)
    {
        return (expression.Body as MemberExpression)!.Member.Name;
    }

    public ValidateOptionsResult Validate(string? name, TemplateProcessorConfiguration options)
    {
        List<string> failures = new();
        
        // delimiters cannot be empty
        if (options.EvaluateContentBlocks)
        {
            CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.LeftContentDelimiter);
            CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.RightContentDelimiter);
            CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.ContentFormatDelimiter);
        }

        if (options.EvaluateStatementBlocks)
        {
            CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.LeftStatementDelimiter);
            CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.RightStatementDelimiter);
        }

        if (options.RemoveCommentBlocks)
        {
            CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.LeftCommentDelimiter);
            CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.RightCommentDelimiter);
        }

        if (CheckCustomBlockConfiguration)
        {
            CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.LeftCustomBlockDelimiter);
            CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.RightCustomBlockDelimiter);
            CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.CustomBlockIdentifierDelimiter);
        }

        // left delimiters must be distinct (cannot be the beginning of another left delimiter)
        var leftDelimiters = new List<Expression<Func<TemplateProcessorConfiguration, string>>>();
        if (options.EvaluateContentBlocks)
        {
            leftDelimiters.Add(o => o.LeftContentDelimiter);
        }
        if (options.EvaluateStatementBlocks)
        {
            leftDelimiters.Add(o => o.LeftStatementDelimiter);
        }
        if (options.RemoveCommentBlocks)
        {
            leftDelimiters.Add(o => o.LeftCommentDelimiter);
        }
        if (CheckCustomBlockConfiguration)
        {
            leftDelimiters.Add(o => o.LeftCustomBlockDelimiter);
        }

        for (int i = 0; i < leftDelimiters.Count; i++)
        {
            for (int j = i+1; j < leftDelimiters.Count; j++)
            {
                var left = leftDelimiters[i];
                var right = leftDelimiters[j];
                CheckAndAddDelimiterFailure(failures, options, left, right);
                CheckAndAddDelimiterFailure(failures, options, right, left);
            }
        }
        
        if (failures.Any())
        {
            return ValidateOptionsResult.Fail(failures);
        }
        
        return ValidateOptionsResult.Success;
    }
}