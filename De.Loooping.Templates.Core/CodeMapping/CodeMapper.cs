using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using De.Loooping.Templates.Core.Diagnostic;

namespace De.Loooping.Templates.Core.CodeMapping;

internal class CodeMapper
{
    private class Newline
    {
        public required int Index { get; init; }
        public required int CharPosition { get; init; }
    }

    private class NewlinePositionComparer : IComparer<Newline>
    {
        public int Compare(Newline? x, Newline? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            return x.CharPosition.CompareTo(y.CharPosition);
        }
    }
    
    private class CodeMapping()
    {
        public required int GeneratedCodeStart { get; init; }
        public required int GeneratedCodeEnd { get; init; }
        
        public required int GeneratingCodeStart { get; init; }
        public required int GeneratingCodeEnd { get; init; }
        public required CodeType CodeType { get; init; }
    }

    private class GeneratedCodePositionComparer : IComparer<CodeMapping>
    {
        public int Compare(CodeMapping? x, CodeMapping? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;

            int codeStartComparison = x.GeneratedCodeStart.CompareTo(y.GeneratedCodeStart);
            if (codeStartComparison != 0)
                return codeStartComparison;
            
            int codeEndComparison = x.GeneratedCodeEnd.CompareTo(y.GeneratedCodeEnd);
            return codeEndComparison;
        }
    }

    private StringBuilder _generatedCode = new();
    private StringBuilder _generatingCode = new();

    private int _generatingCodeNewlines = 0;
    private readonly SortedSet<Newline> _generatingCodeNewlinePositions = new(new NewlinePositionComparer());
    private readonly List<int> _generatedCodeNewlinePositions = new();
    private readonly SortedSet<CodeMapping> _codeMapping = new(new GeneratedCodePositionComparer());

    private readonly Regex _newLineRegex = new Regex("\n", RegexOptions.Compiled);

    public string GeneratedCode => _generatedCode.ToString();
    public int GeneratedCodeLength => _generatedCode.Length;
    
    public string GeneratingCode => _generatingCode.ToString();
    public int GeneratingCodeLength => _generatingCode.Length;
    
    private void AddNewlinePositionsForGeneratingCode(string generatingCode)
    {
        foreach (Match match in _newLineRegex.Matches(generatingCode))
        {
            var newline = new Newline()
            {
                Index = _generatingCodeNewlines,
                CharPosition = match.Index + _generatingCode.Length,
            };
            _generatingCodeNewlinePositions.Add(newline);
            _generatingCodeNewlines++;
        }
    }

    private void AddNewlinePositionsForGeneratedCode(string generatedCode)
    {
        foreach (Match match in _newLineRegex.Matches(generatedCode))
        {
            _generatedCodeNewlinePositions.Add(match.Index + _generatedCode.Length);
        }
    }

    /// <summary>
    /// Adds code from user input that is not reflected in any generated code
    /// </summary>
    /// <param name="generatingCode"></param>
    public void AddNilGeneratingCode(string generatingCode)
    {
        AddNewlinePositionsForGeneratingCode(generatingCode);

        _codeMapping.Add(new CodeMapping()
        {
            GeneratedCodeStart = _generatedCode.Length,
            GeneratedCodeEnd = _generatedCode.Length,
            GeneratingCodeStart = _generatingCode.Length,
            GeneratingCodeEnd = _generatingCode.Length + generatingCode.Length,
            CodeType = CodeType.NilGenerating
        });
        
        _generatingCode.Append(generatingCode);
    }

    /// <summary>
    /// Adds code that is generated from no user input (boilerplate, etc.)
    /// </summary>
    /// <param name="generatedCode">The generated code.</param>
    /// <param name="codeType">The code type.</param>
    public void AddGeneratedCodeFromNil(string generatedCode, CodeType codeType = CodeType.FromNilGenerated)
    {
        AddNewlinePositionsForGeneratedCode(generatedCode);

        _codeMapping.Add(new CodeMapping()
        {
            GeneratedCodeStart = _generatedCode.Length,
            GeneratedCodeEnd = _generatedCode.Length + generatedCode.Length,
            GeneratingCodeStart = _generatingCode.Length,
            GeneratingCodeEnd = _generatingCode.Length,
            CodeType = codeType
        });

        _generatedCode.Append(generatedCode);
    }

    /// <summary>
    /// Adds code where generating code is 1:1 the same as generated code 
    /// </summary>
    /// <param name="code">The code.</param>
    public void AddUserProvidedCode(string code)
    {
        AddNewlinePositionsForGeneratingCode(code);
        AddNewlinePositionsForGeneratedCode(code);

        CodeMapping codeMapping = new CodeMapping()
        {
            GeneratedCodeStart = _generatedCode.Length,
            GeneratedCodeEnd = _generatedCode.Length + code.Length,
            GeneratingCodeStart = _generatingCode.Length,
            GeneratingCodeEnd = _generatingCode.Length + code.Length,
            CodeType = CodeType.UserProvided
        };
        _codeMapping.Add(codeMapping);

        _generatedCode.Append(code);
        _generatingCode.Append(code);
    }
    
    /// <summary>
    /// Adds user input (generating code) that contains escape sequences. 
    /// </summary>
    /// <param name="generatedCode">The generated (already escaped) code.</param>
    /// <param name="escapeRegex">A Regex that identifies escape sequences. Regex group "escape" must contain the escape character(s) and group "escaped" must contain the escaped charcter(s).</param>
    public void AddEscapedUserProvidedCode(string generatedCode, Regex escapeRegex)
    {
        StringBuilder sb = new();
        for (int i = 0; i < generatedCode.Length; i++)
        {
            var match = escapeRegex.Match(generatedCode, i);
            if (match.Success)
            {
                if (sb.Length > 0)
                {
                    AddUserProvidedCode(sb.ToString());
                    sb.Clear();
                }
                
                string escapeString = match.Groups["escape"].Value;
                AddGeneratedCodeFromNil(escapeString, CodeType.EscapeSequence);
                i += match.Length - 1; // skip complete escape string
                
                string escapedString = match.Groups["escaped"].Value;
                sb.Append(escapedString);
            }
            else
            {
                sb.Append(generatedCode[i]);
            }
        }
        
        if (sb.Length > 0)
        {
            AddUserProvidedCode(sb.ToString());
        }
    }

    public CodePosition GetGeneratingCodePosition(CodePosition generatedCodePosition)
    {
        int rowStart = generatedCodePosition.Row > 0 ? _generatedCodeNewlinePositions[generatedCodePosition.Row - 1] + 1 : 0;
        int characterPositionInGeneratedCode = rowStart + generatedCodePosition.Column; // starts one char after the newline 

        return GetGeneratingCodePosition(characterPositionInGeneratedCode);
    }

    public CodePosition GetGeneratingCodePosition(int characterPositionInGeneratedCode)
    {
        CodeType codeType = CodeType.Unknown;

        CodeMapping? mapping = null;
        while (codeType != CodeType.UserProvided) // find the last user provided code
        {
            mapping = GetMapping(characterPositionInGeneratedCode);
            if (mapping == null)
            {
                throw new NullReferenceException($"{nameof(mapping)}");
            }

            if (codeType == CodeType.EscapeSequence && mapping.CodeType != CodeType.UserProvided )
            {
                // this would lead to an endless loop
                throw new UnreachableException("This exception should never be thrown and indicates a bug. Please contact the library maintainer.");
            }
            
            codeType = mapping.CodeType;
            if (codeType == CodeType.EscapeSequence)
            {
                characterPositionInGeneratedCode = mapping.GeneratedCodeEnd; // next segment should be the escaped code
            }
            else if (codeType != CodeType.UserProvided)
            {
                characterPositionInGeneratedCode = mapping.GeneratedCodeStart - 1; // look up previous segment
            }
        }

        if (mapping == null)
        {
            throw new NullReferenceException($"{nameof(mapping)}");
        }

        int characterOffsetFromMappingStart = characterPositionInGeneratedCode - mapping.GeneratedCodeStart;

        int characterPositionInGeneratingCode = mapping.GeneratingCodeStart + characterOffsetFromMappingStart;
        var lastNewline = GetLastNewlineInGeneratingCodeBefore(characterPositionInGeneratingCode);

        int lastNewlineIndex = lastNewline != null ? lastNewline.Index + 1 : 0;
        int column = characterPositionInGeneratingCode - (lastNewline?.CharPosition ?? 0);

        return new CodePosition(lastNewlineIndex, column);
    }

    private Newline? GetLastNewlineInGeneratingCodeBefore(int characterPositionInGeneratingCode)
    {
        if (_generatingCodeNewlinePositions.Count == 0 || _generatingCodeNewlinePositions.Min!.CharPosition >= characterPositionInGeneratingCode)
        {
            return null;
        }

        var lastNewline = _generatingCodeNewlinePositions.GetViewBetween(_generatingCodeNewlinePositions.Min, new Newline()
        {
            CharPosition = characterPositionInGeneratingCode - 1,
            Index = 0 // not needed for comparison
        }).Max;
        return lastNewline;
    }

    private CodeMapping? GetMapping(int characterPositionInGeneratedCode)
    {
        var mapping = _codeMapping.GetViewBetween(_codeMapping.Min, new CodeMapping()
        {
            GeneratedCodeStart = characterPositionInGeneratedCode,
            GeneratedCodeEnd = Int32.MaxValue,
            GeneratingCodeStart = 0,
            GeneratingCodeEnd = 0,
            CodeType = CodeType.Unknown
        }).Max;

        return mapping;
    }
}