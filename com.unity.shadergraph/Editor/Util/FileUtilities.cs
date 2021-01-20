using System;
using System.IO;
using UnityEditor.ShaderGraph.Serialization;
using Debug = UnityEngine.Debug;
using UnityEditor.VersionControl;
using System.Text;

namespace UnityEditor.ShaderGraph
{
    static class FileUtilities
    {
        // if successfully written to disk, returns the serialized file contents as a string
        // on failure, returns null
        public static string WriteShaderGraphToDisk(string path, GraphData data)
        {
            if (data == null)
            {
                // Returning false may be better than throwing this exception, in terms of preserving data.
                // But if GraphData is null, it's likely we don't have any data to preserve anyways.
                // So this exception seems fine for now.
                throw new ArgumentNullException(nameof(data));
            }

            var text = MultiJson.Serialize(data);
            if (WriteToDisk(path, text))
                return text;
            else
                return null;
        }

        // returns true if successfully written to disk
        public static bool WriteToDisk(string path, string text)
        {
            CheckoutIfValid(path);

            while (true)
            {
                try
                {
                    File.WriteAllText(path, text);
                }
                catch (Exception e)
                {
                    if (e.GetBaseException() is UnauthorizedAccessException &&
                        (File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        if (EditorUtility.DisplayDialog("File is Read-Only", path, "Make Writeable", "Cancel Save"))
                        {
                            // make writeable
                            FileInfo fileInfo = new FileInfo(path);
                            fileInfo.IsReadOnly = false;
                            continue; // retry save
                        }
                        else
                            return false;
                    }

                    Debug.LogException(e);

                    if (EditorUtility.DisplayDialog("Exception While Saving", e.ToString(), "Retry", "Cancel"))
                        continue; // retry save
                    else
                        return false;
                }
                break; // no exception, file save success!
            }

            return true;
        }

        // returns contents of the asset file as a string, or null if any error or exception occurred
        public static string SafeReadAllText(string assetPath)
        {
            string result = null;
            try
            {
                result = File.ReadAllText(assetPath, Encoding.UTF8);
            }
            catch
            {
                result = null;
            }
            return result;
        }

        static void CheckoutIfValid(string path)
        {
            if (VersionControl.Provider.enabled && VersionControl.Provider.isActive)
            {
                var asset = VersionControl.Provider.GetAssetByPath(path);
                if (asset != null)
                {
                    if (!VersionControl.Provider.IsOpenForEdit(asset))
                    {
                        var task = VersionControl.Provider.Checkout(asset, VersionControl.CheckoutMode.Asset);
                        task.Wait();

                        if (!task.success)
                            Debug.Log(task.text + " " + task.resultCode);
                    }
                }
            }
        }
    }
}
