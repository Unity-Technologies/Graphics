using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor.Build;
using UnityEditor.Experimental.ShaderTools.Internal;

namespace UnityEditor.Experimental.ShaderTools.PSSLInternal
{
    public class OrbisWavePSSLC
    {
        const string k_ORBIS_SDK_BIN_DIR = @"host_tools\bin";
        const string k_ORBIS_SHADER_COMPILER = "orbis-wave-psslc.exe";

        public static FileInfo GetCompilerExePath()
        {
            var sceOrbisDir = System.Environment.GetEnvironmentVariable("SCE_ORBIS_SDK_DIR");
            if (string.IsNullOrEmpty(sceOrbisDir))
                throw new Exception("SCE SDK Could not be found, please check that the environment variable SCE_ORBIS_SDK_DIR defines a proper SCE ORBIS SDK");

            return new FileInfo(Path.Combine(
                Path.Combine(
                    sceOrbisDir,
                    k_ORBIS_SDK_BIN_DIR
                ),
                k_ORBIS_SHADER_COMPILER
            ));
        }

        string m_CompilerExe;

        public void Initialize()
        {
            // Locate binaries
            m_CompilerExe = GetCompilerExePath().FullName;

            if (!File.Exists(m_CompilerExe))
                throw new Exception(string.Format("The give SCE ORBIS SDK path is invalid, {0} was not found.", k_ORBIS_SHADER_COMPILER));
        }

        public PSSLShaderCompiler.ICompileOperation Compile(FileInfo sourceFile, DirectoryInfo genDir, FileInfo targetFile, ShaderCompilerOptions options, ShaderProfile profile)
        {
            return ComputeShaderProfile(sourceFile, genDir, targetFile, options, SCEUtils.GetProfileString(profile));
        }

        public static void AppendDefaultPSSCLArgs(StringBuilder args)
        {
            // ignore warning about unrecognized pragma
            args.Append("-Wsuppress=3207 ");

            args.Append("-O4 -allow-scratch-buffer-spill -cache-gen-source-hash ");
        }

        static void GenerateCompileArgs(FileInfo targetFile, string entry, HashSet<string> includeFolders, HashSet<string> defines, StringBuilder args, FileInfo intermediateSourceFile, string profile)
        {
            args.Append('"');
            args.Append(intermediateSourceFile.FullName);
            args.Append("\" ");

            foreach (var includeFolder in includeFolders)
            {
                args.Append("-I\"");
                args.Append(includeFolder);
                args.Append("\" ");
            }

            foreach (var define in defines)
            {
                args.Append("-D");
                args.Append(define);
                args.Append(' ');
            }

            args.Append("-entry ");
            args.Append(entry);
            args.Append(' ');

            args.Append("-o \"");
            args.Append(targetFile.FullName);
            args.Append("\" ");

            AppendDefaultPSSCLArgs(args);

            args.Append("-profile ");
            args.Append(profile);
            args.Append(' ');
        }

        public static void FixupSource(FileInfo sourceFile, FileInfo targetFile)
        {
            var lines = File.ReadAllText(sourceFile.FullName);

            if (!string.IsNullOrEmpty(lines) && !lines.Contains("HLSLToPSSL.cginc"))
                lines = "#include \"HLSLToPSSL.cginc\"\n" + lines;

            if (!targetFile.Exists || File.ReadAllText(targetFile.FullName) != lines)
                File.WriteAllText(targetFile.FullName, lines);
        }

        PSSLShaderCompiler.ICompileOperation ComputeShaderProfile(FileInfo sourceFile, DirectoryInfo genDir, FileInfo targetFile, ShaderCompilerOptions options, string profile)
        {
            var intermediateSourceFile = new FileInfo(Path.Combine(
                genDir.FullName,
                Path.GetFileNameWithoutExtension(sourceFile.Name) + ".pssl" + sourceFile.Extension
            ));

            FixupSource(sourceFile, intermediateSourceFile);

            var args = new StringBuilder();
            GenerateCompileArgs(targetFile, options.entry, options.includeFolders, options.defines, args, intermediateSourceFile, profile);

            var startInfo = new ProcessStartInfo
            {
                FileName = m_CompilerExe,
                Arguments = args.ToString()
            };

            var process = new CompileOperation(startInfo, sourceFile, intermediateSourceFile, targetFile, options);
            return process;
        }

        class CompileOperation : PSSLShaderCompiler.ICompileOperation
        {
            StringBuilder m_Errors = new StringBuilder();
            StringBuilder m_Outputs = new StringBuilder();

            ProcessManager.IProcess m_Process;

            public FileInfo sourceFile { get; private set; }
            public FileInfo targetFile { get; private set; }
            public ShaderCompilerOptions options { get; private set; }
            public FileInfo intermediateSourceFile { get; private set; }

            public string errors { get { return m_Errors.ToString(); } }
            public string outputs { get { return m_Outputs.ToString(); } }

            public bool isComplete { get { return m_Process.IsComplete(); } }
            public void Cancel()
            {
                ProcessManager.Cancel(m_Process);
            }

            public CompileOperation(ProcessStartInfo startInfo, FileInfo sourceFile, FileInfo intermediateSourceFile, FileInfo targetFile, ShaderCompilerOptions options)
            {
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                m_Process = ProcessManager.Enqueue(startInfo, PreStart, PostStart);

                this.sourceFile = sourceFile;
                this.targetFile = targetFile;
                this.options = options;

                this.intermediateSourceFile = intermediateSourceFile;
            }

            void PreStart(ProcessManager.IProcess process)
            {
                var proc = process.process;
                proc.OutputDataReceived += ProcessOnOutputDataReceived;
                proc.ErrorDataReceived += ProcessOnErrorDataReceived;
                proc.Exited += ProcessOnExited;
            }

            void PostStart(ProcessManager.IProcess process)
            {
                var proc = process.process;
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
            }

            void ProcessOnErrorDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
            {
                m_Errors.Append(dataReceivedEventArgs.Data);
            }

            void ProcessOnOutputDataReceived(object sender, DataReceivedEventArgs dataReceivedEventArgs)
            {
                m_Outputs.Append(dataReceivedEventArgs.Data);
            }

            void ProcessOnExited(object sender, EventArgs eventArgs)
            {
                var proc = m_Process.process;
                proc.OutputDataReceived -= ProcessOnOutputDataReceived;
                proc.ErrorDataReceived -= ProcessOnErrorDataReceived;
                proc.Exited -= ProcessOnExited;
            }

            public void Dispose()
            {
                ProcessManager.Cancel(m_Process);
            }
        }
    }
}
