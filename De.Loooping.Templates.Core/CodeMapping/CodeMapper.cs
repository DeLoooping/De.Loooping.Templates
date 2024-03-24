using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using De.Loooping.Templates.Core.Diagnostic;

namespace De.Loooping.Templates.Core.CodeMapping;

public class CodeMapper
{
    #region private classes
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

    private class CodeSegment
    {
        public CodeSegment(int start, int end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Segment is including this start character position
        /// </summary>
        public int Start { get; }
        
        /// <summary>
        /// Segment is excluding this end character position
        /// </summary>
        public int End { get; }
    }
    
    private class CodeMapping
    {
        public required int Index { get; init; }
        
        public CodeSegment? GeneratedCode { get; init; }
        public CodeSegment? GeneratingCode { get; init; }
        
        public CodeMapping? PreviousGeneratedCodeMapping { get; init; }
        public CodeMapping? PreviousGeneratingCodeMapping { get; init; }
    }
    #endregion
    
    private StringBuilder _generatedCode = new();
    private StringBuilder _generatingCode = new();

    private int _generatingCodeNewlines = 0;
    private readonly SortedSet<Newline> _generatingCodeNewlinePositions = new(new NewlinePositionComparer());
    private readonly List<int> _generatedCodeNewlinePositions = new();
    
    private readonly List<CodeMapping> _codeMapping = new();

    private readonly Regex _newLineRegex = new Regex("\n", RegexOptions.Compiled);

    internal static readonly EscapeSequenceMatcher VerbatimStringEscapeSequenceMatcher = new EscapeSequenceMatcher(
        new Regex("\\G\"\"", RegexOptions.Compiled),
        match => "\""); 
    
    // TODO:
    // Unescaping with Regex.Unescape might not match exactly with the output
    // from SymbolDisplay.FormatLiteral (used in TemplateCodegenerator)
    internal static readonly EscapeSequenceMatcher StringLiteralEscapeSequenceMatcher = new EscapeSequenceMatcher(
        new Regex(@"\G\\(x[0-9a-fA-F]{2}|u[0-9a-fA-F]{4}|[^xu])", RegexOptions.Compiled),
        match => Regex.Unescape(match.Value)); 
    
    internal static readonly EscapeSequenceMatcher BracketEscapeSequenceMatcher = new EscapeSequenceMatcher(
        new Regex(@"\G({{|}})", RegexOptions.Compiled),
        match =>
        {
            switch (match.Value)
            {
                case "{{":
                    return "{";
                case "}}":
                    return "}";
                default:
                    throw new ArgumentException(nameof(match));
            }
        });

    internal string GeneratedCode => _generatedCode.ToString();
    internal int GeneratedCodeLength => _generatedCode.Length;
    
    internal string GeneratingCode => _generatingCode.ToString();
    internal int GeneratingCodeLength => _generatingCode.Length;
    
    /// <summary>
    /// Adds code from user input that is not reflected in any generated code
    /// </summary>
    /// <param name="generatingCode"></param>
    internal void AddNilGeneratingCode(string generatingCode)
    {
        AddTranslatedCode(generatingCode, null);
    }

    /// <summary>
    /// Adds code that is generated from no user input (boilerplate, etc.)
    /// </summary>
    /// <param name="generatedCode">The generated code.</param>
    internal void AddGeneratedCodeFromNil(string generatedCode)
    {
        AddTranslatedCode(null, generatedCode);
    }

    /// <summary>
    /// Adds code where generating code is 1:1 the same as generated code 
    /// </summary>
    /// <param name="code">The code.</param>
    internal void AddUserProvidedCode(string code)
    {
        AddTranslatedCode(code, code);
    }

    /// <summary>
    /// Adds code that is translated to some different code (escape sequences, etc.)
    /// </summary>
    /// <param name="generatingCode">The generating code.</param>
    /// <param name="generatedCode">The generated code.</param>
    internal void AddTranslatedCode(string? generatingCode, string? generatedCode)
    {
        var generatedCodeSegment = ProcessCode(generatedCode, AddNewlinePositionsForGeneratedCode, _generatedCode);
        var generatingCodeSegment = ProcessCode(generatingCode, AddNewlinePositionsForGeneratingCode, _generatingCode);

        var lastMapping = _codeMapping.LastOrDefault();
        
        _codeMapping.Add(new CodeMapping()
        {
            Index = _codeMapping.Count,
            
            GeneratedCode = generatedCodeSegment,
            GeneratingCode = generatingCodeSegment,
            
            PreviousGeneratedCodeMapping = lastMapping?.GeneratedCode == null ? lastMapping?.PreviousGeneratedCodeMapping : lastMapping,
            PreviousGeneratingCodeMapping = lastMapping?.GeneratingCode == null ? lastMapping?.PreviousGeneratingCodeMapping : lastMapping,
        });
    }
    
    /// <summary>
    /// Adds user input (generating code) that contains escape sequences. 
    /// </summary>
    /// <param name="generatedCode">The generated (already escaped) code.</param>
    /// <param name="escapeSequenceMatcher">A Regex that identifies escape sequences. Regex group "escape" must contain the escape character(s) and group "escaped" must contain the escaped charcter(s).</param>
    internal void AddEscapedUserProvidedCode(string generatedCode, EscapeSequenceMatcher escapeSequenceMatcher)
    {
        StringBuilder sb = new();
        for (int i = 0; i < generatedCode.Length; i++)
        {
            var match = escapeSequenceMatcher.Match(generatedCode, i);
            if (match.Success)
            {
                if (sb.Length > 0)
                {
                    AddUserProvidedCode(sb.ToString());
                    sb.Clear();
                }
                
                string generatingSequence = match.UnescapedSequence!;
                string generatedSequence = match.EscapedSequence!;
                AddTranslatedCode(generatingSequence, generatedSequence);

                i += match.EscapedSequence!.Length - 1; // skip complete escape sequence
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

    /// <summary>
    /// Finds the nearest location in the original (generating) code that
    /// corresponds to the supplied location from the generated code.
    /// </summary>
    /// <param name="generatedCodeLocation"></param>
    /// <returns>The corresponding location inside the generating code.</returns>
    public CodeLocation GetGeneratingCodeLocation(CodeLocation generatedCodeLocation)
    {
        int zeroBasedline = generatedCodeLocation.Line - 1;
        int zeroBasedColumn = generatedCodeLocation.Column - 1;
        
        int lineStart = zeroBasedline > 0 ? _generatedCodeNewlinePositions[zeroBasedline - 1] + 1 : 0;
        int characterPositionInGeneratedCode = lineStart + zeroBasedColumn; // starts one char after the newline 

        return GetGeneratingCodeLocation(characterPositionInGeneratedCode);
    }

    /// <summary>
    /// Finds the nearest location in the original (generating) code that
    /// corresponds to the supplied location from the generated code.
    /// </summary>
    /// <param name="characterPositionInGeneratedCode">The number of characters, counted from the generated codes beginning.</param>
    /// <returns>he corresponding location inside the generating code.</returns>
    public CodeLocation GetGeneratingCodeLocation(int characterPositionInGeneratedCode)
    {
        var mapping = FindCodeMappingFromCharacterPositionInGeneratedCode(characterPositionInGeneratedCode);
        if (mapping == null)
        {
            // no generating code before the given position
            return new CodeLocation(1, 1);
        }

        if (mapping.GeneratedCode == null)
        {
            throw new UnreachableException("Shouldn't have happened");
        }

        int positionFromSegmentStart = characterPositionInGeneratedCode - mapping.GeneratedCode.Start;
        
        int characterPositionInGeneratingCode;
        if (mapping.GeneratingCode != null)
        {
            characterPositionInGeneratingCode = Math.Min( // assure that the result position is still inside the segment
                mapping.GeneratingCode.Start + positionFromSegmentStart, mapping.GeneratingCode.End - 1);
        }
        else
        {
            characterPositionInGeneratingCode = mapping.PreviousGeneratingCodeMapping?.GeneratingCode?.End - 1 ?? 0;
        }
        
        return GetCodeLocationFromGeneratingCodeCharacterPosition(characterPositionInGeneratingCode);
    }

    private CodeSegment? ProcessCode(string? code, Action<string> addNewlinePositions, StringBuilder codeBuilder)
    {
        if (!String.IsNullOrEmpty(code))
        {
            addNewlinePositions(code);
            var segment = new CodeSegment(codeBuilder.Length, codeBuilder.Length + code.Length);
            codeBuilder.Append(code);
            return segment;
        }

        return null;
    }

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
    
    private CodeMapping? FindCodeMappingFromCharacterPositionInGeneratedCode(int characterPositionInGeneratedCode)
    {
        // do a binary search
        int left = 0;
        int right = _codeMapping.Count;
        
        int mid;
        while(true)
        {
            mid = (left + right) / 2;
            if (mid >= _codeMapping.Count)
            {
                // return the last available mapping
                var codeMapping = _codeMapping[^1];
                return codeMapping.GeneratedCode != null ? codeMapping : codeMapping.PreviousGeneratedCodeMapping;
            }
            
            CodeMapping? mapping = _codeMapping[mid];
            mapping = mapping.GeneratedCode != null ? mapping : mapping.PreviousGeneratedCodeMapping;
            if (mapping?.GeneratedCode == null)
            {
                // no code in generated code before the given character position
                return null;
            }

            if (mapping.GeneratedCode.Start <= characterPositionInGeneratedCode && characterPositionInGeneratedCode < mapping.GeneratedCode.End)
            {
                return mapping;
            }

            if (characterPositionInGeneratedCode < mapping.GeneratedCode.Start)
            {
                right = mapping.Index; // optimization: Index can only be smaller than mid
            }
            else // mapping.GeneratedCode.End >= characterPositionInGeneratedCode
            {
                left = mid + 1;
            }
        }
    }

    private CodeLocation GetCodeLocationFromGeneratingCodeCharacterPosition(int characterPositionInGeneratingCode)
    {
        var lastNewline = GetLastNewlineInGeneratingCodeBefore(characterPositionInGeneratingCode);

        int lastNewlineIndex = lastNewline != null ? lastNewline.Index + 1 : 0;
        int column = characterPositionInGeneratingCode - ((lastNewline?.CharPosition + 1) ?? 0);
        
        int oneBasedLine = lastNewlineIndex + 1;
        int oneBasedColumn = column + 1;
        return new CodeLocation(oneBasedLine, oneBasedColumn);
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
}