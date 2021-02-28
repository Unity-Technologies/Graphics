using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.TestTools.Graphics;
using UnityEditor.TestTools.Graphics;


public class AdditionalMenuItems : MonoBehaviour
{
    static void CopyFilesToLeafDirectories(string destPath, string srcPath, string[] imageFiles)
    {
        string[] subDirectories = Directory.GetDirectories(destPath);

        // If this is not a leaf directory then recurse into the subdirectories
        if(subDirectories.Length > 0)
        {
            for(int i=0;i<subDirectories.Length;i++)
            {
                string subDirectory = subDirectories[i];
                CopyFilesToLeafDirectories(subDirectory + "/", srcPath, imageFiles);
            }
        }
        // We are in a leaf directory so copy all files that do not exist here
        else
        {
            for (int i = 0; i < imageFiles.Length; i++)
            {
                string fullSrcPath = srcPath + imageFiles[i];
                string fullDestPath = destPath + imageFiles[i];

                if (!File.Exists(fullDestPath))
                    File.Copy(fullSrcPath, fullDestPath);
            }
        }
    }

    [MenuItem("Tests/Duplicate New Reference Images")]
    static void DuplicateNewReferenceItems()
    {
        var colorSpace = UseGraphicsTestCasesAttribute.ColorSpace;
        var platform = UseGraphicsTestCasesAttribute.Platform;
        var graphicsDevice = UseGraphicsTestCasesAttribute.GraphicsDevice;
        var xrsdk = UseGraphicsTestCasesAttribute.LoadedXRDevice;


        var srcReferencePath = Path.Combine(Application.dataPath + "/ReferenceImages/", string.Format("{0}/{1}/{2}/{3}/", colorSpace, platform, graphicsDevice, xrsdk));

        string[] imageFiles = Directory.GetFiles(srcReferencePath, "*.png");

        //Strip the path from the filenames
        for (int i = 0; i < imageFiles.Length; i++)
            imageFiles[i] = Path.GetFileName(imageFiles[i]);

        CopyFilesToLeafDirectories(Application.dataPath + "/ReferenceImages/", srcReferencePath, imageFiles);
    }
}
