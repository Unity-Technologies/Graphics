using System;
using System.IO;
using Debug = UnityEngine.Debug;
using UnityEditor.VersionControl;

namespace UnityEditor.ShaderGraph
{
    static class FileUtilities
    {
        public static bool WriteShaderGraphToDisk<T>(string path, T data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            return WriteToDisk(path, EditorJsonUtility.ToJson(data, true));
        }

        public static bool WriteToDisk(string path, string text)
        {
            CheckoutIfValid(path);

            try
            {
                File.WriteAllText(path, text);
            }
            catch (Exception e)
            {
                if (e.GetBaseException() is UnauthorizedAccessException &&
                    (File.GetAttributes(path) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                        FileInfo fileInfo = new FileInfo(path);
                        fileInfo.IsReadOnly = false;
                        File.WriteAllText(path, text);
                        return true;
                }
                Debug.LogException(e);
                return false;
            }
            return true;
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
