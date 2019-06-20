#if UNITY_EDITOR
using System;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.TestTools.Graphics
{
    internal static class Utils
    {
        public static RuntimePlatform BuildTargetToRuntimePlatform(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return RuntimePlatform.Android;
                case BuildTarget.iOS:
                    return RuntimePlatform.IPhonePlayer;
#if !UNITY_2019_2_OR_NEWER
                case BuildTarget.StandaloneLinux:
                case BuildTarget.StandaloneLinuxUniversal:
#endif
                case BuildTarget.StandaloneLinux64:
                    return RuntimePlatform.LinuxPlayer;
                case BuildTarget.StandaloneOSX:
                    return RuntimePlatform.OSXPlayer;
                case BuildTarget.PS4:
                    return RuntimePlatform.PS4;
#if !UNITY_2018_3_OR_NEWER
                case BuildTarget.PSP2:
                    return RuntimePlatform.PSP2;
#endif
                case BuildTarget.Switch:
                    return RuntimePlatform.Switch;
                case BuildTarget.WebGL:
                    return RuntimePlatform.WebGLPlayer;
                case BuildTarget.WSAPlayer:
                    return RuntimePlatform.WSAPlayerX64;
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return RuntimePlatform.WindowsPlayer;
                case BuildTarget.XboxOne:
                    return RuntimePlatform.XboxOne;
                case BuildTarget.tvOS:
                    return RuntimePlatform.tvOS;
#if UNITY_2019_3_OR_NEWER
                case BuildTarget.Stadia:
                    return RuntimePlatform.Stadia;
#endif
            }

            throw new ArgumentOutOfRangeException("target", target, "Unknown BuildTarget");
        }

        public static BuildTarget RuntimePlatformToBuildTarget(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.Android:
                    return BuildTarget.Android;
                case RuntimePlatform.IPhonePlayer:
                    return BuildTarget.iOS;
                case RuntimePlatform.LinuxEditor:
                case RuntimePlatform.LinuxPlayer:
#if UNITY_2019_2_OR_NEWER
                    return BuildTarget.StandaloneLinux64;
#else
                    return BuildTarget.StandaloneLinuxUniversal;
#endif
                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    return BuildTarget.StandaloneOSX;
                case RuntimePlatform.PS4:
                    return BuildTarget.PS4;
#if !UNITY_2018_3_OR_NEWER
                case RuntimePlatform.PSP2:
                    return BuildTarget.PSP2;
#endif
                case RuntimePlatform.Switch:
                    return BuildTarget.Switch;
#if !UNITY_2017_2_OR_NEWER
                case RuntimePlatform.TizenPlayer:
                    return BuildTarget.Tizen;
#endif
                case RuntimePlatform.tvOS:
                    return BuildTarget.tvOS;
                case RuntimePlatform.WebGLPlayer:
                    return BuildTarget.WebGL;
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return BuildTarget.StandaloneWindows;
                case RuntimePlatform.WSAPlayerARM:
                case RuntimePlatform.WSAPlayerX64:
                case RuntimePlatform.WSAPlayerX86:
                    return BuildTarget.WSAPlayer;
                case RuntimePlatform.XboxOne:
                    return BuildTarget.XboxOne;
#if UNITY_2019_3_OR_NEWER
                case RuntimePlatform.Stadia:
                    return BuildTarget.Stadia;
#endif
            }

            throw new ArgumentOutOfRangeException("platform", platform, "Unknown RuntimePlatform");
        }

        public static void SetupReferenceImageImportSettings(IEnumerable<string> imageAssetPaths)
        {
            // Make sure that all the images have compression turned off and are readable
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var path in imageAssetPaths)
                {
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (!importer)
                        continue;

                    if (importer.textureCompression == TextureImporterCompression.Uncompressed && importer.isReadable)
                        continue;

                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.isReadable = true;
                    importer.mipmapEnabled = false;
                    importer.npotScale = TextureImporterNPOTScale.None;
                    AssetDatabase.ImportAsset(path);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}
#endif
