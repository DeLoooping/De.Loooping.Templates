using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using De.Loooping.Templates.Core.CodeMapping;
using De.Loooping.Templates.Core.Configuration;
using De.Loooping.Templates.Core.Configuration.Validation;
using De.Loooping.Templates.Core.Diagnostic;
using De.Loooping.Templates.Core.Template.CustomBlocks;
using De.Loooping.Templates.Core.Tokenizers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace De.Loooping.Templates.Core.Template;

public abstract class TemplateBuilderBase<TDelegate>
    where TDelegate: class, MulticastDelegate
{
    private const string _COMPILATION_NAMESPACE = "TemplateCompilation";
    private const string _CALLER_CLASS = "TemplateCaller";
    private const string _CALLER_METHOD = "Run";

    private static readonly Regex _containsWhitespaceRegex = new Regex("\\s", RegexOptions.Compiled);

    private readonly string _template;
    private readonly CSharpParseOptions _parseOptions;
    
    private CodeMapper? _codeMapper = null;

    private readonly Lazy<List<KeyValuePair<string, Type>>> _parameters;
    private IEnumerable<KeyValuePair<string, Type>> Parameters => _parameters.Value;

    private readonly Dictionary<string, ICustomBlock> _customBlocks = new();
    internal IEnumerable<ICustomBlock> CustomBlocks => _customBlocks.Values; // used for tests

    private readonly TemplateProcessorConfiguration _configuration;
    public TemplateProcessorConfiguration Configuration => _configuration;

    public HashSet<string> Usings { get; } = new();
    public HashSet<Assembly> References { get; } = new();
    
    internal TemplateBuilderBase(string template, TemplateProcessorConfiguration? configuration = null)
    {
        if (!typeof(TDelegate).IsSubclassOf(typeof(Delegate)))
        {
            throw new ArgumentException($"{typeof(TDelegate).Name} must be a delegate type");
        }
        
        if (typeof(TDelegate).GetMethod("Invoke")?.ReturnType != typeof(String))
        {
            throw new ArgumentException($"{typeof(TDelegate).Name} must have a return type of string");
        }

        if (configuration != null)
        {
            ValidateConfiguration(configuration);
        }

        _configuration = configuration ?? new();
        
        _template = template;
        _parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(_configuration.LanguageVersion)
            .WithKind(SourceCodeKind.Regular);

        _parameters = new Lazy<List<KeyValuePair<string, Type>>>(() => GetParameters().ToList());
    }

    private void ValidateConfiguration(TemplateProcessorConfiguration configuration)
    {
        var validationResults = 
            new TemplateProcessorConfigurationValidation()
                {
                    CheckCustomBlockConfiguration = _customBlocks.Any()
                }
                .Validate(null, configuration);
        if (validationResults.Failed)
        {
            throw new ArgumentException($"{nameof(TemplateProcessorConfiguration)} {nameof(configuration)} is invalid:\n" +
                                        $"{String.Join('\n', validationResults.Failures)}", nameof(configuration));
        }
    }

    #region convenience methods
    public void AddType<T>()
    {
        AddType(typeof(T));
    }

    public void AddType(Type type)
    {
        AddType(type, References, Usings);
    }
    
    public void AddUsing(string @using)
    {
        Usings.Add(@using);
    }

    public void AddReference(Assembly reference)
    {
        AddReference(reference, References);
    }
    #endregion

    public void AddCustomBlock(ICustomBlock customBlock, string? identifier = null)
    {
        if (identifier != null && _containsWhitespaceRegex.IsMatch(identifier))
        {
            throw new ArgumentException(
                $"A custom block identifier must not contain any whitespaces. Bad identifier: '{identifier}'.",
                nameof(identifier));
        }

        if (identifier == null && _containsWhitespaceRegex.IsMatch(customBlock.DefaultIdentifier))
        {
            throw new ArgumentException(
                $"A custom block identifier must not contain any whitespaces. Bad identifier: '{customBlock.DefaultIdentifier}'.",
                $"{nameof(customBlock)}.{nameof(customBlock.DefaultIdentifier)}");
        }
        
        string effectiveIdentifier = identifier ?? customBlock.DefaultIdentifier;
        if (!_customBlocks.TryAdd(effectiveIdentifier, customBlock))
        {
            throw new ArgumentException($"A custom block with identifier '{identifier}' was already added.");
        }
    }

    private void AddType(Type type, HashSet<Assembly> references, HashSet<string> usings)
    {
        var assembly = type.Assembly;
        AddReference(assembly, references);
        AddForwardingAssemblyReferences(type, references);
        
        if (type.Namespace != null)
        {
            usings.Add(type.Namespace);
        }

        if (type.IsGenericType)
        {
            foreach (Type typeArgument in type.GenericTypeArguments)
            {
                AddType(typeArgument, references, usings);
            }
        }
    }

    private void AddForwardingAssemblyReferences(Type forwardedType, HashSet<Assembly> references)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetForwardedTypes().Contains(forwardedType));

        foreach (var assembly in assemblies)
        {
            AddReference(assembly, references);
        }
    }

    private void AddReference(Assembly reference, HashSet<Assembly> references)
    {
        if (!references.Contains(reference))
        {
            references.Add(reference);

            foreach (var assemblyName in reference.GetReferencedAssemblies())
            {
                AddReference(assemblyName, references);
            }
        }
    }

    private void AddReference(AssemblyName assemblyName, HashSet<Assembly> references)
    {
        // assumption: the assembly is already loaded
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().FullName == assemblyName.FullName);

        if(assembly != null)
        {
            AddReference(assembly);
        }
    }
    
    internal CodeMapper CodeMapper
    {
        get
        {
            if (_codeMapper == null)
            {
                throw new NullReferenceException($"{nameof(CodeMapper)} has not been initialized yet");
            }
            return _codeMapper;
        }
        private set => _codeMapper = value;
    }

    private void AddDefaults(HashSet<Assembly> references, HashSet<string> usings)
    {
        var types = new[] { 
            typeof(int),
            typeof(Enumerable),
            typeof(IEnumerable<>),
            typeof(Object),
            typeof(Regex),

            typeof(RuntimeErrorException),
            typeof(CodeLocation),
            typeof(CodeMapper)
        };
        
        foreach (Type type in types)
        {
            AddReference(type.Assembly, references);
            AddForwardingAssemblyReferences(type, references);
        }
        
        AddReference(AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name == "System.Runtime"), references);
        
        usings.Add("System");
        usings.Add("System.Linq");
        usings.Add("System.Collections.Generic");
    }

    private void AddParameterTypeReferences(HashSet<Assembly> references, HashSet<string> usings)
    {
        foreach (var kvp in Parameters)
        {
            Type type = kvp.Value;
            AddType(type, references, usings);
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

    public TDelegate Build()
    {
        ValidateConfiguration(_configuration);
        
        HashSet<Assembly> references = References.ToHashSet();
        HashSet<string> usings = Usings.ToHashSet();
        AddDefaults(references, usings);
        AddParameterTypeReferences(references, usings);
        
        // generate code
        var templateCode = GenerateTemplateCode(usings, out CodeMapper codeMapper);
        CodeMapper = codeMapper;

        string codeHash = CreateHash(templateCode);
        string assemblyName = $"template_{codeHash}";
        string symbolsName = Path.ChangeExtension(assemblyName, "pdb");
        string sourceCodePath = $"{assemblyName}.cs";
        
        Encoding encoding = Encoding.UTF8;
        byte[] buffer = encoding.GetBytes(templateCode);
        SourceText sourceText = SourceText.From(buffer, buffer.Length, encoding, canBeEmbedded: true);

        // generate syntax tree
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText, _parseOptions, path: sourceCodePath);
        
        var syntaxDiagnostics = syntaxTree.GetDiagnostics();
        var syntaxErrors = syntaxDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (syntaxErrors.Any())
        {
            var errors = syntaxErrors.Select(e => e.ToError(codeMapper));
            throw new SyntaxErrorException("One or more errors were found.", errors);
        }
        
        // generate compilation
        var syntaxRootNode = syntaxTree.GetRoot() as CSharpSyntaxNode;
        var encoded = CSharpSyntaxTree.Create(syntaxRootNode!, null, sourceCodePath, encoding);

        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { encoded },
            references.Select(assembly => MetadataReference.CreateFromFile(assembly.Location)),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, usings: Usings)
                .WithOptimizationLevel(OptimizationLevel.Debug)
                .WithPlatform(Platform.AnyCpu)
        );
        
        var compilerDiagnostics = compilation.GetDiagnostics();
        var compilerErrors = compilerDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (compilerErrors.Any())
        {
            var errors = compilerErrors.Select(e => e.ToError(codeMapper));
            throw new CompilerErrorException("One or more errors were found.", errors);
        }
        
        // generate assembly
        using (var assemblyStream = new MemoryStream())
        {
            var emitOptions = new EmitOptions(
                debugInformationFormat: DebugInformationFormat.Embedded,
                pdbFilePath: symbolsName,
                defaultSourceFileEncoding: encoding);

            var result = compilation.Emit(
                assemblyStream,
                embeddedTexts: new[] { EmbeddedText.FromSource(sourceCodePath, sourceText) },
                options: emitOptions);

            if (!result.Success)
            {
                var errorDiagnostics = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
                var errors = errorDiagnostics.Select(d => d.ToError(codeMapper));
                throw new CompilerErrorException("One or more errors were found.", errors);
            }
            var assemblyBytes = assemblyStream.ToArray();
            var assembly = Assembly.Load(assemblyBytes);
            
            // extract template method from created assembly
            var callerClassType = assembly.GetType($"{_COMPILATION_NAMESPACE}.{_CALLER_CLASS}")!;
            var callerClassConstructor = callerClassType.GetConstructor(new[] { typeof(CodeMapper) })!;
            var callerObject = callerClassConstructor.Invoke(new object[] { codeMapper });
            
            var callerMethodType = callerClassType.GetMethod(_CALLER_METHOD)!;
            var d = CreateDelegate(callerMethodType, callerObject);
            
            // return template method
            return (TDelegate)d;
        }
    }

    /// <summary>
    /// Creates a SHA256 hash
    /// </summary>
    /// <param name="content"></param>
    /// <returns></returns>
    private static string CreateHash(string content)
    {
        using (var sha = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            byte[] hashBytes = sha.ComputeHash(bytes);
            string hash = String.Concat(hashBytes.Select(c => c.ToString("x2")));
            return hash;
        }
    }

    private string GenerateTemplateCode(HashSet<string> usings, out CodeMapper codeMapper)
    {
        Tokenizer tokenizer = new Tokenizer(_configuration, _parseOptions);
        List<Token> tokens = tokenizer.Tokenize(_template);

        TemplateCodeGenerator templateCodeGenerator = new(_COMPILATION_NAMESPACE, _CALLER_CLASS, _CALLER_METHOD, _configuration, _parseOptions, _customBlocks);
        string templateCode = templateCodeGenerator.Generate(tokens, Parameters, usings, out codeMapper);

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