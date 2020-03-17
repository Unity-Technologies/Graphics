using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor.ShaderAnalysis.Internal;

namespace UnityEditor.ShaderAnalysis
{
    /// <summary>Interact with a shader compiler.</summary>
    public class ShaderCompiler
    {
        /// <summary>Interface with external operations.</summary>
        public interface IOperation : IDisposable
        {
            /// <summary>Wether the operation is complete.</summary>
            bool isComplete { get; }

            /// <summary>Cancels the operation.</summary>
            void Cancel();
        }

        /// <summary>Interface for a performance analysis operation.</summary>
        public interface IShaderPerformanceAnalysis : IOperation
        {
            /// <summary>Path of the analyzed file.</summary>
            FileInfo sourceFile { get; }

            /// <summary>Raw analysis report text.</summary>
            string report { get; }
        }

        /// <summary>Represent a compile operation.</summary>
        public class CompileOperation : IOperation
        {
            StringBuilder m_Errors = new StringBuilder();
            StringBuilder m_Outputs = new StringBuilder();

            ProcessManager.IProcess m_Process;

            /// <summary>Path to the source file.</summary>
            public FileInfo sourceFile { get; }
            /// <summary>Path to the generated file.</summary>
            public FileInfo targetFile { get; }
            /// <summary>Options to use for the compilation.</summary>
            public ShaderCompilerOptions options { get; }
            /// <summary>Path to an intermediate file.</summary>
            public FileInfo intermediateSourceFile { get; }

            /// <summary>stderr of the operation.</summary>
            public string errors => m_Errors.ToString();
            /// <summary>stdout of the operation.</summary>
            public string outputs => m_Outputs.ToString();

            /// <summary>Whether the operation has completed.</summary>
            public bool isComplete => m_Process.IsComplete();

            public string commandLine { get; }

            /// <summary>Cancels the operation.</summary>
            public void Cancel() => ProcessManager.Cancel(m_Process);

            /// <summary>Create a new compile operation.</summary>
            /// <param name="startInfo">
            /// Specific info to provide to the process generated.
            ///
            /// Some properties will be override to enable redirection of input and output streams.
            /// </param>
            /// <param name="sourceFile">Path of the source file to compile.</param>
            /// <param name="intermediateSourceFile">Path of the intermediate file to use.</param>
            /// <param name="targetFile">Path to the generated file.</param>
            /// <param name="options">Options to use for compilation.</param>
            public CompileOperation(
                ProcessStartInfo startInfo,
                FileInfo sourceFile,
                FileInfo intermediateSourceFile,
                FileInfo targetFile,
                ShaderCompilerOptions options
            )
            {
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                commandLine = $"{startInfo.FileName} {startInfo.Arguments}";

                m_Process = ProcessManager.Enqueue(startInfo, PreStart, PostStart);

                this.sourceFile = sourceFile;
                this.targetFile = targetFile;
                this.options = options;

                this.intermediateSourceFile = intermediateSourceFile;
            }

            /// <summary>Dispose the operation.</summary>
            public void Dispose() => ProcessManager.Cancel(m_Process);

            void PreStart(ProcessManager.IProcess process)
            {
                var proc = process.process;
                proc.OutputDataReceived += ProcessOnOutputDataReceived;
                proc.ErrorDataReceived += ProcessOnErrorDataReceived;
                proc.Exited += ProcessOnExited;
            }

            static void PostStart(ProcessManager.IProcess process)
            {
                var proc = process.process;
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
            }

            void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
                => m_Errors.AppendLine(dataReceivedEventArgs.Data);

            void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
                => m_Outputs.AppendLine(dataReceivedEventArgs.Data);

            void ProcessOnExited(object sender, EventArgs eventArgs)
            {
                var proc = m_Process.process;
                proc.OutputDataReceived -= ProcessOnOutputDataReceived;
                proc.ErrorDataReceived -= ProcessOnErrorDataReceived;
                proc.Exited -= ProcessOnExited;
            }
        }

        /// <summary>Initialize the compiler.</summary>
        public virtual void Initialize() { }

        /// <summary>Start a compile operation.</summary>
        /// <param name="sourceFile">Path of the file to compile.</param>
        /// <param name="genDir">Path to the generated directory.</param>
        /// <param name="targetFile">Path to the generated binary.</param>
        /// <param name="options">Options to use during compilation.</param>
        /// <param name="profile">Profile to compile.</param>
        /// <param name="shaderTarget">Target to compile.</param>
        /// <returns>The compile operation.</returns>
        public virtual CompileOperation Compile(
            FileInfo sourceFile,
            DirectoryInfo genDir,
            FileInfo targetFile,
            ShaderCompilerOptions options,
            ShaderProfile profile,
            ShaderTarget shaderTarget
        )
            => new CompileOperation(
                new ProcessStartInfo(),
                sourceFile,
                sourceFile,
                targetFile,
                options
            );
    }
}
