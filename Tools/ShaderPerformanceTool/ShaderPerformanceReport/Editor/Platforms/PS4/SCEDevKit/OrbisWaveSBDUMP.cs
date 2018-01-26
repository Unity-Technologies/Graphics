using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor.Experimental.ShaderTools.Internal;

namespace UnityEditor.Experimental.ShaderTools.PSSLInternal
{
    internal class OrbisWaveSBDUMP
    {
        const string k_ORBIS_SDK_BIN_DIR = @"host_tools\bin";
        const string k_ORBIS_SHADER_COMPILER = "orbis-sb-dump.exe";

        string m_DisassembleExe;

        public void Initialize()
        {
            var sceOrbisDir = System.Environment.GetEnvironmentVariable("SCE_ORBIS_SDK_DIR");
            if (string.IsNullOrEmpty(sceOrbisDir))
                throw new Exception("SCE SDK Could not be found, please check that the environment variable SCE_ORBIS_SDK_DIR defines a proper SCE ORBIS SDK");

            // Locate binaries
            m_DisassembleExe = Path.Combine(
                Path.Combine(
                    sceOrbisDir,
                    k_ORBIS_SDK_BIN_DIR
                ),
                k_ORBIS_SHADER_COMPILER
            );

            if (!File.Exists(m_DisassembleExe))
                throw new Exception(string.Format("The give SCE ORBIS SDK path is invalid, {0} was not found.", k_ORBIS_SHADER_COMPILER));
        }

        public PSSLShaderCompiler.IDisassembleOperation Disassemble(FileInfo sourceFile)
        {
            var args = new StringBuilder();
            args.Append("-disassemble \"");
            args.Append(sourceFile.FullName);
            args.Append("\" ");

            var startInfo = new ProcessStartInfo
            {
                FileName = m_DisassembleExe,
                Arguments = args.ToString()
            };

            var result = new DisassembleOperation(startInfo, sourceFile);
            return result;
        }

        class DisassembleOperation : PSSLShaderCompiler.IDisassembleOperation
        {
            StringBuilder m_Errors = new StringBuilder();
            StringBuilder m_Report = new StringBuilder();

            ProcessManager.IProcess m_Process;

            public FileInfo sourceFile { get; private set; }

            public string disassembly { get { return m_Report.ToString(); } }
            public string errors { get { return m_Errors.ToString(); } }

            public bool isComplete { get { return m_Process.IsComplete(); } }
            public void Cancel()
            {
                ProcessManager.Cancel(m_Process);
            }

            public DisassembleOperation(ProcessStartInfo startInfo, FileInfo sourceFile)
            {
                startInfo.RedirectStandardInput = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;

                m_Process = ProcessManager.Enqueue(startInfo, PreStart, PostStart);

                this.sourceFile = sourceFile;
            }

            public void Dispose()
            {
                ProcessManager.Cancel(m_Process);
            }

            void PreStart(ProcessManager.IProcess process)
            {
                var proc = process.process;
                proc.OutputDataReceived += Process_OutputDataReceived;
                proc.ErrorDataReceived += Process_ErrorDataReceived;
                proc.Exited += Process_Exited;
            }

            void PostStart(ProcessManager.IProcess process)
            {
                var proc = process.process;
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
            }

            private void Process_Exited(object sender, EventArgs e)
            {
                var proc = m_Process.process;
                proc.Exited -= Process_Exited;
                proc.OutputDataReceived -= Process_OutputDataReceived;
                proc.ErrorDataReceived -= Process_ErrorDataReceived;
            }

            private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
            {
                m_Errors.Append(e.Data);
            }

            private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                m_Report.Append(e.Data);
            }
        }
    }
}
