//This script is copied from UniversalRP TestProject
// https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/master/TestProjects/UniversalGraphicsTest/Assets/Test/Editor/SetupProject.cs

using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

public static class SetupProject
{
    public static void ApplySettings()
    {
#if UNITY_EDITOR
        var options = new Dictionary<string, Action>
        {
            { "gamma", () => PlayerSettings.colorSpace = ColorSpace.Gamma },
            { "linear", () => PlayerSettings.colorSpace = ColorSpace.Linear },
            { "glcore", () => SetGraphicsAPI(GraphicsDeviceType.OpenGLCore) },
            { "d3d11", () => SetGraphicsAPI(GraphicsDeviceType.Direct3D11) },
            { "d3d12", () => SetGraphicsAPI(GraphicsDeviceType.Direct3D12) },
            { "vulkan", () => SetGraphicsAPI(GraphicsDeviceType.Vulkan) }
        };

        var args = Environment.GetCommandLineArgs();
        foreach (var arg in args)
        {
            Action action;
            if (options.TryGetValue(arg, out action))
                action();
        }
#endif
    }

    static void SetGraphicsAPI(GraphicsDeviceType api)
    {
#if UNITY_EDITOR
        var currentTarget = EditorUserBuildSettings.activeBuildTarget;
        PlayerSettings.SetGraphicsAPIs(currentTarget, new [] { api } );
#endif
    }

#if UNITY_ANDROID
    //from: https://github.com/Unity-Technologies/ScriptableRenderPipeline/commit/34a7fe3574a38fff71cffd5ea48e28be473dffde#diff-d3827c2d1c2b7fcec2d272024df1a0b6
    [InitializeOnLoad]
    public class SetAndroidSdk
    {
        static SetAndroidSdk()
        {
            string sdkPath = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
            if(sdkPath != string.Empty)
            {
                UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath = sdkPath;
                Debug.Log($"SDK Path was set from ANDROID_SDK_ROOT = {sdkPath}");
            }
            else
            {
                Debug.LogWarning($"ANDROID_SDK_ROOT was not set.\nCurrently using SDK from here: {UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath}");
            }
        }
    }
#endif
}
