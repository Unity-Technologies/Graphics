using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Rendering.Utilities
{
    /// <summary>
    /// Editor SampleUtilities utility class.
    /// </summary>
    public static class SampleUtilities
    {
        /// <summary>
        /// Copy the files in the dictionary in the parentFolderName
        /// Do not include "/" in parentFolderName
        /// filesToImport contains the remove filepath in the Key, and the local folder path in the Value, end it with a slash
        /// Example:
        /// Key: "Packages/com.unity.render-pipelines.core/Samples~/Common/Models/UnityMaterialBall.fbx"
        /// Value: "/Models/"
        /// </summary>
        /// <param name="parentFolderName">The name of the folder that is created locally</param>
        /// <param name="filesToImport">The list of files that needs to be copied</param>
        public static void CopyFilesInFolder(string parentFolderName, Dictionary<string, string> filesToImport)
        {
            string path = "/" + parentFolderName;
            if (!Directory.Exists(Application.dataPath + path))
            {
                Directory.CreateDirectory(Application.dataPath + path);
            }

            foreach (KeyValuePair<string, string> file in filesToImport)
            {
                // Retrieve the filename. 
                string[] nameArray = file.Key.Split('/');
                string filename = nameArray[nameArray.Length - 1];

                string remotePath = file.Key;
                string localFolderPath = "Assets/" + file.Value;
                string localFilePath = localFolderPath + filename;

                // Create folders recursively if the path does not exist.
                if (!Directory.Exists(localFolderPath))
                    Directory.CreateDirectory(localFolderPath);

                // Create the file if does not exist.
                if (!File.Exists(localFilePath))
                {
                    FileUtil.CopyFileOrDirectory(remotePath, localFilePath);
                    FileUtil.CopyFileOrDirectory(remotePath + ".meta", localFilePath + ".meta");
                }
            }
        }

        
        /// <summary>
        /// Copy the folder named Common in each of the packages mentionned 
        /// </summary>
        /// <param name="packages">A list of packages (com.unity..) </param>
        /// <param name="parentFolderName">The name of the folder that is created locally</param>
        /// <param name="foldersToRemove">A list of folders to delete after import to avoid conflicts</param>
        public static void CopyCommonSampleFolders(string[] packages, string parentFolderName, string[] foldersToRemove = null)
        {
            // Foreach packages listed there, we need to import the common dependency folder. 
            foreach (string package in packages)
            {
                // Retrieve a readable name for the package. 
                string[] nameArray = package.Split('.');
                string folderName = nameArray[nameArray.Length - 1];
                folderName = folderName.Substring(0, 1).ToUpper() + folderName.Substring(1).ToLower();

                string path = "/" + parentFolderName + "/" + folderName;

                // If parentFolderName is not there locally, create it. 
                if (!Directory.Exists(Application.dataPath + path))
                {
                    // Import the common folder of the samples if it exists.
                    string commonFolderPath = "Packages/" + package + "/Samples~/Common";
                    if (Directory.Exists(commonFolderPath))
                    {
                        Directory.CreateDirectory(Application.dataPath + path);
                        FileUtil.CopyFileOrDirectory("Packages/" + package + "/Samples~/Common", "Assets" + path + "/Common");

                        // This for useless folder in a specific context or to avoid conflicts by importing multiple script folders. 
                        if (foldersToRemove != null)
                        {
                            foreach(string folder in foldersToRemove)
                            {
                                string folderPath = "Assets" + path + "/Common/" + folder;
                                if (Directory.Exists(folderPath))
                                {
                                    FileUtil.DeleteFileOrDirectory(folderPath);
                                    FileUtil.DeleteFileOrDirectory(folderPath + ".meta");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
