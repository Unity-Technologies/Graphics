using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;


public static class SetupProject
{
    public static void ApplySettings()
    {
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
    }

    static void SetGraphicsAPI(GraphicsDeviceType api)
    {
        var currentTarget = EditorUserBuildSettings.activeBuildTarget;
        PlayerSettings.SetGraphicsAPIs(currentTarget, new [] { api } );
    }
}

#if UNITY_ANDROID
[InitializeOnLoad]
public class SetAndroidSdk
{
    static SetAndroidSdk()
    {
        string sdkPath = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
        if(sdkPath != string.Empty)
        {
            UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath = sdkPath;
            Debug.Log($"SDK Path was set to ANDROID_SDK_ROOT = {sdkPath}");
        }
        else
        {
            Debug.LogWarning($"ANDROID_SDK_ROOT was not set.\nCurrently using SDK from here: {UnityEditor.Android.AndroidExternalToolsSettings.sdkRootPath}");
        }
        string jdkPath = Environment.GetEnvironmentVariable("JAVA_HOME");
        if(jdkPath != string.Empty)
        {
            UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath = jdkPath;
            Debug.Log($"JDK Path was set to JAVA_HOME = {jdkPath}");
        }
        else
        {
            Debug.LogWarning($"JAVA_HOME was not set.\nCurrently using JDK from here: {UnityEditor.Android.AndroidExternalToolsSettings.jdkRootPath}");
        }
        string ndkPath = Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT");
        if(ndkPath != string.Empty)
        {
            UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath = ndkPath;
            Debug.Log($"NDK Path was set to ANDROID_NDK_ROOT = {ndkPath}");
        }
        else
        {
            Debug.LogWarning($"ANDROID_NDK_ROOT was not set.\nCurrently using NDK from here: {UnityEditor.Android.AndroidExternalToolsSettings.ndkRootPath}");
        }
    }
}
#endif
