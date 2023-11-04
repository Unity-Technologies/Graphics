using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

public enum RegisterType
{
    Scalar,
    Vector,
    Exec,
    VCC,
    M0,
    TMA,
}


public class AliveRegisterRange
{
    public Register register;
    public int startLineIndex;
    public int endLineIndex;
    public int startAssemblyLine;
    public int endAssemblyLine;
}
public class Register
{
    public RegisterType type;
    public int registerIndex;

    public override int GetHashCode()
    {
        return type.GetHashCode() + 27 * registerIndex.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj is Register r)
            return r.type == type && r.registerIndex == registerIndex;
        return false;
    }

    public static bool operator ==(Register obj1, Register obj2)
        => obj1.Equals(obj2);

    public static bool operator !=(Register obj1, Register obj2)
        => !(obj1 == obj2);
}

public class AssemblyLine
{
    public string assembly;
    public int sgprPressure;
    public int vgprPressure;
    public List<Register> registerReads = new();
    public List<Register> registerWrites = new();
    public List<Register> registersAlive = null;
    public CBufferRead cbufferRead = null;
}

public class CBufferRead
{
    public int dwordReadCount;
    public int offset;
    public bool dynamicScalarOffset;
}

public class JumpData
{
    public int jumpLineIndex;
    public int fromIndex;
    public int indentLevel;
}

public class CodeLine
{
    public string code;
    public string highlightedCode;
    public int line;
    public string filePath;
    public List<AssemblyLine> assemblyLines = new();
    public HashSet<Register> registerReads = new();
    public HashSet<Register> registerWrites = new();
    public int sgprPressure;
    public int vgprPressure;
    public List<CBufferRead> cbufferReads = new();
    public JumpData jumpData;
}

public class AliveRegisterMetaData
{
    public int aliveCodeLineCount;
    public int aliveAssemblyLineCount;
    public CodeLine sourceWriteLine;
    public bool start; // is the line the start of the block
    public int startAssemblyLine;
    public int startCodeLine;
}

public class AnalyzedShader
{
    public int maxVGPRUsed;
    public int maxSGPRUsed;
    public List<CodeLine> lines = new();
    public List<Dictionary<Register, AliveRegisterMetaData>> aliveRegistersPerLine = new();
    public List<Dictionary<Register, AliveRegisterMetaData>> aliveRegistersPerAssemblyLine = new();
    public int maxRegisterAliveLineCount; // max duration in line of code for a VGPR to be alive
    public int maxRegisterAliveAssemblyLineCount;
    public int maxAnalyzedAliveVGPROnLine; // max number of register that are alive during a single line of code in the whole program
    public int maxAnalyzedAliveSGPROnLine; // max number of register that are alive during a single line of code in the whole program
    public int maxVGPRAlive;
    public int maxSGPRAlive;
    public BuildTarget target;
}

public static class ShaderAnalyzer
{
    static readonly Regex readRegex = new(@"Reads: ([^.]*)", RegexOptions.Compiled);
    static readonly Regex writeRegex = new(@"Writes: (.*)", RegexOptions.Compiled);
    static readonly Regex noRegisterRegex = new(@"No registers accessed", RegexOptions.Compiled);
    static readonly Regex codeLineRegex = new(@"//\s+(\d+):(\s+.*)", RegexOptions.Compiled);
    static readonly Regex registerPressureRegex = new(@"Register pressure: (\d+) SGPRs, (\d+) VGPRs:", RegexOptions.Compiled);
    static readonly Regex sourceFilePathRegex = new(@"// --- (.*) ---", RegexOptions.Compiled);
    static readonly Regex assemblyLineRegex = new(@"^(\w+\s+[^//]*)\s*", RegexOptions.Compiled);
    static readonly Regex sLoadRegex = new(@"\bs_load_dword(\b|x(\d+))\s+[^,]+,\s+[^,]+,\s+(0x\d+)", RegexOptions.Compiled);
    static readonly Regex sBufferLoadRegex = new(@"\bs_buffer_load_dword(\b|x(\d+))\s+[^,]+,\s+[^,]+,\s+(\w+)", RegexOptions.Compiled);
    static readonly Regex jumpLabelRegex = new(@"(\w+):$", RegexOptions.Compiled);

    static readonly string registerTableBase = @"\/\* \|\s+(\d+)\s+\|(.+?)(?=\d)(\d+)\s+\|(.+?)(?= \*\/)\s+\*\/\s+";
    static readonly string registerTableBaseNoCapture = @"\/\* \|\s+\d+\s+\|.+?(?=\d)\d+\s+\|.+?(?= \*\/)\s+\*\/\s+";
    static readonly Regex codeLineRegex2Regex = new(registerTableBaseNoCapture + @"\/\* (\d+):(.+)(?=\*\/)");
    static readonly Regex sourceFilePath2Regex = new(registerTableBaseNoCapture + @"\/\*(.+)(?=\*\/)"); // Warning: this also matches code line2 regex
    static readonly Regex assemblyLine2Regex = new(registerTableBase + @"(\w+\s+\w+.+)");
    static readonly Regex jumpLabel2Regex = new(registerTableBaseNoCapture + @"(\w+):");

    public static AnalyzedShader ParseCompiledShader(string compiledShaderContent)
    {
        if (compiledShaderContent == null)
            return null;

        AnalyzedShader analyzedShader;
        Dictionary<string, int> jumps;
        StringBuilder wholeSourceCode;
        if (compiledShaderContent.StartsWith("// guid: "))
            analyzedShader = ParseCompiledShaderFromText2(compiledShaderContent, out jumps, out wholeSourceCode);
        else
            analyzedShader = ParseCompiledShaderFromText(compiledShaderContent, out jumps, out wholeSourceCode);

        if (analyzedShader == null)
            return null;

        // Jumps
        List<JumpData> jumpDatas = new();
        for (int i = 0; i < analyzedShader.lines.Count; i++)
        {
            var line = analyzedShader.lines[i];
            foreach (var assembly in line.assemblyLines)
            {
                foreach (var instruction in assembly.assembly.Split())
                {
                    if (jumps.TryGetValue(instruction, out var jumpIndex))
                    {
                        line.jumpData = new JumpData { jumpLineIndex = jumpIndex, fromIndex = i };
                        jumpDatas.Add(line.jumpData);
                    }
                }
            }
        }

        CalculateIndentLevels(jumpDatas.OrderBy(j => Mathf.Min(j.fromIndex, j.jumpLineIndex)).ToList());

        var highlightedSourceCode = SyntaxHighlight.Highlight(wholeSourceCode.ToString());
        var highlightedLines = highlightedSourceCode.Split("\n");
        // Accumulate vgpr pressure and cbuffers from assembly to code
        for (int i = analyzedShader.lines.Count - 1; i >= 0; i--)
        {
            var line = analyzedShader.lines[i];
            line.highlightedCode = highlightedLines[i];
            foreach (var assembly in line.assemblyLines)
            {
                line.vgprPressure = Mathf.Max(line.vgprPressure, assembly.vgprPressure);
                line.sgprPressure = Mathf.Max(line.sgprPressure, assembly.sgprPressure);

                if (assembly.cbufferRead != null)
                    line.cbufferReads.Add(assembly.cbufferRead);
            }
        }

        // Analyze how much time the VGPR are alive:
        Dictionary<Register, (int codeLine, int assemblyLine)> lastRegisterReadIndex = new();
        List<AliveRegisterRange> aliveRegisters = new();
        int assemblyLineIndex = analyzedShader.lines.Sum(l => l.assemblyLines.Count) - 1;
        for (int i = analyzedShader.lines.Count - 1; i >= 0; i--)
        {
            var line = analyzedShader.lines[i];
            // Accumulate assembly info into the code line
            for (int j = line.assemblyLines.Count - 1; j >= 0; j--)
            {
                var assembly = line.assemblyLines[j];
                analyzedShader.maxSGPRAlive = Mathf.Max(analyzedShader.maxSGPRAlive, assembly.sgprPressure);
                analyzedShader.maxVGPRAlive = Mathf.Max(analyzedShader.maxVGPRAlive, assembly.vgprPressure);

                foreach (var r in assembly.registerWrites)
                {
                    line.registerWrites.Add(r);
                    if (lastRegisterReadIndex.TryGetValue(r, out var value))
                    {
                        var range = new AliveRegisterRange { register = r, startLineIndex = i, startAssemblyLine = assemblyLineIndex};
                        range.endLineIndex = value.codeLine;
                        range.endAssemblyLine = value.assemblyLine;
                        lastRegisterReadIndex.Remove(r);
                        aliveRegisters.Add(range);
                    }
                }

                for (int k = assembly.registerReads.Count - 1; k >= 0 ; k--)
                {
                    var r = assembly.registerReads[assembly.registerReads.Count - k - 1];

                    line.registerReads.Add(r);
                    lastRegisterReadIndex.TryAdd(r, (i, assemblyLineIndex));
                }

                assemblyLineIndex--;
            }
        }

        // Add register that are never written but always read (interpolators, CBuffer addresses, etc.)
        foreach (var (r, v) in lastRegisterReadIndex)
        {
            var range = new AliveRegisterRange { register = r, startLineIndex = 0, startAssemblyLine = 0};
            range.endLineIndex = v.codeLine;
            range.endAssemblyLine = v.assemblyLine;
            aliveRegisters.Add(range);
        }

        int maxAliveLineCount = 0;
        int maxAliveAssemblyLineCount = 0;
        int maxVGPRAllocatedOnLine = 0;
        int maxSGPRAllocatedOnLine = 0;
        for (int i = 0; i < analyzedShader.lines.Count; i++)
        {
            var dic = new Dictionary<Register, AliveRegisterMetaData>();
            foreach (var aliveRegister in aliveRegisters)
            {
                int lineCount = aliveRegister.endLineIndex - aliveRegister.startLineIndex;
                int assemblyLineCount = aliveRegister.endAssemblyLine - aliveRegister.startAssemblyLine;
                if (i >= aliveRegister.startLineIndex && i <= aliveRegister.endLineIndex)
                {
                    bool start = i == aliveRegister.startLineIndex;
                    if (dic.TryGetValue(aliveRegister.register, out var existingWrite))
                    {
                        if ((existingWrite.start && !start) || existingWrite.startAssemblyLine > aliveRegister.startAssemblyLine)
                        {
                            lineCount = existingWrite.aliveCodeLineCount;
                            assemblyLineCount = existingWrite.aliveAssemblyLineCount;
                            start = existingWrite.start;
                        }
                    }
                    dic[aliveRegister.register] = new AliveRegisterMetaData
                    {
                        aliveCodeLineCount = lineCount,
                        aliveAssemblyLineCount = assemblyLineCount,
                        sourceWriteLine = analyzedShader.lines[aliveRegister.startLineIndex],
                        start = start,
                        startCodeLine = aliveRegister.startLineIndex,
                        startAssemblyLine = aliveRegister.startAssemblyLine,
                    };
                }

                maxAliveLineCount = Mathf.Max(maxAliveLineCount, lineCount);
                maxAliveAssemblyLineCount = Mathf.Max(maxAliveAssemblyLineCount, assemblyLineCount);
            }
            maxVGPRAllocatedOnLine = Mathf.Max(maxVGPRAllocatedOnLine, dic.Count(d => d.Key.type == RegisterType.Vector));
            maxSGPRAllocatedOnLine = Mathf.Max(maxSGPRAllocatedOnLine, dic.Count(d => d.Key.type == RegisterType.Scalar));


            analyzedShader.aliveRegistersPerLine.Add(dic);
        }

        assemblyLineIndex = 0;
        int codeLineIndex = 0;
        foreach (var codeLine in analyzedShader.lines)
        {
            foreach (var assemblyLine in codeLine.assemblyLines)
            {
                var dic = new Dictionary<Register, AliveRegisterMetaData>();
                foreach (var aliveRegister in aliveRegisters)
                {
                    int lineCount = aliveRegister.endLineIndex - aliveRegister.startLineIndex;
                    int assemblyLineCount = aliveRegister.endAssemblyLine - aliveRegister.startAssemblyLine;
                    if (assemblyLineIndex >= aliveRegister.startAssemblyLine && assemblyLineIndex <= aliveRegister.endAssemblyLine)
                    {
                        bool start = assemblyLineIndex == aliveRegister.startAssemblyLine;
                        if (dic.TryGetValue(aliveRegister.register, out var existingWrite))
                        {
                            if ((existingWrite.start && !start) || existingWrite.startAssemblyLine > aliveRegister.startAssemblyLine)
                            {
                                lineCount = existingWrite.aliveCodeLineCount;
                                assemblyLineCount = existingWrite.aliveAssemblyLineCount;
                                start = existingWrite.start;
                            }
                        }

                        dic[aliveRegister.register] = new AliveRegisterMetaData
                        {
                            aliveCodeLineCount = lineCount,
                            aliveAssemblyLineCount = assemblyLineCount,
                            sourceWriteLine = analyzedShader.lines[aliveRegister.startLineIndex],
                            start = start,
                            startCodeLine = aliveRegister.startLineIndex,
                            startAssemblyLine = aliveRegister.startAssemblyLine,
                        };
                    }
                }
                analyzedShader.aliveRegistersPerAssemblyLine.Add(dic);

                assemblyLineIndex++;
            }

            codeLineIndex++;
        }

        analyzedShader.maxRegisterAliveLineCount = maxAliveLineCount;
        analyzedShader.maxRegisterAliveAssemblyLineCount = maxAliveAssemblyLineCount;
        analyzedShader.maxAnalyzedAliveVGPROnLine = maxVGPRAllocatedOnLine;
        analyzedShader.maxAnalyzedAliveSGPROnLine = maxSGPRAllocatedOnLine;

        return analyzedShader;
    }

    public static AnalyzedShader ParseCompiledShaderFromText(string compiledShaderContent, out Dictionary<string, int> jumps, out StringBuilder wholeSourceCode)
    {
        AnalyzedShader analyzedShader = new();

        analyzedShader.target = BuildTarget.PS4;
        var lines = compiledShaderContent.Split(new string[]{"\n", "\r\n"}, StringSplitOptions.None);
        string currentSourceFilePath = null;
        CodeLine currentCodeLine = null;
        AssemblyLine currentAssemblyLine = null;
        int codeLineIndex = -1;
        wholeSourceCode = new();
        jumps = new();
        foreach (var line in lines)
        {
            var l = line.Trim();
            if (String.IsNullOrWhiteSpace(l))
                continue;

            if (Match(sourceFilePathRegex, l, out var s))
                currentSourceFilePath = s.Groups[1].Value;

            if (Match(codeLineRegex, l, out var c))
            {
                wholeSourceCode.AppendLine(c.Groups[2].Value);
                currentCodeLine = new CodeLine{ code = c.Groups[2].Value, line = int.Parse(c.Groups[1].Value), filePath = currentSourceFilePath };
                analyzedShader.lines.Add(currentCodeLine);
                codeLineIndex++;
            }

            if (Match(assemblyLineRegex, l, out var a))
            {
                var asm = a.Groups[1].Value;
                currentAssemblyLine = new AssemblyLine() { assembly = asm };
                currentCodeLine.assemblyLines.Add(currentAssemblyLine);

                if (Match(sLoadRegex, asm, out var sl))
                {
                    int readCount = 1;
                    if (!String.IsNullOrWhiteSpace(sl.Groups[2].Value))
                        readCount = int.Parse(sl.Groups[2].Value);
                    currentAssemblyLine.cbufferRead = new CBufferRead { dwordReadCount = readCount, offset = Convert.ToInt32(sl.Groups[3].Value, 16) };
                }

                if (Match(sBufferLoadRegex, asm, out var sbl))
                {
                    int readCount = 1;
                    if (!String.IsNullOrWhiteSpace(sbl.Groups[2].Value))
                        readCount = int.Parse(sbl.Groups[2].Value);
                    if (sbl.Groups[3].Value.StartsWith("0x"))
                    {
                        int.TryParse(sbl.Groups[3].Value.Substring(2), NumberStyles.HexNumber, null, out var offset);
                        currentAssemblyLine.cbufferRead = new CBufferRead { dwordReadCount = readCount, offset = offset };
                    }
                    else
                        currentAssemblyLine.cbufferRead = new CBufferRead { dwordReadCount = readCount, dynamicScalarOffset = true };
                }
            }

            if (Match(readRegex, l, out var r))
                currentAssemblyLine.registerReads.AddRange(ParseRegisters(r.Groups[1].Value));

            if (Match(writeRegex, l, out var w))
                currentAssemblyLine.registerWrites.AddRange(ParseRegisters(w.Groups[1].Value));

            if (Match(registerPressureRegex, l, out var p))
            {
                currentAssemblyLine.vgprPressure = int.Parse(p.Groups[2].Value);
                currentAssemblyLine.sgprPressure = int.Parse(p.Groups[1].Value);
            }

            if (Match(jumpLabelRegex, l, out var j))
            {
                jumps.Add(j.Groups[1].Value, codeLineIndex);
            }
        }

        return analyzedShader;
    }

    public static AnalyzedShader ParseCompiledShaderFromText2(string compiledShaderContent, out Dictionary<string, int> jumps, out StringBuilder wholeSourceCode)
    {
        AnalyzedShader analyzedShader = new();

        analyzedShader.target = BuildTarget.PS5;
        var lines = compiledShaderContent.Split(new string[]{"\n", "\r\n"}, StringSplitOptions.None);
        string currentSourceFilePath = null;
        CodeLine currentCodeLine = null;
        AssemblyLine currentAssemblyLine = null;
        int codeLineIndex = -1;
        wholeSourceCode = new();
        jumps = new();
        foreach (var line in lines)
        {
            var l = line.Trim();
            if (String.IsNullOrWhiteSpace(l))
                continue;

            if (Match(codeLineRegex2Regex, l, out var c))
            {
                wholeSourceCode.AppendLine(c.Groups[2].Value);
                currentCodeLine = new CodeLine{ code = c.Groups[2].Value, line = int.Parse(c.Groups[1].Value), filePath = currentSourceFilePath };
                analyzedShader.lines.Add(currentCodeLine);
                codeLineIndex++;
            }
            else if (Match(sourceFilePath2Regex, l, out var s))
                currentSourceFilePath = s.Groups[1].Value.Trim();

            if (Match(assemblyLine2Regex, l, out var a))
            {
                var asm = a.Groups[5].Value;
                currentAssemblyLine = new AssemblyLine() { assembly = asm };
                currentCodeLine.assemblyLines.Add(currentAssemblyLine);

                if (Match(sLoadRegex, asm, out var sl))
                {
                    int readCount = 1;
                    if (!String.IsNullOrWhiteSpace(sl.Groups[2].Value))
                        readCount = int.Parse(sl.Groups[2].Value);
                    currentAssemblyLine.cbufferRead = new CBufferRead { dwordReadCount = readCount, offset = Convert.ToInt32(sl.Groups[3].Value, 16) };
                }

                if (Match(sBufferLoadRegex, asm, out var sbl))
                {
                    int readCount = 1;
                    if (!String.IsNullOrWhiteSpace(sbl.Groups[2].Value))
                        readCount = int.Parse(sbl.Groups[2].Value);
                    if (sbl.Groups[3].Value.StartsWith("0x"))
                    {
                        int.TryParse(sbl.Groups[3].Value.Substring(2), NumberStyles.HexNumber, null, out var offset);
                        currentAssemblyLine.cbufferRead = new CBufferRead { dwordReadCount = readCount, offset = offset };
                    }
                    else
                        currentAssemblyLine.cbufferRead = new CBufferRead { dwordReadCount = readCount, dynamicScalarOffset = true };
                }

                ParseRegistersFromTable(currentAssemblyLine, a.Groups[2].Value, a.Groups[4].Value);
                currentAssemblyLine.vgprPressure = int.Parse(a.Groups[1].Value);
                currentAssemblyLine.sgprPressure = int.Parse(a.Groups[3].Value);
            }

            if (Match(jumpLabel2Regex, l, out var j))
            {
                jumps.Add(j.Groups[1].Value, codeLineIndex);
            }
        }

        return analyzedShader;
    }

    static void ParseRegistersFromTable(AssemblyLine assemblyLine, string vgprTable, string sgprTable)
    {
        void ParseTable(string table, RegisterType type)
        {
            int registerIndex = 0;
            for (int i = 0; i < table.Length; i++)
            {
                var r = new Register { type = type, registerIndex = registerIndex };
                switch (table[i])
                {
                    case ':':
                        // TODO: use this data
                        if (assemblyLine.registersAlive == null)
                            assemblyLine.registersAlive = new();
                        assemblyLine.registersAlive.Add(r);
                        break;
                    case '^':
                        assemblyLine.registerWrites.Add(r);
                        break;
                    case 'v':
                        assemblyLine.registerReads.Add(r);
                        break;
                    case 'x':
                        assemblyLine.registerWrites.Add(r);
                        assemblyLine.registerReads.Add(r);
                        break;
                }

                if (table[i] != '|')
                    registerIndex++;
            }
        }

        ParseTable(vgprTable, RegisterType.Vector);
        ParseTable(sgprTable, RegisterType.Scalar);
    }

    // Thanks Chat-GPT
    public static void CalculateIndentLevels(List<JumpData> jumpDataList)
    {
        // Sort the jumpDataList by jumpLineIndex in ascending order
        jumpDataList.Sort((a, b) => a.jumpLineIndex.CompareTo(b.jumpLineIndex));

        Stack<JumpData> stack = new Stack<JumpData>();

        foreach (JumpData jumpData in jumpDataList)
        {
            while (stack.Count > 0 && stack.Peek().jumpLineIndex <= jumpData.fromIndex)
            {
                stack.Pop();
            }

            if (stack.Count > 0)
            {
                jumpData.indentLevel = stack.Peek().indentLevel + 1;
            }
            else
            {
                jumpData.indentLevel = 0;
            }

            stack.Push(jumpData);
        }
    }


    static IEnumerable<Register> ParseRegisters(string text) // text should look like "s12, s13, s14, s15, v5"
    {
        foreach (var raw in text.Split(','))
        {
            Register r = new();
            var registerText = raw.Trim();

            switch (registerText)
            {
                case not null when registerText.StartsWith("s"):
                    r.type = RegisterType.Scalar;
                    break;
                case not null when registerText.StartsWith("v"):
                    r.type = RegisterType.Vector;
                    break;
                case not null when registerText.StartsWith("exec"):
                    r.type = RegisterType.Exec;
                    break;
                case not null when registerText.StartsWith("vcc"):
                    r.type = RegisterType.VCC;
                    break;
                case not null when registerText.StartsWith("m0"):
                    r.type = RegisterType.M0;
                    break;
                case not null when registerText.StartsWith("tma"):
                    r.type = RegisterType.TMA;
                    break;
                default:
                    Debug.LogError("Unknown register type: " + registerText);
                    break;
            }

            if (int.TryParse(registerText.Substring(1), out int value))
                r.registerIndex = value;
            yield return r;
        }
    }

    static bool Match(Regex r, string l, out Match match)
    {
        match = r.Match(l);
        return match.Success;
    }

    static Regex kernelRegex = new Regex(@"^\s*#pragma\s+kernel\s+(\w+)", RegexOptions.Multiline);
    static Regex multiCompiles = new Regex(@"#pragma\s+multi_compile\s+(.*)", RegexOptions.Multiline);

    public class ComputeShaderData
    {
        public List<string> kernelNames = new();
        public List<string> multiCompiles = new();
    }

    public static ComputeShaderData ListComputeShaderKernels(ComputeShader cs)
    {
        if (ShaderUtil.GetComputeShaderMessages(cs).Any(m => m.severity == ShaderCompilerMessageSeverity.Error))
        {
            Debug.LogError("Compute Shader " + cs + " has errors!");
            return null;
        }

        var path = AssetDatabase.GetAssetPath(cs);
        string fileContent = File.ReadAllText(path);
        var data = new ComputeShaderData();

        // Fill kernel names
        data.kernelNames.Clear();
        foreach (Match match in kernelRegex.Matches(fileContent))
            data.kernelNames.Add(match.Groups[1].Value);

        data.multiCompiles.Clear();
        foreach (Match match in multiCompiles.Matches(fileContent))
        {
            foreach (var keywordName in match.Groups[1].Value.Split(null))
            {
                var trim = keywordName.Trim();
                if (String.IsNullOrEmpty(trim) || trim == "_")
                    continue;
                data.multiCompiles.Add(trim);
            }
        }

        return data;
    }
}
