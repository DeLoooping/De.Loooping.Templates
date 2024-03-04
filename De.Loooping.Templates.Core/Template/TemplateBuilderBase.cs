using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.TemplateProcessors;
using De.Loooping.Templates.Core.Tokenizers;
using De.Loooping.Templates.Core.Tools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace De.Loooping.Templates.Core.Template;

public delegate string Process();

public abstract class TemplateBuilderBase<TDelegate>
    where TDelegate: class, MulticastDelegate
{
    private const string _COMPILATION_NAMESPACE = "TemplateCompilationNamespace";
    private const string _COMPILATION_CLASS = "TemplateCompilationClass";
    private const string _COMPILATION_ENUMERABLE_METHOD = "GetParts";
    private const string _COMPILATION_METHOD = "Process";

    private readonly TemplateProcessorConfiguration _configuration;
    private readonly string _template;
    private readonly CSharpParseOptions _parseOptions;
    
    #region convenience methods
    public void AddType<T>()
    {
        AddType(typeof(T));
    }
    
    public void AddUsing(string @using)
    {
        Usings.Add(@using);
    }

    public void AddReference(Assembly reference)
    {
        References.Add(reference);
    }
    #endregion

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

    public TemplateBuilderBase(string template, TemplateProcessorConfiguration? configuration = null)
    {
        if (!typeof(TDelegate).IsSubclassOf(typeof(Delegate)))
        {
            throw new ArgumentException($"{typeof(TDelegate).Name} must be a delegate type");
        }
        
        if (typeof(TDelegate).GetMethod("Invoke")?.ReturnType != typeof(String))
        {
            throw new ArgumentException($"{typeof(TDelegate).Name} must have a return type of string");
        }
        
        _configuration = configuration ?? new();
        _template = template;
        _parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(_configuration.LanguageVersion)
            .WithKind(SourceCodeKind.Regular);
    }

    private void AddDefaultReferences(HashSet<Assembly> references)
    {
        references.Add(typeof(int).Assembly);
        references.Add(typeof(Enumerable).Assembly);
        references.Add(typeof(Object).Assembly);
        references.Add(typeof(IEnumerable<>).Assembly);
        references.Add(AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name == "System.Runtime"));
    }

    private void AddDefaultUsings(HashSet<string> usings)
    {
        usings.Add("System");
        usings.Add("System.Linq");
        usings.Add("System.Collections.Generic");
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
        HashSet<Assembly> references = References.ToHashSet();
        AddDefaultReferences(references);

        HashSet<string> usings = Usings.ToHashSet();
        AddDefaultUsings(usings);

        
        // generate code
        var templateCode = GenerateTemplateCode(usings);

        // generate syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(templateCode, _parseOptions);
        
        // generate compilation
        var compilation = CSharpCompilation.Create(
            Path.GetRandomFileName(),
            [syntaxTree],
            references.Select(assembly => MetadataReference.CreateFromFile(assembly.Location)),
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
            
            // return template
            var compilationClassType = assembly.GetType($"{_COMPILATION_NAMESPACE}.{_COMPILATION_CLASS}")!;
            var compilationMethodType = compilationClassType.GetMethod(_COMPILATION_METHOD)!;

            var d = CreateDelegate(compilationMethodType);
            return (TDelegate)d;
        }
        
        throw new NotImplementedException();
    }

    private string GenerateTemplateCode(HashSet<string> usings)
    {
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

        foreach (string u in usings)
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

        TemplateCodeGenerator templateCodeGenerator = new();
        string generatedCode = templateCodeGenerator.Generate(tokens);
        codeBuilder.AppendLine(generatedCode);
        
        codeBuilder.AppendLine("}\n}");

        string templateCode = codeBuilder.ToString();
        return templateCode;
    }

    public static Delegate CreateDelegate(MethodInfo methodInfo, object? target = null)
    {
        if (methodInfo.IsStatic)
        {
            return methodInfo.CreateDelegate<TDelegate>();
        }

        if (target == null)
        {
            throw new ArgumentException($"{nameof(target)} must not be null for non-static methods", nameof(target));
        }

        return Delegate.CreateDelegate(typeof(TDelegate), target, methodInfo.Name);
    }
}