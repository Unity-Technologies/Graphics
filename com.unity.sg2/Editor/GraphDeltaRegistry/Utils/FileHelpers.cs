using System;
using System.Diagnostics;
using System.IO;
using UnityEditorInternal;
using Debug = UnityEngine.Debug;
using Object = System.Object;

namespace UnityEditor.ShaderGraph.GraphDelta.Utils
{
    static class FileHelpers
    {
        public static bool WriteToFile(string path, string content)
        {
            try
            {
                File.WriteAllText(path, content);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }
        }

        public static void OpenFile(string path)
        {
            string filePath = Path.GetFullPath(path);
            if (!File.Exists(filePath))
            {
                Debug.LogError(string.Format("Path {0} doesn't exists", path));
                return;
            }

            string externalScriptEditor = ScriptEditorUtility.GetExternalScriptEditor();
            if (externalScriptEditor != "internal")
            {
                InternalEditorUtility.OpenFileAtLineExternal(filePath, 0);
            }
            else
            {
                Process p = new Process();
                p.StartInfo.FileName = filePath;
                p.EnableRaisingEvents = true;
                p.Exited += (Object obj, EventArgs args) =>
                {
                    if (p.ExitCode != 0)
                        Debug.LogWarningFormat("Unable to open {0}: Check external editor in preferences", filePath);
                };
                p.Start();
            }
        }
    }
}
