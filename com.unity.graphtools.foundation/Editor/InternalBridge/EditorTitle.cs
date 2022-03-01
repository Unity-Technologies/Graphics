using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Bridge
{
    [InitializeOnLoad]
    class EditorTitle
    {
        const string k_DotConfigFileName = ".gtfConfig";
        const string k_GitBranchVariable = "%gitbranch%";
        static string s_RepoPath;

        static EditorTitle()
        {
            if (Unsupported.IsDeveloperMode())
            {
                EditorApplication.updateMainWindowTitle += EditorApplicationOnUpdateMainWindowTitle;
                EditorApplication.UpdateMainWindowTitle();
            }
        }

        static void EditorApplicationOnUpdateMainWindowTitle(ApplicationTitleDescriptor obj)
        {
            if (s_RepoPath == null)
            {
                try
                {
                    // DataPath: C:\path\to\repo\VS\Assets
                    // => repo
                    var repoPath = Path.GetDirectoryName(Path.GetDirectoryName(Application.dataPath));
                    string dotRepoConfigFile = Path.Combine(repoPath, k_DotConfigFileName);
                    if (File.Exists(dotRepoConfigFile))
                    {
                        s_RepoPath = File.ReadAllText(dotRepoConfigFile);
                        if (s_RepoPath.IndexOf(k_GitBranchVariable, StringComparison.InvariantCultureIgnoreCase) != -1)
                        {
                            ProcessStartInfo startInfo = new ProcessStartInfo("git");

                            startInfo.UseShellExecute = false;
                            startInfo.WorkingDirectory = repoPath;
                            startInfo.RedirectStandardInput = true;
                            startInfo.RedirectStandardOutput = true;
                            startInfo.Arguments = "rev-parse --abbrev-ref HEAD";

                            using (Process process = new Process { StartInfo = startInfo })
                            {
                                process.Start();
                                string gitbranch = process.StandardOutput.ReadLine();
                                s_RepoPath = new Regex(k_GitBranchVariable, RegexOptions.IgnoreCase).Replace(s_RepoPath, gitbranch);
                            }
                        }
                    }
                    else
                        s_RepoPath = Path.GetFileName(repoPath);
                }
                catch
                {
                    s_RepoPath = "???";
                }
            }

            obj.title = $"{s_RepoPath}/{obj.title}";
        }
    }
}
