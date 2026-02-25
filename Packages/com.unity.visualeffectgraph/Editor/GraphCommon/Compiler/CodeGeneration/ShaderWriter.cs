using System;
using System.Text;
using UnityEngine;

namespace Unity.GraphCommon.LowLevel.Editor
{
    abstract class ShaderWriter
    {
        protected StringBuilder ShaderBuilder { get; } = new();
        public uint IndentLevel { get; protected set; }

        public virtual void Begin(string name)
        {
            Debug.Assert(ShaderBuilder.Length == 0, $"ShaderBuilder should be empty when beginning a new shader. Now contains: {ShaderBuilder}");
            Debug.Assert(IndentLevel == 0, "IndentLevel should be 0 when beginning a new shader.");
        }

        public virtual string End()
        {
            Debug.Assert(IndentLevel == 0);
            string shader = ShaderBuilder.ToString();

            ShaderBuilder.Clear();

            return shader;
        }

        public void NewLine()
        {
            ShaderBuilder.AppendLine();
        }

        public void Indent()
        {
            for (uint i = 0; i < IndentLevel; ++i)
            {
                ShaderBuilder.Append("\t");
            }
        }

        public void Write(string text)
        {
            ShaderBuilder.Append(text);
        }

        [Flags]
        public enum WriteLineOptions
        {
            None = 0,
            NoIndent = 1 << 0,
            NoNewLine = 1 << 1,
            Multiline = 1 << 2
        }

        public void WriteLine(string line, WriteLineOptions writeLineOptions = WriteLineOptions.None)
        {
            if (writeLineOptions.HasFlag(WriteLineOptions.Multiline))
            {
                WriteLineOptions writeLineOptionsInner = writeLineOptions;
                writeLineOptionsInner &= ~WriteLineOptions.Multiline;
                writeLineOptionsInner &= ~WriteLineOptions.NoNewLine;

                var lines = line.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    for (int i = 0; i < lines.Length - 1; ++i)
                    {
                        WriteLine(lines[i], writeLineOptionsInner);
                    }
                    line = lines[^1];
                }
            }

            if (!writeLineOptions.HasFlag(WriteLineOptions.NoIndent))
            {
                Indent();
            }

            if (writeLineOptions.HasFlag(WriteLineOptions.NoNewLine))
            {
                ShaderBuilder.Append(line);
            }
            else
            {
                ShaderBuilder.AppendLine(line);
            }
        }

        public void OpenBlock(bool newLine = true)
        {
            WriteLine("{", newLine ? WriteLineOptions.None : WriteLineOptions.NoNewLine);
            IndentLevel++;
        }

        public void CloseBlock(bool newLine = true)
        {
            Debug.Assert(IndentLevel > 0);
            IndentLevel--;
            WriteLine("}", newLine ? WriteLineOptions.None : WriteLineOptions.NoNewLine);
        }

        public void IncludeFile(string filePath)
        {
            //Debug.Assert(IndentLevel == 0); TODO: Move the assert to compute only ?
            WriteLine($"#include \"{filePath}\"");
        }

        public void Pragma(string pragma)
        {
            //Debug.Assert(IndentLevel == 0);  TODO: Move the assert to compute only ?
            WriteLine($"#pragma {pragma}");
        }

        public void Define(string define, string value = null)
        {
            //Debug.Assert(IndentLevel == 0);  TODO: Move the assert to compute only ?
            if (string.IsNullOrEmpty(value))
                WriteLine($"#define {define}");
            else
                WriteLine($"#define {define} {value}");
        }

        public void Undefine(string define)
        {
            //Debug.Assert(IndentLevel == 0);  TODO: Move the assert to compute only ?
            WriteLine($"#undef {define}");
        }

        public void WriteTemplateSubtask(TemplateSubtask subtask)
        {
            WriteLine($"// {subtask.Name}"); // Temp name for debug purposes
            OpenBlock();
            WriteLine(subtask.Code, WriteLineOptions.Multiline);
            CloseBlock();
        }
    }
}
