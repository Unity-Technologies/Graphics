using System;
using System.IO;

namespace UnityEditor.Experimental.ShaderTools.PSSLInternal
{
    public class PSSLShaderCompiler
    {
        public interface IOperation : IDisposable
        {
            bool isComplete { get; }

            void Cancel();
        }

        public interface ICompileOperation : IOperation
        {
            FileInfo sourceFile { get; }
            FileInfo targetFile { get; }
            ShaderCompilerOptions options { get; }

            FileInfo intermediateSourceFile { get; }
            string errors { get; }
        }

        public interface IDisassembleOperation : IOperation
        {
            FileInfo sourceFile { get; }

            string disassembly { get; }
        }

        public interface IShaderPerformanceAnalysis : IOperation
        {
            FileInfo sourceFile { get; }

            string report { get; }
        }

        OrbisWavePSSLC m_OrbisWavePsslc = new OrbisWavePSSLC();
        OrbisWaveSBDUMP m_OrbisWaveSbdump = new OrbisWaveSBDUMP();
        OrbisShaderPerf m_OrbisShaderPerf = new OrbisShaderPerf();

        public void Initialize()
        {
            m_OrbisWavePsslc.Initialize();
            m_OrbisWaveSbdump.Initialize();
            m_OrbisShaderPerf.Initialize();
        }

        public ICompileOperation Compile(FileInfo sourceFile, DirectoryInfo genDir, FileInfo targetFile, ShaderCompilerOptions options, ShaderProfile profile)
        {
            return m_OrbisWavePsslc.Compile(sourceFile, genDir, targetFile, options, profile);
        }

        public IDisassembleOperation Disassemble(FileInfo sourceFile)
        {
            return m_OrbisWaveSbdump.Disassemble(sourceFile);
        }

        public IShaderPerformanceAnalysis PerformanceAnalysis(FileInfo sourceFile)
        {
            return m_OrbisShaderPerf.PerformanceAnalysis(sourceFile);
        }
    }
}
