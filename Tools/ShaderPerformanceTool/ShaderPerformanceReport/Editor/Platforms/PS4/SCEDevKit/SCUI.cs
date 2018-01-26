using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace UnityEditor.Experimental.ShaderTools.PSSLInternal
{
    class SCUI
    {
        const string k_ORBIS_SDK_BIN_DIR = @"host_tools\bin";
        const string k_ORBIS_EXE = "orbis-scui.exe";

        string m_Exe;

        public void Initialize()
        {
            var sceOrbisDir = System.Environment.GetEnvironmentVariable("SCE_ORBIS_SDK_DIR");
            if (string.IsNullOrEmpty(sceOrbisDir))
                throw new Exception("SCE SDK Could not be found, please check that the environment variable SCE_ORBIS_SDK_DIR defines a proper SCE ORBIS SDK");

            // Locate binaries
            m_Exe = Path.Combine(
                Path.Combine(
                    sceOrbisDir,
                    k_ORBIS_SDK_BIN_DIR
                ),
                k_ORBIS_EXE
            );

            if (!File.Exists(m_Exe))
                throw new Exception(string.Format("The give SCE ORBIS SDK path is invalid, {0} was not found.", k_ORBIS_EXE));
        }

        public void Open(FileInfo sourceFile, DirectoryInfo sourceDir, ShaderCompilerOptions options, ShaderProfile profile)
        {
            var args = new StringBuilder();
            var compilerArgs = new StringBuilder();

            FillArgs(sourceFile, sourceDir, options, profile, args, compilerArgs);

            var tmpFile = new FileInfo(FileUtil.GetUniqueTempPathInProject() + ".txt");
            File.WriteAllText(tmpFile.FullName, compilerArgs.ToString());
            Application.OpenURL(tmpFile.FullName);

            var workingDirectory = new DirectoryInfo(Application.dataPath).Parent.FullName;

            var startInfo = new ProcessStartInfo
            {
                FileName = m_Exe,
                Arguments = args.ToString(),
                WorkingDirectory = workingDirectory
            };

            Process.Start(startInfo);
        }

        public void OpenDiff(
            FileInfo firstSourceFile, 
            ShaderCompilerOptions firstOptions, 
            DirectoryInfo firstSourceDir, 
            FileInfo secondSourceFile, 
            ShaderCompilerOptions secondOptions,
            DirectoryInfo secondSourceDir,
            ShaderProfile profile)
        {
            var firstArgs = new StringBuilder();
            var secondArgs = new StringBuilder();
            var compilerArgs = new StringBuilder();
            FillArgs(firstSourceFile, firstSourceDir, firstOptions, profile, firstArgs, compilerArgs);
            FillArgs(secondSourceFile, secondSourceDir, secondOptions, profile, secondArgs, compilerArgs);

            var tmpFile = new FileInfo(FileUtil.GetUniqueTempPathInProject() + ".txt");
            File.WriteAllText(tmpFile.FullName, compilerArgs.ToString());

            var workingDirectory = new DirectoryInfo(Application.dataPath).Parent.FullName;
            var firstStartInfo = new ProcessStartInfo
            {
                FileName = m_Exe,
                Arguments = firstArgs.ToString(),
                WorkingDirectory = workingDirectory
            };

            Process.Start(firstStartInfo);
            // Start only once, the opened text file will be used to compare within SCUI
            Application.OpenURL(tmpFile.FullName);
        }

        static void FillArgs(
            FileInfo sourceFile,
            DirectoryInfo sourceDir,
            ShaderCompilerOptions options,
            ShaderProfile profile,
            StringBuilder args,
            StringBuilder compilerArgs)
        {
            args.Append("-i \"");
            args.Append(sourceFile.FullName);
            args.Append("\" ");

            args.Append("--entry ");
            args.Append(options.entry);
            args.Append(' ');

            args.Append("--profile ");
            args.Append(SCEUtils.GetProfileString(profile));
            args.Append(' ');

            args.Append("--compiler-type pssl --compiler-path \"");
            args.Append(OrbisWavePSSLC.GetCompilerExePath());
            args.Append("\" ");

            if (options.defines.Count > 0)
            {
                args.Append("--compiler-args \"");

                foreach (var define in options.defines)
                {
                    args.Append("-D");
                    args.Append(define);
                    args.Append(' ');
                }

                args.Append("\" ");
            }

            var first = true;
            if (options.includeFolders.Count > 0)
            {
                args.Append("--include ");
                args.Append('"');
                foreach (var folder in options.includeFolders)
                {
                    if (!first)
                    {
                        //compilerArgs.Append(';');
                        args.Append(';');
                    }
                    first = false;

                    var d = Path.IsPathRooted(folder)
                        ? new DirectoryInfo(folder).FullName
                        : new DirectoryInfo(Path.Combine(sourceDir.FullName, folder)).FullName;
                    args.Append(d);
                }
                args.Append("\" ");
            }

            compilerArgs.AppendLine();
            compilerArgs.AppendLine("SourceFile:");
            compilerArgs.AppendLine(sourceFile.FullName);
            compilerArgs.AppendLine();

            compilerArgs.AppendLine("Entry:");
            compilerArgs.AppendLine(options.entry);
            compilerArgs.AppendLine();

            compilerArgs.AppendLine("Profile:");
            compilerArgs.AppendLine(SCEUtils.GetProfileString(profile));
            compilerArgs.AppendLine();

            compilerArgs.AppendLine();
            compilerArgs.AppendLine("CompilerArgs:");
            foreach (var define in options.defines)
            {
                compilerArgs.Append("-D");
                compilerArgs.Append(define);
                compilerArgs.Append(' ');
            }
            OrbisWavePSSLC.AppendDefaultPSSCLArgs(compilerArgs);
            compilerArgs.AppendLine();

            compilerArgs.AppendLine();
            compilerArgs.AppendLine("Includes:");
            first = true;
            foreach (var folder in options.includeFolders)
            {
                if (!first)
                    compilerArgs.Append(';');
                first = false;

                var d = Path.IsPathRooted(folder)
                    ? new DirectoryInfo(folder).FullName
                    : new DirectoryInfo(Path.Combine(sourceDir.FullName, folder)).FullName;
                compilerArgs.Append(d);
            }


            compilerArgs.AppendLine();
            compilerArgs.AppendLine();
            compilerArgs.AppendLine("CompilerArgs with includes:");
            foreach (var define in options.defines)
            {
                compilerArgs.Append("-D");
                compilerArgs.Append(define);
                compilerArgs.Append(' ');
            }
            foreach (var folder in options.includeFolders)
            {
                var d = Path.IsPathRooted(folder)
                    ? new DirectoryInfo(folder).FullName
                    : new DirectoryInfo(Path.Combine(sourceDir.FullName, folder)).FullName;
                compilerArgs.Append("-I");
                compilerArgs.Append(d);
                compilerArgs.Append(' ');
            }
        }
    }
}
