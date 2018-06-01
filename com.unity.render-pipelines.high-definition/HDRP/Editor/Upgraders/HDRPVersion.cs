using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    [InitializeOnLoad]
    public class HDRPVersion
    {
        static public float hdrpVersion = 1.0f;

        static public float GetCurrentHDRPProjectVersion()
        {
            string[] version = new string[1];
            version[0] = "0.9"; // Note: When we don't know what a project is, assume worst case

            try
            {
                version = File.ReadAllLines("ProjectSettings/HDRPProjectVersion.txt");
            }
            catch
            {
                Debug.LogWarning("Unable to read from ProjectSettings/HDRPProjectVersion.txt - Assign default version value");
            }

            return float.Parse(version[0]);
        }

        static public void WriteCurrentHDRPProjectVersion()
        {
            string[] newVersion = new string[1];
            newVersion[0] = hdrpVersion.ToString();

            try
            {
                File.WriteAllLines("ProjectSettings/HDRPProjectVersion.txt", newVersion);
            }
            catch
            {
                Debug.LogWarning("Unable to write ProjectSettings/HDRPProjectVersion.txt");
            }
        }

        static HDRPVersion()
        {
            // Compare project version with current version - Trigger an upgrade if user ask for it
            if (GetCurrentHDRPProjectVersion() < hdrpVersion)
            {
                if (EditorUtility.DisplayDialog("A newer version of Unity has been detected",
                                                "Do you want to upgrade your materials to newer version?\n You can also upgrade manually materials in Edit -> Render Pipeline submenu", "Yes", "No"))
                {
                    UpgradeMenuItems.UpdateMaterialToNewerVersion();
                }
            }

            // Update current project version with HDRP version
            WriteCurrentHDRPProjectVersion();
        }
    }
}
