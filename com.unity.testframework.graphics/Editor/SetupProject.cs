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
            { "metal", () => SetGraphicsAPI(GraphicsDeviceType.Metal) },
            { "vulkan", () => SetGraphicsAPI(GraphicsDeviceType.Vulkan) },
            { "gles3", () => SetGraphicsAPI(GraphicsDeviceType.OpenGLES3) },
            { "gles2", () => SetGraphicsAPI(GraphicsDeviceType.OpenGLES2) },
            { "ps4", () => SetGraphicsAPI(GraphicsDeviceType.PlayStation4) },
#if UNITY_PS5
            { "ps5", () => SetGraphicsAPI(GraphicsDeviceType.PlayStation5) },
#endif
            { "xb1d3d11", () => SetGraphicsAPI(GraphicsDeviceType.XboxOne) },
            { "xb1d3d12", () => SetGraphicsAPI(GraphicsDeviceType.XboxOneD3D12) },
#if UNITY_GAMECORE
            { "gamecorexboxone", () => SetGraphicsAPI(GraphicsDeviceType.GameCoreXboxOne) },
            { "gamecorexboxseries", () => SetGraphicsAPI(GraphicsDeviceType.GameCoreXboxSeries) },
#endif
            { "switch", () => SetGraphicsAPI(GraphicsDeviceType.Switch) }
        };

        var args = Environment.GetCommandLineArgs();
        string apiName = "";
        foreach (var arg in args)
        {
            Action action;
            if (options.TryGetValue(arg, out action))
            {
                apiName = arg;
                action();
            }
        }

        CustomBuild.BuildScenes(".", apiName, EditorUserBuildSettings.activeBuildTarget);
    }

    static void SetGraphicsAPI(GraphicsDeviceType api)
    {
        var currentTarget = EditorUserBuildSettings.activeBuildTarget;
        PlayerSettings.SetGraphicsAPIs(currentTarget, new[] { api });
    }

}
