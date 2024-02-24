using System.Linq.Expressions;
using Microsoft.Extensions.Options;

namespace De.Loooping.Templates.Core.Configuration.Validation;

public class TemplateProcessorConfigurationValidation: IValidateOptions<TemplateProcessorConfiguration>
{
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
        CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.LeftContentDelimiter);
        CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.RightContentDelimiter);
        CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.LeftStatementDelimiter);
        CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.RightStatementDelimiter);
        CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.LeftCommentDelimiter);
        CheckForEmptyDelimitersAndAddFailure(failures, options, o => o.RightCommentDelimiter);
        
        // left delimiters must be completely distinct (cannot be part of another left delimiter)
        CheckAndAddDelimiterFailure(failures, options, 
            o => o.LeftContentDelimiter, 
            o => o.LeftStatementDelimiter);
        CheckAndAddDelimiterFailure(failures, options, 
            o => o.LeftContentDelimiter, 
            o => o.LeftCommentDelimiter);
        CheckAndAddDelimiterFailure(failures, options, 
            o => o.LeftStatementDelimiter, 
            o => o.LeftContentDelimiter);
        CheckAndAddDelimiterFailure(failures, options, 
            o => o.LeftStatementDelimiter, 
            o => o.LeftCommentDelimiter);
        CheckAndAddDelimiterFailure(failures, options, 
            o => o.LeftCommentDelimiter, 
            o => o.LeftContentDelimiter);
        CheckAndAddDelimiterFailure(failures, options, 
            o => o.LeftCommentDelimiter, 
            o => o.LeftStatementDelimiter);

        if (failures.Any())
        {
            return ValidateOptionsResult.Fail(failures);
        }
        
        return ValidateOptionsResult.Success;
    }
}