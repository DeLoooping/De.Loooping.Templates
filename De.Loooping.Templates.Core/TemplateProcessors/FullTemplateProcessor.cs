using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.Tokenizers;
using De.Loooping.Templates.Core.Tools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace De.Loooping.Templates.Core.TemplateProcessors;

public class FullTemplateProcessor<TDelegate> where TDelegate: class, MulticastDelegate
{
    private static readonly string _COMPILATION_NAMESPACE = "TemplateCompilationNamespace";
    private static readonly string _COMPILATION_CLASS = "TemplateCompilationClass";
    private static readonly string _COMPILATION_ENUMERABLE_METHOD = "GetParts";
    private static readonly string _COMPILATION_METHOD = "Process";

    private readonly TemplateProcessorConfiguration _configuration;
    private readonly string _template;
    private readonly CSharpParseOptions _parseOptions;

    public FullTemplateProcessor(TemplateProcessorConfiguration configuration, string template, LanguageVersion languageVersion = LanguageVersion.CSharp12)
    {
        if (!typeof(TDelegate).IsSubclassOf(typeof(Delegate)))
        {
            throw new ArgumentException($"{typeof(TDelegate).Name} must be a delegate type");
        }
        
        if (typeof(TDelegate).GetMethod("Invoke")?.ReturnType != typeof(String))
        {
            throw new ArgumentException($"{typeof(TDelegate).Name} must have a return type of string");
        }
        
        _configuration = configuration;
        _template = template;
        _parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(languageVersion)
            .WithKind(SourceCodeKind.Regular);

        AddDefaultReferences();
        AddDefaultUsings();
    }

    private void AddDefaultReferences()
    {
        References.Add(typeof(int).Assembly);
        References.Add(typeof(Enumerable).Assembly);
        References.Add(typeof(Object).Assembly);
        References.Add(typeof(IEnumerable<>).Assembly);
        //References.Add(AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name == "System"));
        References.Add(AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name == "System.Runtime"));
        /*.AddReferences(MetadataReference.CreateFromFile(Path.Combine(rootPath, "System.dll")))
        .AddReferences(MetadataReference.CreateFromFile(Path.Combine(rootPath, "netstandard.dll")))
        .AddReferences(MetadataReference.CreateFromFile(Path.Combine(rootPath, "System.Runtime.dll")));*/  
    }

    private void AddDefaultUsings()
    {
        Usings.Add("System");
        Usings.Add("System.Linq");
        Usings.Add("System.Collections.Generic");
    }

    public HashSet<string> Usings { get; } = new();
    public HashSet<Assembly> References { get; } = new();

    public void AddType(Type type)
    {
        References.Add(type.Assembly);
        if (type.Namespace != null)
        {
            Usings.Add(type.Namespace);
        }
    }

    private IEnumerable<KeyValuePair<string, Type>> GetParameters()
    {
        int index = 0;
        foreach (var parameterInfo in typeof(TDelegate).GetMethod("Invoke")!.GetParameters())
        {
            index++;
            if (String.IsNullOrEmpty(parameterInfo.Name))
            {
                throw new ArgumentException($"Parameter {index} of delegate {typeof(TDelegate).Name} has no name");
            }
            yield return new KeyValuePair<string, Type>(parameterInfo.Name, parameterInfo.ParameterType);
        }
    }

    private string GetFullName(Type type)
    {
        return TypeNameResolver.GetFullName(type);
    }
    
    public TDelegate Build()
    {
        // generate code
        var codeBuilder = new StringBuilder();
        
        List<string> parameters = new();
        List<string> parametersWithType = new();
        foreach (var kvp in GetParameters())
        {
            string name = kvp.Key;
            Type type = kvp.Value;
            parameters.Add(name);
            parametersWithType.Add($"{GetFullName(type)} {name}");
        }

        foreach (string u in Usings)
        {
            codeBuilder.AppendLine($"using {u};");
        }

        codeBuilder.AppendLine($"namespace {_COMPILATION_NAMESPACE};\n");
        codeBuilder.AppendLine($"public class {_COMPILATION_CLASS} {{");

        codeBuilder.AppendLine($"public static string {_COMPILATION_METHOD}({String.Join(", ", parametersWithType)}) {{");
        codeBuilder.AppendLine($"   var result = {_COMPILATION_ENUMERABLE_METHOD}({String.Join(", ", parameters)});");
        codeBuilder.AppendLine($"   return String.Join(\"\", result);");
        codeBuilder.AppendLine("}");
        
        codeBuilder.AppendLine($"private static {GetFullName(typeof(IEnumerable<string>))} {_COMPILATION_ENUMERABLE_METHOD}({String.Join(", ", parametersWithType)})\n{{");

        Tokenizer tokenizer = new Tokenizer(_configuration, _parseOptions);
        List<Token> tokens = tokenizer.Tokenize(_template);

        var tokenEnumerator = tokens.GetEnumerator();
        EvaluateRoot(tokenEnumerator, codeBuilder);
        
        codeBuilder.AppendLine("}\n}");

        // generate syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(codeBuilder.ToString(), _parseOptions);
        
        // generate compilation
        var compilation = CSharpCompilation.Create(
            Path.GetRandomFileName(),
            [syntaxTree],
            References.Select(assembly => MetadataReference.CreateFromFile(assembly.Location)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, usings: Usings)
        );
        
        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            // TODO: generate errors with location references to the original template
            throw new CompilerErrorException("One or more errors were found.", errors.Select(e => e.GetMessage()));
        }
        
        // generate assembly
        using (var ms = new MemoryStream())
        {
            var result = compilation.Emit(ms, pdbStream: null);

            if (!result.Success)
            {
                // TODO: generate errors with location references to the original template
                throw new CompilerErrorException("One or more errors were found.", []);
                //int lineOffset = _codeTemplate.GetCodeLineOffset(code);
                //return EvaluationResult.WithErrors(result.Diagnostics.Where(x => x.Severity == DiagnosticSeverity.Error).Select(x => GetDynamicScriptError(x, lineOffset)));
                
            }
            var assemblyBytes = ms.ToArray();
            var assembly = Assembly.Load(assemblyBytes);
            
            // return template generator
            var compilationClassType = assembly.GetType($"{_COMPILATION_NAMESPACE}.{_COMPILATION_CLASS}")!;
            var compilationMethodType = compilationClassType.GetMethod(_COMPILATION_METHOD)!;

            //var invokeMethod = typeof(MethodInfo).GetMethod("Invoke", types: [typeof(object?[])]);
            //return invokeMethod.Invoke(compilationMethodType) as TDelegate;
            
            var d = CreateDelegate(compilationMethodType);
            return (TDelegate)d;
        }
        
        throw new NotImplementedException();
    }
    
    public static Delegate CreateDelegate(MethodInfo methodInfo, object? target = null) {
        Func<Type[], Type> getType;
        var isAction = methodInfo.ReturnType.Equals((typeof(void)));
        var types = methodInfo.GetParameters().Select(p => p.ParameterType);

        if (isAction) {
            getType = Expression.GetActionType;
        }
        else {
            getType = Expression.GetFuncType;
            types = types.Concat(new[] { methodInfo.ReturnType });
        }

        if (methodInfo.IsStatic)
        {
            return methodInfo.CreateDelegate<TDelegate>();
            return Delegate.CreateDelegate(typeof(TDelegate), methodInfo);
        }

        if (target == null)
        {
            throw new ArgumentException($"{nameof(target)} must not be null for non-static methods", nameof(target));
        }
            
        return Delegate.CreateDelegate(typeof(TDelegate), target, methodInfo.Name);
    }

    private void EvaluateRoot(IEnumerator<Token> tokenEnumerator, StringBuilder codeBuilder)
    {
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.Literal:
                    string literal = SymbolDisplay.FormatLiteral(token.Value, true);
                    codeBuilder.AppendLine($"yield return {literal};");
                    break;
                case TokenType.LeftCommentDelimiter:
                    EvaluateComment(tokenEnumerator, codeBuilder);
                    break;
                case TokenType.LeftContentDelimiter:
                    EvaluateContent(tokenEnumerator, codeBuilder);
                    break;
                case TokenType.LeftStatementDelimiter:
                    EvaluateStatement(tokenEnumerator, codeBuilder);
                    break;
                default:
                    throw new SyntaxException(); // TODO: add specific information about position and kind of the error
            }
        }
    }

    private void EvaluateStatement(IEnumerator<Token> tokenEnumerator, StringBuilder codeBuilder)
    {
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.CSharp:
                    codeBuilder.AppendLine(token.Value);
                    break;
                case TokenType.RightStatementDelimiter:
                    return;
                default:
                    throw new SyntaxException(); // TODO: add specific information about position and kind of the error
            }
        }
    }

    private void EvaluateContent(IEnumerator<Token> tokenEnumerator, StringBuilder codeBuilder)
    {
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.CSharp:
                    string code = token.Value.Trim();
                    if (code.Length == 0)
                    {
                        // TODO: add specific information about the position of the error
                        throw new SyntaxException("Content expression must not be empty");
                    }

                    if (code.Last() != ';')
                    {
                        code += ";";
                    }

                    codeBuilder.AppendLine($"yield return {code}");
                    break;
                case TokenType.RightContentDelimiter:
                    return;
                default:
                    throw new SyntaxException(); // TODO: add specific information about position and kind of the error
            }
        }
    }

    private void EvaluateComment(IEnumerator<Token> tokenEnumerator, StringBuilder codeBuilder)
    {
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            switch (token.TokenType)
            {
                case TokenType.Literal:
                    // do nothing
                    break;
                case TokenType.RightCommentDelimiter:
                    return;
                default:
                    throw new SyntaxException(); // TODO: add specific information about position and kind of the error
            }
        }
    }
}